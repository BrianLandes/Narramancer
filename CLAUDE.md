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
| Runtime | `Assets/Narramancer/Scripts/` (`Narramancer.asmdef`) | ~25k LOC first-party |
| Nodes | `Scripts/Nodes/` | 156 node types, all deriving from xNode's `Node : ScriptableObject` |
| Editor | `Scripts/Editor/` (`Narramancer.Editor.asmdef`) | 77 scripts, incl. 30+ `[CustomNodeEditor]` classes |
| Save/load entry points | `Scripts/Utilities/SaveLoadUtilities.cs` | The **only** two serializer call sites (lines 56, 82) |
| Save orchestration | `Scripts/Data/NarramancerSingleton.cs` | `PrepareStoryForSave()` / `LoadStory()`; a registry of `ISerializableMonoBehaviour` |
| Execution | `Scripts/Data/NodeRunner.cs`, `Promise.cs` | Flag-based suspend/resume, not coroutines |
| Tests | `Assets/Test Suite/Editor/` (`Tests.Editor.asmdef`) | Currently one 50-line file — see `tasks.md` |

**Vendored dependencies (both are modifiable source, no DLLs):**
- **xNode** — `Scripts/Plugins/xNode/`. Graph editor + the `Node`/`NodePort` model. Modified in place; changes
  here are expected, not off-limits.
- **OdinSerializer** — `Plugins/OdinSerializer/` (`Narramancer.OdinSerializer.asmdef`). This is the **free,
  open-source OdinSerializer**, not paid Odin Inspector. Slated for removal — a buyer who owns Odin Inspector
  hits a duplicate-assembly conflict.

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
- **A clean editor compile is not proof the plugin works.** Odin's AOT codegen and the build preprocessors only
  run during a real player build. Any change touching serialization needs an IL2CPP build + save/load smoke
  before it can be called done.
- **There are ~0 users.** Breaking the save format is currently free and will not stay that way. Save-format
  changes are deliberately front-loaded for this reason.
- Sample scenes live in `Assets/Narramancer/Scenes/` — four of them, and they are part of the shipped package,
  so component-level refactors have to update them.
