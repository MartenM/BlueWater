<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { EquipmentTypeForm, breadcrumbs } from '$lib';
	import type { UpsertEquipmentTypeRequest, EquipmentTypeDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let equipmentType = $state<EquipmentTypeDto | null>(null);
	let error = $state(false);

	onMount(async () => {
		try {
			equipmentType = await apiClient.typesGET2(params.id);
		} catch {
			error = true;
		}
	});

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Vloot', href: '/tools/fleet' },
			{ label: 'Materiaaltypen', href: '/tools/fleet/types' },
			{ label: 'Type bewerken' }
		]);
		return () => breadcrumbs.clear();
	});

	async function handleUpdate(request: UpsertEquipmentTypeRequest) {
		await apiClient.typesPUT2(params.id, request);
		goto(resolve('/tools/fleet/types/[id]', { id: params.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Materiaaltype bewerken</h1>
<div class="mt-6">
	{#if error}
		<p class="text-sm text-gray-600">Materiaaltype kon niet worden geladen.</p>
	{:else if equipmentType}
		<EquipmentTypeForm {equipmentType} submitLabel="Opslaan" onSubmit={handleUpdate} />
	{/if}
</div>
