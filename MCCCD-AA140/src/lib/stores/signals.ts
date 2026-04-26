import { writable } from 'svelte/store';
import { CONTRACT } from '../contract';
import { publishDigital, subscribeDigital } from '../CrComLib';

// One Svelte store per piece of UI state. Each store mirrors a feedback signal
// from the processor (subscribe) or drives a command signal up (publish).

export const panelOnline = writable(true);
export const placeholderToggle = writable(false);

// Wire feedback subscriptions on app startup. Called from src/main.ts after
// CrComLib is detected. Add one subscribeDigital/Analog/Serial per feedback.
export function initSignals(): void {
  subscribeDigital(CONTRACT.panelOnlineFeedback, (value) => panelOnline.set(value));
  subscribeDigital(CONTRACT.placeholderFeedback, (value) => placeholderToggle.set(value));
}

// Action helpers wrap the publish call so components stay declarative.
// Pattern: optimistic-update the local store, then pulse the command signal.
export function togglePlaceholder(): void {
  placeholderToggle.update((value) => {
    pulseSignal(CONTRACT.placeholderToggleCommand);
    return !value;
  });
}

function pulseSignal(signalName: string): void {
  publishDigital(signalName, true);
  setTimeout(() => publishDigital(signalName, false), 120);
}
