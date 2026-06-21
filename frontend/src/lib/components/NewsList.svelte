<script lang="ts">
	import { onMount } from 'svelte';
	import { ChevronRight } from '@lucide/svelte';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import NewsItemShort from './NewsItemShort.svelte';
	import type { NewsPostDto } from '$lib/api/apiClient';

	let { limit = 2 }: { limit?: number } = $props();

	let items = $state<NewsPostDto[]>([]);
	let totalCount = $state(0);
	let error = $state(false);

	onMount(async () => {
		try {
			const result = await apiClient.newsGET(1, limit);
			items = result.items;
			totalCount = result.totalCount;
		} catch {
			error = true;
		}
	});
</script>

<section>
	<h2 class="text-lg font-semibold text-gray-900">Nieuws</h2>
	{#if error}
		<p class="mt-4 text-sm text-gray-600">Nieuws kon niet worden geladen.</p>
	{:else}
		<div class="mt-4 divide-y divide-gray-200 border-t border-gray-200">
			{#each items as item (item.id)}
				<NewsItemShort {item} />
			{/each}
		</div>
		{#if totalCount > limit}
			<a
				href={resolve('/news')}
				class="mt-4 inline-flex items-center gap-1 text-sm font-medium text-primary-hover hover:underline"
			>
				Lees meer nieuws
				<ChevronRight class="size-4" />
			</a>
		{/if}
	{/if}
</section>
