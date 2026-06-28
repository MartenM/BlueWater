<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import type { SignupListItemDto } from '$lib/api/apiClient';
	import { breadcrumbs } from '$lib';

	$effect(() => {
		breadcrumbs.set([{ label: 'Inschrijvingen' }]);
		return () => breadcrumbs.clear();
	});

	let items = $state<SignupListItemDto[]>([]);
	let loading = $state(true);
	let error = $state(false);

	onMount(async () => {
		try {
			items = await apiClient.signupsAll2();
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	const grouped = $derived.by(() => {
		const map = new Map<string, SignupListItemDto[]>();
		for (const item of items) {
			const key = item.categoryTitle ?? 'Overig';
			if (!map.has(key)) map.set(key, []);
			map.get(key)!.push(item);
		}
		return [...map.entries()];
	});

	function formatDate(d: Date | undefined) {
		if (!d) return null;
		return new Date(d).toLocaleDateString('nl-NL', { day: 'numeric', month: 'short' });
	}

	function isOpen(item: SignupListItemDto) {
		if (!item.endDate) return true;
		return new Date(item.endDate) >= new Date();
	}
</script>

<div class="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
	<h1 class="text-2xl font-bold text-gray-900">Inschrijvingen</h1>

	{#if loading}
		<p class="mt-4 text-sm text-gray-600">Laden…</p>
	{:else if error}
		<p class="mt-4 text-sm text-gray-600">Inschrijvingen konden niet worden geladen.</p>
	{:else if items.length === 0}
		<p class="mt-4 text-sm text-gray-600">Geen inschrijvingen beschikbaar.</p>
	{:else}
		<table class="mt-6 w-full text-sm">
			<tbody>
				{#each grouped as [category, catItems] (category)}
					<tr>
						<td colspan="4" class="pb-1 pt-6 first:pt-0">
							<h2 class="border-b border-gray-200 pb-1 text-sm font-semibold uppercase tracking-wide text-gray-500">
								{category}
							</h2>
						</td>
					</tr>
					{#each catItems as item (item.id)}
						<tr class="hover:bg-gray-50">
							<td class="py-2 pr-3">
								<a
									href={resolve('/signup/[id]', { id: item.id })}
									class="font-medium text-gray-900 hover:text-primary-hover"
								>
									{item.title}
								</a>
							</td>
							<td class="py-2 pr-3 text-xs text-gray-400 whitespace-nowrap">
								{#if item.endDate}
									{isOpen(item) ? 'tot' : 'gesloten'}
									{formatDate(item.endDate)}
								{/if}
							</td>
							<td class="py-2 pr-3 text-xs text-gray-500 whitespace-nowrap">
								{#if item.maxSignups != null}
									{item.validResponses}/{item.maxSignups}
								{:else}
									{item.validResponses} aangemeld
								{/if}
							</td>
							<td class="py-2 text-right whitespace-nowrap">
								<div class="flex items-center justify-end gap-1.5">
									{#if !isOpen(item)}
										<span class="inline-flex items-center rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600">
											Gesloten
										</span>
									{/if}
									{#if item.myResponseStatus === 'valid' || item.myResponseStatus === 'reservation'}
										<span class="inline-flex items-center rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700">
											Aangemeld
										</span>
									{:else if item.myResponseStatus === 'waitlist'}
										<span class="inline-flex items-center rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-700">
											Wachtlijst
										</span>
									{/if}
								</div>
							</td>
						</tr>
					{/each}
				{/each}
			</tbody>
		</table>
	{/if}
</div>
