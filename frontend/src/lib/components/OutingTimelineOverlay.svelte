<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import Sailboat from '@lucide/svelte/icons/sailboat';
	import {
		DAY_START_MINUTES,
		DAY_END_MINUTES,
		fromApiTime,
		minutesToTimeString,
		timeStringToMinutes
	} from '$lib/constants/availability';
	import type { OutingTimelineEntryDto } from '$lib/api/apiClient';

	let {
		entries,
		showHourLabels = true
	}: { entries: OutingTimelineEntryDto[]; showHourLabels?: boolean } = $props();

	const TOTAL_MINUTES = DAY_END_MINUTES - DAY_START_MINUTES;
	const DEFAULT_DURATION_MINUTES = 60;

	function minutesToPercent(min: number): number {
		const clamped = Math.min(DAY_END_MINUTES, Math.max(DAY_START_MINUTES, min));
		return ((clamped - DAY_START_MINUTES) / TOTAL_MINUTES) * 100;
	}

	function entryMinutes(entry: OutingTimelineEntryDto): { startMin: number; endMin: number } {
		const startMin = timeStringToMinutes(fromApiTime(entry.startTime));
		const endMin = entry.endTime
			? timeStringToMinutes(fromApiTime(entry.endTime))
			: startMin + DEFAULT_DURATION_MINUTES;
		return { startMin, endMin: Math.max(endMin, startMin + 15) };
	}

	const hourMarks = $derived(
		Array.from({ length: Math.floor(TOTAL_MINUTES / 60) + 1 }, (_, i) => DAY_START_MINUTES + i * 60)
	);

	function openOuting(id: string) {
		goto(resolve('/tools/outing-planner/[id]', { id }));
	}
</script>

<div class="relative">
	{#if showHourLabels}
		<div class="sticky top-0 z-10 h-4 select-none bg-white text-[10px] text-gray-400">
			{#each hourMarks as mark (mark)}
				<span class="absolute -translate-x-1/2" style="left: {minutesToPercent(mark)}%">
					{minutesToTimeString(mark)}
				</span>
			{/each}
		</div>
	{/if}

	<div
		class="relative h-6 select-none rounded border border-dashed border-blue-200 bg-blue-50/50"
		data-testid="outing-timeline-overlay"
	>
		{#each hourMarks as mark (mark)}
			<div
				class="pointer-events-none absolute inset-y-0 border-l border-blue-100"
				style="left: {minutesToPercent(mark)}%"
			></div>
		{/each}

		{#each entries as entry (entry.id)}
			{@const { startMin, endMin } = entryMinutes(entry)}
			<button
				type="button"
				class="absolute inset-y-0.5 flex items-center justify-center gap-1 overflow-hidden rounded bg-blue-600/80 hover:bg-blue-600"
				style="left: {minutesToPercent(startMin)}%; width: {minutesToPercent(endMin) -
					minutesToPercent(startMin)}%"
				onclick={() => openOuting(entry.id)}
				title="{entry.label} · {fromApiTime(entry.startTime)}{entry.endTime
					? ` - ${fromApiTime(entry.endTime)}`
					: ''}"
				data-testid="outing-timeline-block"
			>
				<Sailboat class="size-3 shrink-0 text-white" />
				<span class="truncate px-1 text-[10px] whitespace-nowrap text-white">
					{entry.label}
				</span>
			</button>
		{/each}
	</div>
</div>
