<script lang="ts">
  import { onDestroy } from 'svelte';

  interface Props {
    label: string;
    onRecall: () => void;
    onSave: () => void;
    holdMs?: number;   // default 3000
  }

  let { label, onRecall, onSave, holdMs = 3000 }: Props = $props();

  let pressing = $state(false);
  let progress = $state(0);   // 0..1 fill during hold
  let saved = $state(false);
  let saveFired = false;
  let pressStart = 0;
  let progressTimer: ReturnType<typeof setInterval> | undefined;
  let saveTimer: ReturnType<typeof setTimeout> | undefined;
  let resetTimer: ReturnType<typeof setTimeout> | undefined;

  function clearTimers() {
    if (progressTimer !== undefined) clearInterval(progressTimer);
    if (saveTimer !== undefined) clearTimeout(saveTimer);
    progressTimer = undefined;
    saveTimer = undefined;
  }

  onDestroy(() => {
    clearTimers();
    if (resetTimer !== undefined) clearTimeout(resetTimer);
  });

  function startPress() {
    if (pressing) return;
    pressing = true;
    saveFired = false;
    saved = false;
    progress = 0;
    pressStart = Date.now();

    progressTimer = setInterval(() => {
      progress = Math.min(1, (Date.now() - pressStart) / holdMs);
    }, 40);

    saveTimer = setTimeout(() => {
      saveFired = true;
      saved = true;
      onSave();
      // Hold the "Saved" flash for ~1.2s after release
    }, holdMs);
  }

  function endPress(cancelled: boolean) {
    if (!pressing) return;
    pressing = false;
    clearTimers();

    if (!saveFired && !cancelled) {
      // Released before hold completed and not cancelled → recall
      onRecall();
    }
    progress = 0;

    if (saveFired) {
      // Keep "Saved" flash visible briefly, then clear
      resetTimer = setTimeout(() => { saved = false; }, 1200);
    }
  }
</script>

<button
  class="btn preset-btn"
  class:pressing
  class:saved
  onmousedown={startPress}
  onmouseup={() => endPress(false)}
  onmouseleave={() => endPress(true)}
  ontouchstart={(e) => { e.preventDefault(); startPress(); }}
  ontouchend={(e) => { e.preventDefault(); endPress(false); }}
  ontouchcancel={() => endPress(true)}
>
  <span class="preset-label">{label}</span>
  {#if pressing}
    <span class="hint">Hold to save…</span>
    <svg class="progress-ring" viewBox="0 0 40 40" aria-hidden="true">
      <circle cx="20" cy="20" r="17" stroke="rgba(255,255,255,0.18)" stroke-width="2.5" fill="none"></circle>
      <circle
        cx="20" cy="20" r="17"
        stroke="var(--color-accent)" stroke-width="2.5" fill="none" stroke-linecap="round"
        stroke-dasharray={2 * Math.PI * 17}
        stroke-dashoffset={2 * Math.PI * 17 * (1 - progress)}
        transform="rotate(-90 20 20)"
      ></circle>
    </svg>
  {:else if saved}
    <span class="saved-flash">Saved ✓</span>
  {/if}
</button>

<style>
  .preset-btn {
    position: relative;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 4px;
    min-height: 72px;
    padding: 10px 16px;
  }
  .preset-label {
    font-size: 14px;
    font-weight: 700;
    letter-spacing: 0.16em;
  }
  .hint {
    color: var(--color-copy-muted);
    font-size: 10px;
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
  }
  .saved-flash {
    color: var(--color-success);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.12em;
    text-transform: uppercase;
  }

  .progress-ring {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    pointer-events: none;
  }

  .preset-btn.pressing {
    background: rgba(56, 189, 248, 0.12);
    border-color: var(--color-accent);
  }
  .preset-btn.saved {
    border-color: rgba(34, 197, 94, 0.5);
    background: rgba(34, 197, 94, 0.10);
  }

  @media (prefers-reduced-motion: reduce) {
    .progress-ring circle { transition: none; }
  }
</style>
