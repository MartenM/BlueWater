<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { OarSetForm, breadcrumbs } from '$lib';
	import type { UpsertOarSetRequest, OarSetDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let oarSet = $state<OarSetDto | null>(null);
	let error = $state(false);

	onMount(async () => {
		try {
			oarSet = await apiClient.oarSetsGET(params.id);
		} catch {
			error = true;
		}
	});

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Vloot', href: '/tools/fleet' },
			{ label: 'Riemstellen', href: '/tools/fleet/oar-sets' },
			{ label: 'Riemstel bewerken' }
		]);
		return () => breadcrumbs.clear();
	});

	async function handleUpdate(request: UpsertOarSetRequest) {
		await apiClient.oarSetsPUT(params.id, request);
		goto(resolve('/tools/fleet/oar-sets/[id]', { id: params.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Riemstel bewerken</h1>
<div class="mt-6">
	{#if error}
		<p class="text-sm text-gray-600">Riemstel kon niet worden geladen.</p>
	{:else if oarSet}
		<OarSetForm {oarSet} submitLabel="Opslaan" onSubmit={handleUpdate} />
	{/if}
</div>
