<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { ManufacturerForm, breadcrumbs } from '$lib';
	import type { UpsertManufacturerRequest } from '$lib/api/apiClient';

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Vloot', href: '/tools/fleet' },
			{ label: 'Fabrikanten', href: '/tools/fleet/manufacturers' },
			{ label: 'Nieuwe fabrikant' }
		]);
		return () => breadcrumbs.clear();
	});

	async function handleCreate(request: UpsertManufacturerRequest) {
		const created = await apiClient.manufacturersPOST(request);
		goto(resolve('/tools/fleet/manufacturers/[id]', { id: created.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Nieuwe fabrikant</h1>
<div class="mt-6">
	<ManufacturerForm submitLabel="Aanmaken" onSubmit={handleCreate} />
</div>
