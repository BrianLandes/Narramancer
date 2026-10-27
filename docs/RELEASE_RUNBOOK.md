# Release runbook

Checks that **cannot** be automated, and so have to be run by hand before a release. Everything here exists
because the EditMode suite structurally cannot see it.

> The EditMode tests (~520) run in the editor, against the Mono runtime, in a single process. Three things matter
> to this plugin that none of that can reach: **AOT compilation** (IL2CPP resolves types at build time, not by
> reflecting at runtime), **managed stripping** (code nothing statically references can be removed from the
> build), and **identity across processes** (whether a saved reference still means the same asset tomorrow).
> A green suite says the logic is right. It says nothing about whether the thing runs.

---

## 1. The build smoke — required for any change touching serialization

**Build WebGL.** It is IL2CPP-only, so it covers AOT codegen *and* managed stripping in one shot; a desktop
IL2CPP build is strictly weaker evidence and can be skipped. (This is why the original Task Z pass was
considered complete after WebGL — see `done.md`.)

1. Build WebGL and serve it. The asset manifest rebakes automatically —
   `AssetManifestBuilder.BuildPreprocessor` runs on every build.
   Build Settings starts with `0. Sample Launcher`, so one build covers all four samples: pick one from the
   launcher, and the **◀ Samples** button in the corner goes back (starting a fresh story).
2. Run a graph until a verb **suspends** mid-action. A choice prompt or a wait is ideal; the point is to be
   stopped *inside* a verb, not between them.
3. Save.
4. **Close the tab entirely.** Not a reload — the process has to end, or nothing about persistence is being
   tested.
5. Reopen, load the save, and resume.

**Pass:** the story continues from the node it stopped on, not the one before it, and the browser console is
free of Narramancer errors.

**Watch for**, in order of likelihood:

| Symptom | Almost certainly |
|---|---|
| `Could not resolve the type '…'` on load | managed stripping removed a type only reached by name. Add a `link.xml`; the project has none today |
| `Narramancer cannot save a reference to '…'` | an asset outside the manifest — the §6.3 write path. The error names the three fixes |
| The story resumes at the **wrong** node, no errors | a reference-identity bug. This is the failure mode the GUID work removed, and it is silent — which is why step 5 has to be checked by eye, not by the absence of exceptions |
| `ExecutionEngineException` / missing method at load | AOT: a generic instantiation IL2CPP never compiled because only reflection reaches it |

---

## 2. The patch test — required for any change to reference identity

The one check that proves saved references survive a rebuild. It is the whole point of the GUID work, and
**nothing in the editor can stand in for it**: every EditMode test runs in one session, where a session-local
id looks exactly as good as a persistent one.

1. Build. Run. Reach a suspend point. Save. Keep the save file.
2. **Change what the build contains** — add a few assets, or delete an unused one. Anything that shifts load
   order is enough; that is precisely what a shipped patch does.
3. Rebuild. Load the save from step 1.

**Pass:** the story resumes at the same node as before the rebuild.

This test failed before the GUID work, silently: on 2026-08-27 every reference in a save had shifted by
exactly 44, the save loaded with no errors and no nulls, and the story resumed one node early. If this test
ever regresses it will look like nothing is wrong.

---

## 3. Odin Inspector coexistence — required before any release

The commercial reason the vendored OdinSerializer was removed at all: a buyer who owns **Odin Inspector**
previously hit a duplicate-assembly conflict on import, in exactly the segment most likely to pay.

1. Fresh project, import Odin Inspector, then import Narramancer.
2. Confirm the console is clean and the project compiles.

Worth doing directly rather than reasoning about, since it is the entire justification for the work.

---

## 4. Sample scenes

The four scenes in `Assets/Narramancer/Scenes/` ship with the package. Open each and confirm it plays — in a
build, through the launcher. Any
component-level refactor has to update them, and a broken sample is the first thing a new buyer sees.

---

## When to run what

| Change | Build smoke | Patch test | Odin coexistence |
|---|---|---|---|
| Serializer, save format, or execution model | **yes** | if reference identity moved | — |
| Reference identity (`AssetIds`, the manifest, the resolver) | **yes** | **yes** | — |
| Removing or restoring the vendored OdinSerializer | **yes** | — | **yes** |
| Any release | **yes** | **yes** | **yes** |
