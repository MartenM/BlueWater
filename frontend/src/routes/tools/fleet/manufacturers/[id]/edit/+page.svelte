<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { ManufacturerForm, breadcrumbs } from '$lib';
	import type { UpsertManufacturerRequest, ManufacturerDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let manufacturer = $state<ManufacturerDto | null>(null);
	let error = $state(false);

	onMount(async () => {
		try {
			manufacturer = await apiClient.manufacturersGET(params.id);
		} catch {
			error = true;
		}
	});

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Vloot', href: '/tools/fleet' },
			{ label: 'Fabrikanten', href: '/tools/fleet/manufacturers' },
			{ label: 'Fabrikant bewerken' }
		]);
		return () => breadcrumbs.clear();
	});

	async function handleUpdate(request: UpsertManufacturerRequest) {
		await apiClient.manufacturersPUT(params.id, request);
		goto(resolve('/tools/fleet/manufacturers/[id]', { id: params.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Fabrikant bewerken</h1>
<div class="mt-6">
	{#if error}
		<p class="text-sm text-gray-600">Fabrikant kon niet worden geladen.</p>
	{:else if manufacturer}
		<ManufacturerForm {manufacturer} submitLabel="Opslaan" onSubmit={handleUpdate} />
	{/if}
</div>
