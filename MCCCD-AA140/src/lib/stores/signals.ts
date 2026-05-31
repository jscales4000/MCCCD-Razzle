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

// Mic mutes (Lav/Handheld on Home; Ceiling 1-2 on AudioMixer)
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

// Mic real-time levels (0-100, ~10-30 Hz update from Shure P300)
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

// Wire the always-on feedback subscriptions. Called from src/main.ts after
// CrComLib is detected. Only signals consumed by Home, App-level, or multiple
// pages are wired here. Page-specific signals are gated — see per-page init
// functions below.
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

  // Lav + Handheld mutes stay always-on: Home shows mute buttons for both.
  // Ceiling mutes + all mic state beyond mute are AudioMixer-only → gated below.
  subscribeDigital(SIGNALS.micLavMuteFb,       (v) => micLavMuteFb.set(v));
  subscribeDigital(SIGNALS.micHandheldMuteFb,  (v) => micHandheldMuteFb.set(v));

  subscribeAnalog(SIGNALS.occupancyState,      (v) => occupancyState.set(v === 1 ? 1 : v === 2 ? 2 : 0));
  subscribeAnalog(SIGNALS.shutdownCountdown,   (v) => shutdownCountdown.set(v));

  // camTrackingModeFb stays always-on — consumed by Cameras.svelte (user WIP; can't add per-page teardown there).
  subscribeAnalog(SIGNALS.camTrackingModeFb,   (v) => camTrackingModeFb.set(v === 2 ? 2 : v === 3 ? 3 : 1));

  // progAudioLevelFb is consumed by both Home (VolumePopup) and AudioMixer.
  subscribeAnalog(SIGNALS.progAudioLevelFb,    (v) => progAudioLevelFb.set(v));
}

// ── Per-page lazy subscriptions ──────────────────────────────────────────────
//
// Audit H4 (scoped): mic-level signals (10-30 Hz from Shure P300) gate to AudioMixer.
// Audit H4-followup: remaining AudioMixer-only state signals + DisplayRouting
// routing-mode signals gate to their respective pages.
//
// 4-mic redesign note: the legacy "Ceiling 3" strip was dropped (2026-05-26 P300
// redesign — only 2 MXA920W-S arrays exist). micCeiling3* stores are kept for
// contract compatibility but no longer subscribed — nothing reads them.

// ── AudioMixer page ───────────────────────────────────────────────────────────

let audioMixerAnalogIds: string[] = [];
let audioMixerDigitalIds: string[] = [];

export function initAudioMixerSignals(): void {
  if (audioMixerAnalogIds.length > 0) return; // idempotent
  audioMixerDigitalIds = [
    // Mic mutes (ceiling 1-2; ceiling3 strip dropped in 4-mic redesign)
    subscribeDigital(SIGNALS.micCeiling1MuteFb,  (v) => micCeiling1MuteFb.set(v)),
    subscribeDigital(SIGNALS.micCeiling2MuteFb,  (v) => micCeiling2MuteFb.set(v)),
    // Connected
    subscribeDigital(SIGNALS.micLavConnected,        (v) => micLavConnected.set(v)),
    subscribeDigital(SIGNALS.micHandheldConnected,   (v) => micHandheldConnected.set(v)),
    subscribeDigital(SIGNALS.micCeiling1Connected,   (v) => micCeiling1Connected.set(v)),
    subscribeDigital(SIGNALS.micCeiling2Connected,   (v) => micCeiling2Connected.set(v)),
    // Link
    subscribeDigital(SIGNALS.audioLinkCeilings12Fb,  (v) => audioLinkCeilings12Fb.set(v)),
  ];
  audioMixerAnalogIds = [
    // Trim
    subscribeAnalog(SIGNALS.micLavTrimFb,        (v) => micLavTrimFb.set(v)),
    subscribeAnalog(SIGNALS.micHandheldTrimFb,   (v) => micHandheldTrimFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling1TrimFb,   (v) => micCeiling1TrimFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling2TrimFb,   (v) => micCeiling2TrimFb.set(v)),
    // Line-out
    subscribeAnalog(SIGNALS.micLavLineOutFb,     (v) => micLavLineOutFb.set(v)),
    subscribeAnalog(SIGNALS.micHandheldLineOutFb,(v) => micHandheldLineOutFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling1LineOutFb,(v) => micCeiling1LineOutFb.set(v)),
    subscribeAnalog(SIGNALS.micCeiling2LineOutFb,(v) => micCeiling2LineOutFb.set(v)),
    // Scene
    subscribeAnalog(SIGNALS.sceneRecallFb,       (v) => sceneRecallFb.set(v)),
  ];
}

export function teardownAudioMixerSignals(): void {
  for (const id of audioMixerAnalogIds) { if (id) unsubscribeAnalog(id); }
  for (const id of audioMixerDigitalIds) { if (id) unsubscribeDigital(id); }
  audioMixerAnalogIds = [];
  audioMixerDigitalIds = [];
}

// ── AudioMixer VU meters ─────────────────────────────────────────────────────
// Audit H4 (scoped): the 4 mic-level analog signals fire 10-30 Hz EACH
// from the Shure P300, so leaving them subscribed at app boot means a constant
// ~40-120 callback/sec storm even when nothing renders the data.
// Note: ceiling3 level was removed — the P300 only has 4 channels (2 lav/handheld
// + 2 ceiling arrays). micCeiling3Level store kept for contract compat but not wired.

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
  // Reset the stored values so a stale meter level doesn't linger if the user
  // re-enters AudioMixer before the P300 pushes the next update.
  micLavLevel.set(0);
  micHandheldLevel.set(0);
  micCeiling1Level.set(0);
  micCeiling2Level.set(0);
}

// ── DisplayRouting page ───────────────────────────────────────────────────────

let routingAnalogId = '';
let routingDigitalId = '';

export function initRoutingSignals(): void {
  if (routingAnalogId) return; // idempotent
  routingAnalogId  = subscribeAnalog(SIGNALS.routingModeFb,      (v) => routingModeFb.set(v));
  routingDigitalId = subscribeDigital(SIGNALS.autoRouteEnableFb, (v) => autoRouteEnableFb.set(v));
}

export function teardownRoutingSignals(): void {
  if (routingAnalogId)  { unsubscribeAnalog(routingAnalogId);   routingAnalogId  = ''; }
  if (routingDigitalId) { unsubscribeDigital(routingDigitalId); routingDigitalId = ''; }
}
