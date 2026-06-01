<script lang="ts">
  import { onMount } from 'svelte';
  import { publishAnalog, publishDigital, pulseDigital } from '../lib/CrComLib';
  import { SIGNALS, ROOM_NAME } from '../lib/contract';
  import {
    panelOnline,
    camPresenterFramingFb, camGroupFramingFb, camUsbOutputFb, camPresetZoneFb, camTrackingProfileFb,
    camPanPos, camTiltPos, camZoomPos,
  } from '../lib/stores/signals';
  import { goToPage, currentPage } from '../lib/stores/page';
  import { CAMERAS, rtspMain, CAM_USER, CAM_PASS, type Camera } from '../lib/cameras';
  import PresetButton from '../components/PresetButton.svelte';

  let activeCamera: Camera = $state(CAMERAS[0]);

  // I20 (Presenter cam) gets the I20-only controls (zones/profiles).
  let isPresenterCam = $derived(activeCamera.id === 'front');
  // Raw VISCA ushort -> signed 16-bit for pan/tilt display.
  const signed = (v: number) => (v > 32767 ? v - 65536 : v);

  // Zoom position -> optical ratio (per model), interpolated from the IV-CAM tables.
  const ZOOM_TABLE: Record<'i12' | 'i20', Array<[number, number]>> = {
    i12: [[0,1],[0x1982,2],[0x24E2,3],[0x2BC9,4],[0x3099,5],[0x343D,6],[0x3724,7],[0x3988,8],[0x3B8B,9],[0x3D43,10],[0x3EBB,11],[0x4000,12]],
    i20: [[0,1],[0x1851,2],[0x22BE,3],[0x28F6,4],[0x2D45,5],[0x3086,6],[0x3320,7],[0x3549,8],[0x371E,9],[0x38B3,10],[0x3A12,11],[0x3B42,12],[0x3C47,13],[0x3D25,14],[0x3DDF,15],[0x3E7B,16],[0x3EFB,17],[0x3F64,18],[0x3FBA,19],[0x4000,20]],
  };
  function zoomRatio(pos: number, model: 'i12' | 'i20'): string {
    const t = ZOOM_TABLE[model];
    if (pos <= t[0][0]) return '1.0×';
    for (let i = 0; i < t.length - 1; i++) {
      const [p0, r0] = t[i], [p1, r1] = t[i + 1];
      if (pos <= p1) return (r0 + ((pos - p0) / (p1 - p0)) * (r1 - r0)).toFixed(1) + '×';
    }
    return t[t.length - 1][1].toFixed(1) + '×';
  }
  let zoomX = $derived(zoomRatio($camZoomPos, activeCamera.model));

  const presets = [
    { idx: 1, name: 'Default' },
    { idx: 2, name: 'Primary' },
    { idx: 3, name: 'Secondary' },
  ];

  // Preview dock (browser-dev only)
  const BASE_WIDTH = 1280;
  const BASE_HEIGHT = 800;
  const DEVICE_PROFILES = {
    auto: null,
    tsw770: { width: 1280, height: 800, label: 'TSW-770' },
    tsw1070: { width: 1920, height: 1200, label: 'TSW-1070' }
  } as const;
  let previewMode: keyof typeof DEVICE_PROFILES = $state('auto');
  let viewportLabel = $state(`${BASE_WIDTH}x${BASE_HEIGHT}`);
  let scaleLabel = $state('1.00x');
  let profileLabel = $state('Auto');
  let showPreviewDock = $state(false);
  let applyViewport = () => {};

  // === ch5-video positioning ===
  // The ch5-video element lives at body level (build.mjs); we just position it
  // to match this page's .video-window placeholder. This sidesteps the issue
  // where a deeply-nested ch5-video doesn't receive a usable bounding rect.
  // Pattern lifted from c:/Users/scale/CascadeProjects/1Beyond/1beyond-multicam/
  // src/components/CameraPreview.svelte (proven working).
  let videoWindow: HTMLDivElement | undefined = $state();
  let resizeObs: ResizeObserver | null = null;

  function syncVideoToWindow() {
    const vid = document.getElementById('cam-preview') as HTMLElement | null;
    if (!vid || !videoWindow) return;
    const r = videoWindow.getBoundingClientRect();
    vid.style.top = `${r.top}px`;
    vid.style.left = `${r.left}px`;
    vid.style.width = `${r.width}px`;
    vid.style.height = `${r.height}px`;
    vid.style.display = 'block';
  }

  function hideVideo() {
    const vid = document.getElementById('cam-preview') as HTMLElement | null;
    if (vid) vid.style.display = 'none';
  }

  // ch5-video reads url/sourcetype only at CONSTRUCTION — runtime setAttribute()
  // is silently ignored on Crestron's Android Chromium. To switch the camera feed
  // we REPLACE the #cam-preview element via insertAdjacentHTML (the firmware's
  // normal custom-element upgrade path), then reposition it. Proven technique from
  // 1beyond-multicam/CameraPreview.svelte.
  function mountCameraStream(cam: Camera) {
    const old = document.getElementById('cam-preview');
    if (!old || !old.parentNode) return;
    const url = rtspMain(cam);
    const html =
      '<ch5-video id="cam-preview" sourcetype="Network"' +
      ` url="${url}" userid="${CAM_USER}" password="${CAM_PASS}"` +
      ' size="custom" aspectratio="16:9" stretch="true" componentwasresized="true"' +
      ' receivestateplay="CamPlay" sendeventstate="CamState" sendeventerrorcode="CamError"' +
      ' sendeventerrormessage="CamErrorMsg" sendeventresolution="CamResolution"' +
      ' style="display:block;position:fixed;z-index:0;"></ch5-video>';
    old.insertAdjacentHTML('afterend', html);
    old.remove();
    requestAnimationFrame(syncVideoToWindow);
  }

  // Hide the video element BEFORE the route change paints, otherwise the
  // native cutout lingers on top of the next page for a frame or two.
  // Svelte's onDestroy fires after the page swap, so we tear down here first.
  function leaveCameras(target: 'home' | 'settings' = 'home') {
    hideVideo();
    goToPage(target);
  }

  function selectCamera(cam: Camera) {
    activeCamera = cam;
    publishAnalog(SIGNALS.cameraSelect, cam.selectIndex);
    mountCameraStream(cam);   // swap the live feed to the selected camera
  }

  // PTZ press-and-hold (rising edge starts movement, falling edge stops)
  function ptzStart(dir: 'up' | 'down' | 'left' | 'right') {
    const map = {
      up: SIGNALS.ptzUp, down: SIGNALS.ptzDown,
      left: SIGNALS.ptzLeft, right: SIGNALS.ptzRight,
    };
    publishDigital(map[dir], true);
  }
  function ptzEnd(dir: 'up' | 'down' | 'left' | 'right') {
    const map = {
      up: SIGNALS.ptzUp, down: SIGNALS.ptzDown,
      left: SIGNALS.ptzLeft, right: SIGNALS.ptzRight,
    };
    publishDigital(map[dir], false);
  }

  // Zoom press-and-hold
  function zoomStart(dir: 'in' | 'out') {
    publishDigital(dir === 'in' ? SIGNALS.zoomIn : SIGNALS.zoomOut, true);
  }
  function zoomEnd(dir: 'in' | 'out') {
    publishDigital(dir === 'in' ? SIGNALS.zoomIn : SIGNALS.zoomOut, false);
  }

  function recallPreset(i: number) { publishAnalog(SIGNALS.shotPresetRecall, i); }
  function savePreset(i: number)   { publishAnalog(SIGNALS.shotPresetSave, i); }

  function sendToVtc() { pulseDigital(SIGNALS.camSendToVtc); }

  // Framing / Q&A USB switch / shots (v2)
  function togglePresenter() { publishDigital(SIGNALS.camPresenterFraming, !$camPresenterFramingFb); }
  function toggleGroup() { publishDigital(SIGNALS.camGroupFraming, !$camGroupFramingFb); }
  function setUsbOutput(n: 1 | 2 | 3) { publishAnalog(SIGNALS.camUsbOutput, n); }
  function setZone(n: 1 | 2 | 3 | 4) { publishAnalog(SIGNALS.camPresetZone, n); }
  function setProfile(n: 1 | 2 | 3 | 4) { publishAnalog(SIGNALS.camTrackingProfile, n); }
  function recallHome() { pulseDigital(SIGNALS.camHomeShot); }
  function recallTrackingShot() { pulseDigital(SIGNALS.camTrackingShot); }

  function setPreviewMode(mode: keyof typeof DEVICE_PROFILES) {
    previewMode = mode;
    applyViewport();
  }

  onMount(() => {
    showPreviewDock = ['127.0.0.1', 'localhost'].includes(window.location.hostname);
    applyViewport = () => {
      const profile = DEVICE_PROFILES[previewMode];
      const w = profile?.width ?? window.innerWidth;
      const h = profile?.height ?? window.innerHeight;
      const scale = Math.min(w / BASE_WIDTH, h / BASE_HEIGHT);
      document.documentElement.style.setProperty('--panel-scale', scale.toString());
      document.documentElement.style.setProperty('--viewport-width', `${w}px`);
      document.documentElement.style.setProperty('--viewport-height', `${h}px`);
      viewportLabel = `${w}x${h}`;
      scaleLabel = `${scale.toFixed(2)}x`;
      profileLabel = profile?.label ?? 'Auto';
    };
    applyViewport();
    window.addEventListener('resize', applyViewport);

    if (videoWindow) {
      syncVideoToWindow();
      resizeObs = new ResizeObserver(syncVideoToWindow);
      resizeObs.observe(videoWindow);
      // Mount the active camera's RTSP feed (replaces the static stub element).
      mountCameraStream(activeCamera);
    }

    // Page-change pre-emption: hide the body-level ch5-video the instant the
    // page store flips away from 'cameras', BEFORE Svelte renders the new
    // page. onDestroy fires after that paint and was leaving the cutout
    // visible on top of Home/Settings for a frame.
    const unsubPage = currentPage.subscribe((p) => {
      if (p !== 'cameras') hideVideo();
    });

    return () => {
      window.removeEventListener('resize', applyViewport);
      resizeObs?.disconnect();
      resizeObs = null;
      unsubPage();
      hideVideo();
    };
  });
</script>

<svelte:head>
  <title>{ROOM_NAME} Cameras</title>
</svelte:head>

<div class="panel-stage">
  <div class="app-shell layout-cameras">

    <header class="app-header glass-card">
      <button class="btn back-btn" onclick={() => leaveCameras('home')}>← Home</button>
      <div class="header-copy">
        <p class="eyebrow">CH5 Touch Panel</p>
        <h1>{ROOM_NAME} — Cameras</h1>
      </div>
      <div class="status-pill" class:online={$panelOnline}>
        <span class="status-dot"></span>
        <span>{$panelOnline ? 'Online' : 'Offline'}</span>
      </div>
    </header>

    <div class="work-area">

      <!-- Camera selector sidebar -->
      <div class="glass-card camera-sidebar">
        <p class="panel-label">Cameras</p>
        {#each CAMERAS as cam}
          <button
            class="btn camera-select-btn"
            class:active={activeCamera.id === cam.id}
            onclick={() => selectCamera(cam)}
            aria-pressed={activeCamera.id === cam.id}
          >
            <strong>{cam.label}</strong>
            <em>{cam.model}</em>
          </button>
        {/each}
      </div>

      <!-- Live preview + transparent PTZ overlay -->
      <div class="glass-card preview-panel">
        <p class="panel-label">Preview — {activeCamera.label} ({activeCamera.model})</p>
        <!--
          .video-window is a positioning HINT for the body-level <ch5-video>
          (declared in build.mjs). The Svelte mount (above) syncs the body
          element's inline top/left/width/height to this div's bounding rect.
          The PTZ overlay renders ON TOP via z-index because ch5-video sits at
          z-index:0 of the body and the Svelte UI is above it at z-index:1.
        -->
        <div class="video-container" bind:this={videoWindow}>
          <div class="ptz-overlay">
            <button
              class="icon-btn ptz-btn ptz-up"
              aria-label="Tilt up"
              onmousedown={() => ptzStart('up')}
              onmouseup={() => ptzEnd('up')}
              onmouseleave={() => ptzEnd('up')}
              ontouchstart={(e) => { e.preventDefault(); ptzStart('up'); }}
              ontouchend={(e) => { e.preventDefault(); ptzEnd('up'); }}
              ontouchcancel={() => ptzEnd('up')}
            >
              <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
                <path d="M12 5l7 8H5z" fill="currentColor"/>
              </svg>
            </button>
            <button
              class="icon-btn ptz-btn ptz-left"
              aria-label="Pan left"
              onmousedown={() => ptzStart('left')}
              onmouseup={() => ptzEnd('left')}
              onmouseleave={() => ptzEnd('left')}
              ontouchstart={(e) => { e.preventDefault(); ptzStart('left'); }}
              ontouchend={(e) => { e.preventDefault(); ptzEnd('left'); }}
              ontouchcancel={() => ptzEnd('left')}
            >
              <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
                <path d="M5 12l8-7v14z" fill="currentColor"/>
              </svg>
            </button>
            <button
              class="icon-btn ptz-btn ptz-right"
              aria-label="Pan right"
              onmousedown={() => ptzStart('right')}
              onmouseup={() => ptzEnd('right')}
              onmouseleave={() => ptzEnd('right')}
              ontouchstart={(e) => { e.preventDefault(); ptzStart('right'); }}
              ontouchend={(e) => { e.preventDefault(); ptzEnd('right'); }}
              ontouchcancel={() => ptzEnd('right')}
            >
              <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
                <path d="M19 12l-8-7v14z" fill="currentColor"/>
              </svg>
            </button>
            <button
              class="icon-btn ptz-btn ptz-down"
              aria-label="Tilt down"
              onmousedown={() => ptzStart('down')}
              onmouseup={() => ptzEnd('down')}
              onmouseleave={() => ptzEnd('down')}
              ontouchstart={(e) => { e.preventDefault(); ptzStart('down'); }}
              ontouchend={(e) => { e.preventDefault(); ptzEnd('down'); }}
              ontouchcancel={() => ptzEnd('down')}
            >
              <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
                <path d="M12 19l-7-8h14z" fill="currentColor"/>
              </svg>
            </button>
          </div>
        </div>
        <div class="coords-bar">
          <div class="coord"><span class="ck">Pan</span><span class="cv">{signed($camPanPos)}</span></div>
          <div class="coord"><span class="ck">Tilt</span><span class="cv">{signed($camTiltPos)}</span></div>
          <div class="coord"><span class="ck">Zoom</span><span class="cv">{$camZoomPos}</span></div>
          <div class="coord"><span class="ck">Optical</span><span class="cv">{zoomX}</span></div>
          <span class="coord-live">● live</span>
        </div>
      </div>

      <!-- Right-side controls -->
      <div class="glass-card controls-panel">
        <!-- Prominent zoom -->
        <div class="zoom-prom">
          <span class="block-label">Zoom</span>
          <div class="zoom-prom-row">
            <button
              class="zoom-big"
              aria-label="Zoom out"
              onmousedown={() => zoomStart('out')}
              onmouseup={() => zoomEnd('out')}
              onmouseleave={() => zoomEnd('out')}
              ontouchstart={(e) => { e.preventDefault(); zoomStart('out'); }}
              ontouchend={(e) => { e.preventDefault(); zoomEnd('out'); }}
              ontouchcancel={() => zoomEnd('out')}
            >−</button>
            <button
              class="zoom-big"
              aria-label="Zoom in"
              onmousedown={() => zoomStart('in')}
              onmouseup={() => zoomEnd('in')}
              onmouseleave={() => zoomEnd('in')}
              ontouchstart={(e) => { e.preventDefault(); zoomStart('in'); }}
              ontouchend={(e) => { e.preventDefault(); zoomEnd('in'); }}
              ontouchcancel={() => zoomEnd('in')}
            >+</button>
          </div>
          <span class="zoom-cap">press &amp; hold</span>
        </div>

        <!-- Framing on the I20 — independent presenter (live) + group (cached) tracking -->
        <div class="ctl-sec">
          <p class="block-label">Framing · I20</p>
          <button class="btn toggle-row" class:on={$camPresenterFramingFb} onclick={togglePresenter} aria-pressed={$camPresenterFramingFb}>
            <span>Presenter Tracking</span>
            <span class="tg-state">{$camPresenterFramingFb ? '● ON' : 'OFF'}</span>
          </button>
          <button class="btn toggle-row" class:on={$camGroupFramingFb} onclick={toggleGroup} aria-pressed={$camGroupFramingFb}>
            <span>Group Tracking</span>
            <span class="tg-state">{$camGroupFramingFb ? 'ON' : 'OFF'}</span>
          </button>
        </div>

        <!-- Active camera switch (I12 host: which view goes out the USB) -->
        <div class="ctl-sec">
          <p class="block-label">Active Camera · I12 switch</p>
          <div class="seg">
            <button class="seg-btn" class:active={$camUsbOutputFb === 1} onclick={() => setUsbOutput(1)}>Presenter</button>
            <button class="seg-btn" class:active={$camUsbOutputFb === 2} onclick={() => setUsbOutput(2)}>Group</button>
            <button class="seg-btn" class:active={$camUsbOutputFb === 3} onclick={() => setUsbOutput(3)}>Auto</button>
          </div>
          <p class="ctl-hint">Which view the room PC / codec sees. Auto = Intelligent Switching (multicam).</p>
        </div>

        <button class="btn vtc-btn" onclick={sendToVtc}>Send to VTC</button>
      </div>

    </div>

    <!-- Bottom row: shot presets + (I20-only) zones / profiles -->
    <div class="bottom-row">
      <div class="presets-row glass-card">
        <p class="block-label presets-label">Shot Presets</p>
        <div class="presets-grid">
          {#each presets as p}
            <PresetButton
              label={p.name}
              onRecall={() => recallPreset(p.idx)}
              onSave={() => savePreset(p.idx)}
            />
          {/each}
          <button class="btn shot-btn" onclick={recallHome}>Home</button>
          <button class="btn shot-btn" onclick={recallTrackingShot}>Tracking Shot</button>
        </div>
      </div>

      {#if isPresenterCam}
        <div class="i20-row glass-card">
          <div class="i20-block">
            <p class="block-label">Preset Zones <span class="i20-tag">I20</span></p>
            <div class="radio">
              {#each [1, 2, 3, 4] as n}
                <button class="btn radio-btn" class:active={$camPresetZoneFb === n} onclick={() => setZone(n as 1|2|3|4)}>{n}</button>
              {/each}
            </div>
          </div>
          <div class="i20-block">
            <p class="block-label">Tracking Profiles <span class="i20-tag">I20</span></p>
            <div class="radio">
              {#each [1, 2, 3, 4] as n}
                <button class="btn radio-btn" class:active={$camTrackingProfileFb === n} onclick={() => setProfile(n as 1|2|3|4)}>{n}</button>
              {/each}
            </div>
          </div>
        </div>
      {/if}
    </div>

  </div>

  {#if showPreviewDock}
    <aside class="preview-dock glass-card" aria-label="Local resolution preview controls">
      <div class="preview-copy">
        <strong>{profileLabel}</strong>
        <span>{viewportLabel} · {scaleLabel}</span>
      </div>
      <div class="preview-actions">
        <button class="btn preview-button" class:active={previewMode === 'auto'} onclick={() => setPreviewMode('auto')}>Auto</button>
        <button class="btn preview-button" class:active={previewMode === 'tsw770'} onclick={() => setPreviewMode('tsw770')}>770</button>
        <button class="btn preview-button" class:active={previewMode === 'tsw1070'} onclick={() => setPreviewMode('tsw1070')}>1070</button>
      </div>
    </aside>
  {/if}
</div>

<style>
  .layout-cameras {
    display: grid;
    /* Tightened from 92/168 + 20px padding + 16px gap so the work-area row gets
       ~64 more px of height for the camera preview. */
    grid-template-rows: 72px 1fr 138px;
    gap: 12px;
    width: 100%;
    height: 100%;
    padding: 12px;
  }
  .back-btn { min-height: 56px; padding: 0 18px; font-size: 13px; margin-right: 16px; }

  .work-area {
    display: grid;
    /* Tightened sidebar 180→140 and controls 240→200 + gap 16→12 to give the
       middle column ~56 more px for the video. */
    grid-template-columns: 140px 1fr 200px;
    gap: 12px;
    min-height: 0;
  }

  .camera-sidebar {
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .panel-label,
  .block-label {
    margin: 0 0 4px;
    color: var(--color-copy-muted);
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.18em;
    text-transform: uppercase;
  }

  .camera-select-btn {
    text-align: left;
    padding: 12px 14px;
    height: auto;
    min-height: 56px;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 2px;
  }
  .camera-select-btn strong { font-size: 16px; font-weight: 700; }
  .camera-select-btn em { font-style: normal; font-size: 12px; color: var(--color-copy-muted); }

  /*
    CH5 Video Specialist hard rule: any CSS bg on any ANCESTOR of ch5-video
    paints OPAQUE over the native cutout, making the stream invisible.
    .preview-panel inherits a dark gradient from .glass-card and is an ancestor
    of ch5-video, so we override it transparent here. The visual frame is still
    defined by the .glass-card border. Other glass-cards on this page (sidebar,
    controls-panel, presets-row) are NOT ancestors of ch5-video, so they keep
    their gradients.
  */
  .preview-panel {
    /* tight padding — every px the panel keeps for itself is a px the video
       cutout doesn't get. Keep enough horizontal padding to balance the label,
       almost none vertical. */
    padding: 6px 14px 10px;
    display: grid;
    grid-template-rows: auto 1fr;
    gap: 6px;
    min-height: 0;
    background: transparent !important;
    /* The body-level ch5-video sits BEHIND the Svelte UI. backdrop-filter
       (inherited from .glass-card) would blur the video; disable it here. */
    backdrop-filter: none;
    box-shadow: none;
  }

  /*
    Make the bordered container itself 16:9 so the ch5-video cutout (also 16:9
    via aspectratio="16:9") fills it edge-to-edge with no internal letterbox.
    height:100% claims the full 1fr cell; aspect-ratio + max-width:100% then
    fits the largest 16:9 box that fits, centered in the cell.
  */
  .video-container {
    position: relative;
    aspect-ratio: 16 / 9;
    height: 100%;
    width: auto;
    max-width: 100%;
    place-self: center;
    min-height: 0;
    border-radius: var(--radius-button);
    overflow: hidden;
    background: transparent;  /* was #050d1a — opaque, blocked the cutout */
    border: 0.5px solid var(--color-border);
  }
  .video-container :global(ch5-video) {
    position: absolute;
    inset: 0;
    background: transparent;
    width: 100%;
    height: 100%;
    display: block;
  }

  /* Transparent PTZ overlay buttons — flat white icons, no colored borders. */
  .ptz-overlay {
    position: absolute;
    inset: 0;
    pointer-events: none;
  }
  .ptz-btn {
    pointer-events: auto;
    position: absolute;
    width: 60px;
    height: 60px;
    color: #ffffff;
    background: transparent;
    border: none;
    border-radius: 50%;
  }
  .ptz-btn:active {
    background: rgba(255, 255, 255, 0.08);
    transform: scale(0.92);
    box-shadow: inset 0 0 0 1px var(--color-accent);
  }
  .ptz-up    { top: 12px; left: 50%; transform: translateX(-50%); }
  .ptz-down  { bottom: 12px; left: 50%; transform: translateX(-50%); }
  .ptz-up:active    { transform: translateX(-50%) scale(0.92); }
  .ptz-down:active  { transform: translateX(-50%) scale(0.92); }
  .ptz-left  { left: 12px; top: 50%; transform: translateY(-50%); }
  .ptz-right { right: 12px; top: 50%; transform: translateY(-50%); }
  .ptz-left:active  { transform: translateY(-50%) scale(0.92); }
  .ptz-right:active { transform: translateY(-50%) scale(0.92); }

  .controls-panel {
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 18px;
  }
  .vtc-btn {
    min-height: 52px;
    font-size: 14px;
    font-weight: 700;
  }

  .presets-row {
    display: flex;
    gap: 16px;
    padding: 16px 22px;
    align-items: stretch;
  }
  .presets-label {
    writing-mode: vertical-lr;
    transform: rotate(180deg);
    align-self: stretch;
    flex-shrink: 0;
  }
  .presets-grid {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 10px;
    flex: 1;
  }

  /* ── v2 controls ──────────────────────────────────────────────────── */
  .coords-bar {
    display: flex; align-items: center; gap: 14px; justify-content: center;
    padding: 6px 10px; border-radius: var(--radius-button);
    background: rgba(15, 23, 42, 0.6); border: 0.5px solid var(--color-border);
    font-family: ui-monospace, SFMono-Regular, monospace;
  }
  .coord { display: flex; flex-direction: column; align-items: center; min-width: 80px; }
  .coord .ck { font-size: 9px; letter-spacing: 0.12em; text-transform: uppercase; color: var(--color-copy-muted); }
  .coord .cv { font-size: 17px; font-weight: 800; color: var(--color-accent); font-variant-numeric: tabular-nums; }
  .coord-live { margin-left: auto; font-size: 9px; color: #34d399; }

  .zoom-prom { display: flex; flex-direction: column; gap: 4px; }
  .zoom-prom-row { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
  .zoom-big {
    height: 72px; border-radius: var(--radius-button);
    border: 0.5px solid rgba(56, 189, 248, 0.4); background: rgba(56, 189, 248, 0.12);
    color: var(--color-accent); font-size: 34px; font-weight: 800; cursor: pointer;
  }
  .zoom-big:active { background: rgba(56, 189, 248, 0.28); transform: scale(0.97); }
  .zoom-cap { text-align: center; font-size: 9px; letter-spacing: 0.1em; text-transform: uppercase; color: var(--color-copy-muted); }

  .ctl-sec { display: flex; flex-direction: column; gap: 6px; }
  .toggle-row {
    display: flex; align-items: center; justify-content: space-between;
    min-height: 48px; padding: 0 14px; font-weight: 700; font-size: 14px;
  }
  .toggle-row.on { background: rgba(34, 197, 94, 0.14); border-color: rgba(34, 197, 94, 0.5); color: #86efac; }
  .toggle-row .tg-state { font-size: 11px; font-weight: 800; }

  .seg { display: flex; border: 0.5px solid var(--color-border); border-radius: var(--radius-button); overflow: hidden; }
  .seg-btn {
    flex: 1; padding: 11px 4px; border: none; border-right: 0.5px solid var(--color-border);
    background: rgba(30, 41, 59, 0.5); color: var(--color-copy-soft); font-size: 12px; font-weight: 700; cursor: pointer; font-family: inherit;
  }
  .seg-btn:last-child { border-right: none; }
  .seg-btn.active { background: rgba(56, 189, 248, 0.16); color: var(--color-accent); }
  .ctl-hint { margin: 0; font-size: 9px; color: var(--color-copy-muted); line-height: 1.4; }

  .bottom-row { display: grid; grid-template-columns: 1.4fr 1fr; gap: 12px; min-height: 0; }
  .bottom-row .presets-row { margin: 0; }
  .shot-btn { min-height: 0; font-size: 13px; font-weight: 700; }
  .i20-row { display: flex; gap: 18px; padding: 14px 18px; align-items: center; }
  .i20-block { flex: 1; display: flex; flex-direction: column; gap: 8px; }
  .i20-tag { font-size: 8px; color: #f7b7c8; font-weight: 800; letter-spacing: 0.1em; margin-left: 4px; }
  .radio { display: flex; gap: 6px; }
  .radio-btn { flex: 1; min-height: 42px; font-size: 15px; font-weight: 800; }
  .radio-btn.active { background: rgba(56, 189, 248, 0.16); border-color: var(--color-accent); color: var(--color-accent); }
</style>
