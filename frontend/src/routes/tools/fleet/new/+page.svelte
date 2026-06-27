<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { EquipmentForm, breadcrumbs } from '$lib';
	import type { UpsertEquipmentRequest } from '$lib/api/apiClient';

	$effect(() => {
		breadcrumbs.set([{ label: 'Vloot', href: '/tools/fleet' }, { label: 'Nieuw materiaal' }]);
		return () => breadcrumbs.clear();
	});

	async function handleCreate(request: UpsertEquipmentRequest) {
		const created = await apiClient.fleetPOST(request);
		goto(resolve('/tools/fleet/[id]', { id: created.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Nieuw materiaal</h1>
<div class="mt-6">
	<EquipmentForm submitLabel="Aanmaken" onSubmit={handleCreate} />
</div>
