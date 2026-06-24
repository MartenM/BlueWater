<script lang="ts">
	import { untrack } from 'svelte';
	import { ChevronLeft, ChevronRight } from '@lucide/svelte';
	import { pushState } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { AgendaItemShort, HasPermission } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import {
		addMonths,
		formatYearMonth,
		monthLabel,
		monthRangeDates,
		type YearMonth
	} from '$lib/agendaMonth';
	import type { IAgendaItemDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	const MONTHS_SHOWN = 2;

	let { data }: PageProps = $props();

	let yearMonth = $state<YearMonth>(untrack(() => data.yearMonth));
	let items = $state(untrack(() => data.items));
	let error = $state(untrack(() => data.error));

	const months = $derived(Array.from({ length: MONTHS_SHOWN }, (_, i) => addMonths(yearMonth, i)));

	function itemsForMonth(item: IAgendaItemDto, month: YearMonth): boolean {
		return item.date.getUTCFullYear() === month.year && item.date.getUTCMonth() === month.month;
	}

	function navigate(next: YearMonth) {
		yearMonth = next;
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- query-only browsing state, not a static route resolve() can check
		pushState(`?month=${formatYearMonth(next)}`, {});
		const { start, end } = monthRangeDates(next, MONTHS_SHOWN);
		apiClient
			.range(start, end)
			.then((result) => {
				items = result;
				error = false;
			})
			.catch(() => {
				error = true;
			});
	}
</script>

<div class="mx-auto max-w-3xl px-4 py-12 sm:px-6 lg:px-8">
	<div class="flex items-center justify-between">
		<h1 class="text-2xl font-bold text-gray-900">Agenda</h1>
		<HasPermission permission={BluePermission.AgendaModify}>
			<a
				href={resolve('/agenda/new')}
				class="text-sm font-medium text-primary-hover hover:underline"
			>
				Nieuw agendapunt
			</a>
		</HasPermission>
	</div>

	<div class="mt-6 flex items-center justify-center gap-1">
		<button
			type="button"
			onclick={() => navigate(addMonths(yearMonth, -1))}
			aria-label="Vorige maand"
			class="rounded-md p-2 text-gray-500 hover:bg-gray-100"
		>
			<ChevronLeft class="size-4" />
		</button>
		<span class="min-w-48 text-center text-sm font-medium text-gray-700">
			{monthLabel(months[0])} - {monthLabel(months[months.length - 1])}
		</span>
		<button
			type="button"
			onclick={() => navigate(addMonths(yearMonth, 1))}
			aria-label="Volgende maand"
			class="rounded-md p-2 text-gray-500 hover:bg-gray-100"
		>
			<ChevronRight class="size-4" />
		</button>
	</div>

	{#if error}
		<p class="mt-4 text-sm text-gray-600">Agenda kon niet worden geladen.</p>
	{:else}
		{#each months as month (formatYearMonth(month))}
			{@const monthItems = items.filter((item) => itemsForMonth(item, month))}
			<section class="mt-8">
				<h2 class="text-sm font-semibold text-gray-500 capitalize">{monthLabel(month)}</h2>
				{#if monthItems.length === 0}
					<p class="mt-2 text-sm text-gray-500">Geen agendapunten deze maand.</p>
				{:else}
					<div class="mt-2 divide-y divide-gray-200 border-t border-gray-200">
						{#each monthItems as item (item.id)}
							<AgendaItemShort {item} />
						{/each}
					</div>
				{/if}
			</section>
		{/each}
	{/if}
</div>
