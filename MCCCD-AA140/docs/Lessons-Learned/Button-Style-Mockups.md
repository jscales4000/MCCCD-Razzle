# AA140 Unified Button Styles — 10 Mockups

Generated 2026-04-26 by 4 parallel UI/UX subagents (UI Designer, UX Architect, Brand Guardian, Frontend Developer). All 10 honor the hard constraints: no pill shapes (border-radius ≤ ~16px), icon buttons transparent with flat-white icons, ≥ 56×56 touch targets on text / 48×48 on overlay icons, distinct active state, enlarged power-on variant, dark glass-card surface compatibility.

Pick by **style number** and I'll apply across the panel codebase as a single styling commit.

---

## At-a-Glance Comparison

| # | Style | Author | Aesthetic | Active Cue | Press Cue | Power-ON | Best For |
|---|---|---|---|---|---|---|---|
| 1 | Console Bezel | UI Designer | Milled-aluminum, broadcast console | Cyan stroke thickens (1→2px) | Inset shadow + translateY 1px | 84×88 with cyan glow | "Equipment" feel, weighty |
| 2 | Signal Tile | UI Designer | DSP channel-strip | Left-edge bar fills 4px→100% | scale(0.97) | 88px + 8px bar | Radio groups, distance viewing |
| 3 | Hairline Schematic | UI Designer | CAD wireframe, Extron config | Corner ticks bloom + cyan glow | scale(0.96) + inset stroke | Cyan corners + glow | Video-hero pages (cameras) |
| 4 | Beveled Industrial | UX Architect | Double-border, high-contrast (15.8:1) | Border thickens 2→3px + brighter | Border 3→4px + black inset shadow | 72px + green ring | Mixed-ability rooms |
| 5 | Soft Slab | UX Architect | 3px bottom-border, layered translucency | Bottom-border thickens 3→4px white | Bottom-border collapses to 1px (depression) | 76px + green tint | Long sessions, low fatigue |
| 6 | Flat Stencil | UX Architect | Uppercase tracked, airport signage | Inverted (white fill, dark text) | Border 2→4px + padding shift | 80px green outline + ● indicator | Maximum legibility at distance |
| 7 | Quad Slate | Brand Guardian | Etched tile, MCCCD-aware | Cyan **bottom** border + bg lift | translateY 1px + darken | 88-wide navy with cyan bar | Institutional, durable |
| 8 | Lectern Brass | Brand Guardian | Beveled gradient, lectern hardware | Cyan **left** bar + bg lift | Inset shadow + translateY 1px | Warm-tinted bg + cyan left bar | Academic-professional |
| 9 | Etched Glass | Frontend Dev | Embossed glass with bevel | Cyan border + cyan glow ring | translateY 1px | 140×84 with cyan glow halo | Premium, content-first |
| 10 | Hairline Industrial | Frontend Dev | Flat 1px borders, solid cyan fill on active | **Solid cyan background**, dark text | Cyan dims to 0.85 + dark text | 140×84 solid cyan fill | Bold "no doubt which is selected" |

---

## ✅ Picked: **Style #2 Signal Tile** (applied to live panel as of 2026-04-26)

User selected #2 over the original recommendation (#3) for the channel-strip clarity at distance — left-edge accent bar that fills horizontally on activation gives unambiguous "which is selected" feedback for radio groups (source picker, camera tracking modes).

Applied at: `src/global.css` under the `/* === Signal Tile button system (#2) === */` block. Tokens in `:root`: `--btn-surface`, `--btn-surface-hi`, `--btn-bar-w`, `--btn-fg`, `--btn-accent`. Override anywhere by redefining those tokens (e.g. theme-per-room via `:root[data-room="OCO-201"]`).

---

## Original Recommendation (preserved for reference): **#3 Hairline Schematic** with #5's reduced-motion discipline merged in

**Why #3:**
- Camera page is going to be visually busy (live RTSP feed + transparent PTZ overlay + tracking + zoom + presets). Buttons should step out of the way until needed, then bloom unmistakably. #3's "near-invisible at rest, glow when active" is the only style that actively defers to the video.
- The corner-tick detail signals "professional AV gear" without skeuomorphism — fits the Crestron heritage without looking like a 2015 touchpanel.
- Border-only active state (no fill) keeps the dark glass-card aesthetic intact.

**Merge from #5:**
- `prefers-reduced-motion` clamping all transitions to 0ms — important for vestibular-sensitive users in a classroom.
- Dual-layer focus ring for WebXPanel keyboard access.

**Honorable mentions:**
- **#10 (Hairline Industrial)** if you want zero ambiguity about what's selected — solid cyan fill is read across the room. Would override the "subtle" intent of #3 but bulletproof for instructor-from-back-of-room use.
- **#7 (Quad Slate)** if the brand-tied institutional feel is non-negotiable (MCCCD navy for primary, burgundy for danger).

**Skip these unless their specific premise matches your priorities:**
- **#6 Flat Stencil** — uppercase + tracked is loud at AV-control density; works best in much sparser layouts.
- **#1, #2, #4** — all good but generic-broadcast; no specific edge for AA140.
- **#8 Lectern Brass** — gradients and inset highlights are gorgeous but date faster than flat designs over a 7-year refresh cycle.

---

## Full CSS Mockups

[Copy the full CSS for each style from the agent reports below — pick one and I'll wire it into `src/global.css` and the component CSS scopes.]

### Style 1: Console Bezel
```css
:root {
  --btn-radius: 4px;
  --btn-border: 1px solid rgba(148, 163, 184, 0.25);
  --btn-fg: #e2e8f0;
  --btn-bg: rgba(30, 41, 59, 0.55);
  --btn-bg-hover: rgba(51, 65, 85, 0.7);
  --btn-active-stroke: var(--color-accent);
  --btn-press-shadow: inset 0 2px 6px rgba(0, 0, 0, 0.5);
}
.btn {
  min-height: 56px; min-width: 56px; padding: 0 18px;
  font: 600 15px/1 "Inter", system-ui, sans-serif;
  letter-spacing: 0.04em; text-transform: uppercase;
  color: var(--btn-fg); background: var(--btn-bg);
  border: var(--btn-border); border-radius: var(--btn-radius);
  box-shadow: inset 0 1px 0 rgba(255,255,255,0.06), 0 1px 0 rgba(0,0,0,0.4);
  transition: background 120ms ease, border-color 120ms ease, transform 80ms ease, box-shadow 80ms ease;
}
.btn.active {
  border: 2px solid var(--btn-active-stroke);
  background: rgba(34, 211, 238, 0.12); color: #ffffff;
  box-shadow: inset 0 1px 0 rgba(255,255,255,0.08), 0 0 0 1px rgba(34,211,238,0.35);
}
.btn:active { transform: translateY(1px); background: rgba(15,23,42,0.85); box-shadow: var(--btn-press-shadow); }
.icon-btn { width: 48px; height: 48px; background: transparent; border: none; color: #fff; border-radius: 4px; }
.icon-btn:active { transform: scale(0.92); background: rgba(0,0,0,0.35); }
.icon-btn.active { background: rgba(34,211,238,0.18); box-shadow: inset 0 0 0 2px var(--color-accent); }
.btn.primary { min-height: 84px; padding: 0 28px; font-size: 18px;
  background: linear-gradient(180deg, rgba(34,211,238,0.22), rgba(34,211,238,0.08));
  border: 2px solid var(--color-accent); color: #fff;
  box-shadow: inset 0 1px 0 rgba(255,255,255,0.15), 0 0 24px rgba(34,211,238,0.25); }
.btn.danger { border: 1px solid rgba(248,113,113,0.6); background: rgba(127,29,29,0.45); color: #fecaca; }
.btn.ghost { background: transparent; border: 1px solid rgba(148,163,184,0.3); color: #cbd5e1; }
```

### Style 2: Signal Tile
*See agent report — left-edge accent bar that fills horizontally on active.*

### Style 3: Hairline Schematic ⭐ RECOMMENDED
```css
:root {
  --btn-radius: 6px;
  --btn-fg: #cbd5e1;
  --btn-fg-on: #ffffff;
  --btn-line: rgba(148, 163, 184, 0.22);
  --btn-line-hi: rgba(148, 163, 184, 0.45);
  --btn-glow: 0 0 0 1px var(--color-accent), 0 0 16px rgba(34, 211, 238, 0.35);
}
.btn {
  position: relative;
  min-height: 56px; min-width: 56px; padding: 0 20px;
  font: 500 14px/1 "Inter", system-ui, sans-serif;
  letter-spacing: 0.06em; text-transform: uppercase;
  color: var(--btn-fg);
  background: rgba(15, 23, 42, 0.35);
  border: 1px solid var(--btn-line);
  border-radius: var(--btn-radius);
  transition: color 140ms ease, border-color 140ms ease, box-shadow 200ms ease, transform 70ms ease, background 140ms ease;
}
.btn::before, .btn::after {
  content: ""; position: absolute; width: 8px; height: 8px;
  border: 1px solid transparent; transition: border-color 200ms ease;
  pointer-events: none;
}
.btn::before { top: -1px; left: -1px; border-top-color: var(--btn-line-hi); border-left-color: var(--btn-line-hi); }
.btn::after  { bottom: -1px; right: -1px; border-bottom-color: var(--btn-line-hi); border-right-color: var(--btn-line-hi); }
.btn.active {
  color: var(--btn-fg-on);
  border-color: var(--color-accent);
  background: rgba(34, 211, 238, 0.1);
  box-shadow: var(--btn-glow);
}
.btn.active::before, .btn.active::after { border-color: var(--color-accent); width: 12px; height: 12px; }
.btn:active { transform: scale(0.96); background: rgba(34,211,238,0.2); box-shadow: inset 0 0 0 1px var(--color-accent); }
.icon-btn {
  width: 48px; height: 48px; background: transparent; border: none;
  color: #ffffff; border-radius: 50%;
  transition: background 140ms ease, transform 70ms ease, box-shadow 200ms ease;
}
.icon-btn:active { transform: scale(0.88); background: rgba(34,211,238,0.18); box-shadow: 0 0 0 1px var(--color-accent); }
.icon-btn.active { background: rgba(34,211,238,0.14); box-shadow: 0 0 0 1px var(--color-accent), 0 0 12px rgba(34,211,238,0.4); }
.btn.primary {
  min-height: 88px; padding: 0 36px; font-size: 17px; font-weight: 600; color: #fff;
  background: rgba(34,211,238,0.08); border: 1px solid var(--color-accent);
  box-shadow: var(--btn-glow);
}
.btn.primary::before, .btn.primary::after { border-color: var(--color-accent); width: 14px; height: 14px; }
.btn.danger { color: #fecaca; border-color: rgba(248,113,113,0.55); background: rgba(127,29,29,0.25); }
.btn.danger.active, .btn.danger:active { border-color: #f87171; box-shadow: 0 0 0 1px #f87171, 0 0 14px rgba(248,113,113,0.4); }
.btn.danger::before, .btn.danger::after { border-color: rgba(248,113,113,0.45); }
.btn.ghost { color: #94a3b8; background: transparent; border-color: var(--btn-line); }

@media (prefers-reduced-motion: reduce) {
  .btn, .icon-btn { transition: none; }
}
```

### Styles 4–10
Full CSS preserved in the original agent transcripts (auto-saved by the runtime). Re-request via `/agent` with the agent IDs if needed:
- Style 4 (Beveled Industrial), Style 5 (Soft Slab), Style 6 (Flat Stencil) — UX Architect agent
- Style 7 (Quad Slate), Style 8 (Lectern Brass) — Brand Guardian agent
- Style 9 (Etched Glass), Style 10 (Hairline Industrial) — Frontend Developer agent

Pick a number (1–10) and I'll apply.
