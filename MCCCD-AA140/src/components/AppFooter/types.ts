// MCCCD AppFooter — reusable shared types.
//
// Any project that adopts the AppFooter component imports these types and
// builds its own room-specific config out of them. See ./README.md for the
// integration recipe.

/**
 * One row in the shutdown-confirmation modal's checklist.
 * Used to remind the user what will turn off.
 */
export type ShutdownItem = {
  icon: 'display' | 'audio' | 'camera';
  label: string;
};

/**
 * One mic button in the footer. The parent component owns the signal /
 * publishing logic — AppFooter just renders state and calls back on tap.
 *
 * - `isMuted` is passed reactively: when the underlying feedback store
 *   updates, the parent's $derived will re-evaluate and push a new value in.
 * - `onToggle` is what fires when the user taps. The parent decides what
 *   "toggle" means for that mic (typically a `publishDigital(SIGNALS.micXMute, !isMuted)`).
 *
 * Empty array → no mic group is rendered.
 * 1–3 mics → standard layout.
 * 4+ mics → buttons shrink; consider moving to a dedicated Mics page instead.
 */
export type MicChannel = {
  id: string;
  label: string;
  isMuted: boolean;
  onToggle: () => void;
};

/**
 * Power button visual state.
 *
 * When `isOn` is true the chip shows "On" in accent color. When false it
 * dims to a neutral gray "Off" and the icon greys out.
 */
export type FooterPower = {
  isOn: boolean;
};

/**
 * Volume state. Level is 0–100. The component shows it as the big readout
 * between the +/- buttons.
 */
export type FooterVolume = {
  level: number;
};
