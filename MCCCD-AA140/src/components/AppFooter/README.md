# AppFooter — MCCCD Standard Panel Footer

Reusable Svelte 5 component for the MCCCD CH5 touchpanel footer.
Pure presentational — owns no room-specific knowledge. All state arrives via
props, all actions are callback props. Drop it into any room's panel project
and it looks identical.

## What this component is

The visual identity of the MCCCD panel footer:

- **Power** — borderless icon + "POWER" + small "ON" / "OFF" status chip
- **Mics** — borderless waveform-animated buttons (green live, red muted)
- **Vol** — bold "−" + live readout + "+", divider, mute icon

Owns its own shutdown-confirm modal and volume-flash popup. Both are part of
the standard UX.

## What this component is NOT

- **Not a SIMPL/CrComLib client.** It does not subscribe to feedback signals
  and does not publish anything. The consuming page builds reactive arrays
  out of its own stores and passes them in.
- **Not a navigator.** It calls `onShutdownConfirm()` after the user confirms
  shutdown; what happens next (e.g. `goToPage('home')`) is the parent's call.

## Required files to copy

```
src/components/AppFooter/        ← the module (this folder)
src/components/ConfirmShutdownModal.svelte
src/components/VolumePopup.svelte
```

## Required CSS variables

Define these in the consuming project's `global.css` (or theme stylesheet)
on `:root` (or `.theme-*`). The component falls back to MCCCD orange / dark
defaults when a token is missing, so first-paint always looks right.

| Token | Purpose | MCCCD value |
|---|---|---|
| `--color-accent` | Orange highlight (power, volume readout, focus) | `#f5a623` |
| `--color-accent-soft` | Background tint for the ON chip | `rgba(245, 166, 35, 0.18)` |
| `--color-copy` | Primary text color | `#e2e8f0` |
| `--color-copy-soft` | Secondary text + OFF chip | `#94a3b8` |
| `--color-copy-muted` | Eyebrow labels ("Mics", "Vol") | `#64748b` |
| `--color-border` | Divider line between vol and mute | `rgba(148, 163, 184, 0.22)` |
| `--color-mic-live` | Mic icon + waveform when unmuted | `#4ade80` |
| `--color-mic-muted` | Mic icon + label when muted | `#fca5a5` |

Optional override token:

| Token | Purpose | Default |
|---|---|---|
| `--mic-min-width` | Min width per mic button (use when you have 4+ mics) | `auto` |

## API

```ts
import { AppFooter, type MicChannel, type ShutdownItem } from './components/AppFooter';
```

```svelte
<AppFooter
  power={{ isOn }}
  mics={mics}
  volume={{ level }}
  shutdownItems={shutdownItems}
  onPowerOn={...}
  onShutdownConfirm={...}
  onVolumeUp={...}
  onVolumeDown={...}
  onMuteToggle={...}
  vacancyMinutes={...}
  shutdownCountdown={30}
  shutdownTitle="Shut Down Room?"
/>
```

### Props

| Prop | Type | Required | Notes |
|---|---|---|---|
| `power` | `{ isOn: boolean }` | yes | Drives chip + icon state |
| `mics` | `MicChannel[]` | yes | Empty array hides the mic group entirely. 1–3 displays cleanly; 4+ buttons shrink |
| `volume` | `{ level: number }` | yes | 0–100 readout |
| `shutdownItems` | `ShutdownItem[]` | yes | Checklist rendered in the shutdown modal |
| `onPowerOn` | `() => void` | yes | Called when system is off and user taps Power |
| `onShutdownConfirm` | `() => void` | yes | Called when user confirms shutdown in the modal |
| `onVolumeUp` | `() => void` | yes | Tap up — popup flashes automatically |
| `onVolumeDown` | `() => void` | yes | Tap down — popup flashes automatically |
| `onMuteToggle` | `() => void` | yes | Master mute action |
| `vacancyMinutes` | `number` | no | If set, modal shows a vacancy-driven hint |
| `shutdownCountdown` | `number` | no | Auto-confirm ring duration (s). Default `30` |
| `shutdownTitle` | `string` | no | Modal title override. Default `'Shut Down Room?'` |

### `MicChannel` shape

```ts
type MicChannel = {
  id: string;            // unique key
  label: string;         // 'Lav' | 'Handheld' | 'Ceiling' | ...
  isMuted: boolean;      // pass current value reactively from $derived
  onToggle: () => void;  // parent does the publishDigital
};
```

### `ShutdownItem` shape

```ts
type ShutdownItem = {
  icon: 'display' | 'audio' | 'camera';
  label: string;
};
```

## Integration recipe (per room)

A typical parent page wires it like this:

```svelte
<script lang="ts">
  import { AppFooter, type MicChannel, type ShutdownItem } from '../components/AppFooter';
  import { SIGNALS } from '../lib/contract';
  import { publishDigital, pulseDigital } from '../lib/CrComLib';
  import { userPoweredOn } from '../lib/stores/session';
  import { goToPage } from '../lib/stores/page';
  import {
    systemPowerFb, progAudioLevelFb,
    micLavMuteFb, micHandheldMuteFb,
    occupancyState, shutdownCountdown,
  } from '../lib/stores/signals';

  let isOn = $derived($systemPowerFb || $userPoweredOn);

  let mics = $derived<MicChannel[]>([
    {
      id: 'lav',
      label: 'Lav',
      isMuted: $micLavMuteFb,
      onToggle: () => publishDigital(SIGNALS.micLavMute, !$micLavMuteFb),
    },
    {
      id: 'handheld',
      label: 'Handheld',
      isMuted: $micHandheldMuteFb,
      onToggle: () => publishDigital(SIGNALS.micHandheldMute, !$micHandheldMuteFb),
    },
  ]);

  const shutdownItems: ShutdownItem[] = [
    { icon: 'display', label: '4 Displays' },
    { icon: 'audio',   label: 'Audio system + all mics' },
    { icon: 'camera',  label: 'Camera system' },
  ];

  function powerOn() {
    userPoweredOn.set(true);
    pulseDigital(SIGNALS.displayPower);
  }

  function shutdownConfirmed() {
    userPoweredOn.set(false);
    pulseDigital(SIGNALS.displayPower);
    goToPage('home');
  }
</script>

<AppFooter
  power={{ isOn }}
  {mics}
  volume={{ level: $progAudioLevelFb }}
  {shutdownItems}
  onPowerOn={powerOn}
  onShutdownConfirm={shutdownConfirmed}
  onVolumeUp={() => pulseDigital(SIGNALS.volumeUp)}
  onVolumeDown={() => pulseDigital(SIGNALS.volumeDown)}
  onMuteToggle={() => pulseDigital(SIGNALS.muteAll)}
  vacancyMinutes={$occupancyState === 2 ? $shutdownCountdown : undefined}
/>
```

## Adoption checklist for a new room project

1. Copy `src/components/AppFooter/` (this folder).
2. Copy `src/components/ConfirmShutdownModal.svelte` + `VolumePopup.svelte`.
3. Add the 8 CSS tokens to `global.css` (see table above).
4. In each page that needs the footer, build the wiring above (typically a
   shared helper if you have more than two pages).
5. The look should be pixel-identical to AA140's footer. If something looks
   off, check that all 8 CSS tokens resolved — that's the only thing that
   can drift.

## Mic count guidance

- **0 mics** — footer renders Power | (spacer) | Vol. Useful for rooms with
  no built-in mics.
- **1–3 mics** — fits comfortably at 1280×800.
- **4+ mics** — buttons shrink. Consider whether the footer is the right
  place — at 4+ a dedicated Mics page tends to feel cleaner. If you do
  ship 4+ in the footer, set `--mic-min-width: 140px` (or smaller) in the
  consuming project's CSS.

## Visual variant rule

Visual styling — sizes, colors, animation timing, layout proportions — is
intentionally NOT configurable. That's what makes this "the MCCCD footer."
If a future room needs something visually different, build a separate
variant component rather than parameterizing this one. Standards only work
when they hold the line.

## Change log

- **2026-05-29** — Initial extraction from AA140. Locked V4 waveform mics +
  V2 chip power + F bold +/- vol. Restore point: `checkpoint-footer-shipped`.
