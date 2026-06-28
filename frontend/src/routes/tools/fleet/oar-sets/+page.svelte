<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { DataTable, HasPermission } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import type { OarSetDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	let items = $state<OarSetDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	onMount(async () => {
		try {
			items = await apiClient.oarSetsAll();
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
		<h1 class="mt-1 text-2xl font-bold text-gray-900">Riemstellen</h1>
	</div>
	<HasPermission permission={BluePermission.FleetModify}>
		<a
			href={resolve('/tools/fleet/oar-sets/new')}
			class="text-sm font-medium text-primary-hover hover:underline"
		>
			Nieuw riemstel
		</a>
	</HasPermission>
</div>

{#snippet nameCell(item: OarSetDto)}
	<span class="font-medium text-gray-900">{item.name}</span>
{/snippet}
{#snippet typeCell(item: OarSetDto)}
	<span class="text-gray-600">{item.scull ? 'Scull' : 'Sweep'}</span>
{/snippet}
{#snippet manufacturerCell(item: OarSetDto)}
	<span class="text-gray-600">{item.manufacturerName ?? '—'}</span>
{/snippet}

<DataTable
	columns={[
		{ header: 'Naam', cell: nameCell },
		{ header: 'Type', cell: typeCell },
		{ header: 'Fabrikant', cell: manufacturerCell }
	]}
	{items}
	{loading}
	error={error ? 'Riemstellen konden niet worden geladen.' : undefined}
	emptyMessage="Geen riemstellen gevonden."
	rowHref={(item) => resolve('/tools/fleet/oar-sets/[id]', { id: item.id })}
/>
