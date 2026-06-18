import { writable } from 'svelte/store';
import { SIGNALS } from '../contract';
import { subscribeAnalog, subscribeDigital, unsubscribeAnalog, unsubscribeDigital } from '../CrComLib';

// One Svelte store per piece of UI state. Each store mirrors a feedback signal
// from the processor. Components publish commands via the typed CrComLib helpers
// (publishDigital, publishAnalog, pulseDigital) — stores here are subscriptions only.

export const panelOnline = writable(true);

// Display routing feedback
export const display1SourceFb = writable<number>(0);
export const display2SourceFb = writable<number>(0);
export const display3SourceFb = writable<number>(0);
export const display4SourceFb = writable<number>(0);
export const audioOutputSelectFb = writable<1 | 2>(1);

// Per-display power feedback (NVX D200 sink-connected)
export const display1PowerFb = writable<boolean>(false);
export const display2PowerFb = writable<boolean>(false);
export const display3PowerFb = writable<boolean>(false);
export const display4PowerFb = writable<boolean>(false);

// System power (drives Power button enlarged variant + reflects current power state)
export const systemPowerFb = writable<boolean>(false);

// Mic mutes (Lav/Handheld on Home footer; Ceiling 1-2 on AudioMixer)
export const micLavMuteFb = writable<boolean>(false);
export const micHandheldMuteFb = writable<boolean>(false);
export const micCeiling1MuteFb = writable<boolean>(false);
export const micCeiling2MuteFb = writable<boolean>(false);
export const micCeiling3MuteFb = writable<boolean>(false);

// Mic input gain trims (0-100)
export const micLavTrimFb = writable<number>(50);
export const micHandheldTrimFb = writable<number>(50);
export const micCeiling1TrimFb = writable<number>(50);
export const micCeiling2TrimFb = writable<number>(50);
export const micCeiling3TrimFb = writable<number>(50);

// Mic line-out levels (0-100)
export const micLavLineOutFb = writable<number>(50);
export const micHandheldLineOutFb = writable<number>(50);
export const micCeiling1LineOutFb = writable<number>(50);
export const micCeiling2LineOutFb = writable<number>(50);
export const micCeiling3LineOutFb = writable<number>(50);

// Mic real-time levels (0-100, ~10-30 Hz update from Q-SYS)
export const micLavLevel = writable<number>(0);
export const micHandheldLevel = writable<number>(0);
export const micCeiling1Level = writable<number>(0);
export const micCeiling2Level = writable<number>(0);
export const micCeiling3Level = writable<number>(0);

// Mic connection / signal-present
export const micLavConnected = writable<boolean>(false);
export const micHandheldConnected = writable<boolean>(false);
export const micCeiling1Connected = writable<boolean>(false);
export const micCeiling2Connected = writable<boolean>(false);
export const micCeiling3Connected = writable<boolean>(false);

// Occupancy + shutdown
export const occupancyState = writable<0 | 1 | 2>(0); // 0=vacant, 1=occupied, 2=shutdown-pending
export const shutdownCountdown = writable<number>(0);

// Camera tracking mode (Front/Back-L/Back-R selection is local UI state, not stored here)
export const camTrackingModeFb = writable<1 | 2 | 3>(3); // default VX AutoSwitch

// Display routing mode + auto-route (Plan 3 — Mockup #14)
export const routingModeFb = writable<number>(0);            // 0=unset, 1=Manual, 2=Mirror, 3=Extend
export const autoRouteEnableFb = writable<boolean>(false);

// Audio mixer (Plan 4 — Mockup #13)
export const progAudioLevelFb = writable<number>(50);        // master fader fb (0-100)
export const sceneRecallFb = writable<number>(0);            // 0=none, 1-4=active preset
export const audioLinkCeilings12Fb = writable<boolean>(false);

// Wire feedback subscriptions on app startup. Called from src/main.ts after
// CrComLib is detected.
//
// Common signals only — signals consumed by a single page are wired in that
// page's onMount and torn down in onDestroy (audit H4-followup):
//   - routingModeFb / autoRouteEnableFb → initRoutingSignals (DisplayRouting)
//   - sceneRecallFb / audioLinkCeilings12Fb / mic trim+lineOut+connected → initMixerStateSubscriptions (AudioMixer)
//   - micLav/Handheld/Ceiling1/2 levels → initMicLevelSubscriptions (AudioMixer, H4)
//   - camTrackingModeFb stays always-on (Cameras.svelte is user WIP; cannot gate)
export function initSignals(): void {
  subscribeDigital(SIGNALS.panelOnline, (v) => panelOnline.set(v));

  subscribeAnalog(SIGNALS.display1SourceFb,    (v) => display1SourceFb.set(v));
  subscribeAnalog(SIGNALS.display2SourceFb,    (v) => display2SourceFb.set(v));
  subscribeAnalog(SIGNALS.display3SourceFb,    (v) => display3SourceFb.set(v));
  subscribeAnalog(SIGNALS.display4SourceFb,    (v) => display4SourceFb.set(v));
  subscribeAnalog(SIGNALS.audioOutputSelectFb, (v) => audioOutputSelectFb.set(v === 2 ? 2 : 1));

  subscribeDigital(SIGNALS.display1PowerFb,    (v) => display1PowerFb.set(v));
  subscribeDigital(SIGNALS.display2PowerFb,    (v) => display2PowerFb.set(v));
  subscribeDigital(SIGNALS.display3PowerFb,    (v) => display3PowerFb.set(v));
  subscribeDigital(SIGNALS.display4PowerFb,    (v) => display4PowerFb.set(v));
  subscribeDigital(SIGNALS.systemPowerFb,      (v) => systemPowerFb.set(v));

  // micLavMuteFb / micHandheldMuteFb stay always-on — Aa140Footer (mounted on
  // both Home and DisplayRouting) renders the mute buttons using these stores.
  subscribeDigital(SIGNALS.micLavMuteFb,       (v) => micLavMuteFb.set(v));
  subscribeDigital(SIGNALS.micHandheldMuteFb,  (v) => micHandheldMuteFb.set(v));

  // micCeiling3* subscriptions kept for contract compatibility; no component
  // currently reads these stores (ceiling3 strip dropped in 4-mic update).
  subscribeDigital(SIGNALS.micCeiling3MuteFb,  (v) => micCeiling3MuteFb.set(v));
  subscribeAnalog(SIGNALS.micCeiling3TrimFb,   (v) => micCeiling3TrimFb.set(v));
  subscribeAnalog(SIGNALS.micCeiling3LineOutFb,(v) => micCeiling3LineOutFb.set(v));
  subscribeDigital(SIGNALS.micCeiling3Connected,(v) => micCeiling3Connected.set(v));

  subscribeAnalog(SIGNALS.occupancyState,      (v) => occupancyState.set(v === 1 ? 1 : v === 2 ? 2 : 0));
  subscribeAnalog(SIGNALS.shutdownCountdown,   (v) => shutdownCountdown.set(v));

  subscribeAnalog(SIGNALS.camTrackingModeFb,   (v) => camTrackingModeFb.set(v === 2 ? 2 : v === 3 ? 3 : 1));

  subscribeAnalog(SIGNALS.progAudioLevelFb,    (v) => progAudioLevelFb.set(v));
}

// ── Per-page lazy subscriptions ──────────────────────────────────────
// Audit H4 (scoped): the 5 mic-level analog signals fire 10-30 Hz EACH
// from the DSP, so leaving them subscribed at app boot means a constant
// ~50-150 callback/sec storm even when nothing renders the data. Gate
// them so they're only live while AudioMixer is mounted.

let micLevelSubscriptionIds: string[] = [];

export function initMicLevelSubscriptions(): void {
  if (micLevelSubscriptionIds.length > 0) return; // idempotent
  micLevelSubscriptionIds = [
    subscribeAnalog(SIGNALS.micLavLevel,      (v) => micLavLevel.set(v)),
    subscribeAnalog(SIGNALS.micHandheldLevel, (v) => micHandheldLevel.set(v)),
    subscribeAnalog(SIGNALS.micCeiling1Level, (v) => micCeiling1Level.set(v)),
    subscribeAnalog(SIGNALS.micCeiling2Level, (v) => micCeiling2Level.set(v)),
    // micCeiling3Level omitted — ceiling3 strip dropped in 4-mic update (2026-05-26)
  ];
}

export function teardownMicLevelSubscriptions(): void {
  for (const id of micLevelSubscriptionIds) {
    if (id) unsubscribeAnalog(id);
  }
  micLevelSubscriptionIds = [];
  // Reset stored values so stale meter levels don't linger on re-entry.
  micLavLevel.set(0);
  micHandheldLevel.set(0);
  micCeiling1Level.set(0);
  micCeiling2Level.set(0);
}

// ── Routing page signals (H4-followup) ───────────────────────────────
// routingModeFb and autoRouteEnableFb are only consumed by DisplayRouting.
// Gated here so the 2 subscriptions only exist while that page is mounted.

let routingSubIds: { a: string[]; d: string[] } = { a: [], d: [] };

export function initRoutingSignals(): void {
  if (routingSubIds.a.length > 0) return; // idempotent
  routingSubIds.a = [
    subscribeAnalog(SIGNALS.routingModeFb, (v) => routingModeFb.set(v)),
  ];
  routingSubIds.d = [
    subscribeDigital(SIGNALS.autoRouteEnableFb, (v) => autoRouteEnableFb.set(v)),
  ];
}

export function teardownRoutingSignals(): void {
  for (const id of routingSubIds.a) if (id) unsubscribeAnalog(id);
  for (const id of routingSubIds.d) if (id) unsubscribeDigital(id);
  routingSubIds = { a: [], d: [] };
  routingModeFb.set(0);
  autoRouteEnableFb.set(false);
}

// ── AudioMixer state signals (H4-followup) ────────────────────────────
// sceneRecallFb, audioLinkCeilings12Fb, and the per-mic trim/lineOut/connected
// stores are only consumed by AudioMixer. Gate them per-page to eliminate
// ~14 idle subscriptions that serve no purpose outside the mixer view.
// (micLavMuteFb / micHandheldMuteFb stay in initSignals — used by Aa140Footer.)

let mixerStateSubIds: { a: string[]; d: string[] } = { a: [], d: [] };

export function initMixerStateSubscriptions(): void {
  if (mixerStateSubIds.a.length > 0) return; // idempotent
  mixerStateSubIds.a = [
    subscribeAnalog(SIGNALS.sceneRecallFb,         (v) => sceneRecallFb.set(v)),
    subscribeAnalog(SIGNALS.micLavTrimFb,          (v) => micLavTrimFb.set(v)),
    subscribeAnalog(SIGNALS.micHandheldTrimFb,     (v) => micHandheldTrimFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling1TrimFb,     (v) => micCeiling1TrimFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling2TrimFb,     (v) => micCeiling2TrimFb.set(v)),
    subscribeAnalog(SIGNALS.micLavLineOutFb,       (v) => micLavLineOutFb.set(v)),
    subscribeAnalog(SIGNALS.micHandheldLineOutFb,  (v) => micHandheldLineOutFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling1LineOutFb,  (v) => micCeiling1LineOutFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling2LineOutFb,  (v) => micCeiling2LineOutFb.set(v)),
  ];
  mixerStateSubIds.d = [
    subscribeDigital(SIGNALS.audioLinkCeilings12Fb,(v) => audioLinkCeilings12Fb.set(v)),
    subscribeDigital(SIGNALS.micLavConnected,      (v) => micLavConnected.set(v)),
    subscribeDigital(SIGNALS.micHandheldConnected, (v) => micHandheldConnected.set(v)),
    subscribeDigital(SIGNALS.micCeiling1MuteFb,    (v) => micCeiling1MuteFb.set(v)),
    subscribeDigital(SIGNALS.micCeiling2MuteFb,    (v) => micCeiling2MuteFb.set(v)),
    subscribeDigital(SIGNALS.micCeiling1Connected, (v) => micCeiling1Connected.set(v)),
    subscribeDigital(SIGNALS.micCeiling2Connected, (v) => micCeiling2Connected.set(v)),
  ];
}

export function teardownMixerStateSubscriptions(): void {
  for (const id of mixerStateSubIds.a) if (id) unsubscribeAnalog(id);
  for (const id of mixerStateSubIds.d) if (id) unsubscribeDigital(id);
  mixerStateSubIds = { a: [], d: [] };
}
