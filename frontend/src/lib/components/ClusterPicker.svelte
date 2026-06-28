<script lang="ts">
	let {
		clusters,
		selectedIds = $bindable(),
		invalid = false
	}: {
		clusters: Array<{ id: string; name: string }>;
		selectedIds: string[];
		invalid?: boolean;
	} = $props();

	let open = $state(false);
	let query = $state('');
	let searchEl = $state<HTMLInputElement>();
	let containerEl: HTMLDivElement;

	function toggle(id: string) {
		if (selectedIds.includes(id)) {
			selectedIds = selectedIds.filter((x) => x !== id);
		} else {
			selectedIds = [...selectedIds, id];
		}
	}

	function handleClickOutside(e: MouseEvent) {
		if (!containerEl.contains(e.target as Node)) {
			open = false;
		}
	}

	$effect(() => {
		if (open) {
			document.addEventListener('mousedown', handleClickOutside);
			query = '';
			// focus the search input on next tick
			Promise.resolve().then(() => searchEl?.focus());
			return () => document.removeEventListener('mousedown', handleClickOutside);
		}
	});

	const filtered = $derived(
		query.trim()
			? clusters.filter((c) => c.name.toLowerCase().includes(query.toLowerCase()))
			: clusters
	);

	const triggerLabel = $derived.by(() => {
		if (selectedIds.length === 0) return 'Geen clusters geselecteerd';
		const names = clusters.filter((c) => selectedIds.includes(c.id)).map((c) => c.name);
		if (names.length <= 2) return names.join(', ');
		return `${names.slice(0, 2).join(', ')} +${names.length - 2}`;
	});
</script>

<div class="relative" bind:this={containerEl}>
	<button
		type="button"
		onclick={() => (open = !open)}
		class="flex w-full items-center justify-between rounded-md border px-3 py-2 text-left text-sm bg-white
			{invalid ? 'border-red-400' : 'border-gray-300'}
			{selectedIds.length > 0 ? 'text-gray-900' : 'text-gray-400'}
			hover:border-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-0"
	>
		<span class="truncate">{triggerLabel}</span>
		<svg
			class="ml-2 h-4 w-4 shrink-0 text-gray-400 transition-transform {open ? 'rotate-180' : ''}"
			viewBox="0 0 20 20"
			fill="currentColor"
			aria-hidden="true"
		>
			<path
				fill-rule="evenodd"
				d="M5.22 8.22a.75.75 0 0 1 1.06 0L10 11.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 9.28a.75.75 0 0 1 0-1.06Z"
				clip-rule="evenodd"
			/>
		</svg>
	</button>

	{#if open}
		<div class="absolute z-10 mt-1 w-full rounded-md border border-gray-200 bg-white shadow-lg">
			<div class="border-b border-gray-100 px-2 py-2">
				<div class="relative">
					<svg
						class="pointer-events-none absolute left-2 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-gray-400"
						viewBox="0 0 20 20"
						fill="currentColor"
						aria-hidden="true"
					>
						<path
							fill-rule="evenodd"
							d="M9 3.5a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11ZM2 9a7 7 0 1 1 12.452 4.391l3.328 3.329a.75.75 0 1 1-1.06 1.06l-3.329-3.328A7 7 0 0 1 2 9Z"
							clip-rule="evenodd"
						/>
					</svg>
					<input
						bind:this={searchEl}
						bind:value={query}
						type="text"
						placeholder="Zoeken…"
						class="w-full rounded border-gray-200 py-1 pl-7 pr-2 text-sm focus:border-blue-400 focus:ring-1 focus:ring-blue-400"
					/>
				</div>
			</div>

			<ul class="max-h-52 overflow-y-auto divide-y divide-gray-100">
				{#each filtered as cluster (cluster.id)}
					{@const checked = selectedIds.includes(cluster.id)}
					<li>
						<label
							class="flex cursor-pointer items-center gap-2.5 px-3 py-2 text-sm hover:bg-gray-50
								{checked ? 'text-gray-900 font-medium' : 'text-gray-600'}"
						>
							<input
								type="checkbox"
								class="h-4 w-4 rounded border-gray-300 text-blue-600"
								{checked}
								onchange={() => toggle(cluster.id)}
							/>
							<span class="flex-1">{cluster.name}</span>
						</label>
					</li>
				{:else}
					<li class="px-3 py-2 text-sm text-gray-400">Geen resultaten.</li>
				{/each}
			</ul>

			{#if selectedIds.length > 0}
				<div class="border-t border-gray-100 px-3 py-2">
					<button
						type="button"
						class="text-xs text-gray-500 hover:text-gray-700 hover:underline"
						onclick={() => (selectedIds = [])}
					>
						Selectie wissen
					</button>
				</div>
			{/if}
		</div>
	{/if}
</div>
