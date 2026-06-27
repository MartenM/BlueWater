<script lang="ts">
	import { untrack } from 'svelte';
	import { UpsertManufacturerRequest } from '$lib/api/apiClient';
	import { AlertLevel } from '$lib/alert';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { ManufacturerDto } from '$lib/api/apiClient';
	import { Button } from '$lib';
	import BlueAlert from './BlueAlert.svelte';
	import FormField from './FormField.svelte';

	let {
		manufacturer,
		submitLabel,
		onSubmit
	}: {
		manufacturer?: ManufacturerDto;
		submitLabel: string;
		onSubmit: (request: UpsertManufacturerRequest) => Promise<void>;
	} = $props();

	let name = $state(untrack(() => manufacturer?.name) ?? '');
	const form = new FormState();

	function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		form.submit(() => onSubmit(new UpsertManufacturerRequest({ name })));
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

	{#if form.formError}
		<BlueAlert level={AlertLevel.Danger}>{form.formError}</BlueAlert>
	{/if}

	<div class="mt-2 self-start">
		<Button type="submit" disabled={form.submitting}>{submitLabel}</Button>
	</div>
</form>
