<script lang="ts">
	import Modal from './Modal.svelte';
	import Button from './Button.svelte';

	let {
		dialog = $bindable(),
		message = '',
		confirmLabel = 'Verwijderen',
		cancelLabel = 'Annuleren',
		onConfirm = () => {}
	}: {
		dialog?: HTMLDialogElement;
		message?: string;
		confirmLabel?: string;
		cancelLabel?: string;
		onConfirm?: () => void;
	} = $props();
</script>

<Modal bind:dialog>
	<div class="p-6">
		<p class="text-sm text-gray-700">{message}</p>
		<div class="mt-4 flex justify-end gap-3">
			<Button type="button" variant="secondary" size="sm" onclick={() => dialog?.close()}>
				{cancelLabel}
			</Button>
			<Button
				type="button"
				variant="danger"
				size="sm"
				onclick={() => {
					dialog?.close();
					onConfirm();
				}}
			>
				{confirmLabel}
			</Button>
		</div>
	</div>
</Modal>
