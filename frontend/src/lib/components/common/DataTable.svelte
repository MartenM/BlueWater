<script lang="ts" generics="T">
	import { goto } from '$app/navigation';
	import type { Snippet } from 'svelte';
	import Spinner from '../Spinner.svelte';

	type Column = {
		header: string;
		cell: Snippet<[T]>;
		tdClass?: string;
		thClass?: string;
	};

	let {
		columns,
		items,
		emptyMessage = 'Geen items gevonden.',
		loading = false,
		error = undefined,
		rowHref = undefined
	}: {
		columns: Column[];
		items: T[];
		emptyMessage?: string;
		loading?: boolean;
		error?: string;
		rowHref?: (item: T) => string;
	} = $props();
</script>

<div class="mt-4 overflow-x-auto">
	<table class="min-w-full divide-y divide-gray-200 text-sm">
		<thead class="bg-gray-50">
			<tr>
				{#each columns as col}
					<th class={col.thClass ?? 'px-4 py-1 text-left font-medium text-gray-500'}>
						{col.header}
					</th>
				{/each}
			</tr>
		</thead>
		{#if !loading && !error}
			<tbody class="divide-y divide-gray-200 bg-white">
				{#each items as item}
					<tr
						class="hover:bg-gray-50"
						class:cursor-pointer={!!rowHref}
						onclick={rowHref ? () => goto(rowHref(item)) : undefined}
						onkeydown={rowHref ? (e) => e.key === 'Enter' && goto(rowHref(item)) : undefined}
						tabindex={rowHref ? 0 : undefined}
					>
						{#each columns as col}
							<td class={col.tdClass ?? 'px-4 py-1'}>{@render col.cell(item)}</td>
						{/each}
					</tr>
				{:else}
					<tr>
						<td colspan={columns.length} class="px-4 py-6 text-center text-gray-500">
							{emptyMessage}
						</td>
					</tr>
				{/each}
			</tbody>
		{/if}
	</table>
</div>

{#if loading}
	<Spinner />
{:else if error}
	<p class="mt-4 text-sm text-gray-600">{error}</p>
{/if}
