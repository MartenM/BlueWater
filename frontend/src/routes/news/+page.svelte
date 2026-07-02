<script lang="ts">
	import { untrack } from 'svelte';
	import { pushState } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { HasPermission, NewsItemShort, Pagination } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	const NEWS_PAGE_SIZE = 5;

	let { data }: PageProps = $props();

	let currentPage = $state(untrack(() => data.page));
	let items = $state(untrack(() => data.items));
	let totalCount = $state(untrack(() => data.totalCount));
	let error = $state(untrack(() => data.error));

	const totalPages = $derived(Math.max(1, Math.ceil(totalCount / NEWS_PAGE_SIZE)));

	function handlePageChange(next: number) {
		currentPage = next;
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- query string on the current route, not a literal resolve() can check
		pushState(`?page=${next}`, {});
		apiClient
			.newsGET(next, NEWS_PAGE_SIZE)
			.then((result) => {
				items = result.items;
				totalCount = result.totalCount;
				error = false;
			})
			.catch(() => {
				error = true;
			});
	}
</script>

<div class="mx-auto max-w-3xl px-4 py-12 sm:px-6 lg:px-8">
	<div class="flex items-center justify-between">
		<h1 class="text-2xl font-bold text-gray-900">Nieuws</h1>
		<HasPermission permission={BluePermission.NewsModify}>
			<a href={resolve('/news/new')} class="text-sm font-medium text-primary-hover hover:underline">
				Nieuw bericht
			</a>
		</HasPermission>
	</div>
	{#if error}
		<p class="mt-4 text-sm text-gray-600">Nieuws kon niet worden geladen.</p>
	{:else}
		<div class="mt-6 divide-y divide-gray-200 border-t border-gray-200">
			{#each items as item (item.id)}
				<NewsItemShort {item} />
			{/each}
		</div>
		<Pagination page={currentPage} {totalPages} onPageChange={handlePageChange} />
	{/if}
</div>
