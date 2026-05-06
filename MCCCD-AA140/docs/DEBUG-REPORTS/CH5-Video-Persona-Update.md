# DEBUG REPORT: CH5 Video Specialist Persona — Add Cutout Stacking Context Rule

**Date:** 2026-04-26
**Reporter:** Claude (Opus 4.7) + Jordan Scales
**Project:** MCCCD-AA140 Touchpanel
**Persona target:** CH5 Video Integration Specialist (FRED id `f61640cf-bb2b-4807-bc27-97be34688245`)
**Severity:** Medium — causes silent video failure on framework-based CH5 projects
**Related:** `docs/Lessons-Learned/CH5-Video-Cutout-Architecture.md`

---

## Issue

The persona's HTML example assumes a **vanilla HTML page** where `<ch5-background>` and the video container are siblings at `<body>` level:

```html
<body>
  <ch5-background></ch5-background>
  <div style="position:absolute; ..."><ch5-video ...></ch5-video></div>
  ...
</body>
```

In **modern CH5 projects (Svelte / React / Angular / Vue)**, the framework mounts to a `<div id="app">` which is a sibling of `<ch5-background>`. ch5-background's internal CSS self-positions to fill the viewport — **but at default z-index** (`auto`, computed as 0 in normal flow). When the framework's `#app` div is also at default z-index but later in DOM order, painted-over behavior depends on rendering details and is fragile.

---

## Symptoms observed

Two distinct failure modes during MCCCD-AA140 debugging:

### Failure mode 1 — CSS bg blocks cutout
- **Trigger:** `<ch5-background>` has CSS `background-color: <color>` (e.g. as a "fallback")
- **Result:** Entire HTML layer becomes opaque dark, video cutout creates the punch-through but only reveals the same dark color
- **User sees:** ch5-video placeholder forever, looking like "stream not connecting"
- **Actually:** "cutout being painted over by CSS in HTML layer"

### Failure mode 2 — z-index missing
- **Trigger:** `<ch5-background>` has no explicit z-index AND framework app uses #app mount
- **Result:** ch5-background's internal styles position it fullscreen, but at default z-index it paints over the framework's #app div
- **User sees:** Solid `backgroundcolor` filling viewport, no UI visible
- **Actually:** ch5-background literally rendered on top of the Svelte app

---

## Root cause

The persona's hard rule **"Use `<ch5-background>` component instead of CSS backgrounds"** prevents (1) but does NOT explicitly call out the CSS-bg-on-ch5-background-itself sub-case. Framework users who add a CSS fallback to ch5-background will hit (1).

The persona has **no guidance on z-index / stacking context** for framework apps. Users following the bare-bones example will hit (2) randomly depending on browser stacking/painting behavior.

---

## Recommended persona update

### Add new MUST DO rules

```markdown
- **Inline ch5-background's stacking context for framework apps**: When using
  Svelte/React/Angular/Vue (or any framework that mounts to a `<div id="app">`),
  set inline `style="position:fixed;inset:0;z-index:-1;"` on ch5-background AND
  `style="position:relative;z-index:0;"` on the app mount div. Without explicit
  z-index, ch5-background's internal positioning can paint over the app div in
  the HTML layer. The vanilla example in this persona is for plain HTML pages;
  framework apps need explicit stacking.
- **ch5-background must be a direct child of `<body>`**: NOT inside the
  framework component tree. Its IntersectionObserver requires viewport
  visibility on first paint to flip `isInitialized=true`. Framework mount
  happens AFTER body parse — placing ch5-background inside the framework tree
  creates a race that fails silently with -9007 errors.
```

### Add new NEVER DO rule

```markdown
- **NEVER** set CSS `background-color`, `background`, or `background-image`
  on the ch5-background element itself (e.g. as a "defensive fallback"). The
  CSS color paints in the HTML layer ON TOP of the native compositor surface,
  defeating the cutout architecture. Use the `backgroundcolor` attribute,
  which the native renderer honors. The existing rule "use ch5-background
  instead of CSS backgrounds" is meant to apply HERE TOO, not just on body/html.
```

### Add updated example (framework variant)

```html
<!-- For framework apps (Svelte/React/Angular/Vue) — use this variant: -->
<body>
  <!-- Direct child of body, BEFORE the app mount, with explicit z-index. -->
  <!-- DO NOT add CSS background-color — only the attribute. -->
  <ch5-background backgroundcolor="#0f172a"
                  style="position:fixed;inset:0;z-index:-1;"></ch5-background>

  <!-- Framework mount establishes stacking context above ch5-background. -->
  <div id="app" style="position:relative;z-index:0;"></div>

  <!-- ... ch5-video usage as before, inside the framework tree, with the -->
  <!-- ancestor-transparency rule still applying to all video ancestors ... -->

  <script src="./cr-com-lib.js"></script>
  <script src="./ch5-components.js"></script>
  <script type="module" src="./assets/main.js"></script>
</body>
```

### Update error code reference

Add entry to the existing error table:

```markdown
| -9007 (variant) | Stacking issue | If error -9007 appears WITH the placeholder
                                     visible AND `<ch5-background>` present and
                                     loaded, suspect CSS `background-color` on
                                     ch5-background OR missing z-index forcing
                                     it on top of the app. Check stacking. |
```

---

## Action items

1. **Update the CH5 Video Integration Specialist persona content** (FRED id `f61640cf-bb2b-4807-bc27-97be34688245`) with the additions above. Bump `updated_at` and note "framework stacking guidance" in description.
2. **Add a similar note to the Crestron CH5 Extended Developer persona** (`1a965715-...`) under "CH5-Svelte Integration Patterns" — framework users hit this first since they own the app shell.
3. **Consider adding a `crestron_validate_ch5_project` lint rule** that flags CSS `background-color` on ch5-background and warns when ch5-background is missing inline z-index in a framework project.
4. **Backport this knowledge** to other Crestron CH5 framework projects in the workspace (search FRED for projects assigned to the Extended Developer persona — there are ~20).

---

## Evidence

Full debugging timeline + iterations in `docs/Lessons-Learned/CH5-Video-Cutout-Architecture.md`. FRED tasks under project `c1937681-e57d-4354-aa58-a5b0f6e9ca23`, filter by `feature=ch5-video` to replay v1.0 → v1.5.
