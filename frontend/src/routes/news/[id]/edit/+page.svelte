<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { NewsForm, breadcrumbs } from '$lib';
	import type { NewsPostDto, UpsertNewsPostRequest } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let post = $state<NewsPostDto | null>(null);
	let error = $state(false);

	onMount(async () => {
		try {
			post = await apiClient.newsGET2(params.id);
		} catch {
			error = true;
		}
	});

	async function handleEdit(request: UpsertNewsPostRequest) {
		await apiClient.newsPUT(params.id, request);
		goto(resolve('/news/[id]', { id: params.id }));
	}

	$effect(() => {
		breadcrumbs.set([{ label: 'Nieuws', href: '/news' }, { label: 'Bericht bewerken' }]);
		return () => breadcrumbs.clear();
	});
</script>

<div class="mx-auto max-w-2xl px-4 py-12 sm:px-6 lg:px-8">
	<h1 class="text-2xl font-bold text-gray-900">Bericht bewerken</h1>
	<div class="mt-6">
		{#if error}
			<p class="text-sm text-gray-600">Nieuws kon niet worden geladen.</p>
		{:else if post}
			<NewsForm {post} submitLabel="Opslaan" onSubmit={handleEdit} />
		{/if}
	</div>
</div>
