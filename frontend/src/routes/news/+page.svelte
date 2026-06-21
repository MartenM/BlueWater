<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page as pageState } from '$app/state';
	import { apiClient } from '$lib/api/client';
	import { HasPermission, NewsItemShort, Pagination } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import type { NewsPostDto } from '$lib/api/apiClient';

	const NEWS_PAGE_SIZE = 5;

	let items = $state<NewsPostDto[]>([]);
	let totalCount = $state(0);
	let error = $state(false);

	const currentPage = $derived(Number(pageState.url.searchParams.get('page') ?? '1') || 1);
	const totalPages = $derived(Math.max(1, Math.ceil(totalCount / NEWS_PAGE_SIZE)));

	$effect(() => {
		const requestedPage = currentPage;
		apiClient
			.newsGET(requestedPage, NEWS_PAGE_SIZE)
			.then((result) => {
				items = result.items;
				totalCount = result.totalCount;
				error = false;
			})
			.catch(() => {
				error = true;
			});
	});

	function handlePageChange(next: number) {
		goto(resolve(`/news?page=${next}`), { keepFocus: true, noScroll: true });
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
