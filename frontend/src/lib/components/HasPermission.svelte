<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { BluePermission } from '$lib/api/apiClient';
	import { session } from '$lib/auth/session.svelte';

	let {
		permission,
		children,
		fallback
	}: {
		permission: BluePermission;
		children?: Snippet;
		fallback?: Snippet;
	} = $props();

	const authorized = $derived(session.hasPermission(permission));
</script>

{#if authorized}
	{@render children?.()}
{:else}
	{@render fallback?.()}
{/if}
