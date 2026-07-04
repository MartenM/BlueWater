<script lang="ts">
	import { untrack } from 'svelte';
	import { UpsertUserGroupRequest } from '$lib/api/apiClient';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { UserGroupDto } from '$lib/api/apiClient';
	import { BlueForm } from '$lib';
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
	let administrative = $state(untrack(() => group?.administrative) ?? false);
	const form = new FormState();
</script>

<BlueForm
	{form}
	{submitLabel}
	onsubmit={() =>
		onSubmit(
			new UpsertUserGroupRequest({
				name,
				description,
				administrative,
				userGroupCategoryId: categoryId
			})
		)}
>
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

	<label class="flex items-center gap-2 text-sm text-gray-700">
		<input
			type="checkbox"
			bind:checked={administrative}
			class="rounded text-primary focus:ring-primary"
		/>
		Administratief
	</label>
</BlueForm>
