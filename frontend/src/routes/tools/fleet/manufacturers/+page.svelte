<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { HasPermission, Spinner } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import type { ManufacturerDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	let items = $state<ManufacturerDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	onMount(async () => {
		try {
			items = await apiClient.manufacturersAll();
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});
</script>

<div class="flex items-center justify-between">
	<div>
		<a href={resolve('/tools/fleet')} class="text-sm text-gray-500 hover:underline">← Vloot</a>
		<h1 class="mt-1 text-2xl font-bold text-gray-900">Fabrikanten</h1>
	</div>
	<HasPermission permission={BluePermission.FleetModify}>
		<a
			href={resolve('/tools/fleet/manufacturers/new')}
			class="text-sm font-medium text-primary-hover hover:underline"
		>
			Nieuwe fabrikant
		</a>
	</HasPermission>
</div>

{#if loading}
	<Spinner />
{:else if error}
	<p class="mt-4 text-sm text-gray-600">Fabrikanten konden niet worden geladen.</p>
{:else}
	<div class="mt-6 divide-y divide-gray-200 border-t border-gray-200">
		{#each items as item (item.id)}
			<a
				href={resolve('/tools/fleet/manufacturers/[id]', { id: item.id })}
				class="flex items-center justify-between py-3 hover:bg-gray-50"
			>
				<p class="font-medium text-gray-900">{item.name}</p>
			</a>
		{:else}
			<p class="py-6 text-sm text-gray-500">Geen fabrikanten gevonden.</p>
		{/each}
	</div>
{/if}
