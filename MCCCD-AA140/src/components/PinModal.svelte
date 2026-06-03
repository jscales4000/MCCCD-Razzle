<!--
  PinModal — numeric keypad gate for Technician view. On a correct PIN it calls
  enterTech() (which flips the role store) and closes. Wrong PIN shakes + clears.
  Client-side gate only — see stores/role.ts.
-->
<script lang="ts">
  import { enterTech } from '../lib/stores/role';

  interface Props {
    onClose: () => void;
  }
  let { onClose }: Props = $props();

  const MAX = 8;
  let entry = $state('');
  let error = $state(false);

  function press(d: string) {
    if (error) { error = false; entry = ''; }
    if (entry.length < MAX) entry += d;
  }
  function backspace() {
    error = false;
    entry = entry.slice(0, -1);
  }
  function submit() {
    if (enterTech(entry)) {
      onClose();
    } else {
      error = true;
    }
  }

  const KEYS = ['1', '2', '3', '4', '5', '6', '7', '8', '9'];
</script>

<div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="pin-title">
  <div class="pin-card" class:shake={error}>
    <div class="pin-head">
      <span class="pin-eyebrow">Restricted</span>
      <h2 id="pin-title" class="pin-title">Technician PIN</h2>
    </div>

    <div class="pin-dots" aria-hidden="true">
      {#each Array(MAX) as _, i}
        <span class="dot" class:filled={i < entry.length} class:err={error}></span>
      {/each}
    </div>

    {#if error}
      <p class="pin-err" role="alert">Incorrect PIN</p>
    {:else}
      <p class="pin-hint">Enter the technician PIN to reveal advanced controls.</p>
    {/if}

    <div class="keypad">
      {#each KEYS as k}
        <button type="button" class="key" onclick={() => press(k)}>{k}</button>
      {/each}
      <button type="button" class="key key-fn" onclick={backspace} aria-label="Backspace">⌫</button>
      <button type="button" class="key" onclick={() => press('0')}>0</button>
      <button type="button" class="key key-go" onclick={submit} aria-label="Submit">→</button>
    </div>

    <button type="button" class="pin-cancel" onclick={onClose}>Cancel</button>
  </div>
</div>

<style>
  .modal-backdrop {
    position: fixed;
    inset: 0;
    width: 100vw;
    height: 100vh;
    background-color: #04080f;
    display: grid;
    place-items: center;
    z-index: 1100;
    animation: fade-in 140ms ease;
  }

  .pin-card {
    width: 360px;
    max-width: 92%;
    background: rgba(12, 20, 36, 0.98);
    border: 1px solid var(--color-border, rgba(148, 163, 184, 0.18));
    border-radius: 20px;
    box-shadow: 0 40px 80px rgba(0, 0, 0, 0.7);
    padding: 28px 28px 22px;
    display: flex;
    flex-direction: column;
    align-items: center;
    animation: modal-in 220ms cubic-bezier(0.16, 1, 0.3, 1);
  }
  .pin-card.shake { animation: shake 360ms cubic-bezier(0.36, 0.07, 0.19, 0.97); }

  .pin-head { text-align: center; margin-bottom: 18px; }
  .pin-eyebrow {
    font-size: 10px;
    font-weight: 700;
    letter-spacing: 0.2em;
    text-transform: uppercase;
    color: var(--color-accent, #f5a623);
  }
  .pin-title {
    margin: 4px 0 0;
    font-size: 22px;
    font-weight: 900;
    color: #fff;
  }

  .pin-dots { display: flex; gap: 10px; margin-bottom: 12px; }
  .dot {
    width: 11px;
    height: 11px;
    border-radius: 50%;
    border: 1.5px solid var(--color-border, rgba(148, 163, 184, 0.4));
    background: transparent;
    transition: background 100ms ease, border-color 100ms ease;
  }
  .dot.filled { background: var(--color-accent, #f5a623); border-color: var(--color-accent, #f5a623); }
  .dot.err.filled { background: #ef4444; border-color: #ef4444; }

  .pin-hint, .pin-err {
    margin: 0 0 18px;
    font-size: 11px;
    text-align: center;
    line-height: 1.4;
    max-width: 260px;
  }
  .pin-hint { color: var(--color-copy-muted, #64748b); }
  .pin-err { color: #fca5a5; font-weight: 700; }

  .keypad {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 8px;
    width: 100%;
  }
  .key {
    padding: 16px 0;
    border-radius: 10px;
    border: 0.5px solid var(--color-border, rgba(148, 163, 184, 0.18));
    background: rgba(30, 41, 59, 0.6);
    color: var(--color-copy, #e2e8f0);
    font-size: 20px;
    font-weight: 700;
    font-family: inherit;
    cursor: pointer;
    transition: background 100ms ease, transform 80ms ease;
  }
  .key:hover { background: rgba(51, 65, 85, 0.8); }
  .key:active { transform: scale(0.96); }
  .key-fn { color: var(--color-copy-soft, #94a3b8); }
  .key-go {
    background: rgba(245, 166, 35, 0.16);
    border-color: var(--color-accent-soft, rgba(245, 166, 35, 0.4));
    color: var(--color-accent, #f5a623);
  }

  .pin-cancel {
    margin-top: 16px;
    padding: 10px 20px;
    border-radius: 9px;
    border: 0.5px solid var(--color-border, rgba(148, 163, 184, 0.18));
    background: transparent;
    color: var(--color-copy-soft, #94a3b8);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    cursor: pointer;
    font-family: inherit;
  }
  .pin-cancel:hover { color: var(--color-copy, #e2e8f0); }

  @keyframes fade-in { from { opacity: 0; } to { opacity: 1; } }
  @keyframes modal-in {
    from { opacity: 0; transform: scale(0.94) translateY(12px); }
    to   { opacity: 1; transform: scale(1) translateY(0); }
  }
  @keyframes shake {
    10%, 90% { transform: translateX(-1px); }
    20%, 80% { transform: translateX(2px); }
    30%, 50%, 70% { transform: translateX(-4px); }
    40%, 60% { transform: translateX(4px); }
  }
  @media (prefers-reduced-motion: reduce) {
    .modal-backdrop, .pin-card, .pin-card.shake { animation: none; }
  }
</style>
