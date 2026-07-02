<script lang="ts">
	import { featuredItem } from '$lib/featured';
</script>

<section>
	<h2 class="text-lg font-semibold text-gray-900">Uitgelicht</h2>
	<!-- eslint-disable svelte/no-navigation-without-resolve -- href is data-driven (will come from an API), not a static route literal resolve() can check -->
	<a
		href={featuredItem.href}
		class="mt-4 flex flex-col overflow-hidden rounded-md border border-gray-200 bg-white shadow-sm transition hover:shadow-md"
	>
		{#if featuredItem.imageUrl}
			<img src={featuredItem.imageUrl} alt="" class="h-48 w-full object-cover" />
		{/if}
		<div class="flex flex-col p-6">
			<h3 class="text-xl font-bold text-gray-900">{featuredItem.title}</h3>
			<p class="mt-3 text-sm text-gray-600">{featuredItem.summary}</p>
		</div>
	</a>
	<!-- eslint-enable svelte/no-navigation-without-resolve -->
</section>
