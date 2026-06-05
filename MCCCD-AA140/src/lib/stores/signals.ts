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
export const audioOutputSelectFb = writable<1 | 2>(1);

// Per-display power feedback (NVX D200 sink-connected)
export const display1PowerFb = writable<boolean>(false);
export const display2PowerFb = writable<boolean>(false);
export const display3PowerFb = writable<boolean>(false);

// System power (drives Power button enlarged variant + reflects current power state)
export const systemPowerFb = writable<boolean>(false);

// Mic mutes (Lav/Handheld on Home; Ceiling 1-3 on Settings)
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
export function initSignals(): void {
  subscribeDigital(SIGNALS.panelOnline, (v) => panelOnline.set(v));

  subscribeAnalog(SIGNALS.display1SourceFb,    (v) => display1SourceFb.set(v));
  subscribeAnalog(SIGNALS.display2SourceFb,    (v) => display2SourceFb.set(v));
  subscribeAnalog(SIGNALS.display3SourceFb,    (v) => display3SourceFb.set(v));
  subscribeAnalog(SIGNALS.audioOutputSelectFb, (v) => audioOutputSelectFb.set(v === 2 ? 2 : 1));

  subscribeDigital(SIGNALS.display1PowerFb,    (v) => display1PowerFb.set(v));
  subscribeDigital(SIGNALS.display2PowerFb,    (v) => display2PowerFb.set(v));
  subscribeDigital(SIGNALS.display3PowerFb,    (v) => display3PowerFb.set(v));
  subscribeDigital(SIGNALS.systemPowerFb,      (v) => systemPowerFb.set(v));

  // Lav + Handheld mutes stay common — Home.svelte reads them for mic-strip buttons.
  subscribeDigital(SIGNALS.micLavMuteFb,       (v) => micLavMuteFb.set(v));
  subscribeDigital(SIGNALS.micHandheldMuteFb,  (v) => micHandheldMuteFb.set(v));

  // Ceiling 3 state stays here: strip removed from AudioMixer in the 4-mic redesign
  // (2026-05-26) but SIMPL may still push these joins; stores kept as a safety net.
  subscribeDigital(SIGNALS.micCeiling3MuteFb,  (v) => micCeiling3MuteFb.set(v));
  subscribeAnalog(SIGNALS.micCeiling3TrimFb,   (v) => micCeiling3TrimFb.set(v));
  subscribeAnalog(SIGNALS.micCeiling3LineOutFb,(v) => micCeiling3LineOutFb.set(v));
  subscribeDigital(SIGNALS.micCeiling3Connected,(v) => micCeiling3Connected.set(v));

  // Mic real-time levels are subscribed lazily — see initMicLevelSubscriptions()
  // below. They fire 10-30 Hz from Q-SYS and are only consumed by AudioMixer's
  // VuMeter, so we gate them per-page to avoid a callback storm at idle.

  subscribeAnalog(SIGNALS.occupancyState,      (v) => occupancyState.set(v === 1 ? 1 : v === 2 ? 2 : 0));
  subscribeAnalog(SIGNALS.shutdownCountdown,   (v) => shutdownCountdown.set(v));

  // camTrackingModeFb is Cameras-only but stays common — Cameras.svelte is
  // protected WIP and cannot wire per-page teardown. Fires only on user action.
  subscribeAnalog(SIGNALS.camTrackingModeFb,   (v) => camTrackingModeFb.set(v === 2 ? 2 : v === 3 ? 3 : 1));

  // progAudioLevelFb stays common — Home.svelte reads it for the VolumePopup.
  subscribeAnalog(SIGNALS.progAudioLevelFb,      (v) => progAudioLevelFb.set(v));

  // routingModeFb, autoRouteEnableFb → gated to DisplayRouting via
  //   initDisplayRoutingSubscriptions() / teardownDisplayRoutingSubscriptions().
  // sceneRecallFb, audioLinkCeilings12Fb, mic 1/2 trim/lineOut/mute/connected →
  //   gated to AudioMixer via initAudioMixerStateSubscriptions().
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
    subscribeAnalog(SIGNALS.micCeiling3Level, (v) => micCeiling3Level.set(v)),
  ];
}

export function teardownMicLevelSubscriptions(): void {
  for (const id of micLevelSubscriptionIds) {
    if (id) unsubscribeAnalog(id);
  }
  micLevelSubscriptionIds = [];
  // Reset the stored values so a stale meter level doesn't linger if the user
  // re-enters AudioMixer before SIMPL pushes the next update.
  micLavLevel.set(0);
  micHandheldLevel.set(0);
  micCeiling1Level.set(0);
  micCeiling2Level.set(0);
  micCeiling3Level.set(0);
}

// ── DisplayRouting per-page subscriptions ────────────────────────────────
// routingModeFb + autoRouteEnableFb only matter on the routing page. Gating
// them saves 2 crcomlib registry entries at boot with no CPU tradeoff (these
// fire only on user mode changes, not continuously).

let drAnalogIds: string[] = [];
let drDigitalIds: string[] = [];

export function initDisplayRoutingSubscriptions(): void {
  if (drAnalogIds.length > 0) return;
  drAnalogIds  = [subscribeAnalog(SIGNALS.routingModeFb,      (v) => routingModeFb.set(v))];
  drDigitalIds = [subscribeDigital(SIGNALS.autoRouteEnableFb, (v) => autoRouteEnableFb.set(v))];
}

export function teardownDisplayRoutingSubscriptions(): void {
  for (const id of drAnalogIds)  if (id) unsubscribeAnalog(id);
  for (const id of drDigitalIds) if (id) unsubscribeDigital(id);
  drAnalogIds  = [];
  drDigitalIds = [];
}

// ── AudioMixer state per-page subscriptions ──────────────────────────────
// 16 state signals only consumed by AudioMixer (trim/lineOut/mute/connected
// for mics 1+2, plus scene recall and ceiling-link toggle). These fire only
// on user action — gating saves 16 registry entries at boot, zero CPU delta.
// micLavMuteFb + micHandheldMuteFb stay in initSignals() (Home reads them).
// progAudioLevelFb stays in initSignals() (Home VolumePopup reads it).

let amAnalogIds: string[]  = [];
let amDigitalIds: string[] = [];

export function initAudioMixerStateSubscriptions(): void {
  if (amAnalogIds.length > 0) return;
  amAnalogIds = [
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
  amDigitalIds = [
    subscribeDigital(SIGNALS.audioLinkCeilings12Fb,(v) => audioLinkCeilings12Fb.set(v)),
    subscribeDigital(SIGNALS.micLavConnected,      (v) => micLavConnected.set(v)),
    subscribeDigital(SIGNALS.micHandheldConnected, (v) => micHandheldConnected.set(v)),
    subscribeDigital(SIGNALS.micCeiling1Connected, (v) => micCeiling1Connected.set(v)),
    subscribeDigital(SIGNALS.micCeiling2Connected, (v) => micCeiling2Connected.set(v)),
    subscribeDigital(SIGNALS.micCeiling1MuteFb,    (v) => micCeiling1MuteFb.set(v)),
    subscribeDigital(SIGNALS.micCeiling2MuteFb,    (v) => micCeiling2MuteFb.set(v)),
  ];
}

export function teardownAudioMixerStateSubscriptions(): void {
  for (const id of amAnalogIds)  if (id) unsubscribeAnalog(id);
  for (const id of amDigitalIds) if (id) unsubscribeDigital(id);
  amAnalogIds  = [];
  amDigitalIds = [];
}
