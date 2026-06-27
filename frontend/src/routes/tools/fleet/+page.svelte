<script lang="ts">
	import { untrack } from 'svelte';
	import { resolve } from '$app/paths';
	import { HasPermission, Pagination } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	let items = $state(untrack(() => data.items));
	let totalCount = $state(untrack(() => data.totalCount));
	let page = $state(untrack(() => data.page));
	let pageSize = $state(untrack(() => data.pageSize));
	let search = $state(untrack(() => data.search));
	let error = $state(untrack(() => data.error));

	const totalPages = $derived(Math.ceil(totalCount / pageSize));

	async function load() {
		try {
			const result = await apiClient.fleetGET(page, pageSize, search || undefined);
			items = result.items ?? [];
			totalCount = result.totalCount;
			error = false;
		} catch {
			error = true;
		}
	}

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

{#if error}
	<p class="mt-4 text-sm text-gray-600">Vloot kon niet worden geladen.</p>
{:else}
	<div class="mt-4 overflow-x-auto">
		<table class="min-w-full divide-y divide-gray-200 text-sm">
			<thead class="bg-gray-50">
				<tr>
					<th class="px-4 py-3 text-left font-medium text-gray-500">Naam</th>
					<th class="px-4 py-3 text-left font-medium text-gray-500">Type</th>
					<th class="px-4 py-3 text-left font-medium text-gray-500">Fabrikant</th>
					<th class="px-4 py-3 text-left font-medium text-gray-500">Vrije vloot</th>
					<th class="px-4 py-3 text-left font-medium text-gray-500">Buiten gebruik</th>
				</tr>
			</thead>
			<tbody class="divide-y divide-gray-200 bg-white">
				{#each items as item (item.id)}
					<tr class="hover:bg-gray-50">
						<td class="px-4 py-3">
							<a
								href={resolve('/tools/fleet/[id]', { id: item.id })}
								class="font-medium text-primary hover:underline"
							>
								{item.name}
							</a>
						</td>
						<td class="px-4 py-3 text-gray-600">{item.equipmentTypeName ?? '—'}</td>
						<td class="px-4 py-3 text-gray-600">{item.manufacturerName ?? '—'}</td>
						<td class="px-4 py-3">
							{#if item.freeFleet}
								<span
									class="inline-flex items-center rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800"
									>Ja</span
								>
							{:else}
								<span class="text-gray-400">Nee</span>
							{/if}
						</td>
						<td class="px-4 py-3">
							{#if item.outOfOrder}
								<span
									class="inline-flex items-center rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-800"
									>Ja</span
								>
							{:else}
								<span class="text-gray-400">Nee</span>
							{/if}
						</td>
					</tr>
				{:else}
					<tr>
						<td colspan="5" class="px-4 py-6 text-center text-gray-500">Geen materiaal gevonden.</td
						>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>

	{#if totalPages > 1}
		<div class="mt-4">
			<Pagination {page} {totalPages} onPageChange={goToPage} />
		</div>
	{/if}
{/if}
