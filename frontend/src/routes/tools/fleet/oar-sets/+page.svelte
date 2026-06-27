<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { HasPermission, Spinner } from '$lib';
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

{#if loading}
	<Spinner />
{:else if error}
	<p class="mt-4 text-sm text-gray-600">Riemstellen konden niet worden geladen.</p>
{:else}
	<div class="mt-6 divide-y divide-gray-200 border-t border-gray-200">
		{#each items as item (item.id)}
			<a
				href={resolve('/tools/fleet/oar-sets/[id]', { id: item.id })}
				class="flex items-center justify-between py-3 hover:bg-gray-50"
			>
				<div>
					<p class="font-medium text-gray-900">{item.name}</p>
					<p class="text-sm text-gray-500">
						{item.scull ? 'Scull' : 'Sweep'}{item.manufacturerName
							? ` · ${item.manufacturerName}`
							: ''}
					</p>
				</div>
			</a>
		{:else}
			<p class="py-6 text-sm text-gray-500">Geen riemstellen gevonden.</p>
		{/each}
	</div>
{/if}
