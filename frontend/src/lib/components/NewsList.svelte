<script lang="ts">
	import { newsItems } from '$lib/news';

	let { limit = 4 }: { limit?: number } = $props();

	const dateFormatter = new Intl.DateTimeFormat('nl-NL', { day: 'numeric', month: 'long' });
	const visibleItems = $derived(newsItems.slice(0, limit));
</script>

<section>
	<h2 class="text-lg font-semibold text-gray-900">Nieuws</h2>
	<div class="mt-4 divide-y divide-gray-200 border-t border-gray-200">
		{#each visibleItems as item (item.id)}
			<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- href is data-driven (will come from an API), not a static route literal resolve() can check -->
			<a href={item.href} class="block py-5 hover:bg-gray-50">
				<time datetime={item.publishedAt} class="text-xs font-medium text-gray-500">
					{dateFormatter.format(new Date(item.publishedAt))}
				</time>
				<h3 class="mt-1 text-base font-semibold text-gray-900">{item.title}</h3>
				<p class="mt-2 text-sm leading-relaxed text-gray-600">{item.summary}</p>
			</a>
		{/each}
	</div>
</section>
