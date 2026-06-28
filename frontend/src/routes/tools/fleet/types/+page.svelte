<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { DataTable, HasPermission } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import type { EquipmentTypeDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	let items = $state<EquipmentTypeDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	onMount(async () => {
		try {
			items = await apiClient.typesAll2();
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
		<h1 class="mt-1 text-2xl font-bold text-gray-900">Materiaaltypen</h1>
	</div>
	<HasPermission permission={BluePermission.FleetModify}>
		<a
			href={resolve('/tools/fleet/types/new')}
			class="text-sm font-medium text-primary-hover hover:underline"
		>
			Nieuw type
		</a>
	</HasPermission>
</div>

{#snippet nameCell(item: EquipmentTypeDto)}
	<span class="font-medium text-gray-900">{item.name}</span>
{/snippet}
{#snippet codeCell(item: EquipmentTypeDto)}
	<span class="text-gray-600">{item.code}</span>
{/snippet}
{#snippet rowersCell(item: EquipmentTypeDto)}
	<span class="text-gray-600">{item.rowersCount}</span>
{/snippet}
{#snippet scullCell(item: EquipmentTypeDto)}
	{#if item.scull}
		<span
			class="inline-flex items-center rounded-full bg-blue-100 px-2 py-0.5 text-xs font-medium text-blue-800"
			>Ja</span
		>
	{:else}
		<span class="text-gray-400">Nee</span>
	{/if}
{/snippet}
{#snippet coxedCell(item: EquipmentTypeDto)}
	{#if item.coxed}
		<span
			class="inline-flex items-center rounded-full bg-blue-100 px-2 py-0.5 text-xs font-medium text-blue-800"
			>Ja</span
		>
	{:else}
		<span class="text-gray-400">Nee</span>
	{/if}
{/snippet}
{#snippet isBoatCell(item: EquipmentTypeDto)}
	{#if item.isBoat}
		<span
			class="inline-flex items-center rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800"
			>Ja</span
		>
	{:else}
		<span class="text-gray-400">Nee</span>
	{/if}
{/snippet}

<DataTable
	columns={[
		{ header: 'Naam', cell: nameCell },
		{ header: 'Code', cell: codeCell },
		{ header: 'Roeiers', cell: rowersCell },
		{ header: 'Scull', cell: scullCell },
		{ header: 'Stuurman', cell: coxedCell },
		{ header: 'Boot', cell: isBoatCell }
	]}
	{items}
	{loading}
	error={error ? 'Materiaaltypen konden niet worden geladen.' : undefined}
	emptyMessage="Geen materiaaltypen gevonden."
	rowHref={(item) => resolve('/tools/fleet/types/[id]', { id: item.id })}
/>
