<script lang="ts">
	import { untrack } from 'svelte';
	import { UpsertExamTypeRequest } from '$lib/api/apiClient';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { ExamTypeDto } from '$lib/api/apiClient';
	import { BlueForm } from '$lib';
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
</script>

<BlueForm
	{form}
	{submitLabel}
	onsubmit={() => onSubmit(new UpsertExamTypeRequest({ name, description }))}
>
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
</BlueForm>
