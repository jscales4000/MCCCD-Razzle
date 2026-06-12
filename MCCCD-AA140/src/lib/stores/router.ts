import { writable, get } from 'svelte/store';
import { publishAnalog } from '../CrComLib';
import { SIGNALS } from '../contract';
import {
  display1SourceFb,
  display2SourceFb,
  display3SourceFb,
  display4SourceFb,
} from './signals';

// ── Types ──────────────────────────────────────────────────────────────
export type SourceId = 'roomPc' | 'extPc' | 'airMedia' | 'laptop';
export type DisplayId = 'd1' | 'd2' | 'd3' | 'd4';

export const SOURCES: Record<SourceId, { label: string; value: 1 | 2 | 3 | 4 }> = {
  roomPc:   { label: 'Room PC',  value: 1 },
  extPc:    { label: 'Ext PC',   value: 2 },
  airMedia: { label: 'AirMedia', value: 3 },
  laptop:   { label: 'Laptop',   value: 4 },
};

export const VALUE_TO_SOURCE: Record<number, SourceId> = {
  1: 'roomPc',
  2: 'extPc',
  3: 'airMedia',
  4: 'laptop',
};

/** Canonical analog-FB-value → SourceId lookup (0 / unknown → null). */
export function sourceFromValue(v: number): SourceId | null {
  return VALUE_TO_SOURCE[v] ?? null;
}

// ── Reactive UI state (components subscribe) ───────────────────────────
export const armedSource = writable<SourceId | null>(null);
export const draggingSource = writable<SourceId | null>(null);
export const cloneCoords = writable<{ x: number; y: number }>({ x: 0, y: 0 });

// ── Imperative state (module-private) ──────────────────────────────────
let suppressNextClick = false;
let armedTimeoutId: ReturnType<typeof setTimeout> | null = null;
let pressTimerId: ReturnType<typeof setTimeout> | null = null;
let pressOriginEl: HTMLElement | null = null;
let pressOriginX = 0;
let pressOriginY = 0;
let lastPointerX = 0;
let lastPointerY = 0;
let dragCloneEl: HTMLElement | null = null;
let activePointerId: number | null = null;
let pressCapturedEl: HTMLElement | null = null;

const LONG_PRESS_MS = 250;
// Capacitive touch jitter on the TS-1070 easily exceeds 10px even with a
// stationary finger. 30px tolerates wobble while still rejecting deliberate
// scroll-style drags before the long-press fires.
const MOVE_CANCEL_THRESHOLD = 30;

function panelScale(): number {
  const v = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--panel-scale'));
  return isFinite(v) && v > 0 ? v : 1;
}

// ── Utility: read current routing of a display from feedback stores ────
const FB_BY_DISPLAY = {
  d1: display1SourceFb,
  d2: display2SourceFb,
  d3: display3SourceFb,
  d4: display4SourceFb,
} as const;

const SET_SIGNAL_BY_DISPLAY: Record<DisplayId, string> = {
  d1: SIGNALS.display1Source,
  d2: SIGNALS.display2Source,
  d3: SIGNALS.display3Source,
  d4: SIGNALS.display4Source,
};

export function currentRouting(displayId: DisplayId): SourceId | null {
  const v = get(FB_BY_DISPLAY[displayId]);
  return VALUE_TO_SOURCE[v] ?? null;
}

// ── Clone DOM registration (called by DragCloneOverlay onMount) ────────
export function registerCloneEl(el: HTMLElement | null): void {
  dragCloneEl = el;
}

// ── Tile under pointer ─────────────────────────────────────────────────
export function tileUnderPointer(x: number, y: number): HTMLElement | null {
  const el = document.elementFromPoint(x, y);
  return (el as HTMLElement | null)?.closest('.tile') as HTMLElement | null;
}

// ── State transitions ──────────────────────────────────────────────────
export function armChip(sourceId: SourceId): void {
  if (get(armedSource) === sourceId) { disarm(); return; }
  if (get(armedSource)) disarm();
  armedSource.set(sourceId);
  document.body.classList.add('any-armed');
  armedTimeoutId = setTimeout(() => disarm(), 4000);
}

export function disarm(): void {
  if (!get(armedSource)) return;
  armedSource.set(null);
  if (armedTimeoutId) clearTimeout(armedTimeoutId);
  armedTimeoutId = null;
  document.body.classList.remove('any-armed');
}

export function routeSource(sourceId: SourceId, displayId: DisplayId): void {
  // No-op if already routed there (read from feedback store)
  if (currentRouting(displayId) === sourceId) return;
  const value = SOURCES[sourceId].value;
  publishAnalog(SET_SIGNAL_BY_DISPLAY[displayId], value);
  // Marker / sidebar update from the REAL Display{N}SourceFb feedback that SIMPL
  // writes back once the route is applied (name-based contract). No optimistic
  // mirror — the contract-direction bug that required it is fixed.
}

/** Clear a display's route. Marker clears from real Display{N}SourceFb feedback. */
export function clearDisplay(displayId: DisplayId): void {
  publishAnalog(SET_SIGNAL_BY_DISPLAY[displayId], 0);
}

// ── Home display targeting (display strip on Home) ─────────────────────
export const ALL_DISPLAYS: readonly DisplayId[] = ['d1', 'd2', 'd3', 'd4'] as const;

/** Which displays the next Home source-tap routes to. Defaults to all four,
 *  which keeps the source buttons' historical route-everywhere behavior. */
export const targetDisplays = writable<ReadonlySet<DisplayId>>(new Set(ALL_DISPLAYS));

export function allTargeted(set: ReadonlySet<DisplayId>): boolean {
  return set.size === ALL_DISPLAYS.length;
}

// A narrowed target set is transient by design: it auto-reverts to the
// all-targeted default after a quiet period, mirroring armChip's 4s disarm.
// Without this, someone soloing D4 and walking away leaves a trap where the
// next presenter's source tap silently routes to one display.
let targetResetTimerId: ReturnType<typeof setTimeout> | null = null;
const TARGET_RESET_MS = 10000;

function refreshTargetResetTimer(): void {
  if (targetResetTimerId) clearTimeout(targetResetTimerId);
  targetResetTimerId = setTimeout(() => resetTargetDisplays(), TARGET_RESET_MS);
}

/** Tap semantics for the Home display chips:
 *  - From the all-targeted default, a tap SOLOS that display ("just this one").
 *  - Otherwise taps toggle membership.
 *  - Untoggling the last member reverts to the all-targeted default — the
 *    target set can never be empty. */
export function toggleTargetDisplay(displayId: DisplayId): void {
  const cur = get(targetDisplays);
  let next: Set<DisplayId>;
  if (allTargeted(cur)) {
    next = new Set([displayId]);
  } else {
    next = new Set(cur);
    if (next.has(displayId)) next.delete(displayId);
    else next.add(displayId);
    if (next.size === 0) next = new Set(ALL_DISPLAYS);
  }
  targetDisplays.set(next);
  if (allTargeted(next)) resetTargetDisplays();
  else refreshTargetResetTimer();
}

export function resetTargetDisplays(): void {
  if (targetResetTimerId) clearTimeout(targetResetTimerId);
  targetResetTimerId = null;
  targetDisplays.set(new Set(ALL_DISPLAYS));
}

/** Route a source to the current Home target set, then clear the grouping
 *  back to the all-targeted default. The intended loop: pick displays →
 *  tap a source → it routes → selection resets → pick again. The quiet-period
 *  timer above only covers the "picked displays but never routed" case. */
export function routeSourceToTargets(value: 1 | 2 | 3 | 4): void {
  const targets = get(targetDisplays);
  targets.forEach((d) => publishAnalog(SET_SIGNAL_BY_DISPLAY[d], value));
  resetTargetDisplays();
}

// ── Outside signage (D5) ───────────────────────────────────────────────
// Routed independently and kept OFF the in-room map (the display is outside
// the conference space). Publishes Display5Source; marker/state come from
// the real Display5SourceFb feedback.
export function routeSignage(sourceId: SourceId): void {
  publishAnalog(SIGNALS.display5Source, SOURCES[sourceId].value);
}
export function clearSignage(): void {
  publishAnalog(SIGNALS.display5Source, 0);
}

// ── USB peripheral host (USB-SW-400) ───────────────────────────────────
// One-tap host selection routes the room camera + Shure mic/speaker to the
// chosen host. Independent of video routing. PowerUp default = Room PC.
export type UsbHostId = 'roomPc' | 'airMedia' | 'laptop';
export const USB_HOSTS: Record<UsbHostId, { label: string; value: 1 | 2 | 3 }> = {
  roomPc:   { label: 'Room PC',  value: 1 },
  airMedia: { label: 'AirMedia', value: 2 },
  laptop:   { label: 'Laptop',   value: 3 },
};
const USB_VALUE_TO_HOST: Record<number, UsbHostId> = { 1: 'roomPc', 2: 'airMedia', 3: 'laptop' };
export function usbHostFromFb(v: number): UsbHostId | null {
  return USB_VALUE_TO_HOST[v] ?? null;
}
export function selectUsbHost(host: UsbHostId): void {
  publishAnalog(SIGNALS.usbHostSelect, USB_HOSTS[host].value);
}

export function shouldSuppressClick(): boolean {
  if (!suppressNextClick) return false;
  suppressNextClick = false;
  return true;
}

// ── Drag flow ──────────────────────────────────────────────────────────
export function startDrag(sourceId: SourceId, originEl: HTMLElement, x: number, y: number): void {
  draggingSource.set(sourceId);
  document.body.classList.add('any-armed');
  originEl.classList.add('chip-ghost');
  cloneCoords.set({ x, y });
  // Annotate every tile-slot with the hover hint
  const hint = `Drop to route ${SOURCES[sourceId].label}`;
  document.querySelectorAll('.tile-slot').forEach(slot => {
    (slot as HTMLElement).dataset.hoverHint = hint;
  });
}

function updateHover(x: number, y: number): void {
  const tile = tileUnderPointer(x, y);
  document.querySelectorAll('.tile-slot').forEach(s => s.classList.remove('drop-hovering', 'drop-noop'));
  if (!tile) return;
  const slot = tile.querySelector('.tile-slot');
  if (!slot) return;
  const displayId = tile.dataset.display as DisplayId | undefined;
  if (!displayId) return;
  const dragging = get(draggingSource);
  if (dragging && currentRouting(displayId) === dragging) {
    slot.classList.add('drop-noop');
  } else {
    slot.classList.add('drop-hovering');
  }
}

export function endDrag(x: number, y: number): void {
  const tile = tileUnderPointer(x, y);
  const sourceId = get(draggingSource);
  // Find origin chip in the rail by its data-source attribute
  const originEl = sourceId
    ? (document.querySelector(`.chip[data-source="${sourceId}"]`) as HTMLElement | null)
    : null;

  document.querySelectorAll('.tile-slot').forEach(s => s.classList.remove('drop-hovering', 'drop-noop'));
  document.body.classList.remove('any-armed');

  let dropOnTile: HTMLElement | null = null;
  if (tile && sourceId) {
    const displayId = tile.dataset.display as DisplayId | undefined;
    if (displayId && currentRouting(displayId) !== sourceId) {
      dropOnTile = tile;
    }
  }

  const clone = dragCloneEl;

  if (dropOnTile && sourceId && clone) {
    // PHASE 1: animate clone to slot center
    const slot = dropOnTile.querySelector('.tile-slot') as HTMLElement | null;
    if (slot) {
      const slotRect = slot.getBoundingClientRect();
      const targetX = slotRect.left + slotRect.width / 2 - 40;
      const targetY = slotRect.top + slotRect.height / 2 - 44;
      clone.classList.add('snapping');
      clone.style.transform = `translate(${targetX}px, ${targetY}px) scale(1.0) rotate(0deg)`;
      clone.style.opacity = '0';
    }

    setTimeout(() => {
      // PHASE 2: publish signal (feedback re-renders the slot via DisplayTile)
      const displayId = dropOnTile!.dataset.display as DisplayId;
      routeSource(sourceId, displayId);

      // PHASE 3: tile border flash (animation will retrigger on the live tile)
      dropOnTile!.classList.remove('flash');
      void dropOnTile!.offsetWidth;
      dropOnTile!.classList.add('flash');

      // The thunk on the newly-landed chip happens after Svelte re-renders the
      // tile-slot; we can defer adding the .thunk class one more frame so the
      // .landed-chip exists in the DOM. (Use rAF to wait for next paint.)
      requestAnimationFrame(() => {
        const newLanded = dropOnTile!.querySelector('.landed-chip') as HTMLElement | null;
        if (newLanded) {
          newLanded.classList.remove('thunk');
          void newLanded.offsetWidth;
          newLanded.classList.add('thunk');
        }
      });

      cleanupAfterDrag(originEl);
    }, 180);
  } else {
    // SNAP-BACK — animate the clone back to overlap the rail chip's actual
    // on-screen size. The clone is rendered outside .panel-stage so it doesn't
    // inherit the panel's scale; we apply scale(panelScale) here so the clone
    // visually matches the rail chip during the snap, instead of shrinking
    // below it.
    if (!originEl || !clone) {
      cleanupAfterDrag(originEl);
      return;
    }
    const originRect = originEl.getBoundingClientRect();
    const ps = panelScale();
    // transform-origin defaults to 50% 50% of the un-scaled box (40, 44).
    // Offset the translate so visual top-left lands on originRect.left/top.
    const offsetX = 40 * (ps - 1);
    const offsetY = 44 * (ps - 1);
    clone.classList.add('snapping');
    clone.style.transform = `translate(${originRect.left + offsetX}px, ${originRect.top + offsetY}px) scale(${ps}) rotate(0deg)`;
    clone.style.opacity = '0.3';

    setTimeout(() => {
      cleanupAfterDrag(originEl);
    }, 220);
  }

  suppressNextClick = true;
}

function cleanupAfterDrag(originEl: HTMLElement | null): void {
  releasePointerCaptureSafely();
  originEl?.classList.remove('chip-ghost');
  draggingSource.set(null);
  // Reset clone DOM inline styles for next drag
  if (dragCloneEl) {
    dragCloneEl.classList.remove('snapping');
    dragCloneEl.style.transform = '';
    dragCloneEl.style.opacity = '';
  }
}

// ── Pointer event handlers (called from SourceRail's chip handlers) ────
function detachPointerListeners(): void {
  document.removeEventListener('pointermove', onPointerMove);
  document.removeEventListener('pointerup', onPointerUp);
  document.removeEventListener('pointercancel', onPointerCancel);
}

export function chipPointerDown(e: PointerEvent, chipEl: HTMLElement, sourceId: SourceId): void {
  if (e.button !== undefined && e.button !== 0) return;
  // Multi-touch / re-entry guard
  if (get(draggingSource) || pressTimerId) return;
  suppressNextClick = false;
  pressOriginEl = chipEl;
  pressOriginX = lastPointerX = e.clientX;
  pressOriginY = lastPointerY = e.clientY;

  // Pin all subsequent events for this pointer to the chip — without this the
  // Crestron touch driver can re-target events to other elements mid-gesture
  // and the drag "resets" before the user releases.
  try {
    chipEl.setPointerCapture(e.pointerId);
    activePointerId = e.pointerId;
    pressCapturedEl = chipEl;
  } catch {
    // setPointerCapture can throw if the element is detached; safe to ignore.
  }

  pressTimerId = setTimeout(() => {
    if (get(armedSource)) disarm();
    startDrag(sourceId, chipEl, lastPointerX, lastPointerY);
    pressTimerId = null;
  }, LONG_PRESS_MS);

  document.addEventListener('pointermove', onPointerMove);
  document.addEventListener('pointerup', onPointerUp);
  document.addEventListener('pointercancel', onPointerCancel);
}

function releasePointerCaptureSafely(): void {
  if (pressCapturedEl && activePointerId !== null) {
    try { pressCapturedEl.releasePointerCapture(activePointerId); } catch { /* already released */ }
  }
  pressCapturedEl = null;
  activePointerId = null;
}

export function onPointerMove(e: PointerEvent): void {
  lastPointerX = e.clientX;
  lastPointerY = e.clientY;
  if (get(draggingSource)) {
    cloneCoords.set({ x: e.clientX, y: e.clientY });
    updateHover(e.clientX, e.clientY);
    return;
  }
  if (pressTimerId) {
    const dx = Math.abs(e.clientX - pressOriginX);
    const dy = Math.abs(e.clientY - pressOriginY);
    if (dx > MOVE_CANCEL_THRESHOLD || dy > MOVE_CANCEL_THRESHOLD) {
      clearTimeout(pressTimerId);
      pressTimerId = null;
      document.removeEventListener('pointermove', onPointerMove);
      releasePointerCaptureSafely();
    }
  }
}

export function onPointerUp(e: PointerEvent): void {
  detachPointerListeners();
  if (pressTimerId) {
    clearTimeout(pressTimerId);
    pressTimerId = null;
    pressOriginEl = null;
    releasePointerCaptureSafely();
    return;
  }
  if (get(draggingSource)) {
    endDrag(e.clientX, e.clientY);
  } else {
    releasePointerCaptureSafely();
  }
  pressOriginEl = null;
}

export function onPointerCancel(_e: PointerEvent): void {
  detachPointerListeners();
  if (pressTimerId) {
    clearTimeout(pressTimerId);
    pressTimerId = null;
    pressOriginEl = null;
    releasePointerCaptureSafely();
    return;
  }
  if (get(draggingSource)) {
    // Force snap-back: pass coords outside any tile
    endDrag(-1, -1);
  } else {
    releasePointerCaptureSafely();
  }
  pressOriginEl = null;
}

// ── Click-outside disarm (attached once on module load) ────────────────
// passive: true is a hint to the touch-event scheduler that this listener
// will not preventDefault. The handler doesn't, so opting in lets the
// driver dispatch the click without the synchronous-block fallback path.
// Per audit M7. CH5 panels don't scroll so the runtime impact is minor,
// but it's free hygiene.
if (typeof document !== 'undefined') {
  document.addEventListener('click', (e) => {
    if (!get(armedSource)) return;
    const target = e.target as Element | null;
    const onChip = target?.closest('.chip');
    const onTile = target?.closest('.tile');
    if (!onChip && !onTile) disarm();
  }, { passive: true });
}
