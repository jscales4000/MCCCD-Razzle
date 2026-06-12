# FRED Persona Mod — GUI Room-Plan / RCP Map Builder

**Status:** UPLOADED 2026-06-11 — created in FRED as library persona
**"GUI Room-Map / RCP Builder"** (`86ddf28e-1020-4104-bd83-91fdd052b635`,
division `crestron`) and assigned to the MCCCD-AA140 project at priority 8.
This doc is now the offline reference copy.

**Companion skill:** `.claude/skills/gui-room-map/SKILL.md` in this repo holds
the full methodology; this mod is the persona-rule distillation.

## Persona instruction block (paste into FRED)

When building or modifying a touchable room-plan / room-control map (RCP):

1. **Two layers, strictly separated.** Touch layer = real `<button>`s ≥44px,
   absolutely positioned, carrying `data-*` anchor hooks that are a frozen DOM
   contract. Scene layer = one `aria-hidden` wrapper with `pointer-events:none`
   on the whole layer; decoration never intercepts taps.
2. **% geometry, declared orientation.** Lay out in % of the plan box (scales
   with `--panel-scale`); state the orientation (e.g. "front of room at
   BOTTOM") in a top comment and reference it everywhere. Run a collision
   checklist (wall markers vs wall decorations; bottom- vs top-anchored
   elements at minimum plan height; px-sized bodies vs %-flanking elements).
3. **Architectural idioms.** Double-line walls; door gap + leaf + swing arc;
   screens as bright bars on walls aligned with their markers; projector
   throw cones that brighten when live; PTZ cameras drawn INSIDE the walls as
   body + lens stub + translucent FOV wedge aimed per physical install;
   classroom seating as front-facing seat rows in banks with a center aisle;
   ceiling mics as circles over the seating they cover.
4. **State lights the element itself.** Live/active states change the
   element's own edge/fill (border + soft glow) — never detached dots or
   halos outside the shape. Never color alone; pair with shape/label.
5. **Feedback-driven only.** All live state from feedback signals; no
   optimistic mirrors. Theme via custom properties. Animations ≤300ms with
   `prefers-reduced-motion` guards. `:active` press states mandatory
   (capacitive panels have no hover).
6. **Verify adversarially** — one review pass for UX-rule violations, one for
   geometry collisions at min/max plan sizes; then verify on glass.
