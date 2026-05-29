<!--
  Aa140Footer — AA140-specific adapter for the reusable AppFooter.

  Wires the AA140 room's signal stores + CrComLib publishes to the
  standard MCCCD footer. Each room project keeps a thin wrapper like
  this so the pages can just drop in `<Aa140Footer />` (or the next
  room's equivalent) without repeating the wiring.

  See `src/components/AppFooter/README.md` for the standard's API.
-->

<script lang="ts">
  import { SIGNALS } from '../lib/contract';
  import { publishDigital, pulseDigital } from '../lib/CrComLib';
  import { goToPage } from '../lib/stores/page';
  import { userPoweredOn } from '../lib/stores/session';
  import {
    micLavMuteFb, micHandheldMuteFb,
    occupancyState, shutdownCountdown,
    progAudioLevelFb,
    systemPowerFb,
  } from '../lib/stores/signals';
  import { AppFooter, type MicChannel, type ShutdownItem } from './AppFooter';

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
    { icon: 'display', label: '4 Displays (D1 Front Left, D2 Front Right, D3 Rear, D4 Podium)' },
    { icon: 'audio',   label: 'Audio system + all 5 microphone channels' },
    { icon: 'camera',  label: 'Camera system (2 PTZ cameras)' },
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

  let vacancyMinutes = $derived($occupancyState === 2 ? $shutdownCountdown : undefined);
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
  {vacancyMinutes}
/>
