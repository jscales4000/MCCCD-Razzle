<script lang="ts">
  type ShutdownItem = {
    icon: 'display' | 'audio' | 'camera';
    label: string;
  };

  interface Props {
    open: boolean;
    countdown?: number;                  // seconds; default 30
    title?: string;                      // default "Shut Down Room?"
    body?: string;                       // optional; vacancy-aware default
    vacancyMinutes?: number;             // optional; drives the bottom strip
    shutdownItems?: ShutdownItem[];      // optional; checklist rows
    onConfirm: () => void;
    onCancel: () => void;
  }

  let {
    open,
    countdown = 30,
    title = 'Shut Down Room?',
    body,
    vacancyMinutes,
    shutdownItems,
    onConfirm,
    onCancel,
  }: Props = $props();

  let remaining = $state(countdown);

  // SVG ring math: circumference of r=52 ≈ 326.
  // dashoffset = circumference × (1 − remaining/countdown).
  // At remaining=countdown → 0 (full ring). At remaining=0 → 326 (empty).
  const RING_CIRCUMFERENCE = 326;
  let strokeDashoffset = $derived(
    RING_CIRCUMFERENCE * (1 - remaining / countdown)
  );

  let displayBody = $derived(
    body ??
      (vacancyMinutes !== undefined
        ? `The room has been vacant for ${vacancyMinutes} minutes. All displays, audio, and cameras will power off. This cannot be undone without a full restart.`
        : 'Are you sure you want to shut down?')
  );

  // $effect runs whenever `open` flips. When the effect re-runs (or the
  // component unmounts), the returned cleanup function fires automatically,
  // clearing any in-flight interval. This consolidates teardown in one place.
  $effect(() => {
    if (!open) return;
    remaining = countdown;
    const id = setInterval(() => {
      remaining -= 1;
      if (remaining <= 0) {
        onConfirm();
      }
    }, 1000);
    return () => clearInterval(id);
  });

  function handleConfirm() {
    onConfirm();
  }

  function handleCancel() {
    onCancel();
  }
</script>

{#if open}
  <div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="shutdown-title">
    <div class="modal-card">

      <div class="modal-stripe" aria-hidden="true"></div>

      <div class="modal-body">

        <div class="modal-icon" aria-hidden="true">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
            <path d="M12 3v9"/>
            <path d="M6.5 7.5a8 8 0 1 0 11 0"/>
          </svg>
        </div>

        <h2 id="shutdown-title" class="modal-title">{title}</h2>
        <p class="modal-body-text">{displayBody}</p>

        <div class="countdown-wrap">
          <svg class="countdown-svg" viewBox="0 0 120 120" aria-hidden="true">
            <circle class="countdown-track" cx="60" cy="60" r="52"/>
            <circle
              class="countdown-fill"
              cx="60" cy="60" r="52"
              stroke-dasharray={RING_CIRCUMFERENCE}
              stroke-dashoffset={strokeDashoffset}
            />
          </svg>
          <span class="countdown-num" aria-live="polite">{remaining}</span>
          <span class="countdown-sec">sec</span>
        </div>

        <div class="modal-actions">
          <button type="button" class="btn-cancel" onclick={handleCancel}>
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" aria-hidden="true">
              <path d="M18 6L6 18M6 6l12 12"/>
            </svg>
            Cancel
          </button>
          <button type="button" class="btn-confirm" onclick={handleConfirm}>
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
              <path d="M12 3v9"/>
              <path d="M6.5 7.5a8 8 0 1 0 11 0"/>
            </svg>
            Shut Down Now
          </button>
        </div>
      </div>

      {#if shutdownItems && shutdownItems.length > 0}
        <div class="shutdown-list">
          <p class="sl-label">Will be powered off</p>
          {#each shutdownItems as item}
            <div class="sl-item">
              <div class="sl-icon" aria-hidden="true">
                {#if item.icon === 'display'}
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
                {:else if item.icon === 'audio'}
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 1a3 3 0 0 0-3 3v8a3 3 0 0 0 6 0V4a3 3 0 0 0-3-3z"/><path d="M19 10v2a7 7 0 0 1-14 0v-2"/></svg>
                {:else}
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 7h4l2-2h4l2 2h4v12H4z"/><circle cx="12" cy="13" r="3.6"/></svg>
                {/if}
              </div>
              <span>{item.label}</span>
            </div>
          {/each}
        </div>
      {/if}

      {#if vacancyMinutes !== undefined}
        <div class="vacancy-bar">
          <span class="vacancy-dot" aria-hidden="true"></span>
          Triggered by occupancy timeout · Room vacant {vacancyMinutes} min · Auto-shutdown threshold: 15 min
        </div>
      {/if}

    </div>
  </div>
{/if}

<style>
  .modal-backdrop {
    position: fixed;
    top: 0;
    right: 0;
    bottom: 0;
    left: 0;
    background-color: rgba(2, 6, 23, 0.92);
    backdrop-filter: blur(8px);
    display: grid;
    place-items: center;
    z-index: 1000;
    animation: fade-in 140ms ease;
  }

  .modal-card {
    width: 560px;
    max-width: 92%;
    background: rgba(12, 20, 36, 0.98);
    border: 1px solid rgba(239, 68, 68, 0.3);
    border-radius: 20px;
    box-shadow:
      0 0 0 1px rgba(239, 68, 68, 0.08),
      0 40px 80px rgba(0, 0, 0, 0.7),
      0 0 60px rgba(239, 68, 68, 0.06);
    display: flex;
    flex-direction: column;
    align-items: center;
    overflow: hidden;
    animation: modal-in 250ms cubic-bezier(0.16, 1, 0.3, 1);
  }

  /* Animated danger top stripe */
  .modal-stripe {
    width: 100%;
    height: 4px;
    background: linear-gradient(90deg, #ef4444, #f97316, #ef4444);
    background-size: 200% 100%;
    animation: stripe-slide 2s linear infinite;
  }

  .modal-body {
    padding: 36px 40px 32px;
    display: flex;
    flex-direction: column;
    align-items: center;
    width: 100%;
  }

  .modal-icon {
    width: 72px;
    height: 72px;
    border-radius: 50%;
    background: rgba(239, 68, 68, 0.1);
    border: 1.5px solid rgba(239, 68, 68, 0.35);
    display: grid;
    place-items: center;
    color: #fca5a5;
    margin-bottom: 20px;
    box-shadow: 0 0 30px rgba(239, 68, 68, 0.12);
  }

  .modal-title {
    margin: 0 0 8px;
    font-size: 26px;
    font-weight: 900;
    letter-spacing: -0.02em;
    color: #ffffff;
    text-align: center;
  }

  .modal-body-text {
    margin: 0 0 32px;
    font-size: 14px;
    color: var(--color-copy-soft, #94a3b8);
    text-align: center;
    line-height: 1.6;
    max-width: 380px;
  }

  /* Countdown ring */
  .countdown-wrap {
    position: relative;
    width: 120px;
    height: 120px;
    margin-bottom: 32px;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .countdown-svg {
    position: absolute;
    inset: 0;
    transform: rotate(-90deg);
  }
  .countdown-track {
    fill: none;
    stroke: rgba(239, 68, 68, 0.12);
    stroke-width: 6;
  }
  .countdown-fill {
    fill: none;
    stroke: #ef4444;
    stroke-width: 6;
    stroke-linecap: round;
    transition: stroke-dashoffset 1s linear;
    filter: drop-shadow(0 0 4px rgba(239, 68, 68, 0.5));
  }
  .countdown-num {
    font-size: 42px;
    font-weight: 900;
    color: #fca5a5;
    font-variant-numeric: tabular-nums;
    letter-spacing: -0.02em;
    line-height: 1;
    position: relative;
    z-index: 1;
  }
  .countdown-sec {
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: rgba(252, 165, 165, 0.6);
    position: absolute;
    bottom: 16px;
  }

  /* Action buttons */
  .modal-actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 10px;
    width: 100%;
  }
  .btn-cancel,
  .btn-confirm {
    padding: 16px 24px;
    border-radius: 12px;
    font-size: 15px;
    font-weight: 700;
    letter-spacing: 0.02em;
    cursor: pointer;
    transition: background 140ms ease, border-color 140ms ease;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 9px;
  }
  .btn-cancel {
    background: rgba(30, 41, 59, 0.7);
    border: 0.5px solid var(--color-border, rgba(148, 163, 184, 0.14));
    color: var(--color-copy, #e2e8f0);
  }
  .btn-cancel:hover {
    background: rgba(51, 65, 85, 0.8);
    border-color: rgba(148, 163, 184, 0.3);
  }
  .btn-confirm {
    background: rgba(239, 68, 68, 0.15);
    border: 1px solid rgba(239, 68, 68, 0.4);
    color: #fca5a5;
    box-shadow: 0 0 20px rgba(239, 68, 68, 0.06);
  }
  .btn-confirm:hover {
    background: rgba(239, 68, 68, 0.25);
    border-color: rgba(239, 68, 68, 0.6);
  }

  /* Shutdown checklist */
  .shutdown-list {
    width: 100%;
    border-top: 0.5px solid var(--color-border, rgba(148, 163, 184, 0.14));
    padding: 20px 40px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    background: rgba(6, 10, 20, 0.5);
  }
  .sl-label {
    margin: 0 0 4px;
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted, #4d6070);
  }
  .sl-item {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 12px;
    color: var(--color-copy-soft, #7c93a8);
  }
  .sl-icon {
    width: 24px;
    height: 24px;
    border-radius: 5px;
    background: rgba(239, 68, 68, 0.08);
    border: 0.5px solid rgba(239, 68, 68, 0.18);
    display: grid;
    place-items: center;
    color: #fca5a5;
    flex-shrink: 0;
  }

  /* Vacancy strip */
  .vacancy-bar {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    padding: 10px 40px;
    background: rgba(245, 158, 11, 0.05);
    border-top: 0.5px solid rgba(245, 158, 11, 0.12);
    font-size: 11px;
    color: rgba(252, 211, 77, 0.7);
    font-weight: 600;
  }
  .vacancy-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #fcd34d;
    box-shadow: 0 0 6px rgba(252, 211, 77, 0.4);
    animation: vacancy-pulse 2s ease-in-out infinite;
  }

  /* Animations */
  @keyframes fade-in {
    from { opacity: 0; }
    to { opacity: 1; }
  }
  @keyframes modal-in {
    from { opacity: 0; transform: scale(0.94) translateY(12px); }
    to { opacity: 1; transform: scale(1) translateY(0); }
  }
  @keyframes stripe-slide {
    0%   { background-position: 0% 50%; }
    100% { background-position: 200% 50%; }
  }
  @keyframes vacancy-pulse {
    0%, 100% { opacity: 1; }
    50%      { opacity: 0.4; }
  }

  @media (prefers-reduced-motion: reduce) {
    .modal-backdrop,
    .modal-card,
    .modal-stripe,
    .vacancy-dot {
      animation: none;
    }
    .countdown-fill {
      transition: none;
    }
  }
</style>
