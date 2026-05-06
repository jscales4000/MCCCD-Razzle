# Lessons Learned: ch5-video Cutout Architecture

**Date:** 2026-04-26
**Project:** MCCCD-AA140 Touchpanel
**Authors:** Claude (Opus 4.7) + Jordan Scales
**Final state:** ✅ Camera streaming on TS-1070 from `rtsp://192.168.2.79:554/1.h264`
**Iterations to resolve:** 5 deploy rounds (v1.1 → v1.5)

---

## TL;DR

`ch5-video` uses a 3-tier rendering architecture (HTML cutout → native surface → GStreamer decoder) and has a non-obvious DOM stacking invariant that took us five iterations to satisfy:

```html
<body>
  <!-- Direct child of body. Position-fixed, behind app, NO CSS background. -->
  <ch5-background backgroundcolor="#0f172a"
                  style="position:fixed;inset:0;z-index:-1;"></ch5-background>

  <!-- App mount with explicit stacking context above ch5-background. -->
  <div id="app" style="position:relative;z-index:0;"></div>

  <!-- Scripts at end of body. -->
  <script src="./cr-com-lib.js"></script>
  <script src="./ch5-components.js"></script>
  <script type="module" src="./assets/main.js"></script>
</body>
```

The dark theme color comes from the `backgroundcolor` **attribute** (native compositor honors it), NOT from CSS `background-color` (which paints over the cutout in the HTML layer).

---

## What we did right

1. **Pulled the persona before guessing.** Once Jordan asked "did you follow the video persona", we stopped guessing and read the full 6391-char spec verbatim. That surfaced the complete 12-rule checklist.
2. **Searched FRED for prior patterns** before writing new code. Found the existing paramiko deploy pattern (DGE-1000 guide, IDE Rules) instead of fighting `ch5-cli deploy` interactive prompts forever.
3. **Built a non-interactive deploy pipeline** (`scripts/deploy.py` + `npm run deploy`) so iteration was one command, ~9 seconds end-to-end.
4. **Saved cross-session memories** for the workflow + panel target + `.ch5z`/`.cpz` distinction so the next session doesn't relearn this.
5. **Didn't claim success without panel verification.** Every "deployed" got a "test and tell me what you see" loop instead of being marked done prematurely.
6. **Iterated diagnostic hypotheses one at a time** — auth, attrs, transparency, stacking — instead of bundling guesses.
7. **Kept FRED tasks updated in real time** so the history is queryable from any future session (filter project `c1937681-...` by `feature=ch5-video` to replay this thread).

---

## What we did wrong

### Original implementation (entered the session this way)

The inherited `Cameras.svelte` had **12 violations** of the CH5 Video Specialist hard rules:

| # | Violation | Evidence |
|---|---|---|
| 1 | camelCase attrs | `sourceType`, `aspectRatio`, `indexId`, `zindex` |
| 2 | Invalid attrs | `indexId` and `zindex` aren't real ch5-video attributes |
| 3 | Wrong RTSP URL | `rtsp://${ip}/stream1` — no port, wrong path for 1Beyond |
| 4 | No userid/password | RTSP auth omitted entirely |
| 5 | No size/stretch/cwr | Native surface had no sizing hint |
| 6 | No receivestateplay | Playback never gated/triggered |
| 7 | Opaque body bg | Radial+linear gradient blocks the cutout |
| 8 | Opaque .video-container | `background: #050d1a` blocks the cutout |
| 9 | Opaque .preview-panel via .glass-card | Gradient blocks the cutout |
| 10 | No `<ch5-background>` | Native compositor had nothing to paint |
| 11 | Scripts in `<head>` | DGE/TS Android Chromium fails to upgrade ch5-video |
| 12 | No bridge functions | Native pipeline silently dropped events (-9007) |

### Mistakes I made during this session

1. **Set `background-color: #0f172a` on ch5-background as a "fallback".** Persona explicitly bans CSS backgrounds on ch5-background — they paint over the native compositor surface, blocking the cutout. Wasn't caught until v1.4.
2. **Removed all CSS positioning from ch5-background in v1.4 instead of just the bg color.** ch5-background's internal styles made it cover the viewport at default z-index, painting blue OVER the Svelte UI. Whole panel went solid blue.
3. **Initially placed `<ch5-background>` inside the Svelte tree** (App.svelte). Per persona example, it belongs as a direct child of `<body>` so its `IntersectionObserver` flips `isInitialized=true` at first paint. Moved to `build.mjs` template in v1.4.
4. **Wasted iterations on `ch5-cli deploy`** (interactive prompts hang in non-TTY shells) before searching FRED and finding the documented paramiko pattern. Should have searched first.
5. **Pursued JS dimension sync via `ResizeObserver`** as part of the fix when `componentwasresized="true"` already handles it. Backed off to CSS `position:absolute; inset:0` filling the container.

---

## Diagnostic timeline

| Version | Change | Result |
|---|---|---|
| v1.0 (inherited) | All 12 violations above | Placeholder, no stream |
| v1.1 | Style fixes only (borders, source buttons) — unrelated | Style polished |
| v1.2 | Persona compliance: lowercase attrs, creds, size/stretch/cwr, transparent ancestors, ch5-background in App.svelte (with CSS bg-color), bridge functions, RTSP URL → `:554/1.h264` | Placeholder visible (cutout request landed!) but no stream |
| v1.3 | Embed `admin:crestron@` in URL via RTSP basic auth | Same — placeholder, no stream. Auth wasn't the bottleneck. |
| v1.4 | Move ch5-background to `<body>` direct child, drop CSS bg-color, add 1s CamPlay fallback | Panel went 100% blue — Svelte UI hidden behind ch5-background |
| **v1.5** | Inline `style="position:fixed;inset:0;z-index:-1"` on ch5-background; `style="position:relative;z-index:0"` on `#app` | **✅ Stream visible** |

---

## Fixes that didn't work — and why

### 1. Embedding `admin:crestron@` in the RTSP URL (v1.3)

Worth trying because some camera firmware honors URL-based auth differently than the SDP/header path used by the `userid`/`password` attrs. **Didn't move the needle** — the cutout request itself wasn't reaching the native compositor properly, so auth path was irrelevant. Kept the change anyway as belt-and-suspenders for cameras that DO require URL auth.

### 2. Setting CSS `background-color: #0f172a` on ch5-background (v1.2)

Done as a defensive fallback in case the native renderer didn't paint anything. **This was the actual blocker** — CSS `background-color` paints in the HTML layer ON TOP of the native compositor surface, defeating the cutout architecture. Persona ban: *"Use ch5-background component instead of CSS backgrounds."* Should have read that more carefully.

### 3. Removing all CSS from ch5-background (v1.4)

Tried to "let the component self-style" per the persona's bare-bones example. **ch5-background's internal style positions it fullscreen but at default z-index** → painted over the entire app. Persona's example assumes a vanilla HTML page where ch5-background is a sibling at `<body>` level next to a sibling video container, not a framework app with a `#app` mount point.

### 4. Subscribing to `Csig.Platform_Info` as the sole CamPlay trigger (v1.2-v1.3)

Persona-recommended, but on this firmware/restart timing the event might not fire reliably. Added a 1s `setTimeout` fallback in v1.4. Probably wasn't the actual blocker — once the cutout was working in v1.5, both paths fire successfully and we kept both for resilience.

---

## The fix that worked (v1.5)

```html
<!-- build.mjs panel HTML template -->
<body>
  <ch5-background backgroundcolor="#0f172a"
                  style="position:fixed;inset:0;z-index:-1;"></ch5-background>
  <div id="app" style="position:relative;z-index:0;"></div>
  ...
  <script src="./cr-com-lib.js"></script>
  <script src="./ch5-components.js"></script>
  <script type="module" src="./assets/${jsFile}"></script>
</body>
```

**Three things had to be true at the same time:**

1. **ch5-background as a direct child of `<body>`** (not inside `#app`/Svelte tree). Its `IntersectionObserver` needs viewport visibility on first paint to flip `isInitialized=true`. Inside Svelte, mount happens AFTER body parse → race condition.
2. **Native color ONLY** — the dark color via `backgroundcolor="#0f172a"` attribute (native compositor renders it under the cutout). NO CSS `background-color` on ch5-background. NO CSS `background` on body/html or any video ancestor.
3. **Explicit z-index stacking** — ch5-background pushed to `z-index:-1`, `#app` set to `z-index:0` with `position:relative` to establish a stacking context. Without these, ch5-background (which self-positions fullscreen) covers the Svelte UI at default z-index.

---

## Implications for the CH5 Video Specialist persona

The persona's example HTML (vanilla, no framework) is correct but **incomplete for modern CH5 projects** where Svelte/React/Angular all mount to a `<div id="app">`. Recommended additions are tracked in:

- `docs/DEBUG-REPORTS/CH5-Video-Persona-Update.md`
- FRED debug report task in this project

**Key insight to add to the persona:** *ch5-background's internal CSS positions it fullscreen but assumes default z-index. In framework apps where the framework also mounts a div as a sibling, you MUST explicitly z-index ch5-background BEHIND the app root, AND ensure no CSS `background-color` on ch5-background itself.*

---

## Reference files (final state)

- `build.mjs` — panel HTML template with ch5-background + #app stacking
- `src/main.ts` — bridge functions + Csig.Platform_Info subscription + 1s CamPlay fallback
- `src/global.css` — `body { background: transparent !important }`
- `src/App.svelte` — minimal router, no ch5-background (now in build.mjs)
- `src/pages/Cameras.svelte` — persona-compliant `<ch5-video>` element + transparent ancestors
- `src/lib/cameras.ts` — `rtspMain()` returns `rtsp://admin:crestron@${ip}:554/1.h264`
- `scripts/deploy.py` — paramiko SFTP+PROJECTLOAD (replaces interactive ch5-cli)
- `package.json` — `npm run deploy` chains archive + deploy.py
