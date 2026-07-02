<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { goto } from '$app/navigation';
	import { Spinner, breadcrumbs, OutingForm } from '$lib';
	import type { OutingDetailDto, UpsertOutingRequest } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let outing = $state<OutingDetailDto | null>(null);
	let error = $state(false);
	let loading = $state(true);

	onMount(async () => {
		try {
			outing = await apiClient.outingsGET(params.id);
			if (outing.confirmed) {
				goto(resolve('/tools/outing-planner/[id]', { id: params.id }));
			}
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (outing) {
			breadcrumbs.set([
				{ label: 'Outing Planner', href: '/tools/outing-planner' },
				{
					label: outing.outingDate.toLocaleDateString('nl-NL'),
					href: `/tools/outing-planner/${params.id}`
				},
				{ label: 'Bewerken' }
			]);
		}
		return () => breadcrumbs.clear();
	});

	async function handleSubmit(request: UpsertOutingRequest) {
		await apiClient.outingsPUT(params.id, request);
		goto(resolve('/tools/outing-planner/[id]', { id: params.id }));
	}
</script>

{#if loading}
	<Spinner />
{:else if error || !outing}
	<p class="text-sm text-gray-600">Outing kon niet worden geladen.</p>
{:else}
	<h1 class="text-2xl font-bold text-gray-900">Outing bewerken</h1>

	<div class="mt-6">
		<OutingForm {outing} submitLabel="Opslaan" onSubmit={handleSubmit} />
	</div>
{/if}
