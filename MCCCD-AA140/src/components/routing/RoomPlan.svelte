<!--
  RoomPlan — architectural top-down plan for AA140.

  Renders a realistic static scene (double-line walls, door swing, classroom
  seating rows, podium, projector throw cones, speakers, aimed PTZ cameras,
  mic arrays) plus four interactive DisplayMarker instances driven by the
  parent's reactive state. The scene layer is pure decoration: every scene
  element is aria-hidden and pointer-events:none, so the DisplayMarker
  buttons remain the only touch layer (≥44px targets, single tap).

  Layout is built in % units relative to the plan container so the same
  layout file scales from the panel's 1280×800 down to dev-browser sizes
  via the existing --panel-scale system. Orientation: front of room at the
  BOTTOM of the diagram (per the 2026-05-29 reference image), rear at the
  TOP. Marker positions are unchanged from v2 — the popover anchoring and
  `.marker[data-display]` sidebar lookups depend on this DOM shape.
-->

<script lang="ts">
  import type { DisplayId, SourceId } from '../../lib/stores/router';
  import DisplayMarker from './DisplayMarker.svelte';

  interface DisplayInfo {
    id: DisplayId;
    label: string;
    spec: string;
    activeSource: SourceId | null;
    powerOn: boolean;
  }

  interface Props {
    displays: DisplayInfo[];
    openDisplay: DisplayId | null;
    onMarkerTap: (displayId: DisplayId, el: HTMLElement) => void;
  }

  let { displays, openDisplay, onMarkerTap }: Props = $props();

  // Position metadata per display, keyed by id. Unchanged from v2 — front of
  // room at the BOTTOM (D1/D2 projection surfaces on the bottom wall), rear
  // at the TOP (D3 Newline at top-left), D4 podium confidence monitor
  // centered just above the podium frame.
  const MARKER_POS: Record<DisplayId, string> = {
    d1: 'bottom: 2%; left: 14%; width: 22%; height: 44px;',
    d2: 'bottom: 2%; left: 64%; width: 22%; height: 44px;',
    d3: 'top: 2%; left: 8%; width: 22%; height: 44px;',
    d4: 'bottom: 31%; left: 39%; width: 22%; height: 44px;',
  };

  // Projector throw cones light up when their display is live (routed + on).
  let d1Live = $derived(displays.some(d => d.id === 'd1' && d.activeSource && d.powerOn));
  let d2Live = $derived(displays.some(d => d.id === 'd2' && d.activeSource && d.powerOn));

  // Classroom seating — two banks (left/right of a center aisle), three
  // rows per bank, all seats facing the front (bottom) of the room.
  const SEAT_ROWS = [26, 38, 50]; // % from top, one per row
  const SEAT_COLS_LEFT = [15, 23, 31, 39];
  const SEAT_COLS_RIGHT = [61, 69, 77, 85];
  const SEATS = SEAT_ROWS.flatMap(top =>
    [...SEAT_COLS_LEFT, ...SEAT_COLS_RIGHT].map(left => ({ left, top })));

  const micLive = true; // visual hint — could be driven by mic-mute stores later
</script>

<div class="plan-wrap">
  <span class="plan-eyebrow">Room map · Tap any display to route</span>
  <span class="plan-hint">
    <span class="pulse-dot" aria-hidden="true"></span>
    {displays.filter(d => d.activeSource).length} routed · {displays.filter(d => d.powerOn).length} on
  </span>

  <div class="plan">
    <!-- ── Static scene (decoration only) ─────────────────────────────── -->
    <div class="scene" aria-hidden="true">
      <span class="wall-tag rear">Rear Wall</span>
      <span class="wall-tag front">Front Wall</span>

      <!-- Inner wall line (double-line wall read) -->
      <div class="wall-inner"></div>

      <!-- Door: opening in the rear wall, right side, with swing arc -->
      <div class="door-gap"></div>
      <div class="door-leaf"></div>
      <div class="door-arc"></div>

      <!-- Projection screens on the front wall + throw cones from ceiling
           projectors. Cones brighten when their display is live. -->
      <div class="screen s1"></div>
      <div class="screen s2"></div>
      <div class="throw-cone c1" class:lit={d1Live}></div>
      <div class="throw-cone c2" class:lit={d2Live}></div>
      <div class="projector p1" class:lit={d1Live}>VPL</div>
      <div class="projector p2" class:lit={d2Live}>VPL</div>

      <!-- Classroom seating — 3 rows left + 3 rows right, center aisle -->
      {#each SEATS as s}
        <div class="seat" style={`left: ${s.left}%; top: ${s.top}%;`}></div>
      {/each}

      <!-- Wall speakers — rear-left aligns with D3, front pair flanks Cam1 -->
      <div class="speaker rl"></div>
      <div class="speaker fl"></div>
      <div class="speaker fr"></div>

      <!-- PTZ cameras with field-of-view wedges showing their aim:
           Cam1 (Front wall, I20) looks back across the seating; Cam2
           (Rear wall, I12) looks down toward the podium. -->
      <div class="cam-fov fov-1"></div>
      <div class="cam-fov fov-2"></div>
      <div class="cam cam-1"><span class="cam-lens"></span>CAM 1</div>
      <div class="cam cam-2"><span class="cam-lens"></span>CAM 2</div>

      <!-- Ceiling mic arrays, centered over each seating bank -->
      <div class="mic mxa-a" class:live={micLive}>MXA</div>
      <div class="mic mxa-b" class:live={micLive}>MXA</div>

      <!-- Podium (D4 confidence monitor sits just above it) -->
      <div class="podium">Podium</div>
    </div>

    <!-- ── Touch layer ────────────────────────────────────────────────── -->
    {#each displays as d (d.id)}
      <DisplayMarker
        displayId={d.id}
        label={d.label}
        spec={d.spec}
        activeSource={d.activeSource}
        powerOn={d.powerOn}
        selected={openDisplay === d.id}
        position={MARKER_POS[d.id]}
        onTap={onMarkerTap}
      />
    {/each}
  </div>
</div>

<style>
  .plan-wrap {
    width: 100%;
    height: 100%;
    background: var(--color-panel);
    border: 0.5px solid var(--color-border);
    border-radius: 14px;
    padding: 20px;
    position: relative;
    overflow: hidden;
  }
  .plan-wrap::before {
    /* Subtle grid texture, kept inside the panel */
    content: '';
    position: absolute;
    inset: 20px;
    background-image:
      linear-gradient(rgba(148, 163, 184, 0.04) 1px, transparent 1px),
      linear-gradient(90deg, rgba(148, 163, 184, 0.04) 1px, transparent 1px);
    background-size: 28px 28px;
    pointer-events: none;
    border-radius: 8px;
  }

  .plan-eyebrow {
    position: absolute;
    top: 18px;
    left: 24px;
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.22em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
    z-index: 2;
  }
  .plan-hint {
    position: absolute;
    top: 18px;
    right: 24px;
    font-size: 11px;
    color: var(--color-copy-soft);
    z-index: 2;
    display: inline-flex;
    align-items: center;
    gap: 7px;
  }
  .pulse-dot {
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: var(--color-accent);
    box-shadow: 0 0 8px var(--color-accent);
    animation: rp-pulse 1.6s ease-in-out infinite;
  }
  @keyframes rp-pulse {
    50% { opacity: 0.3; }
  }

  /* ── Plan box: outer wall line ──────────────────────────────────────── */
  .plan {
    position: absolute;
    top: 60px;
    left: 60px;
    right: 60px;
    bottom: 50px;
    border: 3px solid rgba(196, 122, 30, 0.5);
    border-radius: 6px;
    background:
      radial-gradient(ellipse at 50% 40%, rgba(56, 80, 120, 0.10), transparent 70%),
      linear-gradient(180deg, rgba(8, 14, 26, 0.45), rgba(13, 27, 46, 0.25));
  }

  /* The entire scene layer is non-interactive decoration. */
  .scene {
    position: absolute;
    inset: 0;
    pointer-events: none;
  }

  /* Inner wall line — reads as wall thickness against the 3px outer line */
  .wall-inner {
    position: absolute;
    inset: 5px;
    border: 1px solid rgba(196, 122, 30, 0.25);
    border-radius: 3px;
  }

  .wall-tag {
    position: absolute;
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.32em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }
  .wall-tag.rear {
    top: -24px;
    left: 50%;
    transform: translateX(-50%);
  }
  .wall-tag.front {
    bottom: -24px;
    left: 50%;
    transform: translateX(-50%);
  }

  /* ── Door (rear wall, right side) ───────────────────────────────────── */
  /* Gap punched through the rear wall lines */
  .door-gap {
    position: absolute;
    top: -4px;
    right: 7%;
    width: 9%;
    height: 8px;
    background: var(--color-panel);
  }
  /* Leaf: shown ajar, hinged at the gap's left edge */
  .door-leaf {
    position: absolute;
    top: -2px;
    right: 16%;
    width: 1.5px;
    height: 13%;
    background: rgba(196, 122, 30, 0.6);
    transform-origin: top center;
    transform: rotate(-28deg);
  }
  /* Quarter-circle swing arc from the hinge */
  .door-arc {
    position: absolute;
    top: 0;
    right: 7%;
    width: 9%;
    aspect-ratio: 1;
    border: 1px dashed rgba(196, 122, 30, 0.3);
    border-top: none;
    border-right: none;
    border-bottom-left-radius: 100%;
  }

  /* ── Projection screens + throw cones + projectors ──────────────────── */
  /* Screen surfaces hug the front (bottom) wall, aligned with D1/D2 markers */
  .screen {
    position: absolute;
    bottom: 1px;
    height: 5px;
    background: linear-gradient(90deg, rgba(226, 232, 240, 0.15), rgba(226, 232, 240, 0.45), rgba(226, 232, 240, 0.15));
    border-radius: 2px;
  }
  .screen.s1 { left: 14%; width: 22%; }
  .screen.s2 { left: 64%; width: 22%; }

  /* Ceiling projectors, mid-room, one per screen */
  .projector {
    position: absolute;
    bottom: 23%; /* clears the D4 marker band even at short plan heights */
    width: 34px;
    height: 22px;
    border-radius: 4px;
    background: rgba(30, 41, 59, 0.9);
    border: 1px solid rgba(148, 163, 184, 0.4);
    display: grid;
    place-items: center;
    font-size: 8px;
    font-weight: 800;
    letter-spacing: 0.08em;
    color: var(--color-copy-muted);
    z-index: 1;
  }
  .projector.p1 { left: 25%; transform: translateX(-50%); }
  .projector.p2 { left: 75%; transform: translateX(-50%); }
  .projector.lit { border-color: rgba(245, 166, 35, 0.55); color: var(--color-accent); }

  /* Throw cone: translucent triangle from projector down to its screen */
  .throw-cone {
    position: absolute;
    bottom: 2%;
    height: 24%;
    width: 22%;
    clip-path: polygon(50% 0%, 0% 100%, 100% 100%);
    background: linear-gradient(180deg, rgba(148, 163, 184, 0.10), rgba(148, 163, 184, 0.03));
    transition: background 220ms ease;
  }
  .throw-cone.c1 { left: 14%; }
  .throw-cone.c2 { left: 64%; }
  .throw-cone.lit {
    background: linear-gradient(180deg, rgba(245, 166, 35, 0.22), rgba(245, 166, 35, 0.04));
  }

  /* ── Classroom seating ──────────────────────────────────────────────── */
  /* Front-facing seats: flat front edge toward the screens (bottom), softly
     rounded backrest toward the rear. */
  .seat {
    position: absolute;
    width: 18px;
    height: 15px;
    transform: translate(-50%, -50%);
    border-radius: 7px 7px 3px 3px;
    background: rgba(51, 65, 85, 0.55);
    border: 1px solid rgba(148, 163, 184, 0.35);
    border-top-width: 2px;
  }

  /* ── Speakers ───────────────────────────────────────────────────────── */
  .speaker {
    position: absolute;
    height: 9px;
    background: rgba(56, 189, 248, 0.22);
    border: 1px solid rgba(56, 189, 248, 0.55);
    border-radius: 2px;
  }
  /* Bars hug the walls in the gaps BESIDE the display markers (the markers
     are 44px tall against the walls and would otherwise paint over them). */
  .speaker.rl { top: 2px;    left: 33%; width: 10%; }
  .speaker.fl { bottom: 2px; left: 38%; width: 6%;  }
  .speaker.fr { bottom: 2px; left: 56%; width: 6%;  }

  /* ── PTZ cameras ────────────────────────────────────────────────────── */
  /* Camera body + protruding lens stub on the aim side + translucent FOV
     wedge into the room, so each camera reads as a aimed PTZ unit. Both
     bodies sit fully INSIDE the plan walls. */
  .cam {
    position: absolute;
    width: 54px;
    height: 24px;
    background: rgba(120, 200, 130, 0.18);
    border: 1.5px solid rgba(120, 200, 130, 0.7);
    border-radius: 6px;
    display: grid;
    place-items: center;
    color: rgba(190, 240, 200, 0.95);
    font-size: 8px;
    font-weight: 800;
    letter-spacing: 0.08em;
    z-index: 1;
  }
  .cam.cam-1 { bottom: 2%; left: 50%; transform: translateX(-50%); }
  .cam.cam-2 { top: 2%; left: 50%; transform: translateX(-50%); }
  .cam-lens {
    position: absolute;
    left: 50%;
    transform: translateX(-50%);
    width: 12px;
    height: 6px;
    border-radius: 2px;
    background: rgba(120, 200, 130, 0.8);
  }
  /* Lens points the way the camera looks: Cam1 up into the room, Cam2 down */
  .cam.cam-1 .cam-lens { top: -6px; }
  .cam.cam-2 .cam-lens { bottom: -6px; }

  .cam-fov {
    position: absolute;
    left: 50%;
    transform: translateX(-50%);
    width: 34%;
    height: 20%;
  }
  /* Cam1 wedge: apex at the front-wall camera, opening toward the seats */
  .cam-fov.fov-1 {
    bottom: calc(2% + 26px);
    clip-path: polygon(50% 100%, 0% 0%, 100% 0%);
    background: linear-gradient(0deg, rgba(120, 200, 130, 0.18), rgba(120, 200, 130, 0.02));
  }
  /* Cam2 wedge: apex at the rear-wall camera, opening toward the podium */
  .cam-fov.fov-2 {
    top: calc(2% + 26px);
    clip-path: polygon(50% 0%, 0% 100%, 100% 100%);
    background: linear-gradient(180deg, rgba(120, 200, 130, 0.18), rgba(120, 200, 130, 0.02));
  }

  /* ── Ceiling mics ───────────────────────────────────────────────────── */
  .mic {
    position: absolute;
    width: 50px;
    height: 50px;
    background: rgba(245, 166, 35, 0.12);
    border: 1.2px solid rgba(245, 166, 35, 0.45);
    border-radius: 50%;
    display: grid;
    place-items: center;
    color: rgba(245, 166, 35, 0.95);
    font-size: 8px;
    font-weight: 800;
  }
  /* Live state lights the mic's own edge — no external dot/halo. */
  .mic.live {
    border-color: var(--color-success);
    box-shadow:
      0 0 10px rgba(34, 197, 94, 0.40),
      inset 0 0 8px rgba(34, 197, 94, 0.22);
  }
  /* Centered over each seating bank (ceiling layer, drawn above the seats) */
  .mic.mxa-a { top: 37%; left: 27%; transform: translate(-50%, -50%); }
  .mic.mxa-b { top: 37%; left: 73%; transform: translate(-50%, -50%); }

  /* ── Podium ─────────────────────────────────────────────────────────── */
  @media (prefers-reduced-motion: reduce) {
    .pulse-dot { animation: none; }
    .throw-cone { transition: none; }
  }

  .podium {
    position: absolute;
    bottom: 16%;
    left: 41%;
    width: 18%;
    height: 14%;
    border: 1.5px solid rgba(148, 163, 184, 0.55);
    border-radius: 14px;
    background: rgba(30, 41, 59, 0.35);
    display: grid;
    place-items: center;
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.2em;
    text-transform: uppercase;
    color: rgba(245, 166, 35, 0.7);
  }
</style>
