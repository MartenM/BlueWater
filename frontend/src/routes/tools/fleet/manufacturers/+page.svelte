<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { DataTable, HasPermission } from '$lib';
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

{#snippet nameCell(item: ManufacturerDto)}
	<span class="font-medium text-gray-900">{item.name}</span>
{/snippet}

<DataTable
	columns={[{ header: 'Naam', cell: nameCell }]}
	{items}
	{loading}
	error={error ? 'Fabrikanten konden niet worden geladen.' : undefined}
	emptyMessage="Geen fabrikanten gevonden."
	rowHref={(item) => resolve('/tools/fleet/manufacturers/[id]', { id: item.id })}
/>
