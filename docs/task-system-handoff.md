# The Three-File Task System — port it to any project

A portable write-up of the `tasks.md` / `inbox.md` / `done.md` system this repo
uses. Nothing in it is language-, framework-, or tool-specific: it needs a text
editor and a version-control repo, and that's all. Copy this doc (and the
templates in §4) into any new or existing project.

It is designed for **one person plus AI coding sessions** — where the "team"
that has to stay in sync is you across weeks, plus however many agent sessions
are running. It is deliberately not an issue tracker.

---

## 1. The shape, in one minute

Three markdown files at the repo root:

| File | Role | Grows | Shrinks |
|---|---|---|---|
| `tasks.md` | The live list. Source of truth for "what's next." | Folding the inbox; adding while working | Moving finished items to `done.md` |
| `inbox.md` | Append-only drop box for raw new ideas. | Anyone, anytime, one line | Emptied on every fold |
| `done.md` | Archive of finished work, newest first. | Only ever grows | Never |

Work flows one way: **inbox → tasks → done.** Nothing ever flows backwards. If
an archived item needs follow-up, you open a *new* task rather than un-archiving.

One rule binds it to reality: **task-file edits ship in the same commit as the
work they describe.**

---

## 2. Why three files and not one

Each file exists to solve a specific failure of the single-`TODO.md` approach.

**`done.md` exists so `tasks.md` stays short.** A single file that accumulates
`- [x]` lines becomes unreadable within a month, and the cost is exactly where
you can least afford it: "what's next" stops being answerable at a glance. But
you don't want to *delete* finished items either — the note you wrote when you
fixed something is the most valuable thing you'll write all week. So completed
items get **moved**, not checked off in place. The active list stays roughly one
screen per section; the archive grows without bound and nobody cares.

**`inbox.md` exists so capturing an idea is never a merge conflict.** Two
sessions editing `tasks.md` — inserting into different priority sections,
reflowing lines — collide constantly. Two sessions *appending to the end of*
`inbox.md` almost never do. That's the whole trick: the inbox is append-only by
convention, so it's structurally conflict-resistant. It also removes the second
tax on capture, which is deciding where a thought goes. You write one line, you
keep working; sorting happens later, in a batch, when you're in a planning
headspace instead of a debugging one.

**`tasks.md` is then free to be curated.** Because raw capture lands elsewhere
and finished work leaves, the live file can be kept in genuine priority order,
rewritten for clarity, and trusted. That trust is the actual product: a new
session (yours or an agent's) reads the top of `tasks.md` and starts working
without an archaeology phase.

---

## 3. The invariants

Five rules. Everything else is taste.

1. **Same commit.** Adding, editing, completing, or moving a task rides in the
   commit that does the work. Git history and the task list can then never
   disagree — the diff shows the code change *and* the line moving to `done.md`.
2. **Move, don't check.** A finished task's line leaves `tasks.md` for
   `done.md`. Never `- [x]` it in place and leave it. (Sub-items of a
   still-open parent are the one exception — see §7.)
3. **Append to the inbox, never insert.** New items go at the bottom under
   _Unsorted_. Don't prioritise, don't place, don't reword.
4. **The archive never gates anything.** `done.md` is a record, not a queue. If
   done work needs more work, open a fresh task in `tasks.md`.
5. **Depth goes in a linked doc, not in the list.** A task line is one or two
   lines. Anything needing real design detail gets its own
   `docs/<thing>-handoff.md` that the task links to.

---

## 4. Drop-in templates

Create these three files at the repo root. They're written so an AI session
reading any one of them learns the whole system.

### `tasks.md`

````markdown
# Tasks

**This is the canonical to-do list for <PROJECT>.** If you're a new session
looking for "what's next," start here. Pick the top unfinished item in the
highest active section unless the user says otherwise.

---

## How this system works

Three markdown files at the repo root work together:

| File | Purpose |
|---|---|
| **`tasks.md`** (this file) | The live roadmap + next unfinished tasks. The source of truth for active work. |
| **`done.md`** | Archive. Completed tasks and whole finished phases get **moved** here (not deleted) so this file stays short. |
| **`inbox.md`** | A drop box for new tasks. Parallel sessions append here instead of editing `tasks.md`, which avoids merge conflicts. Periodically folded into `tasks.md` and emptied. |

### The golden rule

**Update the task docs in the _same commit_ as the work they describe.**
When you add, edit, complete, or move a task, the change to `tasks.md` /
`done.md` / `inbox.md` rides along in the same commit as the code. That way the
git history and the task list never disagree, and the next session can trust
what these files say.

### Task format

Plain markdown checkboxes, grouped under a priority/phase heading:

```markdown
- [ ] **Short title** — one-line description. Optional pointer to a doc or file.
```

- Use `- [ ]` for open, `- [x]` for done.
- **Bold** the short title so the list scans quickly.
- Keep each task to a line or two. Deep design detail belongs in a
  `docs/*-handoff.md` doc that the task links to, not inline here.

### Working a task

1. Pick the top open item in the highest active section (or whatever the user asked for).
2. Do the work. If you learn something the next session needs, jot it on the task line or in the doc it links to.
3. When it's finished, **move** the line to `done.md` (see that file for the archive format) — don't just check it off and leave it here. Small, closely-related items can be batched.
4. Commit the code **and** the task-doc edits together.

### Adding a task

- **Working in this session, on the main line of work?** Add it directly to the right section below.
- **A parallel/background session, or just capturing a stray idea?** Append it to `inbox.md` instead, to avoid stepping on this file.

### Folding the inbox

When `inbox.md` has accumulated items, sort each one into the right section
here, then clear it back to its empty template. Do this as its own small commit
(or alongside related work) — see `inbox.md` for the procedure.

---

## Now / In progress

- [ ] _(the one or two things actually being worked right now)_

---

## Bugs

## Critical

## High Priority

## Medium Priority

## Architecture / tech debt

## Systems & features (design docs / handoffs)

Larger builds that each have their own design doc — read the linked handoff
before starting.

## Future / Nice to Have

---

_See [`done.md`](done.md) for the archive of completed work._
````

### `inbox.md`

````markdown
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
2. Move it into the right priority section of `tasks.md`, rewording to match that file's style.
3. Delete it from here, leaving the _Unsorted_ section empty (the template below).
4. Commit the `tasks.md` additions and this file's clearing **together**.

Keep the folding as its own small commit (or fold alongside related work) so the
history clearly shows the inbox being drained.

---

## Unsorted

_Empty. Append new tasks below this line._
````

### `done.md`

````markdown
# Done

Archive of completed tasks and finished phases, moved here from
[`tasks.md`](tasks.md) so the active list stays short. Newest at the top.

## How to use this file

- When a task in `tasks.md` is finished, **move** its line here (don't just
  check it off and leave it there). Keep it in the **same commit** as the work.
- Change the checkbox to `- [x]` and, when useful, append a short parenthetical
  note for the future: what shipped, where it lives, or a gotcha worth knowing.
- Group related items under a dated `## Completed <YYYY-MM-DD>` heading when you
  finish a batch, so the archive reads as a changelog.
- Never let this file gate anything — it's a record, not a work queue. If a
  "done" item turns out to need follow-up, open a fresh task in `tasks.md`
  rather than un-archiving.

---
````

### The agent-facing snippet

Add this to whatever file your AI tooling loads as project context
(`CLAUDE.md`, `AGENTS.md`, `.cursorrules`, a system prompt — same text):

```markdown
## Task Tracking

The active to-do list lives in three markdown files at the repo root:

- **`tasks.md`** — the live roadmap + next unfinished tasks. **Start here** when
  picking up "what's next."
- **`done.md`** — archive of completed tasks/phases (moved out of `tasks.md` to
  keep it short).
- **`inbox.md`** — drop box for new tasks; parallel sessions append here to
  avoid merge conflicts, then it's periodically folded into `tasks.md`.

**Golden rule:** when you add, edit, complete, or move a task, update the task
doc **in the same commit as the work**. `tasks.md` explains the full workflow.
```

That's the entire integration. The three files teach themselves; the snippet
only has to point at them.

---

## 5. Porting into an existing project

Roughly a 30-minute job.

1. **Drop in the three templates.** Fill in the project name; keep the section
   headings you'll actually use and delete the rest. Six to eight sections is
   the sweet spot — enough to express priority, few enough that placement is
   obvious.
2. **Seed `tasks.md` from whatever you have now.** A stale `TODO.md`, scattered
   `// TODO` comments, a half-used issue tracker, your own memory. Write one
   line per item, bold title first. Don't research anything yet — a rough line
   you can rewrite later beats an unwritten one.
3. **Sweep the seed list for already-done work.** In any project with history,
   a real fraction of the old list already shipped. Move those straight to
   `done.md` under a dated heading like _"Marked done during the task-system
   consolidation."_ This repo's archive has exactly such a section, and it was
   the single most clarifying step — it's what turned an intimidating list into
   an accurate short one.
4. **Decide what the old roadmap becomes.** If you have a `roadmap.md` or
   similar, don't delete it and don't let it compete: demote it to the
   *strategic* view (themes, quarters, why) and let `tasks.md` own the
   *tactical* one (what's next, in order). Cross-link both ways and state the
   split in one line, or you'll end up maintaining two lists.
5. **Add the agent snippet** to your AI context file.
6. **Commit all of it together**, and from that commit onward, obey rule #1.

For a brand-new project, it's steps 1, 5, 6 and you're done.

---

## 6. The operating rhythm

**Picking up work.** Open `tasks.md`, take the top open item of the highest
active section. `## Now / In progress` exists so a session that's mid-something
doesn't get re-prioritised by the next one — keep it to one or two items and
keep it honest.

**During work.** Anything you learn that the next session would need gets
written down immediately: onto the task line, or into the linked handoff doc.
This is where the system pays for itself with AI sessions, which have no memory
of yesterday.

**Finishing.** Move the line to the top of `done.md`, flip to `- [x]`, and
append what you learned. Be generous here — the good archive entries in this
repo record what shipped, where it lives, what was surprising, and what was
deliberately *not* done and why. That last one is worth the most: it's what
stops a future session from "fixing" a decision you made on purpose. Then commit
code and doc edits together.

**Capturing.** Mid-task idea, or working in a parallel session? One line at the
bottom of `inbox.md`. Never break flow to file properly.

**Folding.** When the inbox has accumulated — or before any planning session —
read each item, rewrite it into the right section of `tasks.md` in that file's
voice, and clear the inbox back to its empty template. Tag folded items with
their provenance (`(from inbox 2026-08-08)`) so you can later tell a
deliberately-prioritised task from one that was merely filed. Commit the
addition and the clearing together.

**Pruning.** Occasionally, delete. An idea that's sat in Medium Priority for six
months without ever tempting you is not a task, it's noise — drop it, or move it
to `## Future / Nice to Have` where expectations are lower. The list is only
trustworthy if everything on it is something you'd genuinely do.

---

## 7. Conventions worth copying

**The task line.** `- [ ] **Short title** — description.` The bold title is
what makes the list scannable; the em-dash description is what makes it
actionable. Both matter.

**Nested phases.** A large build sits as one parent task with indented
sub-items, and here sub-items *are* checked in place rather than moved — the
phase list is the parent's progress bar, and moving finished phases out would
destroy the shape of the thing. Move the whole block to `done.md` when the
parent completes. A `[~]` marker is useful for "shipped but with a known
remaining sliver," spelled out on the line.

**Provenance tags.** `(from inbox 2026-08-08)`, `(shipped 2026-08-08)`,
`(found 2026-07-21)`. Cheap to write, and they let you reconstruct how long
something sat, later.

**Absolute dates, never relative.** "Next week" is meaningless to a session
reading it in three months.

**The handoff-doc escape valve.** When a task grows a real design behind it, it
gets `docs/<thing>-handoff.md` and the task line becomes a pointer plus a phase
checklist. This is what keeps `tasks.md` from bloating into a design document,
and it's why the list can hold genuinely large projects without becoming
unreadable.

**Dated batch headings in the archive.** `## Completed 2026-08-08` with the
day's items under it turns `done.md` into a real changelog you can read
chronologically — and into a decent source for release notes.

---

## 8. Failure modes

The system fails quietly, in recognisable ways:

- **`tasks.md` growing past ~200 lines** — you've stopped moving things to
  `done.md`, or stopped pruning. Both, usually.
- **Checked boxes lingering in `tasks.md`** — rule #2 is slipping. The moment
  the active list contains finished work, it stops being scannable.
- **An inbox that never drains** — you're capturing but not folding. Fold
  before every planning session, not "eventually."
- **Task edits landing in separate commits from the work** — the version that
  breaks the whole thing. Once history and list disagree, neither is trusted,
  and you're back to archaeology.
- **`## Now / In progress` holding six items** — nothing is actually in
  progress; it's just become another priority tier.
- **Archive entries that say only "fixed"** — technically compliant, worthless.
  The note is the point.

---

## 9. When to graduate off it

This system is right when there's one person deciding priority and history is
enough of an audit trail. Move to a real tracker when you need any of: multiple
people claiming and assigning work, external bug reports from people without
repo access, or scheduling and estimation. If it's just you (and your agents),
the flat files stay better — zero latency, zero context-switch, versioned with
the code, and readable by every AI session without an API call.

A reasonable hybrid, if a project acquires outside users: issues become the
public intake and `tasks.md` stays the private work queue, with the inbox as the
place triaged issues land as one-liners.
