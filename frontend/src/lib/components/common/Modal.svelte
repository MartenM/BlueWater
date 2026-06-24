<script lang="ts">
	import type { Snippet } from 'svelte';

	let {
		dialog = $bindable(),
		maxWidth = 'max-w-sm',
		children
	}: {
		dialog?: HTMLDialogElement;
		maxWidth?: string;
		children: Snippet;
	} = $props();

	function handleClick(event: MouseEvent) {
		if (event.target === dialog) dialog?.close();
	}
</script>

<dialog
	bind:this={dialog}
	onclick={handleClick}
	class="m-auto w-full {maxWidth} rounded-md border border-gray-200 bg-white p-0 shadow-lg backdrop:bg-black/40"
>
	{@render children()}
</dialog>
