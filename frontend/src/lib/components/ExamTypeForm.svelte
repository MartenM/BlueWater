<script lang="ts">
	import { untrack } from 'svelte';
	import { UpsertExamTypeRequest } from '$lib/api/apiClient';
	import { AlertLevel } from '$lib/alert';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { ExamTypeDto } from '$lib/api/apiClient';
	import { Button } from '$lib';
	import BlueAlert from './BlueAlert.svelte';
	import FormField from './FormField.svelte';

	let {
		examType,
		submitLabel,
		onSubmit
	}: {
		examType?: ExamTypeDto;
		submitLabel: string;
		onSubmit: (request: UpsertExamTypeRequest) => Promise<void>;
	} = $props();

	let name = $state(untrack(() => examType?.name) ?? '');
	let description = $state(untrack(() => examType?.description) ?? '');
	const form = new FormState();

	function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		form.submit(() => onSubmit(new UpsertExamTypeRequest({ name, description })));
	}
</script>

<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
	<FormField label="Naam" errors={form.errorsFor('name')}>
		{#snippet children(invalid)}
			<input
				type="text"
				required
				bind:value={name}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			/>
		{/snippet}
	</FormField>

	<FormField label="Omschrijving" errors={form.errorsFor('description')}>
		{#snippet children(invalid)}
			<textarea
				rows="3"
				bind:value={description}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"></textarea>
		{/snippet}
	</FormField>

	{#if form.formError}
		<BlueAlert level={AlertLevel.Danger}>{form.formError}</BlueAlert>
	{/if}

	<div class="mt-2 self-start">
		<Button type="submit" loading={form.submitting}>{submitLabel}</Button>
	</div>
</form>
