<script lang="ts">
	import { onMount } from 'svelte';
	import { SvelteMap } from 'svelte/reactivity';
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
		const map = new SvelteMap<string, SignupListItemDto[]>();
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
		{#each grouped as [category, catItems] (category)}
			<section class="mt-8 first:mt-6">
				<h2
					class="border-b border-gray-200 pb-1 text-sm font-semibold uppercase tracking-wide text-gray-500"
				>
					{category}
				</h2>
				<ul class="text-sm">
					{#each catItems as item (item.id)}
						<li class="flex items-center gap-3 py-2 hover:bg-gray-50">
							<a
								href={resolve('/signup/[id]', { id: item.id })}
								class="min-w-0 flex-1 font-medium text-gray-900 hover:text-primary-hover"
							>
								{item.title}
							</a>
							<span class="w-24 shrink-0 text-right text-xs text-gray-400">
								{#if item.endDate}
									{isOpen(item) ? 'tot' : 'gesloten'}
									{formatDate(item.endDate)}
								{/if}
							</span>
							<span class="w-24 shrink-0 text-right text-xs text-gray-500">
								{#if item.maxSignups != null}
									{item.validResponses}/{item.maxSignups}
								{:else}
									{item.validResponses} aangemeld
								{/if}
							</span>
							<div class="flex w-28 shrink-0 items-center justify-end gap-1.5">
								{#if !isOpen(item)}
									<span
										class="inline-flex items-center rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600"
									>
										Gesloten
									</span>
								{/if}
								{#if item.myResponseStatus === 'valid' || item.myResponseStatus === 'reservation'}
									<span
										class="inline-flex items-center rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700"
									>
										Aangemeld
									</span>
								{:else if item.myResponseStatus === 'waitlist'}
									<span
										class="inline-flex items-center rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-700"
									>
										Wachtlijst
									</span>
								{/if}
							</div>
						</li>
					{/each}
				</ul>
			</section>
		{/each}
	{/if}
</div>
