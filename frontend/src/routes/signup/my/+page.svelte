<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import type { SignupListItemDto } from '$lib/api/apiClient';
	import { DataTable, breadcrumbs } from '$lib';

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Inschrijvingen', href: resolve('/signup') },
			{ label: 'Mijn aanmeldingen' }
		]);
		return () => breadcrumbs.clear();
	});

	let items = $state<SignupListItemDto[]>([]);
	let loading = $state(true);
	let error = $state(false);

	onMount(async () => {
		try {
			items = await apiClient.my();
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	function formatDate(d: Date | undefined) {
		if (!d) return '—';
		return new Date(d).toLocaleDateString('nl-NL', {
			day: 'numeric',
			month: 'short',
			year: 'numeric'
		});
	}
</script>

<div class="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
	<h1 class="text-2xl font-bold text-gray-900">Mijn aanmeldingen</h1>

	{#if loading}
		<p class="mt-4 text-sm text-gray-600">Laden…</p>
	{:else if error}
		<p class="mt-4 text-sm text-gray-600">Aanmeldingen konden niet worden geladen.</p>
	{:else if items.length === 0}
		<p class="mt-4 text-sm text-gray-600">Je hebt je nog nergens voor aangemeld.</p>
	{:else}
		{#snippet titleCell(item: SignupListItemDto)}
			<a
				href={resolve('/signup/[id]', { id: item.id })}
				class="font-medium text-gray-900 hover:underline"
			>
				{item.title}
			</a>
		{/snippet}

		{#snippet categoryCell(item: SignupListItemDto)}
			<span class="text-gray-600">{item.categoryTitle ?? '—'}</span>
		{/snippet}

		{#snippet endDateCell(item: SignupListItemDto)}
			<span class="text-gray-600">{formatDate(item.endDate)}</span>
		{/snippet}

		{#snippet statusCell(item: SignupListItemDto)}
			{#if item.hasMyResponse}
				<span class="text-green-600 font-medium">Aangemeld</span>
			{/if}
		{/snippet}

		<div class="mt-6">
			<DataTable
				columns={[
					{ header: 'Inschrijving', cell: titleCell },
					{ header: 'Categorie', cell: categoryCell },
					{ header: 'Sluitdatum', cell: endDateCell },
					{ header: 'Status', cell: statusCell }
				]}
				{items}
				loading={false}
				emptyMessage="Geen aanmeldingen."
			/>
		</div>
	{/if}
</div>
