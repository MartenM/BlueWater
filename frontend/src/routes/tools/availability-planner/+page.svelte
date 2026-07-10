<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { Button, OutingPlannerTeamList, breadcrumbs } from '$lib';

	onMount(() => {
		breadcrumbs.set([{ label: 'Beschikbaarheid' }]);
		return () => breadcrumbs.clear();
	});
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Beschikbaarheid</h1>
	<Button href={resolve('/tools/availability-planner/mine')} size="sm">Mijn beschikbaarheid</Button>
</div>

<div class="mt-6 lg:flex lg:items-start">
	<div class="lg:w-3/4">
		<p class="text-sm text-gray-600">
			Kies een ploeg om de beschikbaarheid van de leden te bekijken, of stel je eigen
			beschikbaarheid onafhankelijk van een team in.
		</p>
	</div>

	<div
		class="mt-10 border-t border-gray-200 pt-6 lg:mt-0 lg:w-1/4 lg:border-t-0 lg:border-l lg:pt-0 lg:pl-8"
	>
		<OutingPlannerTeamList basePath="/tools/availability-planner" />
	</div>
</div>
