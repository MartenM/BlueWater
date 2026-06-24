<script lang="ts">
	import type { Snippet } from 'svelte';

	let {
		label,
		errors = [],
		children
	}: { label: string; errors?: string[]; children: Snippet<[boolean]> } = $props();

	const hasError = $derived(errors.length > 0);
</script>

<label class="flex flex-col gap-1">
	<span class="text-sm font-medium text-gray-700">{label}</span>
	{@render children(hasError)}
	{#each errors as message (message)}
		<span class="text-sm text-red-600">{message}</span>
	{/each}
</label>
