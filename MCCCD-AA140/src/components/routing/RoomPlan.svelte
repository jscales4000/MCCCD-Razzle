<!--
  RoomPlan — reflected-ceiling-plan composite for AA140.

  Renders the static scene (room outline, walls, speakers, cameras,
  projectors, mic arrays, conference table) plus three interactive
  DisplayMarker instances driven by the parent's reactive state.

  Layout is built in % units relative to the plan container so the same
  layout file scales from the panel's 1280×800 down to dev-browser sizes
  via the existing --panel-scale system.

  The parent owns popover open-state and supplies an `onMarkerTap` handler.
  We forward the marker's bounding rect so the popover can anchor.
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

  // Position metadata per display, keyed by id. Layout matches the user's
  // reference image (2026-05-29): front of room at the BOTTOM of the diagram
  // (D1/D2 projection surfaces on the bottom wall), rear at the TOP (D3
  // Newline at top-left), D4 podium confidence monitor centered just above
  // the podium frame.
  const MARKER_POS: Record<DisplayId, string> = {
    d1: 'bottom: 2%; left: 14%; width: 22%; height: 44px;',
    d2: 'bottom: 2%; left: 64%; width: 22%; height: 44px;',
    d3: 'top: 2%; left: 8%; width: 22%; height: 44px;',
    d4: 'top: 44%; left: 39%; width: 22%; height: 38px;',
  };

  let micLive = $state(true); // visual hint — could be driven by mic-mute stores later
</script>

<div class="plan-wrap">
  <span class="plan-eyebrow">Live ceiling view · Tap any display to route</span>
  <span class="plan-hint">
    <span class="pulse-dot" aria-hidden="true"></span>
    {displays.filter(d => d.activeSource).length} routed · {displays.filter(d => d.powerOn).length} on
  </span>

  <div class="plan">
    <span class="wall-tag rear">Rear Wall</span>
    <span class="wall-tag front">Front Wall</span>

    <!-- Wall speakers (rear at top, front at bottom) -->
    <div class="speaker rl" aria-hidden="true"></div>
    <div class="speaker rr" aria-hidden="true"></div>
    <div class="speaker fl" aria-hidden="true"></div>
    <div class="speaker fr" aria-hidden="true"></div>

    <!-- Cameras: Cam2 at rear (top-center), Cam1 at front (bottom-center) -->
    <div class="cam cam-2" aria-hidden="true">2</div>
    <div class="cam cam-1" aria-hidden="true">1</div>

    <!-- Projectors (ceiling-mounted near the front-of-room projection surfaces) -->
    <div class="proj p1" aria-hidden="true"><span class="proj-inner">▽</span></div>
    <div class="proj p2" aria-hidden="true"><span class="proj-inner">▽</span></div>

    <!-- Ceiling mic arrays -->
    <div class="mic mxa-a" class:live={micLive} aria-hidden="true">MXA</div>
    <div class="mic mxa-b" class:live={micLive} aria-hidden="true">MXA</div>

    <!-- Podium frame (where D4 confidence monitor lives) -->
    <div class="podium" aria-hidden="true">Podium</div>

    <!-- Displays -->
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

  .plan {
    position: absolute;
    top: 60px;
    left: 60px;
    right: 60px;
    bottom: 50px;
    border: 1.5px solid rgba(196, 122, 30, 0.4);
    border-radius: 8px;
    background: linear-gradient(180deg, rgba(8, 14, 26, 0.4), rgba(13, 27, 46, 0.2));
  }

  .wall-tag {
    position: absolute;
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.32em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
    pointer-events: none;
  }
  /* In the new orientation, front-of-room is at the BOTTOM of the diagram,
     rear at the TOP. */
  .wall-tag.rear {
    top: -22px;
    left: 50%;
    transform: translateX(-50%);
  }
  .wall-tag.front {
    bottom: -22px;
    left: 50%;
    transform: translateX(-50%);
  }

  .speaker {
    position: absolute;
    height: 9px;
    background: rgba(56, 189, 248, 0.22);
    border: 1px solid rgba(56, 189, 248, 0.55);
    border-radius: 2px;
  }
  /* Rear (top) wall — D3 lives at top-left, so left rear speaker is shorter
     and offset to make room. Right rear speaker spans most of the top wall. */
  .speaker.rl { top: 2px;    left: 31%; width: 14%; }
  .speaker.rr { top: 2px;    right: 8%; width: 36%; }
  /* Front (bottom) wall — split bar with Cam1 in the middle. */
  .speaker.fl { bottom: 2px; left: 8%;  width: 30%; }
  .speaker.fr { bottom: 2px; right: 8%; width: 30%; }

  /* Cameras — green pill matching the user's reference image */
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
  /* Cam1 at the front (bottom-center, straddling the front wall) */
  .cam.cam-1 { bottom: -10px; left: 50%; transform: translateX(-50%); }
  /* Cam2 at the rear (top-center, just inside the rear wall) */
  .cam.cam-2 { top: 5%; left: 50%; transform: translateX(-50%); }
  .cam::before {
    content: 'Cam';
    font-size: 9px;
    font-weight: 800;
    margin-right: 2px;
  }

  .proj {
    position: absolute;
    width: 26px;
    height: 26px;
    background: rgba(56, 189, 248, 0.10);
    border: 1.5px solid rgba(56, 189, 248, 0.6);
    transform: rotate(45deg);
    display: grid;
    place-items: center;
  }
  .proj-inner {
    transform: rotate(-45deg);
    font-size: 9px;
    font-weight: 800;
    color: rgba(56, 189, 248, 0.95);
  }
  /* Projectors mounted near the front-of-room projection surfaces (bottom). */
  .proj.p1 { bottom: 18%; left: 22%; }
  .proj.p2 { bottom: 18%; right: 22%; }

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
  .mic.mxa-a { top: 30%; left: 23%; }
  .mic.mxa-b { top: 30%; right: 23%; }

  /* Podium frame — small rounded-rect sitting below the D4 marker. Matches
     the visual style the user requested (same frame as the old conference
     table, smaller, centered). */
  .podium {
    position: absolute;
    top: 55%;
    left: 41%;
    width: 18%;
    height: 14%;
    border: 1.5px solid rgba(148, 163, 184, 0.55);
    border-radius: 14px;
    display: grid;
    place-items: center;
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.2em;
    text-transform: uppercase;
    color: rgba(245, 166, 35, 0.7);
    pointer-events: none;
  }
</style>
