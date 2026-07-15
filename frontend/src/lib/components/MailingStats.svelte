<script lang="ts">
	import type { MailingStatsDto } from '$lib/api/apiClient';

	let { stats }: { stats: MailingStatsDto } = $props();

	const openRate = $derived(
		stats.sentCount > 0 ? Math.round((stats.openedCount / stats.sentCount) * 100) : 0
	);
</script>

<div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
	<div class="rounded-md border border-gray-200 p-4">
		<p class="text-xs font-medium uppercase text-gray-500">Verzonden</p>
		<p class="mt-1 text-2xl font-semibold text-gray-900">{stats.sentCount}</p>
	</div>
	<div class="rounded-md border border-gray-200 p-4">
		<p class="text-xs font-medium uppercase text-gray-500">Geopend</p>
		<p class="mt-1 text-2xl font-semibold text-gray-900">{stats.openedCount}</p>
	</div>
	<div class="rounded-md border border-gray-200 p-4">
		<p class="text-xs font-medium uppercase text-gray-500">Open rate</p>
		<p class="mt-1 text-2xl font-semibold text-gray-900">{openRate}%</p>
	</div>
</div>

{#if stats.linkStats.length > 0}
	<div class="mt-4">
		<h3 class="text-sm font-semibold text-gray-700">Klikken per link</h3>
		<table class="mt-2 w-full text-sm">
			<thead>
				<tr class="border-b border-gray-200 text-left text-gray-500">
					<th class="py-1.5 font-medium">URL</th>
					<th class="py-1.5 font-medium">Klikken</th>
				</tr>
			</thead>
			<tbody>
				{#each stats.linkStats as link (link.originalUrl)}
					<tr class="border-b border-gray-100">
						<td class="max-w-md truncate py-1.5 text-gray-700">{link.originalUrl}</td>
						<td class="py-1.5 text-gray-700">{link.clickCount}</td>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>
{/if}
