<script lang="ts">
  import { onDestroy } from 'svelte';

  interface Props {
    level: number;        // 0..100, current program audio level (from $progAudioLevelFb)
  }

  let { level }: Props = $props();

  let visible = $state(false);
  let hideTimer: ReturnType<typeof setTimeout> | undefined;

  // Public API: parent calls show() each time Vol+/Vol- is tapped.
  // Each call resets the 5-second auto-hide timer so rapid taps keep the popup
  // visible. Mute does NOT call this.
  export function show() {
    visible = true;
    if (hideTimer !== undefined) clearTimeout(hideTimer);
    hideTimer = setTimeout(() => { visible = false; }, 5000);
  }

  onDestroy(() => {
    if (hideTimer !== undefined) clearTimeout(hideTimer);
  });

  // 12 segments matches the existing VuMeter motif
  const SEGMENTS = 12;
  let litCount = $derived(Math.round((level / 100) * SEGMENTS));
</script>

{#if visible}
  <div class="vol-popup" role="status" aria-live="polite" aria-label={`Volume ${level}%`}>
    <div class="vp-row">
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
        <path d="M11 5L6 9H2v6h4l5 4z" fill="currentColor"/>
        <path d="M19.07 4.93a10 10 0 0 1 0 14.14M15.54 8.46a5 5 0 0 1 0 7.07"/>
      </svg>
      <span class="vp-label">Volume</span>
      <span class="vp-value">{level}%</span>
    </div>
    <div class="vp-meter">
      {#each Array(SEGMENTS) as _, i}
        <div class="vp-seg" class:lit={i < litCount}></div>
      {/each}
    </div>
  </div>
{/if}

<style>
  .vol-popup {
    position: fixed;
    right: 22px;
    bottom: 110px;        /* sits just above the 80px footer (+14px gap) */
    z-index: 900;
    background: rgba(12, 20, 36, 0.98);
    border: 1px solid rgba(245, 166, 35, 0.35);
    border-radius: 12px;
    padding: 12px 16px;
    min-width: 240px;
    box-shadow:
      0 0 0 1px rgba(245, 166, 35, 0.08),
      0 16px 40px rgba(0, 0, 0, 0.55),
      0 0 30px rgba(245, 166, 35, 0.08);
    animation: vp-rise 180ms cubic-bezier(0.16, 1, 0.3, 1);
    color: #f5a623;
  }

  .vp-row {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 8px;
  }
  .vp-label {
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted, #64748b);
    flex: 1;
  }
  .vp-value {
    font-size: 18px;
    font-weight: 900;
    font-variant-numeric: tabular-nums;
    color: #f5a623;
  }

  .vp-meter {
    display: grid;
    grid-template-columns: repeat(12, 1fr);
    gap: 3px;
    height: 8px;
  }
  .vp-seg {
    border-radius: 2px;
    background: rgba(148, 163, 184, 0.12);
    transition: background 60ms;
  }
  .vp-seg.lit {
    background: #f5a623;
    box-shadow: 0 0 5px rgba(245, 166, 35, 0.55);
  }

  @keyframes vp-rise {
    from { opacity: 0; transform: translateY(8px); }
    to   { opacity: 1; transform: translateY(0); }
  }

  @media (prefers-reduced-motion: reduce) {
    .vol-popup { animation: none; }
    .vp-seg { transition: none; }
  }
</style>
