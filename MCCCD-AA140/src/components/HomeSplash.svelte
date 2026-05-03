<script lang="ts">
  import { onMount, onDestroy } from 'svelte';

  interface Props {
    roomName: string;
    panelOnline: boolean;
    occupancyState: number;       // 0=idle, 1=occupied, 2=vacant-warn
    shutdownCountdown: number;    // minutes remaining (only meaningful when occupancyState=2)
    onPowerOn: () => void;
  }

  let { roomName, panelOnline, occupancyState, shutdownCountdown, onPowerOn }: Props = $props();

  // Big room name: split into prefix (letters) + suffix (digits) so digits render in accent color.
  // For "AA140" this gives prefix="AA", suffix="140". For something like "B12" it gives "B", "12".
  let namePrefix = $derived(roomName.match(/^\D+/)?.[0] ?? roomName);
  let nameSuffix = $derived(roomName.slice(namePrefix.length));

  let now = $state(new Date());
  let timer: ReturnType<typeof setInterval> | undefined;

  onMount(() => {
    timer = setInterval(() => { now = new Date(); }, 1000);
  });
  onDestroy(() => {
    if (timer !== undefined) clearInterval(timer);
  });

  let timeStr = $derived(
    now.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true })
  );
  let dateStr = $derived(
    now.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' })
  );

  // Occupancy chip data
  let occClass = $derived(
    occupancyState === 1 ? 'ss-ok' :
    occupancyState === 2 ? 'ss-warn' :
    'ss-off'
  );
  let occText = $derived(
    occupancyState === 1 ? 'Occupied' :
    occupancyState === 2 ? `Vacant · ${shutdownCountdown} min` :
    'Vacant'
  );
</script>

<div class="splash-stage">

  <div class="topbar">
    <div class="mcccd-logo">
      <div class="logo-mark" aria-hidden="true">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
      </div>
      MCCCD · Maricopa
    </div>
    <div class="topbar-right">
      <span class="time">{timeStr}</span>
      <span class="date">{dateStr}</span>
    </div>
  </div>

  <div class="hero">
    <p class="room-eyebrow">Classroom AV Control · TSW-770</p>
    <h1 class="room-name">{namePrefix}<span>{nameSuffix}</span></h1>
    <p class="room-sub">Maricopa Community College · Building A</p>

    <div class="power-cta">
      <button class="power-btn-big" onclick={onPowerOn} aria-label="Power on room AV system">
        <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
          <path d="M12 3v9"/>
          <path d="M6.5 7.5a8 8 0 1 0 11 0"/>
        </svg>
      </button>
      <span class="power-hint">Touch to Start</span>
    </div>
  </div>

  <div class="status-strip">
    <div class="ss-item">
      <svg class="ss-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01"/></svg>
      <span class="ss-dot" class:ss-ok={panelOnline} class:ss-off={!panelOnline}></span>
      <span class:strong={panelOnline}>{panelOnline ? 'Network Online' : 'Panel Offline'}</span>
    </div>
    <div class="ss-item">
      <svg class="ss-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>
      <span class="ss-dot ss-off"></span>
      <span>Displays Off</span>
    </div>
    <div class="ss-item">
      <svg class="ss-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M12 1a3 3 0 0 0-3 3v8a3 3 0 0 0 6 0V4a3 3 0 0 0-3-3z"/><path d="M19 10v2a7 7 0 0 1-14 0v-2"/><line x1="12" y1="19" x2="12" y2="23"/><line x1="8" y1="23" x2="16" y2="23"/></svg>
      <span class="ss-dot ss-off"></span>
      <span>Audio Idle</span>
    </div>
    <div class="ss-item">
      <svg class="ss-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg>
      <span class="ss-dot {occClass}"></span>
      <span class:strong={occupancyState === 2}>{occText}</span>
    </div>
  </div>

</div>

<style>
  .splash-stage {
    width: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
    position: relative;
    overflow: hidden;
    color: var(--color-copy, #e2e8f0);
  }

  /* Layered ambient glows */
  .splash-stage::before {
    content: '';
    position: absolute;
    inset: 0;
    background:
      radial-gradient(ellipse 900px 500px at 50% -80px, rgba(245,166,35,.07), transparent),
      radial-gradient(ellipse 600px 600px at 10% 110%, rgba(245,166,35,.04), transparent);
    pointer-events: none;
    z-index: 0;
  }
  /* Subtle grid texture */
  .splash-stage::after {
    content: '';
    position: absolute;
    inset: 0;
    background-image:
      linear-gradient(rgba(148,163,184,.04) 1px, transparent 1px),
      linear-gradient(90deg, rgba(148,163,184,.04) 1px, transparent 1px);
    background-size: 48px 48px;
    pointer-events: none;
    z-index: 0;
  }

  /* Top bar */
  .topbar {
    position: relative;
    z-index: 2;
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 18px 32px;
  }
  .mcccd-logo {
    display: flex;
    align-items: center;
    gap: 12px;
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted, #64748b);
  }
  .logo-mark {
    width: 36px;
    height: 36px;
    border-radius: 8px;
    background: rgba(245, 166, 35, 0.14);
    border: 1px solid rgba(245, 166, 35, 0.3);
    display: grid;
    place-items: center;
    color: #f5a623;
  }
  .topbar-right {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 2px;
  }
  .time {
    font-size: 22px;
    font-weight: 800;
    color: var(--color-copy-soft, #94a3b8);
    letter-spacing: -0.01em;
    font-variant-numeric: tabular-nums;
  }
  .date {
    font-size: 12px;
    font-weight: 600;
    color: var(--color-copy-muted, #64748b);
    letter-spacing: 0.06em;
  }

  /* Center hero */
  .hero {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    position: relative;
    z-index: 2;
    padding-bottom: 60px;
  }
  .room-eyebrow {
    margin: 0 0 16px;
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.28em;
    text-transform: uppercase;
    color: var(--color-copy-muted, #64748b);
  }
  .room-name {
    margin: 0 0 6px;
    font-size: 88px;
    font-weight: 900;
    letter-spacing: -0.04em;
    line-height: 1;
    color: var(--color-copy, #e2e8f0);
    text-shadow: 0 0 80px rgba(245, 166, 35, 0.15);
  }
  .room-name span {
    color: #f5a623;
  }
  .room-sub {
    margin: 0 0 56px;
    font-size: 16px;
    font-weight: 600;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--color-copy-muted, #64748b);
  }

  /* Power CTA */
  .power-cta {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 16px;
  }
  .power-btn-big {
    width: 96px;
    height: 96px;
    border-radius: 50%;
    background: rgba(245, 166, 35, 0.1);
    border: 2px solid rgba(245, 166, 35, 0.4);
    color: #f5a623;
    display: grid;
    place-items: center;
    cursor: pointer;
    box-shadow:
      0 0 0 0 rgba(245, 166, 35, 0.3),
      0 0 40px rgba(245, 166, 35, 0.08);
    animation: ring-pulse 2.8s ease-in-out infinite;
    transition: transform 160ms ease;
    position: relative;
  }
  .power-btn-big:active {
    transform: scale(0.96);
  }
  .power-btn-big::before {
    content: '';
    position: absolute;
    inset: -10px;
    border-radius: 50%;
    border: 1px solid rgba(245, 166, 35, 0.15);
    animation: ring-expand 2.8s ease-in-out infinite;
  }
  .power-btn-big::after {
    content: '';
    position: absolute;
    inset: -22px;
    border-radius: 50%;
    border: 1px solid rgba(245, 166, 35, 0.07);
    animation: ring-expand 2.8s ease-in-out infinite 0.4s;
  }
  .power-hint {
    font-size: 13px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--color-copy-muted, #64748b);
    animation: fade-cycle 2.8s ease-in-out infinite;
  }

  /* Status strip */
  .status-strip {
    position: relative;
    z-index: 2;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 24px;
    padding: 20px 32px 28px;
    flex-wrap: wrap;
  }
  .ss-item {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 12px;
    font-weight: 600;
    color: var(--color-copy-muted, #64748b);
    padding: 7px 14px;
    border-radius: 8px;
    background: rgba(15, 23, 42, 0.6);
    border: 0.5px solid var(--color-border, rgba(148, 163, 184, 0.15));
  }
  .ss-icon { opacity: 0.7; }
  .ss-dot {
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: currentColor;
  }
  .ss-ok { color: #86efac; }
  .ss-warn { color: #f5a623; }
  .ss-off { color: rgba(100, 116, 139, 0.5); }
  .strong { color: var(--color-copy, #e2e8f0); }
  .ss-warn ~ span.strong,
  .ss-item .ss-warn + span { color: #f5a623; }

  @keyframes ring-pulse {
    0%, 100% {
      box-shadow:
        0 0 0 0 rgba(245, 166, 35, 0.25),
        0 0 40px rgba(245, 166, 35, 0.08);
    }
    50% {
      box-shadow:
        0 0 0 12px rgba(245, 166, 35, 0.04),
        0 0 60px rgba(245, 166, 35, 0.14);
    }
  }
  @keyframes ring-expand {
    0%   { transform: scale(1);   opacity: 0.6; }
    100% { transform: scale(1.4); opacity: 0; }
  }
  @keyframes fade-cycle {
    0%, 100% { opacity: 0.5; }
    50%      { opacity: 1; }
  }

  @media (prefers-reduced-motion: reduce) {
    .power-btn-big,
    .power-btn-big::before,
    .power-btn-big::after,
    .power-hint {
      animation: none;
    }
  }
</style>
