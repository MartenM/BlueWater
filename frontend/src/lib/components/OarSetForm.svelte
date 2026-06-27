<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { UpsertOarSetRequest } from '$lib/api/apiClient';
	import { AlertLevel } from '$lib/alert';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { OarSetDto, ManufacturerDto } from '$lib/api/apiClient';
	import { Button, apiClient } from '$lib';
	import BlueAlert from './BlueAlert.svelte';
	import FormField from './FormField.svelte';

	let {
		oarSet,
		submitLabel,
		onSubmit
	}: {
		oarSet?: OarSetDto;
		submitLabel: string;
		onSubmit: (request: UpsertOarSetRequest) => Promise<void>;
	} = $props();

	let name = $state(untrack(() => oarSet?.name) ?? '');
	let manufacturerId = $state(untrack(() => oarSet?.manufacturerId) ?? '');
	let scull = $state(untrack(() => oarSet?.scull) ?? false);
	let manufacturers = $state<ManufacturerDto[]>([]);
	const form = new FormState();

	onMount(async () => {
		try {
			manufacturers = await apiClient.manufacturersAll();
		} catch {
			// non-fatal; dropdown stays empty
		}
	});

	function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		form.submit(() =>
			onSubmit(
				new UpsertOarSetRequest({
					name,
					manufacturerId: manufacturerId || undefined,
					scull
				})
			)
		);
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

	<FormField label="Fabrikant" errors={form.errorsFor('manufacturerId')}>
		{#snippet children(invalid)}
			<select
				bind:value={manufacturerId}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			>
				<option value="">— geen —</option>
				{#each manufacturers as m (m.id)}
					<option value={m.id}>{m.name}</option>
				{/each}
			</select>
		{/snippet}
	</FormField>

	<div>
		<label class="flex items-center gap-2 text-sm text-gray-700">
			<input type="checkbox" bind:checked={scull} class="rounded text-primary focus:ring-primary" />
			Scull
		</label>
	</div>

	{#if form.formError}
		<BlueAlert level={AlertLevel.Danger}>{form.formError}</BlueAlert>
	{/if}

	<div class="mt-2 self-start">
		<Button type="submit" loading={form.submitting}>{submitLabel}</Button>
	</div>
</form>
