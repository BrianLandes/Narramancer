# Replacing serialized closures with a `Continuation` record

> **Status:** ready to build. Both prerequisites and the characterization tests landed in M1/M2
> (2026-09-04); M3 is the last of the breaking work before launch.
> **Sequencing:** after the serializer port, not before — see "Why this is not a port blocker".
> **Problem statement and evidence:** [`ODIN_REMOVAL_PLAN.md`](ODIN_REMOVAL_PLAN.md) §2B.

## The problem, in one paragraph

A suspended verb's continuation is stored as a C# delegate and serialized by reflecting the compiler-generated
closure class. The type name embeds a **compiler-assigned method ordinal**: `PrintTextNode` has been saved as
`<>c__DisplayClass22_0`, `<>c__DisplayClass1_0` and `<>c__DisplayClass19_0` across real saves in this repo and
others. **Editing a node's source file can therefore invalidate existing saved games**, silently, with no
warning. Replacing the delegate with a data record removes the compiler from the save format entirely.

## Why this is not a port blocker

Worth stating plainly, because an earlier version of the plan had this backwards. The serializer port does
**not** need this work:

- `System.Type` support is ~20–40 lines.
- Closure reflection is essentially **free** — a display class is just a type with fields, which a reflecting
  serializer round-trips without knowing what it is.

So this is a genuine bug fix that happens to also simplify the serializer, not a dependency of it. It is still
**only free while there are ~0 users**, so it belongs before launch — just after the port.

---

## The full surface: 18 sites, 3 storage mechanisms

The `WhenDone` count understates it. Three separate fields hold `SerializableAction`:

| Storage | Sites | Shape |
|---|---|---|
| `Promise.doneCallbacks` | 13 | `List<SerializableAction>` |
| `ChoicePrinter.VisibleChoice.callback` + `TextAndChoicePrinter`'s | 4 | one per visible choice |
| `TextPrinter.SetText(…, Action callback, …)` | 1 | routed into a `Promise` |

By difficulty:

**Tier 1 — mechanical (10 sites).** The captured state is a runner reference and at most one primitive.
`WaitNode`, `PlaySoundNode`, `PlayAnimationNode` → `runner.Resume()`. `ParallelNode` → release a sub-runner.
`ChoosePrioritizedBranchNode`'s first callback → set a blackboard bool. The four `AddChoice` callbacks →
resume a runner with the chosen node.

**Tier 2 — restructuring (4 nodes).** `WaitUntilNode`, `ChoosePrioritizedBranchNode`,
`RunActionVerbWhileConditionIsTrueNode`, `RunActionVerbForDurationNode`. These are timer-driven **polling
loops**, and they carry two things a data record cannot express:

1. **A local function** `RunUpdate()` that reschedules itself through a new timer each tick.
2. **Shared mutable locals** — `subRunnerStillRunning` / `subRunnerIsStillRunning`, written by one closure and
   read by another, guarding against the sub-runner *and* the duration timer both firing.

Both are convertible, and the codebase already shows how: `ChoosePrioritizedBranchNode` keeps its equivalent
flag in `runner.Blackboard` under `Blackboard.UniqueKey(this, "Running")` rather than in a local. That is the
migration pattern for the other three.

---

## The design

Store **a stable identity for what to do, plus its arguments as data** — the durable-workflow pattern. What
cannot be serialized safely is a closure, because its identity is compiler-generated; a named intention with
data has no such problem.

```csharp
[Serializable]
public class Continuation {

    public enum Kind {
        ResumeRunner,          // runner.Resume()
        ResumeRunnerWithNode,  // runner.Resume(node) — the choice callbacks
        ReleaseRunner,         // NarramancerSingleton.Instance.ReleaseNodeRunner(runner)
        SetBlackboardBool,     // runner.Blackboard.SetBool(key, boolValue)
        NodeCallback,          // node.OnContinuation(key, runner) — the escape hatch
    }

    [SerializeField] private Kind kind;
    [SerializeField] private string runnerKey;   // -> NarramancerSingleton.GetNodeRunner(runnerKey)
    [SerializeField] private RunnableNode node;  // asset reference; GUID-keyed once the resolver lands
    [SerializeField] private string key;         // blackboard key, or callback id
    [SerializeField] private bool boolValue;

    public void Invoke() { /* resolve runner by key, switch on kind */ }
}
```

`Promise.doneCallbacks` becomes `List<Continuation>`. `VisibleChoice.callback` becomes a `Continuation`.

### The escape hatch

`NodeCallback` is what makes Tier 2 tractable. Add to `RunnableNode`:

```csharp
public virtual void OnContinuation(string callbackId, NodeRunner runner) { }
```

A polling node's `RunUpdate()` local function becomes a real private method, and the timer's continuation
becomes `NodeCallback(this, "poll", runnerKey)`. The callback id is **author-chosen and stable** — that is the
whole point, versus `<>c__DisplayClass22_0`.

### Simplifications that fall out

- `PrintTextNode` captures `waitForContinue` only to decide whether to resume. Instead of encoding the bool,
  **pass no continuation at all** when it's false. The flag disappears from the save format.
- Shared mutable locals move to `runner.Blackboard` under `Blackboard.UniqueKey(this, "…")`, following
  `ChoosePrioritizedBranchNode`. They become inspectable in a save rather than invisible closure state.

### ✅ Prerequisites — both fixed in M2, 2026-09-04

Both were found while designing this, and both were the **same family as the instanceID bug** — an identity
built from something the process invents at runtime. Neither is a blocker any more.

**1. `Blackboard.UniqueKey` embedded a hash code.** ✅ Fixed. And it was far wider than this design assumed:
**13 call sites across 9 node types**, not the two polling flags. `UnityEngine.Object.GetHashCode()` **is**
`GetInstanceID()`, so it was literally the instanceID bug in a second place. Keys are now scoped by durable id
through `Serialization.PersistentId`. This design leans on `UniqueKey` for moving shared mutable state out of
closures, so it is now safe to do so.

**2. Sub-runner names had to be stable.** ✅ Fixed, but not the way this section proposed. Rather than deriving
a *reconstructible* name from asset GUIDs, M2 removed the need to reconstruct anything: a runner now records
the runners it starts (`NodeRunner.SubRunnerNames`), and `StopAndReset` walks that tree. The name became a
label. `CreateNodeRunner` also disambiguates duplicates, which it previously threw on.

> The old note here claimed `RunActionVerbForDurationNode` "already does the right thing" with
> `$"{name} - {graph.name}"`. It was stable but **not unique**, and the runner table is a dictionary — two
> concurrent instances of that node threw on `Add`.

**Still owed, inherited from M2:** timer cancellation. `WaitNode` and the polling nodes leave a
`SerializableTimer` in `StoryInstance.Timers` when cancelled. It fires once against a stopped runner
(`Resume()` early-returns, so it is inert) and clears itself — but the fix wants an owner key on the timer,
and this is the work that rewrites those callbacks anyway.

---

## Not every callback should be data

A distinction the first draft missed, and the reason to make it explicit: right now **every** callback goes
through `SerializableAction`, including ones that could not possibly outlive the frame. Once `doneCallbacks`
becomes `List<Continuation>`, that would force every caller into the data model whether or not the callback
needs to survive a save.

So `Promise` should carry **both**:

```csharp
[SerializeField] private List<Continuation> doneCallbacks;   // survives a save
[NonSerialized] private Action transientDone;                // does not, and should not
```

`WhenDone(Continuation)` for anything that must resume after a load; `WhenDone(Action)` for UI glue, editor
tooling, and anything whose lifetime is the current frame. v2 does exactly this — its `Finished` is a plain
non-serialized C# event.

The rule for choosing: **if the promise can still be pending when the player saves, the callback must be a
`Continuation`.** Everything reachable from a suspended verb falls on that side.

---

## Why `SerializableAction` goes rather than staying for named methods

Worth writing down, because the instability is a property of **lambdas**, not of `SerializableAction`:

```csharp
serializedMethod = new SerializableMethodInfo(srcAction.Method);   // a signature name — stable
serializedTarget = new SerializableObject(action.Target);          // the object — the hard half
```

Pointed at a *named* method, the method half is stable across recompilation. So keeping it for named callbacks
is a coherent idea. Three things sink it:

1. **Nothing uses it that way.** All 18 sites in this repo are `() => { … }` lambdas. Deleting it loses
   nothing that exists.
2. **The target half is broken independently.** `SerializableObject` stores a Unity target as an object
   reference, and a **scene MonoBehaviour is not an asset** — it has no durable id, so the serializer drops it
   (§6.3) and `Delegate.CreateDelegate` then throws on a null target for an instance method. So
   `promise.WhenDone(this.OnDoorOpened)` does not survive a save *today*, and cannot until the `Saveable`
   refactor gives scene objects an identity. This is not a regression introduced by `Continuation`.
3. **The escape hatch is strictly better.** `OnContinuation(callbackId, runner)` is keyed by an
   **author-chosen string**, which survives a method rename. A method name does not.

**If the extensibility need turns out to be real, add it back after the `Saveable` refactor** as a
`Continuation` kind pointing at a `Saveable` GUID plus a named method. That is a better version of the same
idea, and cheap once scene objects have identity.

---

## What this deletes

- The entire `SerializableActionHelper` plugin — `SerializableAction`, `SerializableMethodInfo`,
  `SerializableObject`, `SerializableObjectOneLevel`/`TwoLevel`, `SerializableType`
- Serializer feature **#8** (`System.Type`), needed only by `SerializableType._type`
- Serializer feature **#10** (closure reflection)
- The `System.RuntimeType` and bare `System.Action` entries from the save type inventory
- Tier A item **A10**, which was deferred precisely because it needed staticness on `SerializableMethod`

---

## Order of work

1. ~~**Characterization tests for the Tier 2 nodes**~~ ✅ **Done 2026-08-27** —
   `PollingNodeCharacterizationTests`, 7 tests covering `WaitUntilNode` end to end and
   `RunActionVerbForDurationNode`'s timeout path. *(Original note preserved:)* pin current behaviour before touching them. Drive with
   `NarramancerSingleton.Instance.OnUpdate()`; use `MakeTimer(0)` so timers fire on the next pump (`Time.time`
   is frozen within an EditMode frame). Cover: polling continues while the condition holds, the correct next
   node is prepended, and — most important — **the double-fire guard**, where the sub-runner completing and the
   duration timer firing must not both take effect.
2. ~~**Stabilize sub-runner naming**~~ ✅ **Done 2026-09-04** in M2, by removing the need to reconstruct a
   name at all — see the prerequisites section.
3. `Continuation` + `Promise.doneCallbacks` + `RunnableNode.OnContinuation`, **plus the transient
   `WhenDone(Action)` overload** so callbacks that cannot outlive the frame are not forced into the data model.
4. Tier 1's 10 mechanical sites.
5. Tier 2's 4 nodes, one at a time, each verified against its characterization tests.
6. The choice-callback path: `VisibleChoice` in both printers.
7. Delete `SerializableActionHelper`; drop it from `Third-Party Notices.txt` and `LICENSING.md`.
8. Re-run the Task Z build smoke — serialization changed.

**Estimate: 4–6 evenings**, of which steps 5–6 need manual playtesting of the sample scenes; EditMode cannot
exercise the choice UI.

## Risks

- **Tier 2 is execution-critical.** These nodes drive every "run a verb until X" behaviour. The
  characterization tests in step 1 exist specifically so this refactor is verifiable rather than hopeful.
- **The choice path is UI-coupled** and only playtestable. Convert it last, on its own commit.
- **Sub-runner naming is a hidden dependency** — if step 2 is skipped, `runnerKey` silently fails to resolve
  after a load, and the failure looks exactly like the bug being fixed.
