---
name: finish-item
description: Close out the backlog item just completed — output a commit message and a detailed, ready-to-paste prompt for the next item. Run when an item's work and evidence are done.
disable-model-invocation: true
---

Run the **Item completion protocol** defined in `CLAUDE.md`. Do not implement new code or commit here;
this only produces the two hand-off artifacts.

Two different items are involved — do not confuse them:
- **CURRENT item** = the one worked on in this session (the one being finished). The **commit message**
  is for THIS item.
- **NEXT item** = the one that comes after the current item. Only the **next-item prompt** is about it.

Steps:

1. Identify the **current** item = the one worked on this session (or the ID in `$ARGUMENTS` if given).
   Do not infer it from `Current focus` — that pointer is about to change. Ensure its **Status** is
   `Done` and its **Evidence** is filled in `docs/backlog/<phase>.md`; fix these first if missing.

2. Determine the **next** item = the next `Todo` after the current item in phase/dependency order (the
   first `Todo` of the next phase if the current phase is now finished). Verify its dependencies are all
   `Done`; if not, pick the correct next item and explain why. Then update `docs/backlog.md`: the **Done
   count** for the current item's phase and the **Current focus** → this next item.

3. Output exactly two fenced blocks, nothing else after them:
   - A Conventional-Commits message **for the current item** (`feat(<module>): <subject> (<CURRENT-ID>)`
     plus a body with what changed, acceptance criteria satisfied, and evidence/limitations).
   - A **detailed** ready-to-paste prompt **for the next item**, with the decision bullets filled from
     that item's acceptance criteria and its **Suggested model** noted. Spanish prose, English code terms.

Only touch files to fix a missing Status/Evidence/index update. Never commit unless the user asks.
