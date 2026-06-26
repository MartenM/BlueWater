<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { ExamTypeForm, breadcrumbs } from '$lib';
	import type { UpsertExamTypeRequest, ExamTypeDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let examType = $state<ExamTypeDto | null>(null);
	let error = $state(false);

	onMount(async () => {
		try {
			examType = await apiClient.typesGET(params.id);
		} catch {
			error = true;
		}
	});

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Examentypes', href: '/tools/exams' },
			{ label: 'Examentype bewerken' }
		]);
		return () => breadcrumbs.clear();
	});

	async function handleUpdate(request: UpsertExamTypeRequest) {
		await apiClient.typesPUT(params.id, request);
		goto(resolve('/tools/exams/[id]', { id: params.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Examentype bewerken</h1>
<div class="mt-6">
	{#if error}
		<p class="text-sm text-gray-600">Examentype kon niet worden geladen.</p>
	{:else if examType}
		<ExamTypeForm {examType} submitLabel="Opslaan" onSubmit={handleUpdate} />
	{/if}
</div>
