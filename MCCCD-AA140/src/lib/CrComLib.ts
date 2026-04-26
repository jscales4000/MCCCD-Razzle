// Typed wrapper for CrComLib global API
declare global {
  interface Window {
    CrComLib: any;
  }
}

const CrComLib = window.CrComLib;

export function publishDigital(join: string, value: boolean): void {
  CrComLib?.publishEvent('b', join, value);
}

export function publishAnalog(join: string, value: number): void {
  CrComLib?.publishEvent('n', join, value);
}

export function publishSerial(join: string, value: string): void {
  CrComLib?.publishEvent('s', join, value);
}

export function subscribeDigital(join: string, callback: (value: boolean) => void): string {
  return CrComLib?.subscribeState('b', join, (val: string | boolean) => {
    callback(val === 'true' || val === true);
  }) ?? '';
}

export function subscribeAnalog(join: string, callback: (value: number) => void): string {
  return CrComLib?.subscribeState('n', join, (val: string | number) => {
    callback(typeof val === 'number' ? val : parseInt(val, 10));
  }) ?? '';
}

export function subscribeSerial(join: string, callback: (value: string) => void): string {
  return CrComLib?.subscribeState('s', join, callback) ?? '';
}

export function unsubscribeDigital(id: string): void {
  CrComLib?.unsubscribeState('b', '', id);
}

export function unsubscribeAnalog(id: string): void {
  CrComLib?.unsubscribeState('n', '', id);
}

export function unsubscribeSerial(id: string): void {
  CrComLib?.unsubscribeState('s', '', id);
}

export function pulseDigital(join: string): void {
  publishDigital(join, true);
  setTimeout(() => publishDigital(join, false), 100);
}
