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
// CrComLib is detected. Subscribes only signals consumed at the Home/splash
// level or by Aa140Footer (present on every page). Per-page signals are
// wired lazily — see initDisplayRoutingSubscriptions() and
// initAudioMixerSubscriptions() below.
export function initSignals(): void {
  subscribeDigital(SIGNALS.panelOnline,        (v) => panelOnline.set(v));
  subscribeAnalog(SIGNALS.display1SourceFb,    (v) => display1SourceFb.set(v));
  subscribeDigital(SIGNALS.systemPowerFb,      (v) => systemPowerFb.set(v));
  // Lav/handheld mutes: Aa140Footer shows mic-mute toggles on every page
  subscribeDigital(SIGNALS.micLavMuteFb,       (v) => micLavMuteFb.set(v));
  subscribeDigital(SIGNALS.micHandheldMuteFb,  (v) => micHandheldMuteFb.set(v));
  subscribeAnalog(SIGNALS.occupancyState,      (v) => occupancyState.set(v === 1 ? 1 : v === 2 ? 2 : 0));
  subscribeAnalog(SIGNALS.shutdownCountdown,   (v) => shutdownCountdown.set(v));
  // camTrackingModeFb: Cameras.svelte reads this; gating it would require
  // modifying that file (user WIP), so leave it always-on.
  subscribeAnalog(SIGNALS.camTrackingModeFb,   (v) => camTrackingModeFb.set(v === 2 ? 2 : v === 3 ? 3 : 1));
  // progAudioLevelFb: Aa140Footer volume indicator reads this on every page
  subscribeAnalog(SIGNALS.progAudioLevelFb,    (v) => progAudioLevelFb.set(v));
}

// ── Per-page lazy subscriptions: DisplayRouting ──────────────────────────
// display2-4 source + all display power feedbacks + routing mode are only
// consumed by DisplayRouting. Gate them to avoid idle subscriptions.

let drAnalogIds: string[]  = [];
let drDigitalIds: string[] = [];

export function initDisplayRoutingSubscriptions(): void {
  if (drAnalogIds.length > 0) return; // idempotent
  drAnalogIds = [
    subscribeAnalog(SIGNALS.display2SourceFb, (v) => display2SourceFb.set(v)),
    subscribeAnalog(SIGNALS.display3SourceFb, (v) => display3SourceFb.set(v)),
    subscribeAnalog(SIGNALS.display4SourceFb, (v) => display4SourceFb.set(v)),
    subscribeAnalog(SIGNALS.routingModeFb,    (v) => routingModeFb.set(v)),
  ];
  drDigitalIds = [
    subscribeDigital(SIGNALS.display1PowerFb,   (v) => display1PowerFb.set(v)),
    subscribeDigital(SIGNALS.display2PowerFb,   (v) => display2PowerFb.set(v)),
    subscribeDigital(SIGNALS.display3PowerFb,   (v) => display3PowerFb.set(v)),
    subscribeDigital(SIGNALS.display4PowerFb,   (v) => display4PowerFb.set(v)),
    subscribeDigital(SIGNALS.autoRouteEnableFb, (v) => autoRouteEnableFb.set(v)),
  ];
}

export function teardownDisplayRoutingSubscriptions(): void {
  for (const id of drAnalogIds)  if (id) unsubscribeAnalog(id);
  for (const id of drDigitalIds) if (id) unsubscribeDigital(id);
  drAnalogIds = [];
  drDigitalIds = [];
  display2SourceFb.set(0);
  display3SourceFb.set(0);
  display4SourceFb.set(0);
  display1PowerFb.set(false);
  display2PowerFb.set(false);
  display3PowerFb.set(false);
  display4PowerFb.set(false);
  routingModeFb.set(0);
  autoRouteEnableFb.set(false);
}

// ── Per-page lazy subscriptions: AudioMixer ──────────────────────────────
// Trim, lineOut, connected, ceiling mutes, scene recall, link-arrays, and
// output-select are only consumed by AudioMixer. Gate them so these 18
// subscriptions are absent while the user is on Home or DisplayRouting.
// (Ceiling3 strip was removed from AudioMixer in the 2026-05-26 design
// update — micCeiling3* exports remain for store reference but have no
// subscriber in the app and are intentionally excluded here.)

let amAnalogIds: string[]  = [];
let amDigitalIds: string[] = [];

export function initAudioMixerSubscriptions(): void {
  if (amAnalogIds.length > 0) return; // idempotent
  amAnalogIds = [
    subscribeAnalog(SIGNALS.audioOutputSelectFb,   (v) => audioOutputSelectFb.set(v === 2 ? 2 : 1)),
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
    subscribeDigital(SIGNALS.audioLinkCeilings12Fb, (v) => audioLinkCeilings12Fb.set(v)),
    subscribeDigital(SIGNALS.micCeiling1MuteFb,     (v) => micCeiling1MuteFb.set(v)),
    subscribeDigital(SIGNALS.micCeiling2MuteFb,     (v) => micCeiling2MuteFb.set(v)),
    subscribeDigital(SIGNALS.micLavConnected,       (v) => micLavConnected.set(v)),
    subscribeDigital(SIGNALS.micHandheldConnected,  (v) => micHandheldConnected.set(v)),
    subscribeDigital(SIGNALS.micCeiling1Connected,  (v) => micCeiling1Connected.set(v)),
    subscribeDigital(SIGNALS.micCeiling2Connected,  (v) => micCeiling2Connected.set(v)),
  ];
}

export function teardownAudioMixerSubscriptions(): void {
  for (const id of amAnalogIds)  if (id) unsubscribeAnalog(id);
  for (const id of amDigitalIds) if (id) unsubscribeDigital(id);
  amAnalogIds = [];
  amDigitalIds = [];
  audioOutputSelectFb.set(1);
  sceneRecallFb.set(0);
  micLavTrimFb.set(50);
  micHandheldTrimFb.set(50);
  micCeiling1TrimFb.set(50);
  micCeiling2TrimFb.set(50);
  micLavLineOutFb.set(50);
  micHandheldLineOutFb.set(50);
  micCeiling1LineOutFb.set(50);
  micCeiling2LineOutFb.set(50);
  audioLinkCeilings12Fb.set(false);
  micCeiling1MuteFb.set(false);
  micCeiling2MuteFb.set(false);
  micLavConnected.set(false);
  micHandheldConnected.set(false);
  micCeiling1Connected.set(false);
  micCeiling2Connected.set(false);
}

// ── Per-page lazy subscriptions: mic level meters (H4 original) ─────────
// The 4 mic-level analog signals fire 10-30 Hz EACH from the Shure P300.
// Leaving them subscribed at boot causes a ~40-120 callback/sec storm even
// when nothing renders the data. Gate them so they're only live while
// AudioMixer is mounted. (Ceiling3 level removed — that strip was dropped.)

let micLevelSubscriptionIds: string[] = [];

export function initMicLevelSubscriptions(): void {
  if (micLevelSubscriptionIds.length > 0) return; // idempotent
  micLevelSubscriptionIds = [
    subscribeAnalog(SIGNALS.micLavLevel,      (v) => micLavLevel.set(v)),
    subscribeAnalog(SIGNALS.micHandheldLevel, (v) => micHandheldLevel.set(v)),
    subscribeAnalog(SIGNALS.micCeiling1Level, (v) => micCeiling1Level.set(v)),
    subscribeAnalog(SIGNALS.micCeiling2Level, (v) => micCeiling2Level.set(v)),
  ];
}

export function teardownMicLevelSubscriptions(): void {
  for (const id of micLevelSubscriptionIds) {
    if (id) unsubscribeAnalog(id);
  }
  micLevelSubscriptionIds = [];
  micLavLevel.set(0);
  micHandheldLevel.set(0);
  micCeiling1Level.set(0);
  micCeiling2Level.set(0);
}
