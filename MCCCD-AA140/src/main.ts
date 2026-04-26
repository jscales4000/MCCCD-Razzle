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
  }
}

if (window.CrComLib) {
  console.log('[CH5] CrComLib detected, initializing...');
  initSignals();
} else {
  console.warn('[CH5] CrComLib not detected - signals will not function');
}

const app = mount(App, {
  target: document.getElementById('app')!,
});

export default app;
