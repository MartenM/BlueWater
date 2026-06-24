<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { NewsForm, breadcrumbs } from '$lib';
	import type { UpsertNewsPostRequest } from '$lib/api/apiClient';

	async function handleCreate(request: UpsertNewsPostRequest) {
		const result = await apiClient.newsPOST(request);
		goto(resolve('/news/[id]', { id: result.id }));
	}

	$effect(() => {
		breadcrumbs.set([{ label: 'Nieuws', href: '/news' }, { label: 'Nieuw bericht' }]);
		return () => breadcrumbs.clear();
	});
</script>

<div class="mx-auto max-w-2xl px-4 py-12 sm:px-6 lg:px-8">
	<h1 class="text-2xl font-bold text-gray-900">Nieuw bericht</h1>
	<div class="mt-6">
		<NewsForm submitLabel="Plaatsen" onSubmit={handleCreate} />
	</div>
</div>
