<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import type { OutingHistorySeasonGroupDto } from '$lib/api/apiClient';
	import Spinner from './Spinner.svelte';

	let history = $state<OutingHistorySeasonGroupDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	onMount(async () => {
		try {
			history = await apiClient.instanceHistory();
			error = false;
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});
</script>

{#if loading}
	<Spinner />
{:else if error}
	<p class="text-sm text-gray-600">Teams konden niet worden geladen.</p>
{:else if history.length === 0}
	<p class="text-sm text-gray-500">Geen teams gevonden.</p>
{:else}
	<div class="space-y-6">
		{#each history as season (season.seasonId)}
			<div>
				<h3 class="text-sm font-semibold text-gray-500">{season.seasonName}</h3>
				<ul class="mt-2 divide-y divide-gray-200 rounded-md border border-gray-200">
					{#each season.instances as instance (instance.id)}
						<li>
							<a
								href={resolve('/tools/outing-planner/instance/[instanceId]', {
									instanceId: instance.id
								})}
								class="block px-3 py-2 text-sm text-primary hover:bg-gray-50 hover:underline"
							>
								{instance.name}
							</a>
						</li>
					{/each}
				</ul>
			</div>
		{/each}
	</div>
{/if}
