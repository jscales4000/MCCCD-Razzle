# MCCCD-AA140 Page-Suite Mockups — Shared Design System

These 15 mockups (5 × Advanced Routing, 5 × Cameras, 5 × Audio Mixer) MUST use the same visual language so the suite reads as one product. Pick variations along the variant axis (below), not the design system.

## Locked tokens — DO NOT vary across mockups

```css
:root {
  /* Surface */
  --bg-page:        #050a14;
  --bg-panel:       #0d1b2e;
  --bg-panel-soft:  rgba(15, 23, 42, 0.55);
  --border:         rgba(148, 163, 184, 0.18);
  --border-strong:  rgba(148, 163, 184, 0.32);

  /* Brand (MCCCD orange + lemon) */
  --accent:         #f5a623;
  --accent-soft:    rgba(245, 166, 35, 0.28);
  --accent-glow:    rgba(245, 166, 35, 0.45);
  --active:         #fde047;   /* lemon yellow — used for ACTIVE source text per recent panel update */

  /* Copy */
  --copy:           #e2e8f0;
  --copy-soft:      #cbd5e1;
  --copy-muted:     #94a3b8;

  /* Semantic */
  --success:        #22c55e;
  --warn:           #fb923c;
  --danger:         #ef4444;

  /* Geometry */
  --radius-card:    14px;
  --radius-button:  10px;
  --radius-pill:    999px;

  /* Typography (use system stack) */
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
}

body {
  background: var(--bg-page);
  color: var(--copy);
}
```

## Hard rules

1. **Touch target minimum: 64×64px** on any tappable button. Sliders: thumb >= 28px, track tap area >= 44px tall. The TS-1070 is a capacitive panel and fat fingers hit it — anything under 64 fails.
2. **Cards use the standard glass-card pattern**: `background: linear-gradient(180deg, rgba(13,27,46,0.85), rgba(8,14,26,0.85)); border: 0.5px solid var(--border); border-radius: 14px;`
3. **Active state always = lemon yellow** (`--active`). Don't invent new active colors per page.
4. **Header height = 72px** across all three pages. Consistent home button at left, room name + page eyebrow, status pill + page-specific control at right.
5. **One typeface family, three weights** (500, 700, 800). Don't introduce new fonts or weight stacks.
6. **No emoji, no faux 3D, no glassmorphic noise textures.** Slick = restrained.
7. **Outer page padding: 12px**. Inter-card gap: 10px. Inner card padding: 16-20px depending on density.

## Variant axis — what each mockup explores

For each page, the 5 mockups should each pick **one** of these directions. Same direction shouldn't repeat within a page.

- **V1 — Spacious / High-Touch.** Largest possible touch targets (72-96px). Less content per screen. Generous gaps. Good for accessibility / gloved use.
- **V2 — Dense / At-a-Glance.** Tight grid, more metrics visible at once. Touch targets at the 64px floor. Better for power users.
- **V3 — Asymmetric Hierarchy.** One element (the primary action / focal area) gets ~60% of the canvas; everything else compresses to support it.
- **V4 — Symmetrical / Card-Grid.** Three equal columns or four equal quadrants. Everything reads as same-weight; user navigates by reading labels.
- **V5 — Sidebar-Driven.** Narrow vertical sidebar of contextual controls; main canvas is the work surface. Sidebar can be left or right.

## Page-specific structural requirements

### Advanced Routing (advanced-routing/)

Must include:
- Header: Home back, "AA140", "Display Routing · Live Map" eyebrow, Manual/Mirror/Extend mode tabs, Auto-Route on/off chip.
- Reflected-ceiling-plan canvas with 4 display markers (D1 Front Left projector, D2 Front Right projector, D3 Rear Newline, D4 Podium confidence). Use rectangle markers. Front of room = bottom of plan. D4 near room center, shifted forward.
- Active marker text in lemon yellow per panel update.
- 4-display status sidebar (or inline equivalent in some variants): D1/D2/D3/D4 with current source label + power dot + tap target.
- Audio-follows hint: "Audio Source · D1 · {sourceLabel}" + "Audio always follows D1's routed source."
- Footer: 5 mic chips (Lav, Handheld, Ceiling 1-3) + Power button + Volume -/+. Mic waveforms when live. This footer is reused from the AppFooter standard so it should be identical visually across all 5 routing mockups.

### Cameras (cameras/)

Must include:
- Header: Home back, "AA140 — Cameras", panel-online status pill.
- Camera selector: 2 cameras (IV-CAM-I12-B · 12× zoom, IV-CAM-I20-B · 20× zoom). Show as 2 large buttons.
- Live preview: 16:9 black/dark area labeled "Preview — {camera label}". Place a transparent PTZ overlay (4 arrow buttons: up, down, left, right) over the preview corners/edges.
- PTZ pad buttons: 60px circular, transparent w/ white SVG arrows, scale-down on press. (These already exist — variants can rearrange but must keep PTZ accessible over preview.)
- Speed controls: Pan Speed slider + Tilt Speed slider, each with numeric readout.
- Zoom +/- buttons (large).
- "Send to VTC" primary action.
- Tracking mode: People / Group / VX AutoSwitch (3 toggles).
- Shot Presets row: 3 preset buttons (Default, Primary, Secondary). Tap-to-recall, hold-to-save. Show the hint "Tap to recall · Hold 3 seconds to save".

### Audio Mixer (audio-mixer/)

Must include:
- Header: Home back, "AA140", "Audio Mixer" eyebrow, Master chip (− / dB readout / +), online status pill, Mute All button.
- 4 mic channel strips: Lavalier (Wireless · Ch 1, CCS-UWB Beltpack), Handheld (Wireless · Ch 2, CCS-UWB Handheld), Array A (Ceiling · Ch 3, MXA920W-S), Array B (Ceiling · Ch 4, MXA920W-S). Each strip should show: connection indicator, real-time level meter (VU style), line-out fader, trim slider, mute button.
- Master strip on the right: program audio level fader + audio output select (1=Speakers, 2=Headphones or similar).
- Footer: "Presets" label + 4 preset buttons (Lecture, Presentation, Hybrid, Recording) + "Link Arrays A+B" toggle chip.

## Output format

Each mockup is a single self-contained `.html` file at `docs/mockups/page-suite-2026-05-29/<page>/V<n>-<descriptor>.html`. No external assets. Inline `<style>` block. No JavaScript needed (mockups are static). Use SVG inline for icons.

Page dimensions: design for **1280×800** viewport (TSW-1070). Use `<body>` 100% width/height with the page wrapper at that size, centered.

Filenames:
- `V1-spacious.html`
- `V2-dense.html`
- `V3-asymmetric.html`
- `V4-card-grid.html`
- `V5-sidebar.html`
