<!--
  VuMeter — shared vertical VU primitive (Plan 4 — Mockup #13).

  Column-reverse stack of N segments. Lit count is rounded from level/100*N.
  Coloring rule by index (default 14 segments):
    0..6   → green
    7..10  → yellow
    11..13 → red
  Unlit segments use a dim base color.
-->
<script lang="ts">
  interface Props {
    level: number;       // 0..100
    segments?: number;   // default 14
  }

  let { level, segments = 14 }: Props = $props();

  let lit = $derived(Math.round((Math.max(0, Math.min(100, level)) / 100) * segments));

  function segClass(index: number): string {
    if (index >= lit) return 'vu-seg';
    // index here is from BOTTOM (0 = lowest). Coloring rule is by absolute index.
    const greenMax = Math.floor(0.5 * segments);   // 7 for N=14 → indices 0..6 = green
    const yellowMax = Math.floor(0.75 * segments); // 10 for N=14 → indices 7..10 = yellow
    if (index < greenMax) return 'vu-seg lit-green';
    if (index < yellowMax) return 'vu-seg lit-yellow';
    return 'vu-seg lit-red';
  }
</script>

<div class="vu-col" aria-hidden="true">
  {#each Array(segments) as _, i}
    <div class={segClass(i)}></div>
  {/each}
</div>

<style>
  .vu-col {
    display: flex;
    flex-direction: column-reverse;
    gap: 2px;
    height: 100%;
    width: 16px;
  }

  .vu-seg {
    flex: 1;
    border-radius: 2px;
    background: rgba(30, 41, 59, 0.55);
    border: 0.5px solid rgba(148, 163, 184, 0.08);
    transition: background 60ms linear, box-shadow 60ms linear;
  }

  .lit-green {
    background: #22c55e;
    border-color: rgba(34, 197, 94, 0.6);
    box-shadow: 0 0 6px rgba(34, 197, 94, 0.55);
  }
  .lit-yellow {
    background: #facc15;
    border-color: rgba(250, 204, 21, 0.6);
    box-shadow: 0 0 6px rgba(250, 204, 21, 0.6);
  }
  .lit-red {
    background: #ef4444;
    border-color: rgba(239, 68, 68, 0.65);
    box-shadow: 0 0 8px rgba(239, 68, 68, 0.65);
  }

  @media (prefers-reduced-motion: reduce) {
    .vu-seg { transition: none; }
  }
</style>
