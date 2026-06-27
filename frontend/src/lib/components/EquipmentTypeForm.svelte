<script lang="ts">
	import { untrack } from 'svelte';
	import { UpsertEquipmentTypeRequest } from '$lib/api/apiClient';
	import { AlertLevel } from '$lib/alert';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { EquipmentTypeDto } from '$lib/api/apiClient';
	import { Button } from '$lib';
	import BlueAlert from './BlueAlert.svelte';
	import FormField from './FormField.svelte';

	let {
		equipmentType,
		submitLabel,
		onSubmit
	}: {
		equipmentType?: EquipmentTypeDto;
		submitLabel: string;
		onSubmit: (request: UpsertEquipmentTypeRequest) => Promise<void>;
	} = $props();

	let code = $state(untrack(() => equipmentType?.code) ?? '');
	let name = $state(untrack(() => equipmentType?.name) ?? '');
	let scull = $state(untrack(() => equipmentType?.scull) ?? false);
	let coxed = $state(untrack(() => equipmentType?.coxed) ?? false);
	let rowersCount = $state(untrack(() => equipmentType?.rowersCount) ?? 1);
	let isBoat = $state(untrack(() => equipmentType?.isBoat) ?? true);
	const form = new FormState();

	function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		form.submit(() =>
			onSubmit(new UpsertEquipmentTypeRequest({ code, name, scull, coxed, rowersCount, isBoat }))
		);
	}
</script>

<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
	<FormField label="Code" errors={form.errorsFor('code')}>
		{#snippet children(invalid)}
			<input
				type="text"
				required
				bind:value={code}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			/>
		{/snippet}
	</FormField>

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

	<FormField label="Aantal roeiers" errors={form.errorsFor('rowersCount')}>
		{#snippet children(invalid)}
			<input
				type="number"
				min="0"
				required
				bind:value={rowersCount}
				class="w-24 rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			/>
		{/snippet}
	</FormField>

	<div class="flex flex-col gap-2">
		<label class="flex items-center gap-2 text-sm text-gray-700">
			<input
				type="checkbox"
				bind:checked={isBoat}
				class="rounded text-primary focus:ring-primary"
			/>
			Boot
		</label>
		<label class="flex items-center gap-2 text-sm text-gray-700">
			<input type="checkbox" bind:checked={scull} class="rounded text-primary focus:ring-primary" />
			Scull
		</label>
		<label class="flex items-center gap-2 text-sm text-gray-700">
			<input type="checkbox" bind:checked={coxed} class="rounded text-primary focus:ring-primary" />
			Met stuurman
		</label>
	</div>

	{#if form.formError}
		<BlueAlert level={AlertLevel.Danger}>{form.formError}</BlueAlert>
	{/if}

	<div class="mt-2 self-start">
		<Button type="submit" disabled={form.submitting}>{submitLabel}</Button>
	</div>
</form>
