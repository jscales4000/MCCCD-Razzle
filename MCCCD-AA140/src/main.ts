import './global.css';
import App from './App.svelte';
import { mount } from 'svelte';
import { initSignals } from './lib/stores/signals';

declare global {
  interface Window {
    CrComLib: any;
    bridgeReceiveIntegerFromNative: (name: string, value: number) => void;
    bridgeReceiveBooleanFromNative: (name: string, value: boolean) => void;
    bridgeReceiveStringFromNative: (name: string, value: string) => void;
    bridgeReceiveObjectFromNative: (name: string, value: object) => void;
  }
}

if (window.CrComLib) {
  console.log('[CH5] CrComLib detected, initializing...');

  // CH5 Video Specialist requirement: bridge functions MUST be on window
  // BEFORE the native platform sends initialization data. Without these the
  // panel's video pipeline drops messages silently (error -9007).
  window.bridgeReceiveIntegerFromNative = window.CrComLib.bridgeReceiveIntegerFromNative;
  window.bridgeReceiveBooleanFromNative = window.CrComLib.bridgeReceiveBooleanFromNative;
  window.bridgeReceiveStringFromNative = window.CrComLib.bridgeReceiveStringFromNative;
  window.bridgeReceiveObjectFromNative = window.CrComLib.bridgeReceiveObjectFromNative;

  // Kick the camera play signal so any mounted ch5-video element with
  // receivestateplay="CamPlay" begins streaming. Done two ways:
  //   1. Subscribe to Csig.Platform_Info — proper way per persona, fires
  //      when the native platform is ready.
  //   2. Publish unconditionally after a short delay — fallback in case
  //      Platform_Info never fires (custom firmware, restart timing).
  window.CrComLib.subscribeState('o', 'Csig.Platform_Info', (_info: unknown) => {
    console.log('[CH5] Csig.Platform_Info received — publishing CamPlay');
    window.CrComLib.publishEvent('b', 'CamPlay', true);
  });
  setTimeout(() => {
    console.log('[CH5] Fallback: publishing CamPlay=true after 1s');
    window.CrComLib.publishEvent('b', 'CamPlay', true);
  }, 1000);

  initSignals();
} else {
  console.warn('[CH5] CrComLib not detected - signals will not function');
}

// Activate the MCCCD campus orange theme — overrides the default cyan tokens
// in global.css to amber #f5a623 / navy #0d1b2e to match the existing room
// panel (Mockup 11 in the gallery).
const appEl = document.getElementById('app')!;
appEl.classList.add('theme-orange');

const app = mount(App, {
  target: appEl,
});

export default app;
