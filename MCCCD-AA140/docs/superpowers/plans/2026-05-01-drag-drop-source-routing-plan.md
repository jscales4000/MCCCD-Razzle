# Drag-and-Drop Source Routing Mockup — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a static HTML mockup at `mockups/11-drag-drop-router.html` that demonstrates a left-rail source palette with drag-and-drop routing onto three display drop zones, supporting both long-press-to-drag and tap-to-arm-then-tap gestures.

**Architecture:** Single self-contained HTML file with embedded `<style>` and `<script>`, matching the existing mockup gallery convention. No framework, no build step, no Crestron wiring. State lives in a small in-file state machine (`appState`) that mutates DOM via vanilla JS event handlers. Pointer events (`pointerdown`/`pointermove`/`pointerup`) drive both touch and mouse identically, so the same code runs in browser DevTools and on the TS-1070 panel browser.

**Tech Stack:** HTML5, CSS (custom properties + transitions + keyframes), vanilla JavaScript (Pointer Events API).

**Spec:** `MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-source-routing-design.md`

---

## File Structure

- **Create:** `mockups/11-drag-drop-router.html` — the mockup (self-contained: HTML + embedded CSS + embedded JS, ~700 lines).
- **Modify:** `mockups/index.html` — append a card linking to mockup 11 inside the existing `.grid` container.

The mockup file is organized internally as:

```
<!DOCTYPE html>
<head>
  <style>
    :root { /* tokens */ }
    body  { /* canvas + grid */ }
    .header / .footer    { /* chrome */ }
    .rail / .chip        { /* source palette */ }
    .tile / .slot        { /* display drop zones */ }
    .chip-clone          { /* drag clone */ }
    .state-* / @keyframes { /* state-driven visuals + animations */ }
  </style>
</head>
<body>
  <header>...</header>
  <aside class="rail">...</aside>
  <main class="tiles">...</main>
  <footer>...</footer>
  <script>
    const appState = { armedChip: null, draggingChip: null, ... };
    /* event handlers, state transitions, DOM helpers */
  </script>
</body>
```

We commit to single-file because the gallery's iframe preview at 0.25× scale relies on each mockup being independent.

---

## Conventions

- **CSS tokens** at the top of `<style>` mirror the cyan-accent palette used in `mockups/index.html` (NOT the orange-theme used in mockup 10). Cyan accent: `#38bdf8`.
- **Source IDs** are strings: `'roomPc' | 'extPc' | 'airMedia' | 'laptop'`. Source labels are looked up from a `SOURCES` constant.
- **Display IDs** are strings: `'d1' | 'd2' | 'd3'`.
- **State** is a single object mutated through helpers — no global setters scattered through handlers.

---

### Task 1: Branch + skeleton file

**Files:**
- Create: `mockups/11-drag-drop-router.html`

- [ ] **Step 1: Create feature branch from current main**

```bash
git checkout main
git checkout -b feat/drag-drop-router-mockup
git status
```

Expected: branch switched to `feat/drag-drop-router-mockup`, working tree shows the same uncommitted changes that were on main (they travel along — that's intended; we don't touch any of them).

- [ ] **Step 2: Create the empty file with HTML5 boilerplate**

Create `mockups/11-drag-drop-router.html` with:

```html
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>Mockup 11 — Drag &amp; Drop Source Router</title>
<style>
  :root {
    --bg: #0b1220;
    --panel: rgba(15, 23, 42, 0.96);
    --panel-soft: rgba(15, 23, 42, 0.82);
    --border: rgba(148, 163, 184, 0.22);
    --copy: #e2e8f0;
    --copy-soft: #94a3b8;
    --copy-muted: #64748b;
    --accent: #38bdf8;
    --accent-soft: rgba(56, 189, 248, 0.18);
    --accent-dim: rgba(56, 189, 248, 0.10);
    --success: #22c55e;
    --rad: 14px;
    --rad-sm: 10px;
  }
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body {
    width: 1280px;
    height: 800px;
    overflow: hidden;
    background: var(--bg);
    color: var(--copy);
    font-family: "Segoe UI", system-ui, sans-serif;
    user-select: none;
    -webkit-user-select: none;
  }
  body::before {
    content: '';
    position: fixed;
    top: -200px;
    left: 50%;
    transform: translateX(-50%);
    width: 900px;
    height: 500px;
    background: radial-gradient(ellipse, rgba(56,189,248,.06), transparent 70%);
    pointer-events: none;
  }
  .stage {
    width: 100%;
    height: 100%;
    display: grid;
    grid-template-rows: 92px 1fr 104px;
    grid-template-columns: 96px 1fr;
    grid-template-areas:
      "header header"
      "rail   tiles"
      "footer footer";
    gap: 12px;
    padding: 12px;
  }
  header { grid-area: header; background: var(--panel); border: 0.5px solid var(--border); border-radius: var(--rad); }
  .rail { grid-area: rail; background: var(--panel-soft); border: 0.5px solid var(--border); border-radius: var(--rad); }
  .tiles { grid-area: tiles; display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }
  footer { grid-area: footer; background: var(--panel); border: 0.5px solid var(--border); border-radius: var(--rad); }
</style>
</head>
<body>
  <div class="stage">
    <header></header>
    <aside class="rail"></aside>
    <main class="tiles">
      <section class="tile" data-display="d1"></section>
      <section class="tile" data-display="d2"></section>
      <section class="tile" data-display="d3"></section>
    </main>
    <footer></footer>
  </div>
  <script>
    /* state machine + handlers — added in later tasks */
  </script>
</body>
</html>
```

- [ ] **Step 3: Open in browser to verify skeleton renders**

Open `mockups/11-drag-drop-router.html` in a browser (or via the gallery's preview). Expected:
- Page renders at 1280×800 with no scrollbars.
- 5 visible regions: header strip top, footer strip bottom, narrow rail on the left (96px), three placeholder tiles to the right.
- Subtle cyan radial glow centered above.
- No console errors.

- [ ] **Step 4: Commit**

```bash
git add mockups/11-drag-drop-router.html
git commit -m "feat(mockup-11): scaffold drag-drop router skeleton"
```

---

### Task 2: Static visuals — header, footer, rail chips (IDLE state), display tiles with initial routing

**Files:**
- Modify: `mockups/11-drag-drop-router.html`

This task adds all the static visual content. No interaction yet. Initial routing per spec: D1 → Room PC, D2 → AirMedia, D3 → unrouted.

- [ ] **Step 1: Add header content**

Inside `<header></header>`, replace with:

```html
<header>
  <div class="hdr">
    <div class="hdr-copy">
      <p class="eyebrow">CH5 Touch Panel</p>
      <h1>AA140</h1>
    </div>
    <div class="hdr-right">
      <div class="occ-block ok">Occupied</div>
      <div class="status-pill online">
        <span class="status-dot"></span>
        <span>Online</span>
      </div>
      <button class="hdr-btn">Cameras</button>
      <button class="hdr-btn">Settings</button>
    </div>
  </div>
</header>
```

Add to `<style>` (append after the existing rules):

```css
.hdr { height: 100%; padding: 0 20px; display: flex; align-items: center; justify-content: space-between; }
.hdr-copy .eyebrow { font-size: 11px; font-weight: 700; letter-spacing: .18em; text-transform: uppercase; color: var(--copy-muted); margin-bottom: 4px; }
.hdr-copy h1 { font-size: 28px; font-weight: 900; letter-spacing: -.02em; }
.hdr-right { display: flex; align-items: center; gap: 12px; }
.occ-block { padding: 10px 14px; border-radius: 8px; font-size: 12px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; border: 0.5px solid var(--border); }
.occ-block.ok { background: rgba(34,197,94,.12); border-color: rgba(34,197,94,.4); color: #bbf7d0; }
.status-pill { display: flex; align-items: center; gap: 6px; padding: 6px 12px; border-radius: 20px; font-size: 11px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; }
.status-pill.online { background: rgba(34,197,94,.1); border: 0.5px solid rgba(34,197,94,.25); color: #86efac; }
.status-dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; box-shadow: 0 0 6px currentColor; }
.hdr-btn { background: transparent; border: 0.5px solid var(--border); color: var(--copy-soft); padding: 8px 14px; border-radius: 8px; font-size: 12px; font-weight: 700; cursor: pointer; }
.hdr-btn:hover { color: var(--copy); border-color: var(--accent-soft); }
```

- [ ] **Step 2: Add footer content (Power | Mics | Volume)**

Inside `<footer></footer>`:

```html
<footer>
  <div class="ftr">
    <button class="ftr-power">
      <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round">
        <path d="M12 3v9"/>
        <path d="M6.5 7.5a8 8 0 1 0 11 0"/>
      </svg>
      <span>Power</span>
    </button>
    <div class="ftr-divider"></div>
    <div class="ftr-mics">
      <span class="ftr-label">MICS</span>
      <button class="ftr-btn">Lav</button>
      <button class="ftr-btn">Handheld</button>
    </div>
    <div class="ftr-divider"></div>
    <div class="ftr-vol">
      <button class="ftr-btn">Vol −</button>
      <button class="ftr-btn">Mute</button>
      <button class="ftr-btn">Vol +</button>
    </div>
  </div>
</footer>
```

Append to `<style>`:

```css
.ftr { height: 100%; display: flex; align-items: stretch; padding: 12px 18px; gap: 12px; }
.ftr-power { display: flex; align-items: center; gap: 8px; padding: 0 18px; background: transparent; border: 0.5px solid var(--border); border-radius: var(--rad-sm); color: var(--copy-soft); cursor: pointer; font-size: 13px; font-weight: 700; }
.ftr-divider { width: 1px; background: var(--border); }
.ftr-mics, .ftr-vol { display: flex; align-items: center; gap: 8px; }
.ftr-label { font-size: 11px; font-weight: 700; letter-spacing: .16em; text-transform: uppercase; color: var(--copy-muted); margin-right: 4px; }
.ftr-btn { padding: 0 16px; min-height: 56px; background: rgba(30,41,59,.5); border: 0.5px solid var(--border); border-radius: var(--rad-sm); color: var(--copy-soft); font-size: 13px; font-weight: 700; cursor: pointer; }
.ftr-btn:hover { color: var(--copy); }
.ftr-vol { margin-left: auto; }
```

- [ ] **Step 3: Add rail content (4 source chips, IDLE state)**

Replace `<aside class="rail"></aside>` with:

```html
<aside class="rail">
  <p class="rail-title">SOURCES</p>
  <p class="rail-help">long-press or tap to route</p>
  <button class="chip" data-source="roomPc">
    <svg class="chip-ico" viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.8">
      <rect x="3" y="4" width="18" height="12" rx="2"/>
      <path d="M8 20h8M12 16v4"/>
    </svg>
    <span class="chip-label">Room PC</span>
  </button>
  <button class="chip" data-source="extPc">
    <svg class="chip-ico" viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.8">
      <rect x="3" y="4" width="18" height="12" rx="2"/>
      <path d="M8 20h8M12 16v4"/>
      <circle cx="18" cy="9" r="1.4" fill="currentColor"/>
    </svg>
    <span class="chip-label">Ext PC</span>
  </button>
  <button class="chip" data-source="airMedia">
    <svg class="chip-ico" viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round">
      <path d="M5 12a10 10 0 0 1 14 0"/>
      <path d="M8.5 15.5a5 5 0 0 1 7 0"/>
      <circle cx="12" cy="19" r="1.3" fill="currentColor"/>
    </svg>
    <span class="chip-label">AirMedia</span>
  </button>
  <button class="chip" data-source="laptop">
    <svg class="chip-ico" viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round">
      <rect x="4" y="5" width="16" height="10" rx="1.5"/>
      <path d="M2 19h20"/>
    </svg>
    <span class="chip-label">Laptop</span>
  </button>
</aside>
```

Append to `<style>`:

```css
.rail { display: flex; flex-direction: column; align-items: center; padding: 14px 6px 12px; gap: 10px; }
.rail-title { font-size: 10px; font-weight: 800; letter-spacing: .22em; color: var(--copy-muted); }
.rail-help { font-size: 9px; font-weight: 600; letter-spacing: .04em; color: var(--copy-muted); text-align: center; line-height: 1.3; margin-bottom: 6px; }
.chip {
  width: 80px; min-height: 88px;
  display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 6px;
  padding: 10px 6px;
  background: linear-gradient(180deg, rgba(30,41,59,.7), rgba(30,41,59,.5));
  border: 0.5px solid var(--border);
  border-radius: var(--rad-sm);
  color: var(--copy-soft);
  cursor: pointer;
  transition: border-color 160ms, color 160ms, transform 160ms, box-shadow 160ms;
  touch-action: none; /* required for pointer events to fire on touch without scroll interception */
}
.chip:hover { color: var(--copy); border-color: var(--accent-soft); }
.chip-ico { color: var(--copy-soft); transition: color 160ms; }
.chip:hover .chip-ico { color: var(--accent); }
.chip-label { font-size: 11px; font-weight: 700; letter-spacing: .04em; }
```

- [ ] **Step 4: Add display tile content with initial routing (D1=roomPc, D2=airMedia, D3=empty)**

Replace the three `<section class="tile" data-display="dN"></section>` blocks. Define a helper function in the script later, but for now hard-code initial state:

```html
<main class="tiles">
  <section class="tile" data-display="d1">
    <div class="tile-head">
      <span class="tile-power on"></span>
      <span class="tile-label">DISPLAY 1</span>
    </div>
    <div class="tile-slot" data-routed="roomPc">
      <div class="landed-chip">
        <svg class="chip-ico" viewBox="0 0 24 24" width="32" height="32" fill="none" stroke="currentColor" stroke-width="1.8">
          <rect x="3" y="4" width="18" height="12" rx="2"/>
          <path d="M8 20h8M12 16v4"/>
        </svg>
        <span class="landed-label">Room PC</span>
      </div>
    </div>
    <div class="tile-actions">
      <button class="ib" title="Route audio">🔊</button>
      <button class="ib" title="Mirror to D3">⇗</button>
    </div>
  </section>
  <section class="tile" data-display="d2">
    <div class="tile-head">
      <span class="tile-power on"></span>
      <span class="tile-label">DISPLAY 2</span>
    </div>
    <div class="tile-slot" data-routed="airMedia">
      <div class="landed-chip">
        <svg class="chip-ico" viewBox="0 0 24 24" width="32" height="32" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round">
          <path d="M5 12a10 10 0 0 1 14 0"/>
          <path d="M8.5 15.5a5 5 0 0 1 7 0"/>
          <circle cx="12" cy="19" r="1.3" fill="currentColor"/>
        </svg>
        <span class="landed-label">AirMedia</span>
      </div>
    </div>
    <div class="tile-actions">
      <button class="ib" title="Route audio">🔊</button>
      <button class="ib" title="Mirror to D3">⇗</button>
    </div>
  </section>
  <section class="tile" data-display="d3">
    <div class="tile-head">
      <span class="tile-power"></span>
      <span class="tile-label">DISPLAY 3</span>
    </div>
    <div class="tile-slot" data-routed="">
      <span class="slot-empty">— No source —</span>
    </div>
    <div class="tile-actions"></div>
  </section>
</main>
```

Append to `<style>`:

```css
.tile {
  background: var(--panel);
  border: 0.5px solid var(--border);
  border-radius: var(--rad);
  display: grid;
  grid-template-rows: auto 1fr auto;
  padding: 14px;
  gap: 10px;
  transition: border-color 200ms, box-shadow 200ms, transform 160ms;
}
.tile-head { display: flex; align-items: center; gap: 8px; font-size: 11px; font-weight: 800; letter-spacing: .18em; color: var(--copy-muted); }
.tile-power { width: 8px; height: 8px; border-radius: 50%; background: rgba(100,116,139,.3); }
.tile-power.on { background: var(--success); box-shadow: 0 0 8px rgba(34,197,94,.6); }
.tile-slot {
  flex: 1;
  border-radius: var(--rad-sm);
  border: 1px dashed transparent;
  display: flex; align-items: center; justify-content: center;
  min-height: 200px;
  transition: border-color 200ms, background-color 200ms, transform 200ms;
}
.slot-empty { font-size: 13px; color: var(--copy-muted); font-style: italic; }
.landed-chip {
  display: flex; flex-direction: column; align-items: center; gap: 8px;
  padding: 18px 22px;
  background: rgba(30,41,59,.5);
  border: 0.5px solid var(--border);
  border-radius: var(--rad-sm);
  color: var(--copy);
  transition: transform 200ms;
}
.landed-chip .chip-ico { color: var(--accent); }
.landed-label { font-size: 14px; font-weight: 700; }
.tile-actions { display: flex; gap: 6px; justify-content: flex-end; }
.ib { width: 36px; height: 36px; background: transparent; border: 0.5px solid var(--border); border-radius: 8px; color: var(--copy-soft); cursor: pointer; }
.ib:hover { color: var(--copy); }
```

- [ ] **Step 5: Open in browser to verify all static content**

Reload the file. Expected:
- Header shows "AA140" with Occupied / Online pills and Cameras / Settings buttons.
- Left rail shows "SOURCES" header, help text, and 4 chips stacked (Room PC, Ext PC, AirMedia, Laptop).
- D1 shows the Room PC chip landed inside; D2 shows AirMedia; D3 shows "— No source —".
- Footer shows Power / Mics (Lav, Handheld) / Vol−, Mute, Vol+.
- Hovering a rail chip lightens its label and tints the icon cyan.
- No console errors.

- [ ] **Step 6: Commit**

```bash
git add mockups/11-drag-drop-router.html
git commit -m "feat(mockup-11): static layout (header, rail chips, display tiles, footer)"
```

---

### Task 3: Tap-to-arm + tap-to-route happy path

**Files:**
- Modify: `mockups/11-drag-drop-router.html`

Adds the `appState` object, a `route()` helper that swaps the landed chip in a tile, and click handlers wiring tap-to-arm + tap-tile-to-route.

- [ ] **Step 1: Add the SOURCES lookup constant and `appState` to the script block**

Replace the empty `<script>` block with:

```html
<script>
  /* ── Source metadata (id → label + SVG markup) ────────────────────── */
  const SOURCES = {
    roomPc:   { label: 'Room PC',  svg: '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4"/>' },
    extPc:    { label: 'Ext PC',   svg: '<rect x="3" y="4" width="18" height="12" rx="2"/><path d="M8 20h8M12 16v4"/><circle cx="18" cy="9" r="1.4" fill="currentColor"/>' },
    airMedia: { label: 'AirMedia', svg: '<path d="M5 12a10 10 0 0 1 14 0" stroke-linecap="round"/><path d="M8.5 15.5a5 5 0 0 1 7 0" stroke-linecap="round"/><circle cx="12" cy="19" r="1.3" fill="currentColor"/>' },
    laptop:   { label: 'Laptop',   svg: '<rect x="4" y="5" width="16" height="10" rx="1.5" stroke-linejoin="round"/><path d="M2 19h20" stroke-linejoin="round"/>' },
  };

  /* ── State ────────────────────────────────────────────────────────── */
  const appState = {
    armedSource: null,        // 'roomPc' | 'extPc' | 'airMedia' | 'laptop' | null
    armedSince: 0,            // ms timestamp; used for the 4s arm timeout
    armedTimeoutId: null,     // setTimeout handle
    draggingSource: null,     // (used in Task 5)
    suppressNextClick: false, // set by endDrag (Task 5) so click after drag is ignored
    routing: { d1: 'roomPc', d2: 'airMedia', d3: null },  // current routing
  };

  /* ── DOM helpers ──────────────────────────────────────────────────── */
  function $$(sel, root = document) { return Array.from(root.querySelectorAll(sel)); }
  function $(sel, root = document)  { return root.querySelector(sel); }

  /* Render the landed chip (or empty placeholder) inside a tile slot. */
  function renderSlot(displayId) {
    const slot = $(`.tile[data-display="${displayId}"] .tile-slot`);
    const sourceId = appState.routing[displayId];
    slot.dataset.routed = sourceId || '';
    if (!sourceId) {
      slot.innerHTML = '<span class="slot-empty">— No source —</span>';
      return;
    }
    const meta = SOURCES[sourceId];
    slot.innerHTML = `
      <div class="landed-chip">
        <svg class="chip-ico" viewBox="0 0 24 24" width="32" height="32" fill="none" stroke="currentColor" stroke-width="1.8">${meta.svg}</svg>
        <span class="landed-label">${meta.label}</span>
      </div>
    `;
  }

  /* ── State transitions ────────────────────────────────────────────── */
  function armChip(sourceId) {
    if (appState.armedSource === sourceId) { disarm(); return; }
    if (appState.armedSource) disarm();
    appState.armedSource = sourceId;
    appState.armedSince = Date.now();
    document.body.classList.add('any-armed');
    $(`.chip[data-source="${sourceId}"]`).classList.add('chip-armed');
    appState.armedTimeoutId = setTimeout(() => disarm(), 4000);
  }

  function disarm() {
    if (!appState.armedSource) return;
    $(`.chip[data-source="${appState.armedSource}"]`)?.classList.remove('chip-armed');
    appState.armedSource = null;
    appState.armedSince = 0;
    if (appState.armedTimeoutId) clearTimeout(appState.armedTimeoutId);
    appState.armedTimeoutId = null;
    document.body.classList.remove('any-armed');
  }

  function routeSource(sourceId, displayId) {
    if (appState.routing[displayId] === sourceId) return; // no-op
    appState.routing[displayId] = sourceId;
    renderSlot(displayId);
  }

  /* If a click is the synthetic event the browser fires after a pointerup
     at the end of a drag, swallow it so we don't accidentally re-arm or
     re-route. Task 5's endDrag sets this flag; Task 3 always reads it. */
  function shouldSuppressClick() {
    if (!appState.suppressNextClick) return false;
    appState.suppressNextClick = false;
    return true;
  }

  /* ── Wire chip taps ───────────────────────────────────────────────── */
  $$('.chip').forEach(chip => {
    chip.addEventListener('click', e => {
      if (shouldSuppressClick()) return;
      const sourceId = chip.dataset.source;
      if (appState.armedSource === sourceId) { disarm(); return; }
      armChip(sourceId);
    });
  });

  /* ── Wire tile taps (route when armed) ────────────────────────────── */
  $$('.tile').forEach(tile => {
    tile.addEventListener('click', e => {
      if (shouldSuppressClick()) return;
      if (!appState.armedSource) return;
      const sourceId = appState.armedSource;
      const displayId = tile.dataset.display;
      disarm();
      routeSource(sourceId, displayId);
    });
  });
</script>
```

- [ ] **Step 2: Add CSS for `chip-armed` and `any-armed` (drop-valid hint on tiles)**

Append to `<style>`:

```css
/* ── ARMED state on a chip ──────────────────────────────────────────── */
.chip-armed {
  border-color: var(--accent);
  box-shadow: 0 0 0 1.5px var(--accent-soft), 0 0 18px var(--accent-soft);
  color: var(--copy);
  animation: chip-arm-pulse 1.5s ease-in-out infinite;
}
.chip-armed .chip-ico { color: var(--accent); }
@keyframes chip-arm-pulse {
  0%, 100% { box-shadow: 0 0 0 1.5px var(--accent-soft), 0 0 18px var(--accent-soft); }
  50%      { box-shadow: 0 0 0 1.5px var(--accent),       0 0 24px rgba(56,189,248,.35); }
}

/* ── DROP-VALID — every tile lights up while a chip is armed ────────── */
body.any-armed .tile-slot {
  border-color: var(--accent-soft);
  background-color: var(--accent-dim);
}
```

- [ ] **Step 3: Open in browser and verify tap-to-arm**

Reload. Expected:
- Click "Room PC" chip in rail → chip gets cyan border + soft glow + pulses; all three display slots get a dashed cyan outline + faint cyan tint (DROP-VALID).
- Click "Room PC" again → returns to IDLE; all dashed outlines clear.
- Click "Ext PC" while Room PC is armed → Ext PC arms, Room PC un-arms (only one chip can be armed at a time).

- [ ] **Step 4: Verify tap-to-route**

Expected (continuing in browser):
- Arm Laptop chip → click D3 tile → D3's "— No source —" placeholder is replaced by a Laptop landed chip. Laptop chip in rail returns to IDLE. Tile dashed outlines clear.
- Arm Room PC → click D2 → D2's AirMedia chip is replaced by Room PC landed chip.
- Arm AirMedia → click D2 (which is already AirMedia after the next step ... actually, click D1 which is Room PC) → D1 swaps to AirMedia.

(Animations are not yet polished — this just verifies the state machine works.)

- [ ] **Step 5: Verify 4-second arm timeout**

Expected:
- Arm any chip → wait 4 seconds without clicking anything → chip returns to IDLE on its own; tile outlines clear.

- [ ] **Step 6: Commit**

```bash
git add mockups/11-drag-drop-router.html
git commit -m "feat(mockup-11): tap-to-arm and tap-to-route flow"
```

---

### Task 4: Cancel paths — tap outside, tap chip again, click elsewhere

**Files:**
- Modify: `mockups/11-drag-drop-router.html`

The existing handler already disarms when re-tapping the same chip. Need to add: tap outside the chip and outside any tile = disarm.

- [ ] **Step 1: Add document-level click handler that disarms when clicking elsewhere**

Append to the `<script>` block (after the existing tile-click handler, before the closing `</script>`):

```javascript
  /* Tapping outside the rail and outside any tile disarms. */
  document.addEventListener('click', e => {
    if (!appState.armedSource) return;
    const onChip = e.target.closest('.chip');
    const onTile = e.target.closest('.tile');
    if (!onChip && !onTile) disarm();
  });
```

NOTE: `click` events bubble, so the chip- and tile-handlers run *before* this document handler. The document handler only fires for clicks that didn't hit a chip or a tile.

- [ ] **Step 2: Open in browser and verify**

Reload. Expected:
- Arm Room PC → click anywhere on header / footer / rail blank space → chip disarms.
- Arm Room PC → click a tile (any tile, even if no-op'd) → chip disarms (existing behavior, regression check).
- Arm Room PC → click Room PC again → chip disarms (existing behavior, regression check).

- [ ] **Step 3: Commit**

```bash
git add mockups/11-drag-drop-router.html
git commit -m "feat(mockup-11): disarm on tap outside chip/tile"
```

---

### Task 5: Long-press → drag flow with chip clone following finger

**Files:**
- Modify: `mockups/11-drag-drop-router.html`

This task adds pointer-event handling: a 250ms hold on a chip enters DRAGGING, a clone of the chip follows the pointer, and lifting over a tile routes (lifting outside snaps back). For now we share the routing logic with Task 3 — animations are added in Task 6.

- [ ] **Step 1: Add CSS for the dragging clone, ghost slot, and DROP-HOVERING state**

Append to `<style>`:

```css
/* ── DRAGGING — original chip becomes a 30% ghost ──────────────────── */
.chip-ghost {
  opacity: 0.3;
  pointer-events: none;
}

/* The drag clone is appended to <body> and absolutely positioned. */
.chip-clone {
  position: fixed;
  top: 0; left: 0;
  width: 80px; min-height: 88px;
  display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 6px;
  padding: 10px 6px;
  background: linear-gradient(180deg, rgba(30,41,59,.95), rgba(30,41,59,.85));
  border: 1.5px solid var(--accent);
  border-radius: var(--rad-sm);
  color: var(--copy);
  transform: scale(1.08) rotate(2deg);
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.5), 0 0 24px rgba(56, 189, 248, 0.25);
  pointer-events: none;
  z-index: 1000;
  will-change: transform;
}
.chip-clone .chip-ico { color: var(--accent); }
.chip-clone .chip-label { font-size: 11px; font-weight: 700; letter-spacing: .04em; }

/* ── DROP-HOVERING — the one tile the finger is over ────────────────── */
.tile-slot.drop-hovering {
  border-color: var(--accent);
  border-style: solid;
  background-color: var(--accent-soft);
  transform: scale(1.02);
}
.tile-slot.drop-hovering::after {
  content: attr(data-hover-hint);
  position: absolute;
  bottom: 12px; left: 50%; transform: translateX(-50%);
  font-size: 11px; font-weight: 700; color: var(--accent);
  letter-spacing: .04em;
  white-space: nowrap;
}
.tile-slot { position: relative; }

/* Lower-emphasis DROP-VALID for the no-op case (drop on already-routed display). */
.tile-slot.drop-noop {
  border-color: rgba(148, 163, 184, 0.3);
  background-color: rgba(148, 163, 184, 0.04);
  border-style: dashed;
}
```

- [ ] **Step 2: Add pointer-event handlers for drag**

Append to `<script>` (after the document-click handler):

```javascript
  /* ── DRAGGING flow (long-press 250ms or drag from armed) ───────────── */
  const LONG_PRESS_MS = 250;
  const MOVE_CANCEL_THRESHOLD = 10; // px from press origin before a pending press is cancelled
  let pressTimerId = null;
  let pressOriginChip = null;
  let pressOriginX = 0, pressOriginY = 0;
  let lastPointerX = 0, lastPointerY = 0;
  let dragClone = null;

  function startDrag(sourceId, originChip, x, y) {
    appState.draggingSource = sourceId;
    document.body.classList.add('any-armed');
    originChip.classList.add('chip-ghost');
    dragClone = originChip.cloneNode(true);
    dragClone.classList.remove('chip', 'chip-ghost', 'chip-armed');
    dragClone.classList.add('chip', 'chip-clone');
    document.body.appendChild(dragClone);
    moveCloneTo(x, y);
    const hint = `Drop to route ${SOURCES[sourceId].label}`;
    $$('.tile-slot').forEach(slot => slot.dataset.hoverHint = hint);
  }

  function moveCloneTo(x, y) {
    if (!dragClone) return;
    // center the 80×88 clone on the pointer
    dragClone.style.transform = `translate(${x - 40}px, ${y - 44}px) scale(1.08) rotate(2deg)`;
  }

  function tileUnderPointer(x, y) {
    const el = document.elementFromPoint(x, y);
    return el?.closest('.tile') ?? null;
  }

  function updateHover(x, y) {
    const tile = tileUnderPointer(x, y);
    $$('.tile-slot').forEach(s => s.classList.remove('drop-hovering', 'drop-noop'));
    if (!tile) return;
    const slot = $('.tile-slot', tile);
    const displayId = tile.dataset.display;
    if (appState.routing[displayId] === appState.draggingSource) {
      slot.classList.add('drop-noop');
    } else {
      slot.classList.add('drop-hovering');
    }
  }

  function endDrag(x, y) {
    const tile = tileUnderPointer(x, y);
    const sourceId = appState.draggingSource;
    const originChip = $(`.chip[data-source="${sourceId}"]`);

    let dropped = false;
    if (tile) {
      const displayId = tile.dataset.display;
      if (appState.routing[displayId] !== sourceId) {
        routeSource(sourceId, displayId);
        dropped = true;
      }
    }

    // Snap back the clone if not dropped — for now, just remove it.
    // (Polish in Task 6 — actual snap-back animation.)
    if (dragClone) { dragClone.remove(); dragClone = null; }
    originChip?.classList.remove('chip-ghost');
    appState.draggingSource = null;
    document.body.classList.remove('any-armed');
    $$('.tile-slot').forEach(s => s.classList.remove('drop-hovering', 'drop-noop'));

    // Suppress the synthetic click that the browser fires after pointerup,
    // so a drag-and-release-on-the-original-chip doesn't accidentally re-arm.
    appState.suppressNextClick = true;
  }

  $$('.chip').forEach(chip => {
    chip.addEventListener('pointerdown', e => {
      if (e.button !== undefined && e.button !== 0) return; // primary only
      // Clear any stale suppression flag left over from a drag that ended in empty space.
      appState.suppressNextClick = false;
      pressOriginChip = chip;
      pressOriginX = lastPointerX = e.clientX;
      pressOriginY = lastPointerY = e.clientY;

      pressTimerId = setTimeout(() => {
        // Promote to DRAG. If chip was tap-armed already, disarm first.
        if (appState.armedSource) disarm();
        // Use the LATEST pointer position (not the initial one), so the clone
        // appears under the finger if it has drifted slightly during the press.
        startDrag(chip.dataset.source, chip, lastPointerX, lastPointerY);
        pressTimerId = null;
      }, LONG_PRESS_MS);

      // Listen on document so the drag continues even if the pointer leaves the chip.
      document.addEventListener('pointermove', onPointerMove);
      document.addEventListener('pointerup', onPointerUp, { once: true });
      document.addEventListener('pointercancel', onPointerUp, { once: true });
    });
  });

  function onPointerMove(e) {
    lastPointerX = e.clientX;
    lastPointerY = e.clientY;
    if (appState.draggingSource) {
      moveCloneTo(e.clientX, e.clientY);
      updateHover(e.clientX, e.clientY);
      return;
    }
    // Cancel a pending long-press if the pointer drifts past the threshold —
    // this prevents accidental drags from a brushed screen or a scroll.
    if (pressTimerId) {
      const dx = Math.abs(e.clientX - pressOriginX);
      const dy = Math.abs(e.clientY - pressOriginY);
      if (dx > MOVE_CANCEL_THRESHOLD || dy > MOVE_CANCEL_THRESHOLD) {
        clearTimeout(pressTimerId);
        pressTimerId = null;
        document.removeEventListener('pointermove', onPointerMove);
      }
    }
  }

  function onPointerUp(e) {
    document.removeEventListener('pointermove', onPointerMove);
    if (pressTimerId) {
      clearTimeout(pressTimerId);
      pressTimerId = null;
      // Press released before 250ms → fall through to the click handler (tap-to-arm).
      pressOriginChip = null;
      return;
    }
    if (appState.draggingSource) {
      endDrag(e.clientX, e.clientY);
    }
    pressOriginChip = null;
  }
```

- [ ] **Step 3: Open in browser and verify long-press → drag**

Reload. Use mouse (or browser DevTools touch emulation) to test:
- **Quick click on Room PC chip** → tap-to-arm fires (Room PC chip pulses cyan; tile slots dashed). Existing behavior, regression check.
- **Press and hold on Room PC for ~300ms then drag** → chip clone appears under the pointer, tilted +2° with cyan border. Original Room PC slot in rail goes to 30% opacity ghost.
- **Drag clone over D3** → D3's slot turns solid cyan with `Drop to route Room PC` hint at the bottom.
- **Release over D3** → D3 changes to Room PC. Clone disappears. Rail chip returns to full opacity.
- **Drag Room PC over D1 (already routed to Room PC)** → D1 shows the dimmer DROP-NOOP styling instead of bright DROP-HOVERING. Releasing on D1 → no change, clone disappears, no animation glitch.
- **Drag Room PC and release over the header** → no routing change; clone disappears.

- [ ] **Step 4: Commit**

```bash
git add mockups/11-drag-drop-router.html
git commit -m "feat(mockup-11): long-press to drag with hover/no-op states"
```

---

### Task 6: Drop animation + snap-back animation

**Files:**
- Modify: `mockups/11-drag-drop-router.html`

Currently, dropping on a tile just swaps the slot content instantly, and dropping outside removes the clone abruptly. This task adds the spec's three-phase drop animation (~280ms total) and the curved snap-back on cancel (~220ms).

- [ ] **Step 1: Add keyframes and animation classes for drop + snap-back**

Append to `<style>`:

```css
/* ── Drop animation (phase 2: thunk on the landed chip) ─────────────── */
@keyframes thunk {
  0%   { transform: scale(1.0); }
  40%  { transform: scale(1.06); }
  100% { transform: scale(1.0); }
}
.landed-chip.thunk { animation: thunk 100ms ease-out; }

/* ── Drop animation (phase 3: tile cyan border flash) ───────────────── */
@keyframes tile-flash {
  0%   { box-shadow: 0 0 0 0px var(--accent-soft); border-color: var(--accent); }
  100% { box-shadow: 0 0 0 6px transparent;        border-color: var(--border); }
}
.tile.flash { animation: tile-flash 150ms ease-out; }

/* ── Snap-back (clone curves back to its rail slot) ────────────────── */
.chip-clone.snapping {
  transition: transform 220ms cubic-bezier(0.4, 0.0, 0.2, 1), opacity 220ms;
}
```

- [ ] **Step 2: Animate drop on success and snap-back on cancel**

Replace the entire `endDrag` function in the `<script>` with:

```javascript
  function endDrag(x, y) {
    const tile = tileUnderPointer(x, y);
    const sourceId = appState.draggingSource;
    const originChip = $(`.chip[data-source="${sourceId}"]`);

    // Clear hover hints regardless of outcome
    $$('.tile-slot').forEach(s => s.classList.remove('drop-hovering', 'drop-noop'));
    document.body.classList.remove('any-armed');

    let dropOnTile = null;
    if (tile && appState.routing[tile.dataset.display] !== sourceId) {
      dropOnTile = tile;
    }

    if (dropOnTile) {
      // PHASE 1: animate clone to slot center
      const slot = $('.tile-slot', dropOnTile);
      const slotRect = slot.getBoundingClientRect();
      const targetX = slotRect.left + slotRect.width / 2 - 40;
      const targetY = slotRect.top + slotRect.height / 2 - 44;
      dragClone.classList.add('snapping');
      dragClone.style.transform = `translate(${targetX}px, ${targetY}px) scale(1.0) rotate(0deg)`;
      dragClone.style.opacity = '0';

      setTimeout(() => {
        // PHASE 2: swap slot content and thunk
        const displayId = dropOnTile.dataset.display;
        routeSource(sourceId, displayId);
        const newLanded = $('.landed-chip', slot);
        if (newLanded) {
          // restart animation reliably
          newLanded.classList.remove('thunk');
          void newLanded.offsetWidth;
          newLanded.classList.add('thunk');
        }
        // PHASE 3: tile border flash
        dropOnTile.classList.remove('flash');
        void dropOnTile.offsetWidth;
        dropOnTile.classList.add('flash');

        if (dragClone) { dragClone.remove(); dragClone = null; }
        originChip?.classList.remove('chip-ghost');
        appState.draggingSource = null;
      }, 180);
    } else {
      // SNAP-BACK: animate clone back to origin chip's rail position
      const originRect = originChip.getBoundingClientRect();
      dragClone.classList.add('snapping');
      dragClone.style.transform = `translate(${originRect.left}px, ${originRect.top}px) scale(1.0) rotate(0deg)`;

      setTimeout(() => {
        if (dragClone) { dragClone.remove(); dragClone = null; }
        originChip?.classList.remove('chip-ghost');
        appState.draggingSource = null;
      }, 220);
    }
  }
```

- [ ] **Step 3: Open in browser and verify animations**

Reload. Test:
- **Successful drop** (long-press Room PC, drag to D3, release) → clone slides into D3's slot center over ~180ms, the new Room PC chip in D3 thunks (subtle scale-up-and-back), D3's tile border briefly flashes cyan. Total feel: ~280ms, satisfying and immediate.
- **Cancel** (long-press Room PC, drag to header, release) → clone curves back to its rail position over 220ms, then the rail chip returns to full opacity.
- **No-op** (drag Room PC onto D1 which is already Room PC) → clone snap-backs to the rail (no slot change, since `dropOnTile` is null).

- [ ] **Step 4: Commit**

```bash
git add mockups/11-drag-drop-router.html
git commit -m "feat(mockup-11): drop animation + snap-back curves"
```

---

### Task 7: Add gallery card to mockups/index.html

**Files:**
- Modify: `mockups/index.html`

- [ ] **Step 1: Add a card for mockup 11 inside the existing `.grid`**

Open `mockups/index.html`. Find the closing `</div>` of `<div class="grid">` (right before `</body>`). Insert this card before it, just after the mockup 10 card:

```html
  <a class="card" href="11-drag-drop-router.html" target="_blank">
    <div class="card-preview">
      <iframe src="11-drag-drop-router.html" scrolling="no" loading="lazy"></iframe>
      <div class="card-preview-overlay"></div>
    </div>
    <div class="card-body">
      <p class="card-num">Mockup 11</p>
      <p class="card-name">Drag &amp; Drop Router</p>
      <p class="card-desc">Left-rail source palette (4 chips). Drag onto a display tile to route, or tap-to-arm and tap a tile. Landed chips show what's playing where. Long-press 250ms to start a drag; released outside snaps back.</p>
      <div class="card-tags"><span class="tag">Routing</span><span class="tag">Drag &amp; Drop</span><span class="tag-green tag">Experimental</span></div>
      <a class="open-btn" href="11-drag-drop-router.html" target="_blank">Open Full Size →</a>
    </div>
  </a>
```

- [ ] **Step 2: Open `mockups/index.html` in a browser and verify the gallery**

Expected:
- Card 11 appears at the end of the grid.
- Iframe preview shows mockup 11 scaled to 0.25× (rail + 3 tiles + footer all visible).
- Click "Open Full Size →" → opens `11-drag-drop-router.html` in a new tab.
- Hovering the card lifts it (existing gallery behavior).

- [ ] **Step 3: Commit**

```bash
git add mockups/index.html
git commit -m "feat(mockup-11): add gallery card linking to drag-drop router"
```

---

### Task 8: Final review — run through all the spec's success criteria

**Files:** none (validation only)

This is the gate that decides whether Stage 1 promotes to Stage 2. From the spec's "Success criteria" section:

- [ ] **Step 1: Verify long-press feel**

Open `mockups/11-drag-drop-router.html` in a browser. Try:
- A quick click on a chip should NOT start a drag — only arm.
- Press and hold ~250ms — should "lift" cleanly without feeling sluggish.

Adjust `LONG_PRESS_MS` ±50ms if needed; commit any tuning.

- [ ] **Step 2: Verify drop reads as "signal landed"**

Drag a chip onto a tile. The thunk + flash should make it feel like a thing landed — not like a button changed state. If it doesn't feel right, iterate on animation timing in Task 6's keyframes (the budget is 280ms total but individual phases can shift).

- [ ] **Step 3: Verify glance test**

Stand back from the screen (or zoom out to ~75%). With four rail chips and three tiles each holding a landed chip, can you tell at a glance what's playing where? Compare against an open tab of `mockups/02-full-bleed-tiles.html` (current style). Note your subjective answer in the commit message of any follow-up tuning.

- [ ] **Step 4: Verify tap-to-route fallback speed**

Time yourself routing all three displays via tap-to-arm + tap-tile. Compare against the current 12-button-grid Home (open the running Svelte app or mockup #10). The drag flow should not be slower for tap-only users.

- [ ] **Step 5: Decide promote-or-kill**

Write a short note (3–5 sentences) at the bottom of the spec under a new heading `## Stage 1 Outcome`. Capture: did the four success criteria pass? Any ergonomic surprises? Recommend "promote to Stage 2 (Svelte branch)", "iterate further on Stage 1", or "kill the concept".

```bash
git add MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-source-routing-design.md
git commit -m "docs(spec): capture Stage 1 outcome for drag-drop router mockup"
```

---

## Self-Review Checklist (run before handing off to executor)

- ✅ Every task has explicit file paths (relative to repo root: `mockups/11-drag-drop-router.html`, `mockups/index.html`).
- ✅ Every code-change step includes the actual code (no "implement appropriately" stubs).
- ✅ Browser verification steps include explicit expected outcomes (what to look for visually).
- ✅ Spec coverage:
  - Layout (header, rail, 3 tiles, footer) → Task 1, 2
  - Source rail with 4 chips → Task 2
  - Display tiles with landed chips / empty placeholder → Task 2
  - Tap-to-arm flow → Task 3
  - Tap-to-route flow → Task 3
  - 4-second arm timeout → Task 3
  - Disarm by re-tapping chip → Task 3
  - Disarm by tapping outside → Task 4
  - Long-press (250ms) → drag → Task 5
  - Drop on tile while dragging → Task 5
  - Drop on already-routed (no-op + DROP-NOOP styling) → Task 5
  - DROP-VALID dashed outline on all tiles → Task 3
  - DROP-HOVERING solid outline on hovered tile → Task 5
  - Drop animation (3 phases, ~280ms) → Task 6
  - Snap-back curve (~220ms) → Task 6
  - Multi-touch ignored (only first finger; pointer-event default) → Task 5
  - Gallery card → Task 7
  - Stage 1 outcome captured → Task 8
- ✅ Type/name consistency: `appState`, `routeSource`, `armChip`, `disarm`, `startDrag`, `endDrag`, `dragClone`, `LONG_PRESS_MS` all used identically across Tasks 3–6.
- ✅ Commit cadence: one commit per task (8 commits total on `feat/drag-drop-router-mockup`).
