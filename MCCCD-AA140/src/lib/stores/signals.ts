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

  subscribeDigital(SIGNALS.systemPowerFb,      (v) => systemPowerFb.set(v));

  // micLavMuteFb / micHandheldMuteFb are read by Home's mic-strip buttons;
  // keep them always-on. micCeiling3* are legacy dead subscriptions retained
  // for signal-contract completeness; they fire only on user action.
  subscribeDigital(SIGNALS.micLavMuteFb,       (v) => micLavMuteFb.set(v));
  subscribeDigital(SIGNALS.micHandheldMuteFb,  (v) => micHandheldMuteFb.set(v));
  subscribeDigital(SIGNALS.micCeiling3MuteFb,  (v) => micCeiling3MuteFb.set(v));
  subscribeAnalog(SIGNALS.micCeiling3TrimFb,   (v) => micCeiling3TrimFb.set(v));
  subscribeAnalog(SIGNALS.micCeiling3LineOutFb,(v) => micCeiling3LineOutFb.set(v));
  subscribeDigital(SIGNALS.micCeiling3Connected,(v) => micCeiling3Connected.set(v));

  // Mic real-time levels are subscribed lazily — see initMicLevelSubscriptions()
  // below. They fire 10-30 Hz from Q-SYS and are only consumed by AudioMixer's
  // VuMeter, so we gate them per-page to avoid a callback storm at idle.

  subscribeAnalog(SIGNALS.occupancyState,      (v) => occupancyState.set(v === 1 ? 1 : v === 2 ? 2 : 0));
  subscribeAnalog(SIGNALS.shutdownCountdown,   (v) => shutdownCountdown.set(v));

  // camTrackingModeFb stays here because Cameras.svelte is user-WIP (cannot
  // receive onMount/onDestroy wiring). Low-frequency; fires only on mode change.
  subscribeAnalog(SIGNALS.camTrackingModeFb,   (v) => camTrackingModeFb.set(v === 2 ? 2 : v === 3 ? 3 : 1));

  subscribeAnalog(SIGNALS.progAudioLevelFb,    (v) => progAudioLevelFb.set(v));
}

// ── Per-page lazy subscriptions ──────────────────────────────────────
// Audit H4-followup: gate low-frequency state signals to the page that
// consumes them. These don't fire continuously, so the win is ~21 fewer
// crcomlib registrations at idle rather than callback-storm prevention.

// ── DisplayRouting signals ─────────────────────────────────────────────
// display1/2/3PowerFb, routingModeFb, autoRouteEnableFb — only consumed by
// DisplayRouting.svelte. Called from its onMount / teardown on onDestroy.

let routingDigitalIds: string[] = [];
let routingAnalogIds: string[] = [];

export function initRoutingSignals(): void {
  if (routingDigitalIds.length > 0) return;
  routingDigitalIds = [
    subscribeDigital(SIGNALS.display1PowerFb,   (v) => display1PowerFb.set(v)),
    subscribeDigital(SIGNALS.display2PowerFb,   (v) => display2PowerFb.set(v)),
    subscribeDigital(SIGNALS.display3PowerFb,   (v) => display3PowerFb.set(v)),
    subscribeDigital(SIGNALS.autoRouteEnableFb, (v) => autoRouteEnableFb.set(v)),
  ];
  routingAnalogIds = [
    subscribeAnalog(SIGNALS.routingModeFb, (v) => routingModeFb.set(v)),
  ];
}

export function teardownRoutingSignals(): void {
  for (const id of routingDigitalIds) unsubscribeDigital(id);
  for (const id of routingAnalogIds) unsubscribeAnalog(id);
  routingDigitalIds = [];
  routingAnalogIds = [];
  display1PowerFb.set(false);
  display2PowerFb.set(false);
  display3PowerFb.set(false);
  routingModeFb.set(0);
  autoRouteEnableFb.set(false);
}

// ── AudioMixer state signals ───────────────────────────────────────────
// sceneRecallFb, audioLinkCeilings12Fb, and all lav/handheld/ceiling1/ceiling2
// trim/lineOut/mute/connected — only consumed by AudioMixer.svelte.
// micLavMuteFb + micHandheldMuteFb stay always-on (Home reads them for the
// mic-strip mute buttons).

let mixerDigitalIds: string[] = [];
let mixerAnalogIds: string[] = [];

export function initMixerStateSignals(): void {
  if (mixerDigitalIds.length > 0) return;
  mixerDigitalIds = [
    subscribeDigital(SIGNALS.audioLinkCeilings12Fb,(v) => audioLinkCeilings12Fb.set(v)),
    subscribeDigital(SIGNALS.micLavConnected,       (v) => micLavConnected.set(v)),
    subscribeDigital(SIGNALS.micHandheldConnected,  (v) => micHandheldConnected.set(v)),
    subscribeDigital(SIGNALS.micCeiling1Connected,  (v) => micCeiling1Connected.set(v)),
    subscribeDigital(SIGNALS.micCeiling2Connected,  (v) => micCeiling2Connected.set(v)),
    subscribeDigital(SIGNALS.micCeiling1MuteFb,     (v) => micCeiling1MuteFb.set(v)),
    subscribeDigital(SIGNALS.micCeiling2MuteFb,     (v) => micCeiling2MuteFb.set(v)),
  ];
  mixerAnalogIds = [
    subscribeAnalog(SIGNALS.sceneRecallFb,          (v) => sceneRecallFb.set(v)),
    subscribeAnalog(SIGNALS.micLavTrimFb,           (v) => micLavTrimFb.set(v)),
    subscribeAnalog(SIGNALS.micHandheldTrimFb,      (v) => micHandheldTrimFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling1TrimFb,      (v) => micCeiling1TrimFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling2TrimFb,      (v) => micCeiling2TrimFb.set(v)),
    subscribeAnalog(SIGNALS.micLavLineOutFb,        (v) => micLavLineOutFb.set(v)),
    subscribeAnalog(SIGNALS.micHandheldLineOutFb,   (v) => micHandheldLineOutFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling1LineOutFb,   (v) => micCeiling1LineOutFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling2LineOutFb,   (v) => micCeiling2LineOutFb.set(v)),
  ];
}

export function teardownMixerStateSignals(): void {
  for (const id of mixerDigitalIds) unsubscribeDigital(id);
  for (const id of mixerAnalogIds) unsubscribeAnalog(id);
  mixerDigitalIds = [];
  mixerAnalogIds = [];
  audioLinkCeilings12Fb.set(false);
  sceneRecallFb.set(0);
  micLavConnected.set(false);    micHandheldConnected.set(false);
  micCeiling1Connected.set(false); micCeiling2Connected.set(false);
  micCeiling1MuteFb.set(false);  micCeiling2MuteFb.set(false);
  micLavTrimFb.set(50);    micHandheldTrimFb.set(50);
  micCeiling1TrimFb.set(50); micCeiling2TrimFb.set(50);
  micLavLineOutFb.set(50);    micHandheldLineOutFb.set(50);
  micCeiling1LineOutFb.set(50); micCeiling2LineOutFb.set(50);
}

// ── Mic real-time levels (H4 original) ────────────────────────────────
// The 5 mic-level analog signals fire 10-30 Hz EACH from the DSP. Gate
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
