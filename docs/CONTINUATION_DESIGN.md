# Replacing serialized closures with a `Continuation` record

> **Status:** design only. Prerequisite work (characterization tests) in `tasks.md` §2.
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

### ⚠️ Prerequisite: sub-runner names must be stable

`Continuation` addresses runners by string key, which works because `CreateNodeRunner(name)` registers them in
`storyInstance.NodeRunners`. **But `ParallelNode.NameKey` builds that name from `GetHashCode()`:**

```csharp
$"Name {runner.GetHashCode()} {this.GetHashCode()} {node.GetHashCode()}"
```

Object hash codes are **not stable across sessions**, so a sub-runner's key is already unstable across a save —
a latent bug of the same family as the instanceID one, and a hard blocker for `runnerKey`. Fix first: derive
sub-runner names from the node's asset GUID plus the parent runner key. *(Worth checking whether this is
already causing sub-runner leaks after load.)*

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

1. **Characterization tests for the 4 Tier 2 nodes** — pin current behaviour *before* touching them. Drive with
   `NarramancerSingleton.Instance.OnUpdate()`; use `MakeTimer(0)` so timers fire on the next pump (`Time.time`
   is frozen within an EditMode frame). Cover: polling continues while the condition holds, the correct next
   node is prepended, and — most important — **the double-fire guard**, where the sub-runner completing and the
   duration timer firing must not both take effect.
2. **Stabilize sub-runner naming** (the prerequisite above).
3. `Continuation` + `Promise.doneCallbacks` + `RunnableNode.OnContinuation`.
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
