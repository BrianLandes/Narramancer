# Narramancer

A node-based narrative/behavior system for Unity, sold as an Asset Store plugin. The core value is that
**saving and loading a *running* graph is built into the execution model** — a verb can be suspended
mid-action, serialized, reloaded, and resumed exactly where it left off. Node graphs are the mechanism; the
save system is the product.

This repo is both the plugin (`Assets/Narramancer/`) and the demo project that exercises it.

## Task Tracking

The active to-do list lives in three markdown files at the repo root:

- **[`tasks.md`](tasks.md)** — the live roadmap + next unfinished tasks. **Start here** when picking up
  "what's next."
- **[`done.md`](done.md)** — archive of completed tasks/phases (moved out of `tasks.md` to keep it short).
- **[`inbox.md`](inbox.md)** — drop box for new tasks; parallel sessions append here to avoid merge
  conflicts, then it's periodically folded into `tasks.md`.

**Golden rule:** when you add, edit, complete, or move a task, update the task doc **in the same commit as the
work**. `tasks.md` explains the full workflow.

The strategic view — why the work is ordered the way it is — lives in [`docs/V1_ROADMAP.md`](docs/V1_ROADMAP.md).
`tasks.md` owns the tactical view. Don't let them drift into two competing lists.

## Domain vocabulary

The codebase is built around a deliberate metaphor; matching it matters when naming things.

- **Nouns** — the things in the game (characters, items, areas, ideas). Entity-like: a named container.
  `NounScriptableObject` is the authored template; `NounInstance` is the runtime instance.
- **Verbs** — what things *do*, authored as graphs. `ActionVerb` runs over time (async-like, suspendable);
  `ValueVerb` returns information instantly. ActionVerbs may use ValueVerbs, never the reverse.
- **Adjectives** — data attached to a noun, component-style: **Properties** (flags/tags), **Stats** (numbers),
  **Relationships** (unidirectional noun→noun links).
- **Blackboards** — general keyed value tables, present on nouns, verbs, and global story state.

## Architecture map

| Area | Where | Notes |
|---|---|---|
| Runtime | `Assets/Narramancer/Scripts/` (`Narramancer.asmdef`) | ~27k LOC first-party |
| Nodes | `Scripts/Nodes/` | 156 node types, all deriving from xNode's `Node : ScriptableObject` |
| Editor | `Scripts/Editor/` (`Narramancer.Editor.asmdef`) | 78 scripts, incl. 30+ `[CustomNodeEditor]` classes |
| Serializer | `Scripts/Serialization/` | `NarramancerSerializer` — in-house, reflection-based JSON. Unity objects are saved by durable id (asset GUID, Resources path, runtime registry key, or `scene:<guid>`), never by instanceID |
| Save/load entry points | `Scripts/Utilities/SaveLoadUtilities.cs` | `SerializePayload` / `DeserializePayload` are the **only** serializer call sites |
| Save orchestration | `Scripts/Data/NarramancerSingleton.cs` | `PrepareStoryForSave()` / `LoadStory()`; a registry of `ISerializableMonoBehaviour` |
| Scene-object identity | `Scripts/Components/Saveable.cs` | A minted guid per GameObject; every `SerializableMonoBehaviour` keys its state off it |
| Execution | `Scripts/Data/NodeRunner.cs`, `Promise.cs`, `Continuation` | Flag-based suspend/resume, not coroutines. Never serialize a closure — use a `Continuation` |
| Tests | `Assets/Test Suite/Editor/` (`Tests.Editor.asmdef`) | ~520 EditMode tests; run with `run_tests` (EditMode, assembly `Tests.Editor`) |

**Vendored dependency (modifiable source, no DLLs):**
- **xNode** — `Scripts/Plugins/xNode/`. Graph editor + the `Node`/`NodePort` model. Modified in place; changes
  here are expected, not off-limits.
- OdinSerializer **was** vendored and was deleted on 2026-09-04 (a buyer who owns Odin Inspector hit a
  duplicate-assembly conflict). Don't reintroduce it, or any other copy of it.

**The load-bearing constraint:** nodes derive from xNode's `Node : ScriptableObject`, so node logic cannot be
moved into an engine-agnostic assembly while the project stays on xNode. Any refactor that starts pulling
nodes out of Unity has drifted into the paused v2 rewrite. The xNode boundary is the stopping point.

## Conventions

- **Tabs for indentation**, opening brace on the same line — match the surrounding file.
- Namespace is `Narramancer` for runtime and editor alike. **No `Narra*` type-name prefix** — that's a v2
  convention. v1 uses bare names (`Blackboard`, `NodeRunner`) or the full word (`NarramancerScene`,
  `NarramancerSingleton`).
- Prefer the `GameObjectExtensions` `Find*` wrappers over calling Unity's `Find*` APIs directly.

## Working notes

- **Unity 6000.4.12f1.** The project is already upgraded and compiles clean.
- **A clean editor compile is not proof the plugin works.** IL2CPP AOT compilation, managed stripping and the
  build preprocessors (e.g. the asset-manifest bake) only run during a real player build. Any change touching
  serialization needs a WebGL build + save/load smoke before it can be called done — see
  [`docs/RELEASE_RUNBOOK.md`](docs/RELEASE_RUNBOOK.md).
- **There are ~0 users.** Breaking the save format is currently free and will not stay that way. Save-format
  changes are deliberately front-loaded for this reason.
- Sample scenes live in `Assets/Narramancer/Scenes/` — four of them, and they are part of the shipped package,
  so component-level refactors have to update them.
