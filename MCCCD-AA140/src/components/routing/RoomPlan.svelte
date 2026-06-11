<!--
  RoomPlan — architectural top-down plan for AA140.

  Renders a realistic static scene (double-line walls, door swing, conference
  table + chairs, podium, projector throw cones, speakers, cameras, mic
  arrays) plus four interactive DisplayMarker instances driven by the
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

  // Chair seats around the conference table, % of plan box. Six per long
  // side. Head seats are omitted — the table's short ends face the podium
  // and rear wall walkways.
  const CHAIRS_TOP = [36, 44, 52, 60].map(left => ({ left, top: 21 }));
  const CHAIRS_BOTTOM = [36, 44, 52, 60].map(left => ({ left, top: 51 }));
  const CHAIRS_LEFT = [{ left: 29.5, top: 30 }, { left: 29.5, top: 42 }];
  const CHAIRS_RIGHT = [{ left: 66.5, top: 30 }, { left: 66.5, top: 42 }];
  const CHAIRS = [...CHAIRS_TOP, ...CHAIRS_BOTTOM, ...CHAIRS_LEFT, ...CHAIRS_RIGHT];

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

      <!-- Conference table + chairs -->
      <div class="table">
        <span class="table-label">Conference</span>
      </div>
      {#each CHAIRS as c}
        <div class="chair" style={`left: ${c.left}%; top: ${c.top}%;`}></div>
      {/each}

      <!-- Wall speakers — rear-left aligns with D3, front pair flanks Cam1 -->
      <div class="speaker rl"></div>
      <div class="speaker fl"></div>
      <div class="speaker fr"></div>

      <!-- Cameras: Cam2 at rear (top-center), Cam1 at front (bottom-center) -->
      <div class="cam cam-2">2</div>
      <div class="cam cam-1">1</div>

      <!-- Ceiling mic arrays -->
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

  /* ── Conference table + chairs ──────────────────────────────────────── */
  .table {
    position: absolute;
    top: 25%;
    left: 32%;
    width: 32%;
    height: 22%;
    border-radius: 16px;
    background: linear-gradient(180deg, rgba(71, 85, 105, 0.30), rgba(51, 65, 85, 0.18));
    border: 1.5px solid rgba(148, 163, 184, 0.45);
    box-shadow: inset 0 0 18px rgba(8, 16, 30, 0.5);
    display: grid;
    place-items: center;
  }
  .table-label {
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.26em;
    text-transform: uppercase;
    color: rgba(148, 163, 184, 0.55);
  }
  .chair {
    position: absolute;
    width: 16px;
    height: 16px;
    transform: translate(-50%, -50%);
    border-radius: 5px;
    background: rgba(51, 65, 85, 0.55);
    border: 1px solid rgba(148, 163, 184, 0.35);
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

  /* ── Cameras ────────────────────────────────────────────────────────── */
  .cam {
    position: absolute;
    width: 38px;
    height: 30px;
    background: rgba(120, 200, 130, 0.20);
    border: 1.5px solid rgba(120, 200, 130, 0.7);
    border-radius: 6px;
    display: grid;
    place-items: center;
    color: rgba(190, 240, 200, 0.95);
    font-size: 10px;
    font-weight: 800;
  }
  .cam.cam-1 { bottom: -10px; left: 50%; transform: translateX(-50%); }
  .cam.cam-2 { top: 5%; left: 50%; transform: translateX(-50%); }
  .cam::before {
    content: 'Cam';
    font-size: 9px;
    font-weight: 800;
    margin-right: 2px;
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
  .mic::before {
    content: '';
    position: absolute;
    inset: -8px;
    border: 1px dashed rgba(245, 166, 35, 0.18);
    border-radius: 50%;
  }
  .mic.live::after {
    content: '';
    position: absolute;
    top: -8px;
    right: -8px;
    width: 10px;
    height: 10px;
    border-radius: 50%;
    background: var(--color-success);
    box-shadow: 0 0 8px var(--color-success);
  }
  .mic.mxa-a { top: 30%; left: 19%; }
  .mic.mxa-b { top: 30%; right: 19%; }

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
