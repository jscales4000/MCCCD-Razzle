<!--
  DisplayRouting v2 — Reflected Ceiling Plan with inline source popover.

  Replaces the source-list + matrix layout with a top-down room schematic.
  Tap any DisplayMarker in the RoomPlan (or any DisplayStatusCard in the
  sidebar) to open a SourcePopover anchored to that display. Selection
  routes via publishAnalog and the popover dismisses.

  Spec: docs/superpowers/specs/2026-05-29-rcp-routing-design.md
-->

<script lang="ts">
  import { onMount } from 'svelte';
  import { ROOM_NAME, SIGNALS } from '../lib/contract';
  import { publishAnalog, publishDigital } from '../lib/CrComLib';
  import { goToPage } from '../lib/stores/page';
  import {
    SOURCES,
    routeSource,
    clearDisplay,
    routeSignage,
    clearSignage,
    selectUsbHost,
    usbHostFromFb,
    USB_HOSTS,
    type UsbHostId,
    type DisplayId,
    type SourceId,
  } from '../lib/stores/router';
  import {
    autoRouteEnableFb,
    display1PowerFb,
    display2PowerFb,
    display3PowerFb,
    display4PowerFb,
    display1SourceFb,
    display2SourceFb,
    display3SourceFb,
    display4SourceFb,
    display5SourceFb,
    usbHostSelectFb,
    routingModeFb,
  } from '../lib/stores/signals';
  import RoomPlan from '../components/routing/RoomPlan.svelte';
  import SourcePopover from '../components/routing/SourcePopover.svelte';
  import DisplayStatusCard from '../components/routing/DisplayStatusCard.svelte';
  import Aa140Footer from '../components/Aa140Footer.svelte';

  // ── Source-value (1..4) ↔ SourceId map ─────────────────────────────────
  const VALUE_TO_SOURCE: Record<number, SourceId> = {
    1: 'roomPc',
    2: 'extPc',
    3: 'airMedia',
    4: 'laptop',
  };
  function srcFromFb(v: number): SourceId | null {
    return VALUE_TO_SOURCE[v] ?? null;
  }

  // ── Static display metadata ────────────────────────────────────────────
  const DISPLAY_META = [
    { id: 'd1' as DisplayId, label: 'Front Left',  spec: 'Sony VPL · 100" Projection' },
    { id: 'd2' as DisplayId, label: 'Front Right', spec: 'Sony VPL · 100" Projection' },
    { id: 'd3' as DisplayId, label: 'Rear Newline', spec: 'Newline 86" · Interactive' },
    { id: 'd4' as DisplayId, label: 'Podium',      spec: 'Confidence · Defaults to D3' },
  ];

  // ── Reactive merged view: meta + live source/power feedback ───────────
  let displays = $derived(DISPLAY_META.map(m => ({
    ...m,
    activeSource: srcFromFb(
      m.id === 'd1' ? $display1SourceFb :
      m.id === 'd2' ? $display2SourceFb :
      m.id === 'd3' ? $display3SourceFb : $display4SourceFb
    ),
    powerOn:
      m.id === 'd1' ? $display1PowerFb :
      m.id === 'd2' ? $display2PowerFb :
      m.id === 'd3' ? $display3PowerFb : $display4PowerFb,
  })));

  // ── Popover state ──────────────────────────────────────────────────────
  let openDisplay = $state<DisplayId | null>(null);
  let anchor = $state<{ top: number; left: number; width: number; height: number; containerHeight: number; containerWidth: number } | null>(null);
  let planCellEl: HTMLDivElement | undefined = $state();

  // ── Header: mode + auto-route ──────────────────────────────────────────
  const MODES: Array<{ value: 1 | 2 | 3; label: string }> = [
    { value: 1, label: 'Manual' },
    { value: 2, label: 'Mirror' },
    { value: 3, label: 'Extend' },
  ];
  function setMode(v: 1 | 2 | 3) { publishAnalog(SIGNALS.routingMode, v); }
  function toggleAutoRoute() { publishDigital(SIGNALS.autoRouteEnable, !$autoRouteEnableFb); }

  // ── Marker / sidebar-card tap handlers ─────────────────────────────────
  function openPopoverFor(displayId: DisplayId, markerEl: HTMLElement): void {
    if (!planCellEl) return;
    const planRect = planCellEl.getBoundingClientRect();
    const markerRect = markerEl.getBoundingClientRect();
    anchor = {
      top: markerRect.top - planRect.top,
      left: markerRect.left - planRect.left,
      width: markerRect.width,
      height: markerRect.height,
      containerHeight: planRect.height,
      containerWidth: planRect.width,
    };
    openDisplay = displayId;
  }

  function onMarkerTap(displayId: DisplayId, el: HTMLElement) {
    openPopoverFor(displayId, el);
  }

  function onSidebarTap(displayId: DisplayId) {
    // Anchor the popover to the matching plan marker, not the sidebar card,
    // so the visual focus stays on the room view.
    if (!planCellEl) return;
    const markerEl = planCellEl.querySelector(`.marker[data-display="${displayId}"]`) as HTMLElement | null;
    if (markerEl) openPopoverFor(displayId, markerEl);
  }

  function closePopover() {
    openDisplay = null;
    anchor = null;
  }

  // ── Popover actions ────────────────────────────────────────────────────
  function onSelectSource(sourceId: SourceId): void {
    if (!openDisplay) return;
    routeSource(sourceId, openDisplay);
    closePopover();
  }

  function onMirrorD1FromPopover(): void {
    if (!openDisplay || openDisplay === 'd1') { closePopover(); return; }
    const d1Source = srcFromFb($display1SourceFb);
    if (!d1Source) { closePopover(); return; }
    routeSource(d1Source, openDisplay);
    closePopover();
  }

  function onClearFromPopover(): void {
    if (!openDisplay) return;
    clearDisplay(openDisplay);
    closePopover();
  }

  // ── Click-outside disarm ───────────────────────────────────────────────
  function onDocPointerDown(e: PointerEvent) {
    if (!openDisplay) return;
    const target = e.target as Element | null;
    if (!target) return;
    if (target.closest('.popover') || target.closest('.marker') || target.closest('.sd-row')) return;
    closePopover();
  }

  onMount(() => {
    document.addEventListener('pointerdown', onDocPointerDown);
    return () => document.removeEventListener('pointerdown', onDocPointerDown);
  });

  // ── Audio-follows hint (read-only, sidebar) ─────────────────────────────
  let audioFollowsLabel = $derived.by(() => {
    const d1 = srcFromFb($display1SourceFb);
    return d1 ? `D1 · ${SOURCES[d1].label}` : 'D1 · —';
  });

  // ── Outside signage (D5) — sidebar source picker (off the room map) ─────
  const SIGNAGE_SOURCES: SourceId[] = ['roomPc', 'extPc', 'airMedia', 'laptop'];
  let signageSource = $derived(srcFromFb($display5SourceFb));
  function onSignagePick(sourceId: SourceId): void {
    if (signageSource === sourceId) clearSignage();
    else routeSignage(sourceId);
  }

  // ── USB peripheral host (USB-SW-400) ────────────────────────────────────
  const USB_HOST_IDS: UsbHostId[] = ['roomPc', 'airMedia', 'laptop'];
  let usbHost = $derived(usbHostFromFb($usbHostSelectFb));
</script>

<svelte:head>
  <title>{ROOM_NAME} CH5 Panel — Display Routing</title>
</svelte:head>

<div class="routing-page">
  <!-- HEADER ──────────────────────────────────────────────────────────── -->
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
    <span class="eyebrow">Display Routing · Live Map</span>
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

  <!-- MAIN: plan + sidebar ─────────────────────────────────────────────── -->
  <div class="routing-main">
    <div class="plan-cell" bind:this={planCellEl}>
      <RoomPlan
        displays={displays}
        openDisplay={openDisplay}
        onMarkerTap={onMarkerTap}
      />

      {#if openDisplay && anchor}
        {@const d = displays.find(x => x.id === openDisplay)}
        {#if d}
          <SourcePopover
            displayId={d.id}
            displayLabel={d.label}
            activeSource={d.activeSource}
            anchor={anchor}
            canMirrorD1={d.id !== 'd1'}
            onSelectSource={onSelectSource}
            onMirrorD1={onMirrorD1FromPopover}
            onClear={onClearFromPopover}
            onClose={closePopover}
          />
        {/if}
      {/if}
    </div>

    <aside class="status-cell">
      <span class="side-h">Display Status</span>
      <div class="side-disp">
        {#each displays as d (d.id)}
          <DisplayStatusCard
            displayId={d.id}
            label={d.label}
            spec={d.spec}
            activeSource={d.activeSource}
            powerOn={d.powerOn}
            selected={openDisplay === d.id}
            onTap={onSidebarTap}
          />
        {/each}
      </div>

      <div class="side-aud">
        <span class="side-h">Audio Source</span>
        <div class="aud-row">
          <span class="aud-label">Follows</span>
          <span class="aud-val">{audioFollowsLabel}</span>
        </div>
        <div class="aud-hint">Audio always follows D1's routed source.</div>
      </div>

      <!-- USB peripheral host (USB-SW-400) — camera + mic follow the host -->
      <div class="side-usb">
        <span class="side-h">USB Host</span>
        <div class="usb-row">
          {#each USB_HOST_IDS as h}
            <button
              type="button"
              class="usb-btn"
              class:active={usbHost === h}
              aria-pressed={usbHost === h}
              onclick={() => selectUsbHost(h)}
            >{USB_HOSTS[h].label}</button>
          {/each}
        </div>
        <div class="aud-hint">Camera + room mic follow the selected host. Default: Room PC.</div>
      </div>

      <!-- Outside signage (D5) — independent of the in-room displays -->
      <div class="side-sign">
        <span class="side-h">Outside Signage</span>
        <div class="sign-row">
          {#each SIGNAGE_SOURCES as s}
            <button
              type="button"
              class="sign-btn"
              class:active={signageSource === s}
              aria-pressed={signageSource === s}
              onclick={() => onSignagePick(s)}
            >{SOURCES[s].label}</button>
          {/each}
          <button
            type="button"
            class="sign-btn clear"
            class:active={signageSource === null}
            onclick={() => clearSignage()}
          >Off</button>
        </div>
      </div>
    </aside>
  </div>

  <Aa140Footer />
</div>

<style>
  .routing-page {
    width: 100%;
    height: 100%;
    display: grid;
    /* Footer row sized for AppFooter (mics min-height 96px + chrome). */
    grid-template-rows: 60px 1fr 124px;
    gap: 10px;
    padding: 10px;
    box-sizing: border-box;
  }

  /* ── Header (matches existing matrix-page header styling) ───────────── */
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

  .room { font-size: 18px; font-weight: 900; color: var(--color-copy); }
  .sep  { width: 1px; height: 20px; background: var(--color-border); }
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
    font-family: inherit;
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
    font-family: inherit;
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
    animation: dr-auto-pulse 2s ease-in-out infinite;
  }
  @keyframes dr-auto-pulse {
    0%, 100% { opacity: 1; }
    50%      { opacity: 0.4; }
  }

  /* ── Main grid: plan + sidebar ──────────────────────────────────────── */
  .routing-main {
    display: grid;
    grid-template-columns: 1fr 300px;
    gap: 10px;
    min-height: 0;
  }

  /* The .plan-cell is the popover's positioning context. RoomPlan fills it,
     and SourcePopover absolute-positions itself inside, anchored against
     marker rects. */
  .plan-cell {
    position: relative;
    min-height: 0;
  }

  .status-cell {
    background: var(--color-panel);
    border: 0.5px solid var(--color-border);
    border-radius: 14px;
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 14px;
    overflow: hidden;
    min-height: 0;
  }

  .side-h {
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.22em;
    text-transform: uppercase;
    color: var(--color-copy-muted);
  }

  .side-disp {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .side-aud {
    border-top: 0.5px solid var(--color-border);
    padding-top: 14px;
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .aud-row {
    display: flex;
    align-items: baseline;
    gap: 10px;
  }

  .aud-label {
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--color-copy-soft);
    flex-shrink: 0;
  }

  .aud-val {
    font-size: 14px;
    font-weight: 800;
    color: var(--color-copy);
  }

  .aud-hint {
    font-size: 10px;
    color: var(--color-copy-muted);
    line-height: 1.4;
  }

  /* ── USB host + Signage sidebar sections ────────────────────────────── */
  .side-usb, .side-sign {
    border-top: 0.5px solid var(--color-border);
    padding-top: 14px;
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .usb-row, .sign-row {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }

  .usb-btn, .sign-btn {
    flex: 1 1 auto;
    min-width: 64px;
    padding: 8px 10px;
    border-radius: 8px;
    border: 0.5px solid var(--color-border);
    background: rgba(30, 41, 59, 0.5);
    color: var(--color-copy-soft);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.02em;
    cursor: pointer;
    font-family: inherit;
    transition: background 110ms ease, color 110ms ease, border-color 110ms ease;
  }
  .usb-btn:hover, .sign-btn:hover {
    background: rgba(51, 65, 85, 0.7);
    color: var(--color-copy);
  }
  .usb-btn.active {
    background: rgba(56, 189, 248, 0.14);
    border-color: var(--color-accent-soft);
    color: var(--color-accent);
  }
  .sign-btn.active {
    background: rgba(140, 29, 64, 0.22);
    border-color: rgba(140, 29, 64, 0.55);
    color: #f7b7c8;
  }
  .sign-btn.clear.active {
    background: rgba(100, 116, 139, 0.18);
    border-color: var(--color-border);
    color: var(--color-copy-soft);
  }

  @media (prefers-reduced-motion: reduce) {
    .auto-dot.on { animation: none; }
    .back-btn, .mode-btn, .auto-chip { transition: none; }
  }
</style>
