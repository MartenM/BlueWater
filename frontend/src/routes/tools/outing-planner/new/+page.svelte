<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { goto } from '$app/navigation';
	import { breadcrumbs } from '$lib';
	import { OutingForm } from '$lib';
	import type { UpsertOutingRequest } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	onMount(() => {
		breadcrumbs.set([
			{ label: 'Outing Planner', href: '/tools/outing-planner' },
			{ label: 'Nieuwe outing' }
		]);
		return () => breadcrumbs.clear();
	});

	async function handleSubmit(request: UpsertOutingRequest) {
		const created = await apiClient.outingsPOST(request);
		goto(resolve('/tools/outing-planner/[id]', { id: created.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Nieuwe outing</h1>

<div class="mt-6">
	<OutingForm submitLabel="Aanmaken" onSubmit={handleSubmit} />
</div>
