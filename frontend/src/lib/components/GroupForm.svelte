<script lang="ts">
	import { untrack } from 'svelte';
	import { UpsertUserGroupRequest } from '$lib/api/apiClient';
	import { AlertLevel } from '$lib/alert';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { UserGroupDto } from '$lib/api/apiClient';
	import BlueAlert from './BlueAlert.svelte';
	import FormField from './FormField.svelte';

	let {
		group,
		categoryId,
		categoryName,
		submitLabel,
		onSubmit
	}: {
		group?: UserGroupDto;
		categoryId: string;
		categoryName: string;
		submitLabel: string;
		onSubmit: (request: UpsertUserGroupRequest) => Promise<void>;
	} = $props();

	let name = $state(untrack(() => group?.name) ?? '');
	let description = $state(untrack(() => group?.description) ?? '');
	const form = new FormState();

	function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		form.submit(() =>
			onSubmit(new UpsertUserGroupRequest({ name, description, userGroupCategoryId: categoryId }))
		);
	}
</script>

<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
	<div>
		<span class="text-sm font-medium text-gray-700">Categorie</span>
		<p class="text-sm text-gray-900">{categoryName}</p>
	</div>

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

	{#if form.formError}
		<BlueAlert level={AlertLevel.Danger}>{form.formError}</BlueAlert>
	{/if}

	<button
		type="submit"
		disabled={form.submitting}
		class="mt-2 self-start rounded-md bg-primary px-4 py-2 font-medium text-primary-content hover:bg-primary-hover disabled:opacity-60"
	>
		{submitLabel}
	</button>
</form>
