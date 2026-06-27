<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { EquipmentTypeForm, breadcrumbs } from '$lib';
	import type { UpsertEquipmentTypeRequest } from '$lib/api/apiClient';

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Vloot', href: '/tools/fleet' },
			{ label: 'Materiaaltypen', href: '/tools/fleet/types' },
			{ label: 'Nieuw type' }
		]);
		return () => breadcrumbs.clear();
	});

	async function handleCreate(request: UpsertEquipmentTypeRequest) {
		const created = await apiClient.typesPOST2(request);
		goto(resolve('/tools/fleet/types/[id]', { id: created.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Nieuw materiaaltype</h1>
<div class="mt-6">
	<EquipmentTypeForm submitLabel="Aanmaken" onSubmit={handleCreate} />
</div>
