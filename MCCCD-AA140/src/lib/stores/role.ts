import { writable } from 'svelte/store';

// Panel view role. 'user' is the default everyday view (boots here, every time).
// 'tech' reveals installer-level controls (mic trims, output-max, advanced camera
// settings, D5 signage routing). Role is NEVER persisted — the panel always comes
// up in 'user' view after a reload.
export type Role = 'user' | 'tech';

// Client-side PIN gate. This deters end users from the technician controls; it is
// NOT a security boundary. For a hard boundary, validate processor-side via a
// contract signal instead. Set the production value at commissioning.
export const TECH_PIN = '1988';

// Auto-revert to 'user' after this much inactivity in tech view.
export const TECH_TIMEOUT_MS = 5 * 60 * 1000;

export const role = writable<Role>('user');

let idleTimer: ReturnType<typeof setTimeout> | undefined;
let inTech = false;

function clearIdle() {
  if (idleTimer) { clearTimeout(idleTimer); idleTimer = undefined; }
}

function armIdle() {
  clearIdle();
  idleTimer = setTimeout(() => exitTech(), TECH_TIMEOUT_MS);
}

/** Validate the PIN and enter technician view. Returns false on a bad PIN. */
export function enterTech(pin: string): boolean {
  if (pin !== TECH_PIN) return false;
  inTech = true;
  role.set('tech');
  armIdle();
  return true;
}

/** Drop back to user view (manual Exit button or inactivity timeout). */
export function exitTech(): void {
  inTech = false;
  clearIdle();
  role.set('user');
}

/** Reset the inactivity timer on panel interaction — only matters while in tech. */
export function bumpActivity(): void {
  if (inTech) armIdle();
}
