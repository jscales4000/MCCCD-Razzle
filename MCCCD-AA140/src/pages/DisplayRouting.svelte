<!--
  DisplayRouting — Mockup #14 source-to-display matrix routing page.

  Reached via tile-tap on Home (`goToPage('routing')`). Reuses the existing
  drag-drop store at lib/stores/router.ts unmodified — class names `chip`,
  `tile`, and `tile-slot` are mandated by that store.
-->

<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { ROOM_NAME, SIGNALS } from '../lib/contract';
  import { publishAnalog, publishDigital, pulseDigital } from '../lib/CrComLib';
  import { goToPage } from '../lib/stores/page';
  import {
    armChip,
    armedSource,
    chipPointerDown,
    shouldSuppressClick,
    SOURCES,
    type DisplayId,
    type SourceId,
  } from '../lib/stores/router';
  import {
    autoRouteEnableFb,
    audioOutputSelectFb,
    display1PowerFb,
    display2PowerFb,
    display3PowerFb,
    display1SourceFb,
    display2SourceFb,
    display3SourceFb,
    routingModeFb,
    initDisplayRoutingSubscriptions,
    teardownDisplayRoutingSubscriptions,
  } from '../lib/stores/signals';

  // DisplayRouting-exclusive signals are subscribed lazily. Per-audit H4-followup.
  onMount(initDisplayRoutingSubscriptions);
  onDestroy(teardownDisplayRoutingSubscriptions);
  import SourceListItem from '../components/routing/SourceListItem.svelte';
  import DisplayCell from '../components/routing/DisplayCell.svelte';

  // ── Source-list metadata (4 sources, matching SOURCES in router.ts) ────
  // Sub-labels match the mockup: connector type per input.
  const SOURCE_ROWS: Array<{ id: SourceId; sub: string }> = [
    { id: 'roomPc',   sub: 'Input 1 · HDMI' },
    { id: 'extPc',    sub: 'Input 2 · HDMI 2' },
    { id: 'airMedia', sub: 'Input 3 · Wireless' },
    { id: 'laptop',   sub: 'Input 4 · HDMI 3' },
  ];

  // ── Display metadata ───────────────────────────────────────────────────
  const DISPLAY_ROWS: Array<{
    id: DisplayId;
    label: string;
    spec: string;
    hasMirror: boolean;
  }> = [
    { id: 'd1', label: 'Front Left',  spec: '65" 4K · NEC',     hasMirror: true  },
    { id: 'd2', label: 'Front Right', spec: '65" 4K · NEC',     hasMirror: true  },
    { id: 'd3', label: 'Rear Center', spec: '55" · Samsung',    hasMirror: false },
  ];

  // ── Reactive routing map: { roomPc: ['d1','d2'], airMedia: ['d3'], ... }
  let routing = $derived<Record<SourceId, DisplayId[]>>({
    roomPc:   collectFor('roomPc'),
    extPc:    collectFor('extPc'),
    airMedia: collectFor('airMedia'),
    laptop:   collectFor('laptop'),
  });

  // Track all three feedback stores so $derived reruns when any change.
  function collectFor(srcId: SourceId): DisplayId[] {
    const ds: DisplayId[] = [];
    // Reads via $-prefixed stores keep $derived dependent on them.
    if (sourceForFb($display1SourceFb) === srcId) ds.push('d1');
    if (sourceForFb($display2SourceFb) === srcId) ds.push('d2');
    if (sourceForFb($display3SourceFb) === srcId) ds.push('d3');
    return ds;
  }

  function sourceForFb(v: number): SourceId | null {
    switch (v) {
      case 1: return 'roomPc';
      case 2: return 'extPc';
      case 3: return 'airMedia';
      case 4: return 'laptop';
      default: return null;
    }
  }

  // ── Header — mode segmented + auto-route ──────────────────────────────
  const MODES: Array<{ value: 1 | 2 | 3; label: string }> = [
    { value: 1, label: 'Manual' },
    { value: 2, label: 'Mirror All' },
    { value: 3, label: 'Extend' },
  ];

  function setMode(v: 1 | 2 | 3) {
    publishAnalog(SIGNALS.routingMode, v);
  }

  function toggleAutoRoute() {
    publishDigital(SIGNALS.autoRouteEnable, !$autoRouteEnableFb);
  }

  // ── Source list interactions (arm + drag) ──────────────────────────────
  function onSourceClick(e: MouseEvent, sourceId: SourceId) {
    if (shouldSuppressClick()) return;
    armChip(sourceId);
  }

  function onSourcePointerDown(e: PointerEvent, el: HTMLElement, sourceId: SourceId) {
    chipPointerDown(e, el, sourceId);
  }

  // ── Display power + audio ─────────────────────────────────────────────
  function powerToggle(_id: DisplayId) {
    // Per-display power is not in the current signal set; the global
    // displayPower pulse is the closest action available. SIMPL catch-up
    // can split this when per-display signals land.
    pulseDigital(SIGNALS.displayPower);
  }

  function setAudioOutput(v: 1 | 2) {
    publishAnalog(SIGNALS.audioOutputSelect, v);
  }

  // ── Mirror quick-actions ──────────────────────────────────────────────
  function mirrorD1ToD3() { pulseDigital(SIGNALS.d1MirrorToD3); }
  function mirrorD2ToD3() { pulseDigital(SIGNALS.d2MirrorToD3); }
  function mirrorAll()    { pulseDigital(SIGNALS.mirrorAllSame); }

  // ── Footer quick routes ───────────────────────────────────────────────
  function routeAllTo(value: 1 | 2 | 3 | 4 | 0) {
    publishAnalog(SIGNALS.display1Source, value);
    publishAnalog(SIGNALS.display2Source, value);
    publishAnalog(SIGNALS.display3Source, value);
  }

  function clearAll()       { routeAllTo(0); }
  function routeRoomPcAll() { routeAllTo(1); }
  function routeAirMediaAll() { routeAllTo(3); }
</script>

<svelte:head>
  <title>{ROOM_NAME} CH5 Panel — Display Routing</title>
</svelte:head>

<div class="routing-page">
  <!-- HEADER ───────────────────────────────────────────────────────── -->
  <header class="routing-header">
    <button class="back-btn" onclick={() => goToPage('home')} aria-label="Back to Home" type="button">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" aria-hidden="true">
        <path d="M19 12H5M12 5l-7 7 7 7"/>
      </svg>
      Home
    </button>
    <div class="sep"></div>
    <span class="room">{ROOM_NAME}</span>
    <div class="sep"></div>
    <span class="eyebrow">Display Routing</span>
    <div class="hsp"></div>

    <div class="mode-seg" role="tablist" aria-label="Routing mode">
      {#each MODES as m}
        {@const isActive = ($routingModeFb === m.value) || ($routingModeFb === 0 && m.value === 1)}
        <button
          class="mode-btn"
          class:active={isActive}
          onclick={() => setMode(m.value)}
          role="tab"
          aria-selected={isActive}
          type="button"
        >{m.label}</button>
      {/each}
    </div>

    <button
      class="auto-chip"
      class:on={$autoRouteEnableFb}
      onclick={toggleAutoRoute}
      aria-pressed={$autoRouteEnableFb}
      type="button"
    >
      <span class="auto-dot" class:on={$autoRouteEnableFb}></span>
      Auto-Route {$autoRouteEnableFb ? 'On' : 'Off'}
    </button>
  </header>

  <!-- MAIN ──────────────────────────────────────────────────────────── -->
  <div class="routing-main">

    <!-- Source list (left) -->
    <aside class="src-panel">
      <div class="sp-head">Input Sources · Tap to arm · Long-press to drag</div>
      <div class="src-list">
        {#each SOURCE_ROWS as row (row.id)}
          <SourceListItem
            sourceId={row.id}
            name={SOURCES[row.id].label}
            subLabel={row.sub}
            routedTo={routing[row.id]}
            selected={$armedSource === row.id}
            onClick={(e) => onSourceClick(e, row.id)}
            onPointerDown={(e, el) => onSourcePointerDown(e, el, row.id)}
          />
        {/each}
      </div>
    </aside>

    <!-- Matrix (right) -->
    <section class="matrix-panel">
      <div class="mp-head">
        <span class="mp-title">Output Displays · Tap cell to route armed source</span>
        <span class="mp-hint">Drag-to-route supported</span>
      </div>

      <div class="matrix-body">
        <DisplayCell
          displayId="d1"
          label={DISPLAY_ROWS[0].label}
          spec={DISPLAY_ROWS[0].spec}
          activeSourceFb={$display1SourceFb}
          powerOn={$display1PowerFb}
          audioActive={$audioOutputSelectFb === 1}
          onPowerToggle={() => powerToggle('d1')}
          onAudioToggle={() => setAudioOutput(1)}
          onMirror={mirrorD1ToD3}
        />
        <DisplayCell
          displayId="d2"
          label={DISPLAY_ROWS[1].label}
          spec={DISPLAY_ROWS[1].spec}
          activeSourceFb={$display2SourceFb}
          powerOn={$display2PowerFb}
          audioActive={$audioOutputSelectFb === 2}
          onPowerToggle={() => powerToggle('d2')}
          onAudioToggle={() => setAudioOutput(2)}
          onMirror={mirrorD2ToD3}
        />
        <DisplayCell
          displayId="d3"
          label={DISPLAY_ROWS[2].label}
          spec={DISPLAY_ROWS[2].spec}
          activeSourceFb={$display3SourceFb}
          powerOn={$display3PowerFb}
          audioActive={false}
          onPowerToggle={() => powerToggle('d3')}
          onAudioToggle={() => { /* D3 has no dedicated audio route */ }}
        />

        <div class="mirror-row">
          <button class="mirror-btn" onclick={mirrorD1ToD3} type="button">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
              <path d="M8 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h3M16 3h3a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-3M12 3v18"/>
            </svg>
            Mirror D1 → D3
          </button>
          <button class="mirror-btn" onclick={mirrorD2ToD3} type="button">Mirror D2 → D3</button>
          <button class="mirror-btn" onclick={mirrorAll} type="button">All Displays Same</button>
        </div>
      </div>
    </section>
  </div>

  <!-- FOOTER ────────────────────────────────────────────────────────── -->
  <footer class="routing-footer">
    <span class="f-label">Quick Routes</span>
    <button class="route-all" onclick={routeRoomPcAll} type="button">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
        <rect x="2" y="3" width="20" height="14" rx="2"/>
        <path d="M8 21h8M12 17v4"/>
      </svg>
      Room PC → All
    </button>
    <button class="route-all" onclick={routeAirMediaAll} type="button">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
        <path d="M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01"/>
      </svg>
      AirMedia → All
    </button>
    <div class="fsp"></div>
    <button class="clear-btn" onclick={clearAll} type="button">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
        <polyline points="3 6 5 6 21 6"/>
        <path d="M19 6l-1 14H6L5 6"/>
      </svg>
      Clear All Routes
    </button>
  </footer>
</div>

<style>
  .routing-page {
    width: 100%;
    height: 100%;
    display: grid;
    grid-template-rows: 60px 1fr 88px;
    gap: 10px;
    padding: 10px;
    box-sizing: border-box;
  }

  /* ── Header ─────────────────────────────────────────────────────── */
  .routing-header {
    background: var(--color-panel);
    border: 0.5px solid var(--color-border);
    border-radius: 14px;
    display: flex;
    align-items: center;
    padding: 0 20px;
    gap: 14px;
  }

  .back-btn {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 8px 14px;
    border-radius: 8px;
    border: 0.5px solid var(--color-border);
    background: rgba(30, 41, 59, 0.5);
    color: var(--color-copy-soft);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.06em;
    text-transform: uppercase;
    cursor: pointer;
    transition: background 110ms ease, color 110ms ease, border-color 110ms ease;
  }
  .back-btn:hover {
    background: rgba(30, 41, 59, 0.85);
    color: var(--color-copy);
    border-color: var(--color-accent-soft);
  }

  .room {
    font-size: 18px;
    font-weight: 900;
    color: var(--color-copy);
  }

  .sep {
    width: 1px;
    height: 20px;
    background: var(--color-border);
  }

  .eyebrow {
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }

  .hsp { flex: 1; }

  .mode-seg {
    display: flex;
    border-radius: 9px;
    overflow: hidden;
    border: 0.5px solid var(--color-border);
  }

  .mode-btn {
    padding: 8px 16px;
    border: none;
    background: rgba(30, 41, 59, 0.5);
    color: var(--color-copy-soft);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.06em;
    text-transform: uppercase;
    cursor: pointer;
    border-right: 0.5px solid var(--color-border);
    transition: background 110ms ease, color 110ms ease;
  }
  .mode-btn:last-child { border-right: none; }
  .mode-btn:hover { background: rgba(51, 65, 85, 0.7); color: var(--color-copy); }
  .mode-btn.active {
    background: rgba(245, 166, 35, 0.14);
    color: var(--color-accent);
  }

  .auto-chip {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 8px 14px;
    border-radius: 9px;
    background: rgba(100, 116, 139, 0.08);
    border: 0.5px solid var(--color-border);
    color: var(--color-copy-soft);
    font-size: 12px;
    font-weight: 700;
    cursor: pointer;
    transition: background 110ms ease, border-color 110ms ease, color 110ms ease;
  }
  .auto-chip.on {
    background: rgba(34, 197, 94, 0.10);
    border-color: rgba(34, 197, 94, 0.28);
    color: #86efac;
  }

  .auto-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: rgba(100, 116, 139, 0.5);
  }
  .auto-dot.on {
    background: currentColor;
    box-shadow: 0 0 6px currentColor;
    animation: routing-auto-pulse 2s ease-in-out infinite;
  }

  @keyframes routing-auto-pulse {
    0%, 100% { opacity: 1; }
    50%      { opacity: 0.4; }
  }

  /* ── Main grid ──────────────────────────────────────────────────── */
  .routing-main {
    display: grid;
    grid-template-columns: 260px 1fr;
    gap: 10px;
    min-height: 0;
  }

  .src-panel {
    background: var(--color-panel);
    border: 0.5px solid var(--color-border);
    border-radius: 14px;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    min-height: 0;
  }

  .sp-head {
    padding: 14px 16px 10px;
    border-bottom: 0.5px solid var(--color-border);
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.2em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
    background: rgba(8, 14, 26, 0.4);
  }

  .src-list {
    flex: 1;
    overflow-y: auto;
    padding: 8px;
    min-height: 0;
  }

  .matrix-panel {
    background: var(--color-panel);
    border: 0.5px solid var(--color-border);
    border-radius: 14px;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    min-height: 0;
  }

  .mp-head {
    padding: 14px 20px 10px;
    border-bottom: 0.5px solid var(--color-border);
    display: flex;
    align-items: center;
    gap: 12px;
    background: rgba(8, 14, 26, 0.4);
  }

  .mp-title {
    flex: 1;
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.2em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }

  .mp-hint {
    font-size: 11px;
    color: var(--color-copy-muted);
  }

  .matrix-body {
    flex: 1;
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    grid-auto-rows: auto;
    gap: 14px;
    padding: 16px;
    min-height: 0;
  }

  .mirror-row {
    grid-column: 1 / -1;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 12px;
    padding: 2px 0;
  }

  .mirror-btn {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 7px 14px;
    border-radius: 8px;
    background: rgba(245, 166, 35, 0.08);
    border: 0.5px solid rgba(245, 166, 35, 0.20);
    color: var(--color-accent);
    font-size: 11px;
    font-weight: 700;
    cursor: pointer;
    transition: background 110ms ease;
  }
  .mirror-btn:hover { background: rgba(245, 166, 35, 0.16); }

  /* ── Footer ─────────────────────────────────────────────────────── */
  .routing-footer {
    background: var(--color-panel);
    border: 0.5px solid var(--color-border);
    border-radius: 14px;
    display: flex;
    align-items: center;
    padding: 0 20px;
    gap: 14px;
  }

  .f-label {
    font-size: 9px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }

  .route-all {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 9px 16px;
    border-radius: 9px;
    background: rgba(245, 166, 35, 0.10);
    border: 0.5px solid rgba(245, 166, 35, 0.28);
    color: var(--color-accent);
    font-size: 12px;
    font-weight: 700;
    cursor: pointer;
    transition: background 120ms ease;
  }
  .route-all:hover { background: rgba(245, 166, 35, 0.20); }

  .fsp { flex: 1; }

  .clear-btn {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 9px 16px;
    border-radius: 9px;
    background: rgba(239, 68, 68, 0.10);
    border: 0.5px solid rgba(239, 68, 68, 0.25);
    color: #fca5a5;
    font-size: 12px;
    font-weight: 700;
    cursor: pointer;
    transition: background 120ms ease;
  }
  .clear-btn:hover { background: rgba(239, 68, 68, 0.18); }

  @media (prefers-reduced-motion: reduce) {
    .auto-dot.on { animation: none; }
    .back-btn,
    .mode-btn,
    .auto-chip,
    .mirror-btn,
    .route-all,
    .clear-btn { transition: none; }
  }
</style>
