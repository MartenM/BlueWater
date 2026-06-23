<script lang="ts">
	import { untrack } from 'svelte';
	import { ApiException, UpsertNewsPostRequest } from '$lib/api/apiClient';
	import { AlertLevel } from '$lib/alert';
	import type { NewsPostDto } from '$lib/api/apiClient';
	import BlueAlert from './BlueAlert.svelte';
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
	let error = $state<string | null>(null);
	let submitting = $state(false);

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		error = null;
		submitting = true;
		try {
			await onSubmit(
				new UpsertNewsPostRequest({
					title,
					shortText,
					additionalText: additionalText || undefined,
					membersOnly,
					iconId
				})
			);
		} catch (e) {
			error =
				e instanceof ApiException && e.result?.errors
					? Object.values(e.result.errors as Record<string, string[]>)
							.flat()
							.join(' ')
					: 'Er ging iets mis. Probeer het later opnieuw.';
		} finally {
			submitting = false;
		}
	}
</script>

<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
	<label class="flex flex-col gap-1">
		<span class="text-sm font-medium text-gray-700">Titel</span>
		<input
			type="text"
			required
			bind:value={title}
			class="rounded-md border-gray-300 focus:border-primary focus:ring-primary"
		/>
	</label>

	<label class="flex flex-col gap-1">
		<span class="text-sm font-medium text-gray-700">Korte tekst</span>
		<textarea
			required
			rows="4"
			bind:value={shortText}
			class="rounded-md border-gray-300 focus:border-primary focus:ring-primary"></textarea>
	</label>

	<label class="flex flex-col gap-1">
		<span class="text-sm font-medium text-gray-700">Aanvullende tekst</span>
		<textarea
			rows="8"
			bind:value={additionalText}
			class="rounded-md border-gray-300 focus:border-primary focus:ring-primary"></textarea>
	</label>

	<label class="flex items-center gap-2">
		<input
			type="checkbox"
			bind:checked={membersOnly}
			class="rounded border-gray-300 text-primary focus:ring-primary"
		/>
		<span class="text-sm font-medium text-gray-700">Alleen zichtbaar voor leden</span>
	</label>

	<IconPicker bind:iconId />

	{#if error}
		<BlueAlert level={AlertLevel.Danger}>{error}</BlueAlert>
	{/if}

	<button
		type="submit"
		disabled={submitting}
		class="mt-2 self-start rounded-md bg-primary px-4 py-2 font-medium text-primary-content hover:bg-primary-hover disabled:opacity-60"
	>
		{submitLabel}
	</button>
</form>
