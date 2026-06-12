---
name: gui-room-map
description: Use when building or modifying a touchable room-plan / room-control map (RCP) for a touchpanel GUI — interactive floor plans with display markers, cameras, mics, seating. Encodes the two-layer architecture, geometry rules, and collision checklist learned on MCCCD-AA140.
---

# Building Touchable GUI Room Plans / Maps

A room plan on a touchpanel is TWO layers with strictly separated jobs:

1. **Touch layer** — real HTML `<button>` elements, absolutely positioned,
   ≥44px (44×44 minimum; prefer a fixed 44px height with % width). These are
   the ONLY interactive elements. They carry `data-*` hooks (e.g.
   `data-display="d1"`) that popovers/sidebars anchor to — treat that DOM
   shape as a frozen contract once anything external looks it up.
2. **Scene layer** — pure decoration. One wrapper with `aria-hidden="true"`
   and `pointer-events: none` on the WHOLE layer (never per-element). Nothing
   in it may intercept a tap or appear in the accessibility tree.

## Geometry rules

- Lay out in **% of the plan box**, not px, so one layout scales across panel
  sizes via a `--panel-scale` transform. Fixed px only for things that must
  stay legible at any scale (marker heights, icon bodies) — and then verify
  they clear neighboring %-positioned elements at the SHORTEST plan height.
- Pick one **orientation** (e.g. front of room at the BOTTOM) and write it in
  a comment at the top. Every element comment references it.
- **Collision checklist** before calling layout done — walk every pair of:
  wall-hugging markers vs wall decorations (speakers, doors), bottom-anchored
  vs top-anchored elements at min height, center-positioned bodies vs flanking
  elements (compute the px→% span), label tags vs the plan border.

## Element idioms that read correctly

- **Walls**: outer 3px border + inner 1px inset border = double-line
  architectural wall. Door = gap punched in the wall + leaf + dashed
  quarter-circle swing arc.
- **Displays/screens**: thin bright bars ON the wall, aligned 1:1 with their
  touch markers.
- **Projector throw cones**: clip-path triangles from projector body to
  screen bar; brighten when that display is live (`routed && powerOn`).
- **PTZ cameras**: body INSIDE the walls + lens stub on the aim side +
  translucent FOV wedge (clip-path triangle) opening into the room. The wedge
  apex sits at the camera. Aim must match physical install.
- **Seating**: rows of small seat glyphs (rounded backrest edge toward rear,
  flat edge toward front) — banks with a center aisle for classrooms; chairs
  around a table only for conference rooms.
- **Ceiling mics**: circles centered over the seating they cover. Live state
  lights the mic's OWN edge (border-color + soft glow) — never an external
  dot or halo; detached indicators read as clutter.

## State and style

- All live state comes from **feedback signals** — never optimistic local
  mirrors. Decoration derives (`$derived`) from the same stores the touch
  layer uses.
- Colors via **theme custom properties** (`var(--color-accent)` etc. or the
  established rgba accent patterns) so the map rethemes with the app.
- Animations ≤300ms, and every transition/animation gets a
  `prefers-reduced-motion: reduce` guard.
- Never color-alone state: pair color changes with a shape/label change.

## Process

- Adversarial-review the result: one pass for touch-target/contrast/UX-rule
  violations, one pass for geometry collisions at min and max plan sizes.
- On CH5/Crestron panels: hover does not exist — `:active` press states are
  mandatory; test on glass, not just the dev browser.
