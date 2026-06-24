<script lang="ts">
	import { untrack } from 'svelte';
	import { UpsertNewsPostRequest } from '$lib/api/apiClient';
	import { AlertLevel } from '$lib/alert';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { NewsPostDto } from '$lib/api/apiClient';
	import { Button } from '$lib';
	import BlueAlert from './BlueAlert.svelte';
	import FormField from './FormField.svelte';
	import IconPicker from './IconPicker.svelte';

	let {
		post,
		submitLabel,
		onSubmit
	}: {
		post?: NewsPostDto;
		submitLabel: string;
		onSubmit: (request: UpsertNewsPostRequest) => Promise<void>;
	} = $props();

	let title = $state(untrack(() => post?.title) ?? '');
	let shortText = $state(untrack(() => post?.shortText) ?? '');
	let additionalText = $state(untrack(() => post?.additionalText) ?? '');
	let membersOnly = $state(untrack(() => post?.membersOnly) ?? false);
	let iconId = $state<string | undefined>(untrack(() => post?.iconId));
	const form = new FormState();

	function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		form.submit(() =>
			onSubmit(
				new UpsertNewsPostRequest({
					title,
					shortText,
					additionalText: additionalText || undefined,
					membersOnly,
					iconId
				})
			)
		);
	}
</script>

<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
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

	<FormField label="Korte tekst" errors={form.errorsFor('shortText')}>
		{#snippet children(invalid)}
			<textarea
				required
				rows="4"
				bind:value={shortText}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"></textarea>
		{/snippet}
	</FormField>

	<FormField label="Aanvullende tekst" errors={form.errorsFor('additionalText')}>
		{#snippet children(invalid)}
			<textarea
				rows="8"
				bind:value={additionalText}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"></textarea>
		{/snippet}
	</FormField>

	<label class="flex items-center gap-2">
		<input
			type="checkbox"
			bind:checked={membersOnly}
			class="rounded border-gray-300 text-primary focus:ring-primary"
		/>
		<span class="text-sm font-medium text-gray-700">Alleen zichtbaar voor leden</span>
	</label>

	<IconPicker bind:iconId />
	{#each form.errorsFor('iconId') as message (message)}
		<span class="text-sm text-red-600">{message}</span>
	{/each}

	{#if form.formError}
		<BlueAlert level={AlertLevel.Danger}>{form.formError}</BlueAlert>
	{/if}

	<div class="mt-2 self-start">
		<Button type="submit" disabled={form.submitting}>{submitLabel}</Button>
	</div>
</form>
