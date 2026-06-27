<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { EquipmentForm, breadcrumbs } from '$lib';
	import type { UpsertEquipmentRequest, EquipmentDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let equipment = $state<EquipmentDto | null>(null);
	let error = $state(false);

	onMount(async () => {
		try {
			equipment = await apiClient.fleetGET2(params.id);
		} catch {
			error = true;
		}
	});

	$effect(() => {
		breadcrumbs.set([{ label: 'Vloot', href: '/tools/fleet' }, { label: 'Materiaal bewerken' }]);
		return () => breadcrumbs.clear();
	});

	async function handleUpdate(request: UpsertEquipmentRequest) {
		await apiClient.fleetPUT(params.id, request);
		goto(resolve('/tools/fleet/[id]', { id: params.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Materiaal bewerken</h1>
<div class="mt-6">
	{#if error}
		<p class="text-sm text-gray-600">Materiaal kon niet worden geladen.</p>
	{:else if equipment}
		<EquipmentForm {equipment} submitLabel="Opslaan" onSubmit={handleUpdate} />
	{/if}
</div>
