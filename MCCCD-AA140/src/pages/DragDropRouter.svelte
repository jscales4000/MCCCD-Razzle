<!--
  DragDropRouter — experimental sub-page preserving the Stage 2 drag-drop UX.

  STATUS: Parked. Not reachable from main nav. Reach by calling
  goToPage('dragdrop') from a dev console or a future settings toggle.

  CONTEXT: Built across 22 commits on feat/drag-drop-router-mockup.
  See:
    - MCCCD-AA140/docs/Lessons-Learned/Drag-Drop-Source-Routing-Lessons.md
    - MCCCD-AA140/docs/Lessons-Learned/Drag-Drop-Source-Routing-Writeup.md
    - MCCCD-AA140/docs/superpowers/specs/2026-05-01-drag-drop-stage-2-svelte-port-design.md

  When the panel chrome is realigned to Mockup 10, this page will be redesigned
  to fit (the source-rail-as-source-palette conflicts with Mockup 10's
  source-rail-as-nav-rail). For now it lives as a self-contained snapshot of
  the working drag-drop interaction model so the code isn't lost.
-->

<script lang="ts">
  import { SIGNALS, ROOM_NAME } from '../lib/contract';
  import {
    display1SourceFb, display2SourceFb, display3SourceFb,
    display1PowerFb, display2PowerFb, display3PowerFb,
    audioOutputSelectFb,
  } from '../lib/stores/signals';
  import { publishAnalog, pulseDigital } from '../lib/CrComLib';
  import { goToPage } from '../lib/stores/page';
  import SourceRail from '../components/SourceRail.svelte';
  import DropZoneTile from '../components/DropZoneTile.svelte';

  function setAudioOutput(v: 1 | 2) { publishAnalog(SIGNALS.audioOutputSelect, v); }
  function mirrorD1ToD3() { pulseDigital(SIGNALS.d1MirrorToD3); }
  function mirrorD2ToD3() { pulseDigital(SIGNALS.d2MirrorToD3); }
</script>

<svelte:head>
  <title>{ROOM_NAME} CH5 Panel — Drag-Drop (Experimental)</title>
</svelte:head>

<div class="panel-stage">
  <div class="dd-shell">
    <header class="dd-header">
      <div class="dd-eyebrow">Experimental — Drag &amp; Drop Router</div>
      <button class="dd-back" onclick={() => goToPage('home')} aria-label="Back to Home">
        ← Back
      </button>
    </header>

    <aside class="dd-rail-host">
      <SourceRail />
    </aside>

    <main class="dd-tiles">
      <DropZoneTile
        label="Display 1"
        displayId="d1"
        activeSourceFb={$display1SourceFb}
        powerOn={$display1PowerFb}
        audioActive={$audioOutputSelectFb === 1}
        onAudioToggle={() => setAudioOutput(1)}
        onMirrorToD3={mirrorD1ToD3}
      />
      <DropZoneTile
        label="Display 2"
        displayId="d2"
        activeSourceFb={$display2SourceFb}
        powerOn={$display2PowerFb}
        audioActive={$audioOutputSelectFb === 2}
        onAudioToggle={() => setAudioOutput(2)}
        onMirrorToD3={mirrorD2ToD3}
      />
      <DropZoneTile
        label="Display 3"
        displayId="d3"
        activeSourceFb={$display3SourceFb}
        powerOn={$display3PowerFb}
      />
    </main>
  </div>
</div>

<style>
  .dd-shell {
    display: grid;
    grid-template-rows: 56px 1fr;
    grid-template-columns: 96px 1fr;
    grid-template-areas:
      "header header"
      "rail   tiles";
    gap: 16px;
    width: 100%;
    height: 100%;
    padding: 16px;
  }

  .dd-header {
    grid-area: header;
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0 16px;
    background: var(--color-panel);
    border: 0.5px solid var(--color-border);
    border-radius: var(--radius-panel);
  }

  .dd-eyebrow {
    font-size: 11px;
    font-weight: 800;
    letter-spacing: 0.2em;
    text-transform: uppercase;
    color: var(--color-accent);
  }

  .dd-back {
    background: transparent;
    border: 0.5px solid var(--color-border);
    color: var(--color-copy-soft);
    padding: 8px 14px;
    border-radius: var(--radius-button);
    font-size: 12px;
    font-weight: 700;
    cursor: pointer;
  }
  .dd-back:hover {
    color: var(--color-copy);
    border-color: var(--color-accent-soft);
  }

  .dd-rail-host {
    grid-area: rail;
    min-height: 0;
  }

  .dd-tiles {
    grid-area: tiles;
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 16px;
    min-height: 0;
  }
</style>
