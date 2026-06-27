<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { OarSetForm, breadcrumbs } from '$lib';
	import type { UpsertOarSetRequest } from '$lib/api/apiClient';

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Vloot', href: '/tools/fleet' },
			{ label: 'Riemstellen', href: '/tools/fleet/oar-sets' },
			{ label: 'Nieuw riemstel' }
		]);
		return () => breadcrumbs.clear();
	});

	async function handleCreate(request: UpsertOarSetRequest) {
		const created = await apiClient.oarSetsPOST(request);
		goto(resolve('/tools/fleet/oar-sets/[id]', { id: created.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Nieuw riemstel</h1>
<div class="mt-6">
	<OarSetForm submitLabel="Aanmaken" onSubmit={handleCreate} />
</div>
