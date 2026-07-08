<script lang="ts">
	import { AvailabilityTimeline, Spinner, breadcrumbs } from '$lib';
	import {
		AvailabilityBlockInputDto,
		SetDayAvailabilityRequest,
		type MyWeekAvailabilityDto
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { extractApiError } from '$lib/forms/apiError';
	import {
		addDays,
		dateKey,
		fromApiTime,
		startOfWeek,
		toApiTime
	} from '$lib/constants/availability';
	import type { TimeBlock } from '$lib/utils/availabilityMerge';

	const dayLabels = [
		'Maandag',
		'Dinsdag',
		'Woensdag',
		'Donderdag',
		'Vrijdag',
		'Zaterdag',
		'Zondag'
	];

	let weekStart = $state(startOfWeek(new Date()));
	let data = $state<MyWeekAvailabilityDto | null>(null);
	let loading = $state(true);
	let error = $state(false);
	let dayErrors = $state<Record<string, string>>({});

	const days = $derived(Array.from({ length: 7 }, (_, i) => addDays(weekStart, i)));

	async function load() {
		loading = true;
		try {
			data = await apiClient.myWeek(dateKey(weekStart));
			error = false;
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	}

	function blocksFor(day: Date): TimeBlock[] {
		const key = dateKey(day);
		const dayData = data?.days.find((d) => dateKey(d.date) === key);
		return (dayData?.blocks ?? []).map((b) => ({
			startTime: fromApiTime(b.startTime),
			endTime: fromApiTime(b.endTime)
		}));
	}

	async function saveDay(day: Date, blocks: TimeBlock[]) {
		const key = dateKey(day);
		try {
			const result = await apiClient.day(
				new SetDayAvailabilityRequest({
					date: day,
					blocks: blocks.map(
						(b) =>
							new AvailabilityBlockInputDto({
								startTime: toApiTime(b.startTime),
								endTime: toApiTime(b.endTime)
							})
					)
				})
			);
			const dayEntry = data?.days.find((d) => dateKey(d.date) === key);
			if (dayEntry) dayEntry.blocks = result;
			dayErrors = { ...dayErrors, [key]: '' };
		} catch (e) {
			dayErrors = { ...dayErrors, [key]: extractApiError(e).formError ?? 'Opslaan is mislukt.' };
		}
	}

	function prevWeek() {
		weekStart = addDays(weekStart, -7);
	}

	function nextWeek() {
		weekStart = addDays(weekStart, 7);
	}

	$effect(() => {
		void weekStart;
		load();
	});

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Beschikbaarheid', href: '/tools/availability-planner' },
			{ label: 'Mijn beschikbaarheid' }
		]);
		return () => breadcrumbs.clear();
	});
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Mijn beschikbaarheid</h1>
	<div class="flex items-center gap-2">
		<button
			type="button"
			onclick={prevWeek}
			class="rounded-md border border-gray-300 px-3 py-1.5 text-sm hover:bg-gray-50"
		>
			← Vorige week
		</button>
		<button
			type="button"
			onclick={nextWeek}
			class="rounded-md border border-gray-300 px-3 py-1.5 text-sm hover:bg-gray-50"
		>
			Volgende week →
		</button>
	</div>
</div>

<div class="mt-6">
	{#if loading}
		<Spinner />
	{:else if error}
		<p class="text-sm text-gray-600">Beschikbaarheid kon niet worden geladen.</p>
	{:else if data}
		<div class="space-y-6">
			{#each days as day, dayIndex (dateKey(day))}
				{@const key = dateKey(day)}
				<div>
					<h2 class="text-sm font-semibold text-gray-900">
						{dayLabels[dayIndex]}
						<span class="font-normal text-gray-500">
							{day.toLocaleDateString('nl-NL', { day: 'numeric', month: 'long' })}
						</span>
					</h2>
					{#if dayErrors[key]}
						<p class="mt-1 text-xs text-red-600">{dayErrors[key]}</p>
					{/if}
					<div class="mt-2">
						<AvailabilityTimeline
							blocks={blocksFor(day)}
							editable={true}
							onchange={(blocks) => saveDay(day, blocks)}
						/>
					</div>
				</div>
			{/each}
		</div>
	{/if}
</div>
