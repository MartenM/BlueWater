<script lang="ts" generics="T">
	import type { Snippet } from 'svelte';

	type Column = {
		header: string;
		cell: Snippet<[T]>;
		tdClass?: string;
	};

	let {
		columns,
		items,
		emptyMessage = 'Geen items gevonden.'
	}: {
		columns: Column[];
		items: T[];
		emptyMessage?: string;
	} = $props();
</script>

<div class="mt-4 overflow-x-auto">
	<table class="min-w-full divide-y divide-gray-200 text-sm">
		<thead class="bg-gray-50">
			<tr>
				{#each columns as col}
					<th class="px-4 py-1 text-left font-medium text-gray-500">{col.header}</th>
				{/each}
			</tr>
		</thead>
		<tbody class="divide-y divide-gray-200 bg-white">
			{#each items as item}
				<tr class="hover:bg-gray-50">
					{#each columns as col}
						<td class={col.tdClass ?? 'px-4 py-1'}>{@render col.cell(item)}</td>
					{/each}
				</tr>
			{:else}
				<tr>
					<td colspan={columns.length} class="px-4 py-6 text-center text-gray-500"
						>{emptyMessage}</td
					>
				</tr>
			{/each}
		</tbody>
	</table>
</div>
