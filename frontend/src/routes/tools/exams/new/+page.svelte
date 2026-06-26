<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { ExamTypeForm, breadcrumbs } from '$lib';
	import type { UpsertExamTypeRequest } from '$lib/api/apiClient';

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Examentypes', href: '/tools/exams' },
			{ label: 'Nieuw examentype' }
		]);
		return () => breadcrumbs.clear();
	});

	async function handleCreate(request: UpsertExamTypeRequest) {
		const created = await apiClient.typesPOST(request);
		goto(resolve('/tools/exams/[id]', { id: created.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Nieuw examentype</h1>
<div class="mt-6">
	<ExamTypeForm submitLabel="Aanmaken" onSubmit={handleCreate} />
</div>
