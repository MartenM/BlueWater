<script lang="ts">
	import { resolve } from '$app/paths';
	import { markdownToPlainText } from '$lib/markdown';
	import type { IAgendaItemDto } from '$lib/api/apiClient';

	let { item }: { item: IAgendaItemDto } = $props();

	const dayFormatter = new Intl.DateTimeFormat('nl-NL', { day: 'numeric' });
	const weekdayFormatter = new Intl.DateTimeFormat('nl-NL', { weekday: 'short' });
	const timeFormatter = new Intl.DateTimeFormat('nl-NL', { hour: '2-digit', minute: '2-digit' });

	function formatTime(time: string): string {
		const [hours, minutes] = time.split(':');
		return timeFormatter.format(new Date(0, 0, 0, Number(hours), Number(minutes)));
	}

	const preview = $derived(markdownToPlainText(item.description));
</script>

<a
	href={resolve('/agenda/[id]', { id: item.id })}
	class="flex items-center gap-3 py-2 hover:bg-gray-50"
>
	<div class="flex h-10 w-10 shrink-0 flex-col items-center justify-center rounded-md bg-gray-100">
		<span class="text-sm font-bold leading-none text-gray-900"
			>{dayFormatter.format(item.date)}</span
		>
		<span class="text-[10px] font-medium uppercase leading-none text-gray-500">
			{weekdayFormatter.format(item.date)}
		</span>
	</div>
	<div class="min-w-0 flex-1">
		<div class="flex items-baseline gap-2">
			<h3 class="truncate text-sm font-semibold text-gray-900">{item.title}</h3>
			{#if item.time}
				<span class="shrink-0 text-xs text-gray-500">{formatTime(item.time)}</span>
			{/if}
		</div>
		<p class="truncate text-xs text-gray-500">{preview}</p>
	</div>
</a>
