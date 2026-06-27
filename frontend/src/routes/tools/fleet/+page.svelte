<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { DataTable, HasPermission, Pagination, Spinner } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import type { EquipmentDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	let items = $state<EquipmentDto[]>([]);
	let totalCount = $state(0);
	let page = $state(1);
	const pageSize = 25;
	let search = $state('');
	let error = $state(false);
	let loading = $state(true);

	const totalPages = $derived(Math.ceil(totalCount / pageSize));

	async function load() {
		loading = true;
		try {
			const result = await apiClient.fleetGET(page, pageSize, search || undefined);
			items = result.items ?? [];
			totalCount = result.totalCount;
			error = false;
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	}

	onMount(load);

	function handleSearchSubmit(event: SubmitEvent) {
		event.preventDefault();
		page = 1;
		load();
	}

	function goToPage(p: number) {
		page = p;
		load();
	}
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Vloot</h1>
	<HasPermission permission={BluePermission.FleetModify}>
		<a
			href={resolve('/tools/fleet/new')}
			class="text-sm font-medium text-primary-hover hover:underline"
		>
			Nieuw materiaal
		</a>
	</HasPermission>
</div>

<div class="mt-4 flex items-center justify-between gap-4">
	<form class="flex gap-2" onsubmit={handleSearchSubmit}>
		<input
			type="search"
			placeholder="Zoeken..."
			bind:value={search}
			class="rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
		/>
		<button
			type="submit"
			class="rounded-md bg-primary px-3 py-1.5 text-sm text-white hover:bg-primary-hover"
		>
			Zoeken
		</button>
	</form>
	<div class="flex gap-4 text-sm text-gray-500">
		<a href={resolve('/tools/fleet/types')} class="hover:underline">Types</a>
		<a href={resolve('/tools/fleet/manufacturers')} class="hover:underline">Fabrikanten</a>
		<a href={resolve('/tools/fleet/oar-sets')} class="hover:underline">Riemstellen</a>
	</div>
</div>

{#if loading}
	<Spinner />
{:else if error}
	<p class="mt-4 text-sm text-gray-600">Vloot kon niet worden geladen.</p>
{:else}
	{#snippet nameCell(item: EquipmentDto)}
		<a
			href={resolve('/tools/fleet/[id]', { id: item.id })}
			class="font-medium text-primary hover:underline"
		>
			{item.name}
		</a>
	{/snippet}
	{#snippet typeCell(item: EquipmentDto)}
		<span class="text-gray-600">{item.equipmentTypeName ?? '—'}</span>
	{/snippet}
	{#snippet manufacturerCell(item: EquipmentDto)}
		<span class="text-gray-600">{item.manufacturerName ?? '—'}</span>
	{/snippet}
	{#snippet freeFleetCell(item: EquipmentDto)}
		{#if item.freeFleet}
			<span
				class="inline-flex items-center rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800"
				>Ja</span
			>
		{:else}
			<span class="text-gray-400">Nee</span>
		{/if}
	{/snippet}
	{#snippet outOfOrderCell(item: EquipmentDto)}
		{#if item.outOfOrder}
			<span
				class="inline-flex items-center rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-800"
				>Ja</span
			>
		{:else}
			<span class="text-gray-400">Nee</span>
		{/if}
	{/snippet}

	<DataTable
		columns={[
			{ header: 'Naam', cell: nameCell },
			{ header: 'Type', cell: typeCell },
			{ header: 'Fabrikant', cell: manufacturerCell },
			{ header: 'Vrije vloot', cell: freeFleetCell },
			{ header: 'Buiten gebruik', cell: outOfOrderCell }
		]}
		{items}
		emptyMessage="Geen materiaal gevonden."
	/>

	{#if totalPages > 1}
		<div class="mt-4">
			<Pagination {page} {totalPages} onPageChange={goToPage} />
		</div>
	{/if}
{/if}
