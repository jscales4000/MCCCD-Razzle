# FRED Sync Queue — Drag-and-Drop Source Routing Lessons (APPLIED)

**Date queued:** 2026-05-02
**Date applied:** 2026-05-02 (same day, after FRED reconnected at http://100.67.23.64:8051/mcp)
**Status:** ✅ Applied. Kept in repo as a record of what got pushed.

## What was pushed

**Documents (6 total):**

In MCCCD-AA140 project (`c1937681-e57d-4354-aa58-a5b0f6e9ca23`):
- Lessons Learned: `3e710793-f815-4296-9dfc-73f4e0d050d6`
- Retrospective Writeup: `7c1c1b57-54a1-4166-bfd3-1f136346cef0`
- Session Handoff: `f3740f6b-3b2d-49ea-b68c-ca885a00b9e7`

In FRED Server project (`cef01706-4b13-4588-8d8d-281e1412510a`) for future agent training:
- Lessons Learned: `da2e2cad-d7df-4ca4-b342-96a9ac299fa8`
- Retrospective Writeup: `c9f86377-5d22-4d35-9fb4-cfa0cb65f818`
- Session Handoff: `926a5068-aaef-4456-9496-9687b933d639`

**Tasks (4 ops):**

- Updated `318aec66-c590-4d48-8f3e-6d3404e028d6` (Build Stage 1 drag-and-drop source router mockup) → `done`
- Created `ebf66396-cca9-41c8-a2f2-015c417c61ad` (Drag-drop polish: long-press reliability, hit zones, animation feel)
- Created `e517ff7c-c6bc-4a5e-a75e-dda3a2e3b2ba` (Drag-drop redesign: align to Mockup 10 chrome)
- Created `e9a686f0-a5c6-49ea-bf23-1200799d3da9` (Realign panel chrome to Mockup 10 / Full Synthesis layout)

**Activity log:** `fb18c173-5e3b-4b3c-9488-bb4f6bf74a63`

---

## Original queue (preserved below for reference)

**Reason for queueing:** FRED MCP server was offline at the time these artifacts were produced (`mcp__fred__health_check` returned "MCP server fred is not connected"). Once FRED reconnects, push this queue to the FRED knowledge base in one batch.

---

## How to apply this queue

Run these MCP calls when FRED is back online. Working directory doesn't matter — these are remote MCP operations.

### Step 0 — Verify FRED is back

```
mcp__fred__health_check()
```

Expected: success response with uptime / service availability. If still offline, leave queue in place and try again later.

### Step 1 — Find the FRED-side project for FRED itself

We want both:
- The MCCCD-AA140 project on FRED to receive the docs (pinned to `c1937681-e57d-4354-aa58-a5b0f6e9ca23`)
- The FRED-self project (the one that holds knowledge for future agent training) to also receive copies

```
mcp__fred__find_projects(query="FRED")
```

Pick the project ID that represents FRED's self-knowledge base. Capture as `FRED_PROJECT_ID`.

### Step 2 — Push the three docs to MCCCD project as `lesson` documents

```
mcp__fred__manage_document(
  action="create",
  project_id="c1937681-e57d-4354-aa58-a5b0f6e9ca23",
  title="Drag-and-Drop Source Routing — Lessons Learned",
  document_type="lesson",
  tags=["drag-drop", "ch5", "touch-panel", "svelte", "pointer-events", "patterns"],
  author="Jordan Scales",
  content={ "source_path": "MCCCD-AA140/docs/Lessons-Learned/Drag-Drop-Source-Routing-Lessons.md", "summary": "20 patterns and anti-patterns from building a drag-and-drop source routing UX on a Crestron CH5 TS-1070 panel. Covers process (mockup-first, subagent-driven dev, two-stage review), architecture (writable stores + module-level let, source-of-truth shift to feedback signals), touch-panel pointer hardening (setPointerCapture, MOVE_CANCEL_THRESHOLD = 30, listener leak fix, multi-touch guard, pointercancel-as-snap-back), and animation patterns (three-phase drop, void offsetWidth reflow, opacity-fade-on-snap-back)." }
)

mcp__fred__manage_document(
  action="create",
  project_id="c1937681-e57d-4354-aa58-a5b0f6e9ca23",
  title="Drag-and-Drop Source Routing — Retrospective Write-up",
  document_type="lesson",
  tags=["drag-drop", "retrospective", "narrative", "process"],
  author="Jordan Scales",
  content={ "source_path": "MCCCD-AA140/docs/Lessons-Learned/Drag-Drop-Source-Routing-Writeup.md", "summary": "Step-by-step narrative retrospective of the drag-and-drop UX build. Covers original ask, brainstorming forks, Stage 1 mockup execution, Stage 2 Svelte port, hardware-driven tuning round, theme alignment, and the eventual park-as-subpage pivot. 22 commits across one driving session. Cross-references the Lessons-Learned doc for distilled patterns." }
)

mcp__fred__manage_document(
  action="create",
  project_id="c1937681-e57d-4354-aa58-a5b0f6e9ca23",
  title="Session Handoff — Drag-Drop Stage 1+2 (2026-05-02)",
  document_type="handoff",
  tags=["drag-drop", "handoff", "open-items"],
  author="Jordan Scales",
  content={ "source_path": "MCCCD-AA140/docs/Handoffs/2026-05-02-drag-drop-stage-1-2-handoff.md", "summary": "Point-in-time session log. Lists 22 commits, all changed files, panel IP migration (192.168.2.53 → 192.168.1.175), and 7 open polish items. Use this for picking up the work mid-state." }
)
```

### Step 3 — Push the same docs to FRED's self-knowledge project

Repeat Step 2's three calls but with `project_id=<FRED_PROJECT_ID>` from Step 1. The documents become teaching material for future agent sessions to draw on regardless of which project they're working in.

If FRED's content storage requires the document body inline (not a path reference), read the source files and pass `content` as `{ "body": <full markdown content> }`. The exact schema depends on how `manage_document` handles `content`.

### Step 4 — Activity log

```
mcp__fred__log_agent_activity(
  agent_name="Claude Code (claude-opus-4-7)",
  action_type="document_update",
  summary="Captured drag-and-drop source routing patterns into 3 docs in MCCCD-AA140/docs/Lessons-Learned/ and queued FRED sync. Stage 2 was deployed to TS-1070 (192.168.1.175), drag-drop UX functional but parked as un-linked sub-page pending chrome realignment to Mockup 10. 22 commits on feat/drag-drop-router-mockup branch.",
  machine_name="Lil Boogie",
  resource_type="project",
  resource_id="c1937681-e57d-4354-aa58-a5b0f6e9ca23"
)
```

### Step 5 — Update the existing Stage-1 task, add follow-up tasks

```
mcp__fred__manage_task(
  action="update",
  task_id="318aec66-c590-4d48-8f3e-6d3404e028d6",
  status="done",
  description="DONE — Stage 1 mockup complete (mockups/18-drag-drop-router.html). Stage 2 Svelte port also complete and deployed to TS-1070 in same session. UX functional but parked as un-linked sub-page; redesign to align with Mockup 10's layout pending. See follow-up tasks for the redesign and polish work."
)

mcp__fred__manage_task(
  action="create",
  project_id="c1937681-e57d-4354-aa58-a5b0f6e9ca23",
  title="Drag-drop polish: long-press reliability, hit zones, animation feel",
  description="Stage 2 drag-drop is parked as a sub-page (currentPage='dragdrop'). Open polish items from the 2026-05-02 session: (1) Long-press threshold 30px may need bump to 50px or chip-bounding-box detection; (2) Drag motion smoothness via rAF throttling if still jumpy; (3) Verify audio/mirror icon buttons don't intercept tile clicks via elementFromPoint; (4) tile-flash class is on .tile vs .tile-slot — verify visual reads correctly on hardware; (5) Fix pre-existing svelte-check error in MicVolumeModal.svelte:64; (6) Update package.json deploy:tabletop default IP from 192.168.2.53 to 192.168.1.175. Reference: MCCCD-AA140/docs/Lessons-Learned/Drag-Drop-Source-Routing-Lessons.md.",
  status="todo",
  assignee="AI IDE Agent",
  task_order=70,
  feature="drag-drop-routing"
)

mcp__fred__manage_task(
  action="create",
  project_id="c1937681-e57d-4354-aa58-a5b0f6e9ca23",
  title="Drag-drop redesign: align to Mockup 10 chrome (left nav rail + status bar + footer)",
  description="The drag-drop UX conflicts structurally with Mockup 10 — both want the left rail. Mockup 10 uses left rail for navigation (Home/Cams/Setup/Power); drag-drop uses it for source palette. Redesign options: (a) Move source chips to a horizontal strip above tiles, leaving rail for nav; (b) Widen rail to hold both nav (bottom) and source chips (top); (c) Move nav into the status bar, keep rail for sources. Decide direction during the chrome realignment pass; rebuild the dragdrop sub-page accordingly. Spec: MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-stage-2-svelte-port-design.md.",
  status="todo",
  assignee="User",
  task_order=65,
  feature="drag-drop-routing"
)

mcp__fred__manage_task(
  action="create",
  project_id="c1937681-e57d-4354-aa58-a5b0f6e9ca23",
  title="Realign panel chrome to Mockup 10 (Full Synthesis)",
  description="User picked Mockup 10 as the canonical look (Mockup 11 was originally chosen but switched). Mockup 10 layout: 72px left nav rail (Home/Cams/Setup + Power at bottom); thin status bar (room name + occupancy timer + online + occupied pills); per-display source button grid (4 buttons each); 3-zone footer (System power info + Mics with state tags + Vol). Project is currently using Mockup 11 orange theme (already applied in commit 4ec9f41) but Mockup 11 layout, not 10. This task: refactor Home/App/footer/header to match Mockup 10 structure. Mockup 10 already uses orange so no theme change needed.",
  status="todo",
  assignee="User",
  task_order=80,
  feature="ui-chrome"
)
```

---

## Source files to push (paths relative to repo root)

These are the artifacts FRED should ingest:

- `MCCCD-AA140/docs/Lessons-Learned/Drag-Drop-Source-Routing-Lessons.md` (~430 lines)
- `MCCCD-AA140/docs/Lessons-Learned/Drag-Drop-Source-Routing-Writeup.md` (~340 lines)
- `MCCCD-AA140/docs/Handoffs/2026-05-02-drag-drop-stage-1-2-handoff.md` (~190 lines)
- `MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-source-routing-design.md` (Stage 1 spec)
- `MCCCD-AA140/docs/superpowers/plans/2026-05-01-drag-drop-source-routing-plan.md` (Stage 1 plan)
- `MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-stage-2-svelte-port-design.md` (Stage 2 spec)

Optional reference (the static mockup as a code example):
- `mockups/18-drag-drop-router.html`

---

## Done criteria

This queue is "applied" when:

1. All 6 docs above are stored as documents in BOTH the MCCCD-AA140 FRED project AND the FRED-self project.
2. The 4 task operations (1 update, 3 create) have succeeded.
3. The activity log entry is recorded.
4. This file (`FRED-Sync-Queue.md`) is moved to `MCCCD-AA140/docs/Lessons-Learned/FRED-Sync-Queue.applied.md` or deleted, with a note in the directory README that it was applied on `<date>`.
