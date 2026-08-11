# Inbox

A drop box for **new tasks and stray ideas**. Append here instead of editing
[`tasks.md`](tasks.md) directly — appending to the end of this file rarely
conflicts, so parallel/background sessions can capture work without stepping on
each other or on the main task list.

## How to use this file

**To add a task:** append a bullet under _Unsorted_ below. One line is enough —
title plus a sentence of context. Don't worry about priority or placement; that
happens when it's folded in.

```markdown
- [ ] **Short title** — what it is / why. (who/when, optional)
```

**To fold the inbox into `tasks.md`** (do this periodically, or before starting
a planning session):

1. Read each item under _Unsorted_.
2. Move it into the right priority section of `tasks.md`, rewording to match that file's style, and tag it with its provenance: `(from inbox YYYY-MM-DD)`.
3. Delete it from here, leaving the _Unsorted_ section empty (the template below).
4. Commit the `tasks.md` additions and this file's clearing **together**.

Keep the folding as its own small commit (or fold alongside related work) so the
history clearly shows the inbox being drained.

---

## Unsorted

- [ ] **Vendored xNode has its own Unity 6 deprecation warnings** — surfaced while fixing ours, out of scope at
  the time. `NodeEditorUtilities.cs:267,277` (`EndNameEditAction`, `StartNameEditingIfProjectWindowExists` —
  both want the `EntityId`/`AssetCreationEndAction` overloads) and `NodeEditorWindow.cs:196`
  (`EditorUtility.InstanceIDToObject` → `EntityIdToObject`). We own the vendored source, so these are fixable;
  natural to fold into the multi-window xNode work, which already touches `NodeEditorWindow`. (found 2026-08-11)
- [ ] **`ChooseRankedWeightedActionNode` is not a quick win — size it before scheduling** — looked at it as a
  candidate and backed off. The single-graph reference cases (`ListFilterNode`, `OfferObjectsAsChoicesNode`)
  don't transfer cleanly: this node holds a *list* of `RankedWeightedAction`, each with its own effect graph, so
  `UpdatePorts()` has to build a **union** of every action's graph inputs and decide what happens when two
  actions expose same-named inputs of different types. That collision rule is a design decision, not a port.
  (found 2026-08-11)
- [ ] **OdinSerializer emits obsolete-platform warnings** (`ArchitectureInfo.cs:67,69,74` — PS3, XBOX360, WiiU).
  Harmless, and moot once Odin is removed — noted only so it isn't re-investigated. (found 2026-08-11)
