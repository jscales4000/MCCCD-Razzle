<script lang="ts">
  import { onDestroy } from 'svelte';

  interface Props {
    open: boolean;
    countdown?: number;       // seconds, default 30
    title?: string;
    body?: string;
    onConfirm: () => void;
    onCancel: () => void;
  }

  let {
    open,
    countdown = 30,
    title = 'Shutdown AA140',
    body = 'Are you sure you want to shut down?',
    onConfirm,
    onCancel,
  }: Props = $props();

  let remaining = $state(countdown);
  let timer: ReturnType<typeof setInterval> | undefined;

  $effect(() => {
    if (open) {
      remaining = countdown;
      timer = setInterval(() => {
        remaining -= 1;
        if (remaining <= 0) {
          if (timer !== undefined) clearInterval(timer);
          timer = undefined;
          onConfirm();
        }
      }, 1000);
    } else if (timer !== undefined) {
      clearInterval(timer);
      timer = undefined;
    }
  });

  onDestroy(() => {
    if (timer !== undefined) clearInterval(timer);
  });

  function handleConfirm() {
    if (timer !== undefined) {
      clearInterval(timer);
      timer = undefined;
    }
    onConfirm();
  }

  function handleCancel() {
    if (timer !== undefined) {
      clearInterval(timer);
      timer = undefined;
    }
    onCancel();
  }
</script>

{#if open}
  <div class="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="shutdown-title">
    <div class="glass-card modal-card">
      <h2 id="shutdown-title">{title}</h2>
      <p class="modal-body">{body}</p>
      <p class="countdown">
        The system will power off automatically in
        <strong>{remaining}</strong>
        second{remaining === 1 ? '' : 's'}.
      </p>
      <div class="modal-actions">
        <button class="btn ghost" onclick={handleCancel}>No, keep on</button>
        <button class="btn danger" onclick={handleConfirm}>Yes, shut down</button>
      </div>
    </div>
  </div>
{/if}

<style>
  .modal-backdrop {
    position: fixed;
    inset: 0;
    background: rgba(2, 6, 23, 0.78);
    display: grid;
    place-items: center;
    z-index: 1000;
    backdrop-filter: blur(8px);
    animation: fade-in 140ms ease;
  }
  .modal-card {
    width: 540px;
    max-width: 92%;
    padding: 32px 32px 28px;
    display: flex;
    flex-direction: column;
    gap: 16px;
    animation: lift-in 200ms cubic-bezier(0.2, 0.8, 0.2, 1);
  }
  .modal-card h2 {
    margin: 0;
    font-size: 26px;
    font-weight: 700;
    color: #ffffff;
    letter-spacing: 0.02em;
  }
  .modal-body {
    margin: 0;
    color: var(--color-copy-soft);
    font-size: 16px;
    line-height: 1.5;
  }
  .countdown {
    margin: 0;
    color: var(--color-copy);
    font-size: 14px;
  }
  .countdown strong {
    color: var(--color-accent);
    font-size: 22px;
    font-variant-numeric: tabular-nums;
    margin: 0 4px;
  }
  .modal-actions {
    display: flex;
    gap: 12px;
    margin-top: 8px;
    justify-content: flex-end;
  }
  .modal-actions .btn {
    min-width: 160px;
  }

  @keyframes fade-in {
    from { opacity: 0; }
    to { opacity: 1; }
  }
  @keyframes lift-in {
    from { opacity: 0; transform: translateY(8px); }
    to { opacity: 1; transform: translateY(0); }
  }

  @media (prefers-reduced-motion: reduce) {
    .modal-backdrop,
    .modal-card { animation: none; }
  }
</style>
