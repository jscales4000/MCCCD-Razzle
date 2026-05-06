# Session Handoff — MCCCD-AA140 Touchpanel — 2026-04-26 (Session 2)

**Date:** 2026-04-26
**Driver:** Jordan Scales
**AI:** Claude (Opus 4.7, 1M context)
**Repo state:** 10 modified files + 1 new file, **uncommitted on `main`**
**Final state:** ✅ Camera streaming on TS-1070 from `rtsp://192.168.2.79:554/1.h264`
**Session entry point:** Resumed from `2026-04-26-session-handoff.md` (Session 1)

---

## TL;DR

Three big things landed this session:

1. **Style polish** — 0.5px hairline borders + sleek source buttons matching mockup #2 Signal Tile.
2. **Deploy automation** — `python scripts/deploy.py` (paramiko SFTP+PROJECTLOAD) replaces interactive `ch5-cli deploy` (was unusable in non-TTY shells). `npm run deploy` is now non-interactive, ~9 seconds end-to-end.
3. **ch5-video working on the panel** — 12 persona violations fixed across 5 deploy iterations (v1.1 → v1.5). Camera stream renders. Lessons captured in [`docs/Lessons-Learned/CH5-Video-Cutout-Architecture.md`](../Lessons-Learned/CH5-Video-Cutout-Architecture.md).

---

## What changed (10 modified, 1 new)

```
M  MCCCD-AA140/build.mjs                            panel HTML template — scripts→body, ch5-background direct child, z-index stacking
M  MCCCD-AA140/package.json                         deploy → paramiko; deploy:tabletop / deploy:wall variants
M  MCCCD-AA140/src/App.svelte                       drop CSS-positioned ch5-background (now in body via build.mjs)
M  MCCCD-AA140/src/components/DisplayTile.svelte    source-grid → grid-auto-rows:56px, drop min-height/font-weight overrides
M  MCCCD-AA140/src/components/MicChannel.svelte     border 1px → 0.5px
M  MCCCD-AA140/src/global.css                       body/html → transparent, all 1px borders → 0.5px
M  MCCCD-AA140/src/lib/cameras.ts                   rtspMain → admin:crestron@IP:554/1.h264; export CAM_USER, CAM_PASS; cam IPs → 192.168.2.79
M  MCCCD-AA140/src/main.ts                          register 4 bridge functions, Csig.Platform_Info subscribe + 1s CamPlay fallback
M  MCCCD-AA140/src/pages/Cameras.svelte             ch5-video persona-compliant attrs, transparent .preview-panel + .video-container
M  MCCCD-AA140/src/pages/Home.svelte                border 1px → 0.5px
?? MCCCD-AA140/scripts/deploy.py                    NEW — paramiko SFTP+PROJECTLOAD (Python 3.11.9 + paramiko 4.0.0)
```

Plus 5 new local docs (this handoff, lessons-learned, debug-report) and 5 cross-session memories under `~/.claude/projects/.../memory/`.

---

## How to deploy

```bash
cd "c:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/MCCCD-AA140"
npm run deploy             # → TS-1070 (192.168.2.53), default
npm run deploy:wall        # → TSW-1070 (192.168.2.123)
PANEL_HOST=192.168.2.123 python scripts/deploy.py    # custom override
```

Each deploy: vite build → ch5-cli archive → paramiko SFTP → SSH `PROJECTLOAD <name>.ch5z`. Total ~9 seconds. Panel UI auto-restarts with the new bundle.

**Auth:** admin / password baked into `scripts/deploy.py` defaults; override via env vars `PANEL_HOST`, `PANEL_USER`, `PANEL_PASS`, `PANEL_DIR`.

---

## What's open

| Area | Status |
|---|---|
| Camera stream on TS-1070 (Tabletop) | ✅ Working, verified by user |
| Camera stream on TSW-1070 (Wall) | Not deployed yet — `npm run deploy:wall` to send |
| WebXPanel video | Not supported by ch5-video architecture (firmware-level decoding only). UI works. |
| Other v1.1 features (occupancy timer, mirror-to-D3, audio routing, mics, presets) | Not yet tested on panel — only video is verified |
| `.cce` contract rebuild | Still pending — Phase 4 from Session 1 handoff (76 entries, needs Crestron Contract Editor Build) |
| SIMPL# Pro project bootstrap | Still pending — Phase 5 from Session 1 handoff |
| Field config (NVX/Q-SYS/1Beyond IPs) | Still pending — Phase 7 from Session 1 handoff |
| Persona update for cutout stacking | Tracked as FRED task; debug report at `docs/DEBUG-REPORTS/CH5-Video-Persona-Update.md` |
| Git commit | All changes uncommitted — user may want to review before committing |

---

## Critical knowledge — DON'T LOSE

1. **`.ch5z` deploys to TOUCHPANEL, `.cpz` to PROCESSOR.** User confused this once → uploaded to processor and got nothing. Saved to memory: `reference_crestron_deploy_destinations.md`.

2. **`<ch5-background>` MUST be a direct child of `<body>`** (set in `build.mjs`, NOT in App.svelte). Inline `style="position:fixed;inset:0;z-index:-1"` is required. NO CSS `background-color` — only the `backgroundcolor` attribute.

3. **`#app` needs `style="position:relative;z-index:0"`** to establish a stacking context above ch5-background. Without it, ch5-background paints over the Svelte UI.

4. **`ch5-cli deploy` does NOT work in Claude Code shells** (interactive prompts hang on SSH passphrase + SFTP creds). Use `python scripts/deploy.py` or `npm run deploy`.

5. **All 12 persona violations are addressed in the file changes — don't revert without re-validating.** See `docs/Lessons-Learned/CH5-Video-Cutout-Architecture.md` for the full audit.

6. **`ch5-video` cannot render in browser dev mode** — RTSP decoding is firmware-level. Use the panel for video testing. Browser dev is fine for everything else.

---

## Architecture diagram (rough)

```
┌─ TS-1070 panel (Android Chromium) ───────────────────────────────────┐
│                                                                      │
│  [body] ─── transparent (no CSS bg, per persona)                     │
│   │                                                                  │
│   ├─ <ch5-background> ── style: position:fixed inset:0 z-index:-1    │
│   │     attribute: backgroundcolor="#0f172a"                         │
│   │     → native compositor paints under cutout                      │
│   │                                                                  │
│   ├─ <div id="app"> ─── style: position:relative z-index:0           │
│   │     ├─ Svelte App.svelte router                                  │
│   │     │   └─ Cameras.svelte                                        │
│   │     │       └─ .preview-panel (transparent override)             │
│   │     │           └─ .video-container (transparent)                │
│   │     │               └─ <ch5-video> ── creates HTML cutout        │
│   │     │                     attrs: sourcetype, url, userid,        │
│   │     │                     password, size=custom, stretch=true,   │
│   │     │                     componentwasresized=true,              │
│   │     │                     receivestateplay="CamPlay"             │
│   │                                                                  │
│   ├─ <script src="cr-com-lib.js">                                    │
│   ├─ <script src="ch5-components.js">                                │
│   └─ <script type="module" src="./assets/index-XXX.js">              │
│         main.ts:                                                     │
│           - Register 4 bridge functions on window                    │
│           - Subscribe Csig.Platform_Info → publish CamPlay=true      │
│           - 1s setTimeout fallback → publish CamPlay=true            │
│           - Mount Svelte app                                         │
│                                                                      │
│  [native compositor]                                                 │
│   └─ GStreamer decoder ── reads rtsp://admin:crestron@.79:554/1.h264 │
│       → renders frames to native surface                             │
│       → cutout exposes them through the HTML transparent layers      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## How to resume in a new session

```
Read this handoff: MCCCD-AA140/docs/Handoffs/2026-04-26-session-2-handoff.md
Read lessons learned: MCCCD-AA140/docs/Lessons-Learned/CH5-Video-Cutout-Architecture.md
Read debug report: MCCCD-AA140/docs/DEBUG-REPORTS/CH5-Video-Persona-Update.md
Read prior handoff: MCCCD-AA140/docs/Handoffs/2026-04-26-session-handoff.md
Run: git status; git diff --stat
Then ask: continue testing other v1.1 features on panel? commit + PR? handle .cce rebuild? deploy to TSW-1070?
```

The 19 FRED tasks from this session are at project `c1937681-e57d-4354-aa58-a5b0f6e9ca23`. Filter:
- `feature=ch5-video` → 8 tasks (the violation fixes + the cutout/stacking saga)
- `feature=deploy-pipeline` → 3 tasks (paramiko script + package.json fix + deploy execution)
- `feature=border-style` → 2 tasks (border halving + source-button cleanup)
- `feature=session-memory` → 1 task (4 memory entries saved)
- 5 prior session tasks remain — see Session 1 handoff

---

## Memory updates this session

Saved 5 cross-session memories under `~/.claude/projects/<this-project>/memory/`:
- `feedback_panel_deploy_workflow.md` — archive+deploy after every relevant change without re-asking
- `reference_mcccd_aa140_panel.md` — TS-1070 @ 192.168.2.53 admin/password default
- `reference_mcccd_aa140_personas.md` — 6 FRED personas (NOT Claude subagents — use `mcp__fred__*` tools)
- `reference_crestron_deploy_destinations.md` — .ch5z→panel, .cpz→processor
- `feedback_ch5_background_cutout.md` — direct child of body, no CSS bg, explicit z-index — violations cause silent video failure

`MEMORY.md` index updated.

---

## Recent git history

```
(uncommitted — 10 modified, 1 new)
52cb605 docs(handoff): comprehensive session handoff for new session pickup    ← Session 1 closed here
e11dde6 feat(style): swap from #3 Hairline Schematic to #2 Signal Tile
6d2e129 feat(simpl#): v1.1 — wire ceiling mics, trims, zoom, SystemPowerFb
0c236f8 feat(v1.1): contract + style + behavior changes
43d9800 docs(spec): v2 — 4 sources, ceiling mics, Settings page, etc.
ccd16b7 docs(lessons-learned): Crestron Contract Editor hand-authoring constraints
...
```

If you want to commit, suggested split:
1. `feat(style): halve all borders to 0.5px hairline; sleek source buttons` — global.css, DisplayTile, MicChannel, Home, Cameras (style portions)
2. `feat(deploy): paramiko deploy.py replacing interactive ch5-cli` — package.json, scripts/deploy.py
3. `feat(ch5-video): persona-compliant ch5-video + ch5-background cutout architecture` — build.mjs, App.svelte, main.ts, global.css (bg part), Cameras.svelte (video part), cameras.ts
4. `docs: lessons learned + debug report for ch5-video cutout` — docs/

Or one bundled commit if simpler.

---

**End of handoff.**
