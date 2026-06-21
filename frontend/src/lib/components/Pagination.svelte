<script lang="ts">
	import { ChevronLeft, ChevronRight } from '@lucide/svelte';

	let {
		page,
		totalPages,
		onPageChange
	}: { page: number; totalPages: number; onPageChange: (page: number) => void } = $props();

	type Token = number | 'ellipsis';

	const tokens = $derived.by<Token[]>(() => {
		if (totalPages <= 7) return Array.from({ length: totalPages }, (_, i) => i + 1);

		const result: Token[] = [1];
		if (page > 3) result.push('ellipsis');

		for (let p = Math.max(2, page - 1); p <= Math.min(totalPages - 1, page + 1); p++) {
			result.push(p);
		}

		if (page < totalPages - 2) result.push('ellipsis');
		result.push(totalPages);
		return result;
	});
</script>

{#if totalPages > 1}
	<nav aria-label="Paginering" class="mt-6 flex items-center justify-center gap-1">
		<button
			type="button"
			disabled={page <= 1}
			onclick={() => onPageChange(page - 1)}
			aria-label="Vorige pagina"
			class="rounded-md p-2 text-gray-500 hover:bg-gray-100 disabled:opacity-40 disabled:hover:bg-transparent"
		>
			<ChevronLeft class="size-4" />
		</button>

		{#each tokens as token, i (i)}
			{#if token === 'ellipsis'}
				<span class="px-2 text-sm text-gray-400">…</span>
			{:else}
				<button
					type="button"
					onclick={() => onPageChange(token)}
					aria-current={token === page ? 'page' : undefined}
					class={[
						'min-w-9 rounded-md px-3 py-2 text-sm font-medium',
						token === page ? 'bg-primary text-primary-content' : 'text-gray-700 hover:bg-gray-100'
					]}
				>
					{token}
				</button>
			{/if}
		{/each}

		<button
			type="button"
			disabled={page >= totalPages}
			onclick={() => onPageChange(page + 1)}
			aria-label="Volgende pagina"
			class="rounded-md p-2 text-gray-500 hover:bg-gray-100 disabled:opacity-40 disabled:hover:bg-transparent"
		>
			<ChevronRight class="size-4" />
		</button>
	</nav>
{/if}
