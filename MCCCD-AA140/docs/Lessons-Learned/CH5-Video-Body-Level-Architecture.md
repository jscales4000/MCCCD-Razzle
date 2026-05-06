# Lessons Learned: ch5-video Belongs at Body Level (Framework Apps)

**Date:** 2026-04-26 (Session 3, follow-up to Session 2)
**Project:** MCCCD-AA140 Touchpanel
**Authors:** Claude (Opus 4.7) + Jordan Scales
**Final state:** ✅ Camera preview fills its glass-card cleanly on TS-1070, no overflow, no expand-icon ghost, no stale-cutout flash on page change.
**Iterations to resolve:** 5 in-band CSS/attribute fixes (all failed) → 1 architecture pivot → 1 page-transition polish.

---

## TL;DR

**You cannot reliably size `<ch5-video>` from inside a Svelte/React/Angular component tree.** The native compositor is decoupled from your framework's render lifecycle, and `componentwasresized="true"` does not save you when the host element's bounding rect is computed late in the tree. The persona's small example works because the persona's example is vanilla HTML at body level. **In a framework app, hoist `<ch5-video>` to body level (next to `<ch5-background>`) and use `getBoundingClientRect()` + `ResizeObserver` to sync its inline `top/left/width/height` to a placeholder div in the page.** This is the proven 1Beyond MultiCam pattern.

A second, separate finding: **`ch5-theme.css` is not optional.** Without it, `<ch5-video>` renders at wrong dimensions and ships a default fullscreen-icon button positioned by browser default (i.e., visible in random spots). Both symptoms had been masquerading as "almost works" in Session 2.

---

## What we did right

1. **Stopped to read the persona** when 2 CSS guesses in a row failed. Re-read the "Mandatory Requirements" table. Caught the missing `ch5-theme.css` and `component.js` requirements that Session 2 had glossed over because the cutout *worked* (color was visible) so we assumed the install was complete.
2. **Added a runtime debug overlay** (live `getBoundingClientRect` of `.work-area / .preview-panel / .video-container / ch5-video`) when the user reported "still the same" after 4 fixes. The numbers proved my CSS *was* applying — the video was at 753×423 inside a 759×428 container — and that the bottleneck was layout proportions, not container shape. That data forced the architectural pivot.
3. **Looked at the working multicam project** as soon as the user pointed at `c:/Users/scale/CascadeProjects/1Beyond/1beyond-multicam`. Reading their `index.html` + `CameraPreview.svelte` end-to-end revealed the body-level + ResizeObserver pattern in ~30 seconds. Should have done this before fix #1.
4. **Counted strikes per the systematic-debugging skill.** After 3 failed fixes the rule says question architecture, not patterns. Hitting strike 5 was the signal to pivot, not "try fix #6 with a different CSS clamp."
5. **Pre-empted the page transition** instead of relying on Svelte's `onDestroy`. The native cutout is a screen-space surface — releasing it after the next page paints leaves a frame of stale video on top of Home/Settings.

## What we did wrong

1. **Trusted the Session 2 "it works" without reading the persona's full Mandatory Requirements table.** Session 2 nailed the cutout color via `<ch5-background>` and considered the install complete; the missing `ch5-theme.css` only surfaced as a sizing/icon issue when we actually looked at the video closely.
2. **Five iterations of CSS/attribute fixes inside the Svelte component tree.** Every fix moved 5–20 px and felt like progress, but each had the same root cause (host bounding rect not reaching the native compositor cleanly) and the same failure mode (user: "still the same"). The systematic-debugging "if 3+ fixes fail, question the architecture" rule existed for exactly this situation.
3. **Assumed `aspectratio="16:9"` + `stretch="true"` + `size="custom"` would just work in a flex/grid host.** They do — but only when the host's rect is stable at first paint and reachable to the native compositor. In a framework's deeply-nested mount-time render, neither is guaranteed.
4. **Forgot that the cutout is screen-space, not layout-space.** When `display:none` is set late (in Svelte's onDestroy, which runs after the new page paints), the native surface lingers. Hide it pre-emptively when the page store flips.

---

## Diagnostic timeline

| Strike | Hypothesis | Fix | Result |
|---|---|---|---|
| 1 | Container CSS aspect-ratio wider than 16:9 → cutout overflows | `.video-container { aspect-ratio: 16/9; max-width:100%; max-height:100% }` | No change |
| 2 | The `aspectratio="16:9"` attr forces height = width × 9/16 inside a wider host | Drop `aspectratio="16:9"` from ch5-video | No change |
| 3 (root-cause) | Persona's "Mandatory Requirements" table calls for `ch5-theme.css` — never installed | Copy `ch5-theme.css` from working `1beyond-multicam` to `public/`; link in `<head>` BEFORE app CSS; restore `aspectratio="16:9"` | ✅ Aspect ratio correct, expand-icon now styled and tucked corner. **But video looked too small.** |
| 4 | Container is wider than 16:9 → cutout letterboxes inside | Container = 16:9 box, panel padding tightened 18px → 6/14/10 | "Still the same" — actually was ~7px bigger but imperceptible |
| 5 | Layout proportions starve the video | Header 92→72, presets 168→112, sidebar 180→140, controls 240→200, gaps/padding tightened | "Still no" — debug overlay confirmed CSS *was* applying; cap was elsewhere |
| **PIVOT** | ch5-video deeply nested in Svelte tree never sizes reliably; multicam pattern hoists it to body level | Move ch5-video to `build.mjs` body template; `.video-container` becomes a placeholder div; ResizeObserver syncs the body element's inline `top/left/width/height` to the placeholder's `getBoundingClientRect()` | ✅ Video fills the bordered card edge-to-edge, ~890×500 in panel-base coords (~1335×750 on TS-1070) |
| Polish | Video lingers on Home page for ~1 frame after navigating away | `currentPage.subscribe` inside Cameras' `onMount` calls `hideVideo()` synchronously when the store flips away from `'cameras'`; `leaveCameras()` helper does the same on the back button click | ✅ Clean transitions |

---

## The architecture, end state

```
┌─ build.mjs panel HTML template ────────────────────────────────────────────┐
│ <body>                                                                      │
│   <ch5-background ...>                          ← native bg compositor      │
│   <ch5-video id="cam-preview" ...               ← ONE static, body-level    │
│              style="display:none;                  cutout host. Inline      │
│                     position:fixed;                 top/left/width/height   │
│                     z-index:0;">                    written by JS at mount  │
│   <div id="app" style="z-index:1;"></div>       ← Svelte UI on top          │
│   <script src="cr-com-lib.js"></script>                                     │
│   <script src="ch5-components.js"></script>                                 │
│   <script type="module" src="...index.js"></script>                         │
│ </body>                                                                     │
└─────────────────────────────────────────────────────────────────────────────┘

┌─ Cameras.svelte mount ─────────────────────────────────────────────────────┐
│ - Render <div class="video-container" bind:this={videoWindow}>             │
│ - syncVideoToWindow(): getBoundingClientRect(videoWindow) → write top,      │
│   left, width, height inline on #cam-preview, set display:block             │
│ - new ResizeObserver(syncVideoToWindow).observe(videoWindow)                │
│ - currentPage.subscribe(p => p !== 'cameras' && hideVideo())                │
└─────────────────────────────────────────────────────────────────────────────┘

┌─ Cameras.svelte unmount / page change ─────────────────────────────────────┐
│ - PRE-EMPT: currentPage subscription fires hideVideo() before Svelte paints│
│   the next page                                                             │
│ - resizeObs.disconnect()                                                    │
│ - hideVideo() (idempotent — sets display:none on #cam-preview)              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Key invariants:**
- `<ch5-video>` is a singleton living for the whole app lifetime. It only becomes visible while on the Cameras page.
- The Svelte tree never renders a `<ch5-video>` element. `.video-container` is just a transparent positioning hint.
- Z-index stack from back to front: `ch5-background (-1)` → `ch5-video (0)` → `#app (1)`. Anything in the Svelte tree paints on top of the video by default; the video shows because `.preview-panel` and `.video-container` are transparent and `backdrop-filter` is disabled on the preview panel.
- `backdrop-filter: blur(16px)` from `.glass-card` *would* blur the body-level video underneath, even when the panel itself has a transparent background. Disable it on `.preview-panel` only.
- The page transition pattern (`hideVideo()` before `goToPage`) and the store subscription (`currentPage.subscribe(p => p !== 'cameras' && hideVideo())`) are belt-and-suspenders. Either alone would cover the back button; both together also cover deep links and external page changes.

---

## Persona additions this surfaces

The CH5 Video Integration Specialist persona's framework-app guidance is incomplete in two ways:

**1. The "Mandatory Requirements" table needs a non-negotiable rule:**
> *In framework apps (Svelte/React/Angular/Vue), `<ch5-video>` MUST be a body-level static element, not a child of the framework's mount root. Sync its inline `top/left/width/height` to a framework-rendered placeholder via `getBoundingClientRect()` + `ResizeObserver`. The "size to your CSS via componentwasresized" path is unreliable when the host element's rect resolves late in a deep component tree.*

**2. Page transitions need explicit teardown guidance:**
> *Hide the body-level `<ch5-video>` (`display:none`) BEFORE the framework's route change paints, not in your component's `onDestroy` / `componentWillUnmount`. The native cutout is screen-space and lingers ~1 frame past the framework's reconcile, leaving a stale video patch on the next page.*

Already filed as a follow-up against `f61640cf-bb2b-4807-bc27-97be34688245` (CH5 Video Integration Specialist persona). See FRED task `e51de4a3-b1d0-4495-8dc6-06a0aa318bcd` (now updated with this session's findings).

---

## What did NOT work — and why

### 1. CSS `aspect-ratio: 16/9` on `.video-container` (strike 1)
The container reshape *did* apply, and the cutout did follow. But because the cutout was already letterboxing within the host element (rather than escaping it), reshaping the host did not change the visible video size — only the dark band placement.

### 2. Dropping `aspectratio="16:9"` (strike 2)
Without `ch5-theme.css`, the attr was effectively a no-op anyway: there was no theme rule mapping it to `aspect-ratio: 16/9`. The actual element was rendering at default browser-derived dimensions, hence the wrong-shape symptom.

### 3. Tightening layout proportions (strike 5)
The math projected ~17% more video area. The debug probe confirmed the *container* grew by ~7×17 px. But the *cutout* stayed within ~5% of its prior size — because the rect change didn't propagate to the native compositor reliably mid-render. Numbers prove the architecture limit, not the layout limit.

### 4. Relying on `onDestroy` to hide the video on page change
Svelte's component cleanup runs after the route change reconciles. The native compositor doesn't release the cutout until the inline `display:none` is set, and that is now ~1 paint later than the new page's first frame. Result: stale video patch on Home for one frame. Fix is to hide pre-emptively on the store change.

---

## Reference files (final state)

- [build.mjs](../../build.mjs) — body-level `<ch5-video>` declared once with all attrs; `display:none` default
- [src/pages/Cameras.svelte](../../src/pages/Cameras.svelte) — `videoWindow` placeholder, `syncVideoToWindow()`, `ResizeObserver`, `currentPage.subscribe` pre-emption, `leaveCameras()` helper, `.preview-panel { backdrop-filter: none }`
- [public/ch5-theme.css](../../public/ch5-theme.css) — copied from `1beyond-multicam`; required for `<ch5-video>` internal layout + the styled fullscreen-icon

## Cross-reference

- Prior session: [CH5-Video-Cutout-Architecture.md](./CH5-Video-Cutout-Architecture.md) — Session 2 lessons (cutout / `<ch5-background>` z-index)
- Reference implementation: `c:/Users/scale/CascadeProjects/1Beyond/1beyond-multicam/src/components/CameraPreview.svelte` — the body-level + ResizeObserver pattern this session adopted
