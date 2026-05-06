# Session Handoff — MCCCD-AA140 Touchpanel — 2026-04-26 (Session 3)

**Date:** 2026-04-26
**Driver:** Jordan Scales
**AI:** Claude (Opus 4.7, 1M context)
**Repo state:** 11 modified, 2 new files (uncommitted, on `main`) — extends Session 2's uncommitted state
**Final state:** ✅ Camera preview fills the bordered glass-card cleanly on TS-1070; transitions to/from Home and Settings are clean (no stale cutout)
**Session entry point:** Resumed from `2026-04-26-session-2-handoff.md`

---

## TL;DR

Session 2 got the camera streaming on TS-1070 but the cutout was sized wrong (vertical overflow + a stray expand-icon overlay). Session 3 closed both issues plus a third (perceived "too small"). Three big changes:

1. **Installed `ch5-theme.css`** — the persona's "Mandatory Requirements" table calls for it; Session 2 missed it. Without the theme, `<ch5-video>` renders at wrong dimensions and the default fullscreen-icon ships unstyled.
2. **Architecture pivot — `<ch5-video>` hoisted to body level.** After 5 in-band CSS/attribute fixes failed to grow the video perceptibly, we adopted the proven `1beyond-multicam` pattern: a single body-level `<ch5-video>` (declared in `build.mjs`) whose inline `top/left/width/height` are synced via `ResizeObserver` to a placeholder `<div class="video-container">` in `Cameras.svelte`.
3. **Page-transition pre-emption** — Svelte's `onDestroy` runs after the new page paints, so the cutout lingers for a frame. Subscribed to `currentPage` in Cameras' `onMount` so the video hides synchronously the instant the store flips away from `'cameras'`. `leaveCameras()` helper wraps the back-button click for the same reason.

Lessons captured in [`docs/Lessons-Learned/CH5-Video-Body-Level-Architecture.md`](../Lessons-Learned/CH5-Video-Body-Level-Architecture.md).

---

## What changed (relative to Session 2 end-state)

```
M  MCCCD-AA140/build.mjs                            ch5-theme.css link in <head>; <ch5-video> hoisted to body level (display:none default); #app z-index:0 → 1
M  MCCCD-AA140/src/pages/Cameras.svelte             remove inline <ch5-video>; add videoWindow placeholder + syncVideoToWindow + ResizeObserver + currentPage.subscribe pre-emption; leaveCameras() helper for back button; tightened layout (header 92→72, presets 168→112, sidebar 180→140, controls 240→200, padding/gap reduced); .preview-panel backdrop-filter:none, box-shadow:none
?? MCCCD-AA140/public/ch5-theme.css                 NEW — 3MB; copied from c:/Users/scale/CascadeProjects/1Beyond/1beyond-multicam/ch5-theme.css; supplies internal layout for <ch5-video>
?? MCCCD-AA140/docs/Lessons-Learned/CH5-Video-Body-Level-Architecture.md   NEW — this session's lessons-learned
?? MCCCD-AA140/docs/Handoffs/2026-04-26-session-3-handoff.md               NEW — this file
```

Plus all Session 2 changes still uncommitted underneath. Total uncommitted footprint:

```
M  MCCCD-AA140/build.mjs
M  MCCCD-AA140/package.json
M  MCCCD-AA140/src/App.svelte
M  MCCCD-AA140/src/components/DisplayTile.svelte
M  MCCCD-AA140/src/components/MicChannel.svelte
M  MCCCD-AA140/src/global.css
M  MCCCD-AA140/src/lib/cameras.ts
M  MCCCD-AA140/src/main.ts
M  MCCCD-AA140/src/pages/Cameras.svelte
M  MCCCD-AA140/src/pages/Home.svelte
?? MCCCD-AA140/docs/DEBUG-REPORTS/CH5-Video-Persona-Update.md
?? MCCCD-AA140/docs/Handoffs/2026-04-26-session-2-handoff.md
?? MCCCD-AA140/docs/Handoffs/2026-04-26-session-3-handoff.md
?? MCCCD-AA140/docs/Lessons-Learned/CH5-Video-Cutout-Architecture.md
?? MCCCD-AA140/docs/Lessons-Learned/CH5-Video-Body-Level-Architecture.md
?? MCCCD-AA140/public/ch5-theme.css
?? MCCCD-AA140/scripts/deploy.py
```

---

## How to deploy

(unchanged from Session 2)
```bash
cd "c:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/MCCCD-AA140"
npm run deploy             # → TS-1070 (192.168.2.53), default
npm run deploy:wall        # → TSW-1070 (192.168.2.123)
```

---

## What's open

| Area | Status |
|---|---|
| Camera stream sized correctly on TS-1070 | ✅ Working, verified by Jordan |
| Page transition (Cameras → Home / Settings) clean | ✅ Verified — pre-empt subscription + leaveCameras() |
| Camera stream on TSW-1070 (Wall) | Not deployed — `npm run deploy:wall` to send |
| Camera switching changes processor source, but URL is hardcoded | Acceptable for now — all 3 1Beyond IPs are 192.168.2.79; processor routes the active stream. When real-world IPs differ, wire `receivestateurl="Cam.Url"` and publish from Svelte. |
| Stop CamPlay on leave (free panel CPU) | Open follow-up — currently we only `display:none` the element. Publishing `CamPlay=false` on `leaveCameras` would also stop GStreamer decoding. |
| Other v1.1 features (occupancy timer, mirror-to-D3, audio routing, mics, presets) | Not yet tested on panel — only video is verified |
| `.cce` contract rebuild | Still pending — Phase 4 from Session 1 |
| SIMPL# Pro project bootstrap | Still pending — Phase 5 from Session 1 (FRED task `0945a771-…` open & doing) |
| Field config (NVX/Q-SYS/1Beyond IPs) | Still pending — Phase 7 from Session 1 |
| Persona update task | FRED task `e51de4a3-…` updated with this session's body-level + page-transition findings |
| Git commit | All changes uncommitted — Jordan to review across both Session 2 and Session 3 |

---

## Critical knowledge — DON'T LOSE

(Session 2 critical knowledge still applies — see that handoff. New in Session 3:)

1. **`<ch5-video>` lives at body level. Period.** In a Svelte/React/Angular app, you cannot reliably size it from inside the component tree even with `componentwasresized="true"`. Use the body-level + `ResizeObserver` pattern from `1beyond-multicam`. See [Lessons-Learned/CH5-Video-Body-Level-Architecture.md](../Lessons-Learned/CH5-Video-Body-Level-Architecture.md).

2. **`ch5-theme.css` is non-optional.** Without it, the element renders at wrong dimensions AND the default fullscreen-icon ships unstyled (visible black square). 3MB unminified, but the `.ch5z` ships compressed (+102KB on the wire). Copied from `c:/Users/scale/CascadeProjects/1Beyond/1beyond-multicam/ch5-theme.css`.

3. **Hide the body-level video BEFORE the route change paints.** Svelte's `onDestroy` runs after the next page renders, so the native cutout lingers ~1 frame as a stale patch. Subscribe to the page store and call `hideVideo()` synchronously on flip; ALSO wrap navigation calls in a `leaveCameras()` helper that hides first then navigates.

4. **`backdrop-filter: blur(...)` on any layer above the body-level video blurs the video** even when the layer's own background is transparent. Set `backdrop-filter: none` on `.preview-panel` (the .glass-card sitting in front of the cutout area). Box-shadow does not block the cutout but adds visual weight to a panel that is now mostly invisible — also disabled here.

5. **Stack order** (back → front): `ch5-background (z-index:-1)` → `ch5-video (z-index:0)` → `#app (z-index:1)`. Make sure `#app` is z-index:1, not 0, or the video draws on top of the UI.

6. **The `.video-container` div in `Cameras.svelte` does NOT contain a `<ch5-video>` element anymore.** It's just a transparent positioning hint with `aspect-ratio: 16/9`. The PTZ overlay buttons inside it sit above the body-level video automatically because the Svelte tree is at z-index:1.

---

## Architecture diagram (Session 3 update)

```
┌─ TS-1070 panel (Android Chromium) ───────────────────────────────────┐
│                                                                      │
│  [body] ─── transparent (no CSS bg, per persona)                     │
│   │                                                                  │
│   ├─ <ch5-background backgroundcolor="#0f172a"                       │
│   │     style="position:fixed;inset:0;z-index:-1;">                  │
│   │       → native compositor paints under cutout                    │
│   │                                                                  │
│   ├─ <ch5-video id="cam-preview"                                     │
│   │     ...persona-compliant attrs...                                │
│   │     style="display:none; position:fixed; z-index:0;">            │
│   │       → ONE static, body-level cutout host                       │
│   │       → Cameras.svelte writes top/left/width/height + display    │
│   │       → ResizeObserver keeps it in sync                          │
│   │       → currentPage.subscribe hides it on page change            │
│   │                                                                  │
│   ├─ <div id="app" style="position:relative; z-index:1;">            │
│   │     ├─ Svelte App.svelte router                                  │
│   │     │   └─ Cameras.svelte                                        │
│   │     │       └─ .preview-panel (transparent, backdrop-filter:none)│
│   │     │           └─ .video-container (aspect-ratio:16/9, hint)    │
│   │     │               └─ .ptz-overlay (above the cutout)           │
│   │                                                                  │
│   ├─ <link rel="stylesheet" href="./ch5-theme.css">                  │
│   ├─ <script src="cr-com-lib.js">                                    │
│   ├─ <script src="ch5-components.js">                                │
│   └─ <script type="module" src="./assets/index-XXX.js">              │
│         main.ts: bridge functions + Csig.Platform_Info subscribe     │
│                  + 1s CamPlay fallback + initSignals + mount(App)    │
│                                                                      │
│  [native compositor]                                                 │
│   └─ GStreamer decoder ── reads rtsp://admin:crestron@.79:554/1.h264 │
│       → renders frames to native surface                             │
│       → cutout exposes them through the HTML transparent layers      │
│       → cutout RECT comes from inline top/left/width/height on       │
│         #cam-preview (set by Cameras.svelte JS, not CSS)             │
└──────────────────────────────────────────────────────────────────────┘
```

---

## How to resume in a new session

```
Read this handoff: MCCCD-AA140/docs/Handoffs/2026-04-26-session-3-handoff.md
Read this session's lessons: MCCCD-AA140/docs/Lessons-Learned/CH5-Video-Body-Level-Architecture.md
Read prior session's lessons: MCCCD-AA140/docs/Lessons-Learned/CH5-Video-Cutout-Architecture.md
Read prior handoffs: MCCCD-AA140/docs/Handoffs/2026-04-26-session-2-handoff.md (and -session-handoff.md for Session 1)
Run: git status; git diff --stat
Then ask: commit + PR? CamPlay=false on leave? .cce rebuild? deploy:wall? continue v1.1 feature testing?
```

The 19 FRED tasks from Session 2 + new Session 3 tasks are at project `c1937681-e57d-4354-aa58-a5b0f6e9ca23`. Filter:
- `feature=ch5-video` → 14 tasks (the violation fixes + the cutout/stacking saga)
- `feature=ch5-video-architecture` → 3 new tasks this session (theme install, body-level pivot, page-transition preempt)
- `feature=session-memory` → 2 tasks (S2 + S3 writeups)

---

## Memory updates this session

No new cross-session memory entries created — Session 2's 5 entries still cover the workflow. The body-level pattern is captured in this session's lessons-learned doc rather than memory because it's a *project pattern* (specific to MCCCD-AA140's framework choice) rather than a *workflow rule*.

If a future session works on a fresh CH5+Svelte project, the `feedback_ch5_background_cutout.md` memory should be expanded to add: *"For `<ch5-video>` (not just `<ch5-background>`), hoist the element to body level and sync via ResizeObserver. See MCCCD-AA140's Cameras.svelte for the reference implementation."*

---

## Recent git history

```
(uncommitted — 10 modified, 7 new since main)
52cb605 docs(handoff): comprehensive session handoff for new session pickup    ← Session 1 closed here
e11dde6 feat(style): swap from #3 Hairline Schematic to #2 Signal Tile
6d2e129 feat(simpl#): v1.1 — wire ceiling mics, trims, zoom, SystemPowerFb
0c236f8 feat(v1.1): contract + style + behavior changes
43d9800 docs(spec): v2 — 4 sources, ceiling mics, Settings page, etc.
ccd16b7 docs(lessons-learned): Crestron Contract Editor hand-authoring constraints
...
```

If committing, suggested split (extends Session 2's split):
1. `feat(style): halve all borders to 0.5px hairline; sleek source buttons` — global.css, DisplayTile, MicChannel, Home, Cameras (style)
2. `feat(deploy): paramiko deploy.py replacing interactive ch5-cli` — package.json, scripts/deploy.py
3. `feat(ch5-video): persona-compliant ch5-video + ch5-background cutout architecture` — Session 2 ch5-video work
4. `feat(ch5-video): hoist ch5-video to body level + ResizeObserver sync` — build.mjs, Cameras.svelte (architecture pivot), public/ch5-theme.css (S3)
5. `feat(ch5-video): pre-empt page transition to release native cutout` — Cameras.svelte (page-store subscription, leaveCameras helper) (S3)
6. `docs: lessons learned + debug report + handoffs` — all docs/

Or two bundled commits: "Session 2 (cutout)" + "Session 3 (architecture pivot)".

---

**End of handoff.**
