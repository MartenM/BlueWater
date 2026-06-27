<script lang="ts">
	import { untrack } from 'svelte';
	import { UpsertUserGroupCategoryRequest } from '$lib/api/apiClient';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { UserGroupCategoryDto } from '$lib/api/apiClient';
	import { BlueForm } from '$lib';
	import FormField from './FormField.svelte';

	let {
		category,
		submitLabel,
		onSubmit
	}: {
		category?: UserGroupCategoryDto;
		submitLabel: string;
		onSubmit: (request: UpsertUserGroupCategoryRequest) => Promise<void>;
	} = $props();

	let name = $state(untrack(() => category?.name) ?? '');
	let description = $state(untrack(() => category?.description) ?? '');
	const form = new FormState();
</script>

<BlueForm {form} {submitLabel} onsubmit={() => onSubmit(new UpsertUserGroupCategoryRequest({ name, description }))}>
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
			<input
				type="text"
				bind:value={description}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			/>
		{/snippet}
	</FormField>
</BlueForm>
