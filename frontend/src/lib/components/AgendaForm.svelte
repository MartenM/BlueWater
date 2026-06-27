<script lang="ts">
	import { untrack } from 'svelte';
	import { UpsertAgendaItemRequest } from '$lib/api/apiClient';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { AgendaItemDto } from '$lib/api/apiClient';
	import { BlueForm } from '$lib';
	import FormField from './FormField.svelte';

	let {
		item,
		submitLabel,
		onSubmit
	}: {
		item?: AgendaItemDto;
		submitLabel: string;
		onSubmit: (request: UpsertAgendaItemRequest) => Promise<void>;
	} = $props();

	function toDateInputValue(date?: Date): string {
		if (!date) return '';
		return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}-${String(date.getUTCDate()).padStart(2, '0')}`;
	}

	function toTimeInputValue(time?: string): string {
		return time ? time.slice(0, 5) : '';
	}

	let title = $state(untrack(() => item?.title) ?? '');
	let description = $state(untrack(() => item?.description) ?? '');
	let date = $state(untrack(() => toDateInputValue(item?.date)));
	let time = $state(untrack(() => toTimeInputValue(item?.time)));
	let endDate = $state(untrack(() => toDateInputValue(item?.endDate)));
	let endTime = $state(untrack(() => toTimeInputValue(item?.endTime)));
	const form = new FormState();
</script>

<BlueForm
	{form}
	{submitLabel}
	onsubmit={() =>
		onSubmit(
			new UpsertAgendaItemRequest({
				date: new Date(date),
				time: time || undefined,
				title,
				description,
				endDate: endDate ? new Date(endDate) : undefined,
				endTime: endTime || undefined
			})
		)}
>
	<FormField label="Titel" errors={form.errorsFor('title')}>
		{#snippet children(invalid)}
			<input
				type="text"
				required
				bind:value={title}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			/>
		{/snippet}
	</FormField>

	<FormField label="Omschrijving" errors={form.errorsFor('description')}>
		{#snippet children(invalid)}
			<textarea
				required
				rows="6"
				bind:value={description}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"></textarea>
		{/snippet}
	</FormField>

	<div class="grid grid-cols-2 gap-4">
		<FormField label="Datum" errors={form.errorsFor('date')}>
			{#snippet children(invalid)}
				<input
					type="date"
					required
					bind:value={date}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="Tijd" errors={form.errorsFor('time')}>
			{#snippet children(invalid)}
				<input
					type="time"
					bind:value={time}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="Einddatum" errors={form.errorsFor('endDate')}>
			{#snippet children(invalid)}
				<input
					type="date"
					bind:value={endDate}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="Eindtijd" errors={form.errorsFor('endTime')}>
			{#snippet children(invalid)}
				<input
					type="time"
					bind:value={endTime}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>
	</div>
</BlueForm>
