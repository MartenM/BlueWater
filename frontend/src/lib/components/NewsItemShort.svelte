<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { renderMarkdown } from '$lib/markdown';
	import type { INewsPostDto } from '$lib/api/apiClient';

	let { item }: { item: INewsPostDto } = $props();

	const dateFormatter = new Intl.DateTimeFormat('nl-NL', { day: 'numeric', month: 'long' });

	let iconUrl = $state<string | null>(null);

	onMount(async () => {
		if (!item.iconId) return;
		try {
			const icon = await apiClient.content(item.iconId);
			iconUrl = URL.createObjectURL(icon.data);
		} catch {
			// Icon failed to load; the post still renders without it.
		}
	});

	onDestroy(() => {
		if (iconUrl) URL.revokeObjectURL(iconUrl);
	});
</script>

<a href={resolve('/news/[id]', { id: item.id })} class="flex gap-4 py-5 hover:bg-gray-50">
	{#if iconUrl}
		<img src={iconUrl} alt="" class="h-[100px] w-[100px] shrink-0 rounded-md object-cover" />
	{/if}
	<div>
		<time datetime={item.createdAt.toISOString()} class="text-xs font-medium text-gray-500">
			{dateFormatter.format(item.createdAt)}
		</time>
		<h3 class="mt-1 text-base font-semibold text-gray-900">{item.title}</h3>
		<div class="prose prose-sm mt-2 max-w-none text-gray-700">
			<!-- eslint-disable-next-line svelte/no-at-html-tags -- shortText is rendered Markdown, sanitized in $lib/markdown -->
			{@html renderMarkdown(item.shortText)}
		</div>
	</div>
</a>
