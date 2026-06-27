<script lang="ts">
	import { untrack } from 'svelte';
	import { UpsertManufacturerRequest } from '$lib/api/apiClient';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { ManufacturerDto } from '$lib/api/apiClient';
	import { BlueForm } from '$lib';
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
</script>

<BlueForm {form} {submitLabel} onsubmit={() => onSubmit(new UpsertManufacturerRequest({ name }))}>
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
</BlueForm>
