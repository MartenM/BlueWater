<script lang="ts">
	import type { Snippet } from 'svelte';
	import { AlertLevel } from '$lib/alert';
	import { FormState } from '$lib/forms/formState.svelte';
	import { Button } from '$lib';
	import BlueAlert from '../BlueAlert.svelte';

	let {
		form,
		submitLabel,
		onsubmit,
		children
	}: {
		form: FormState;
		submitLabel: string;
		onsubmit: () => Promise<void>;
		children: Snippet;
	} = $props();

	function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		form.submit(onsubmit);
	}
</script>

<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
	{@render children()}

	{#if form.formError}
		<BlueAlert level={AlertLevel.Danger}>{form.formError}</BlueAlert>
	{/if}

	<div class="mt-2 self-start">
		<Button type="submit" loading={form.submitting}>{submitLabel}</Button>
	</div>
</form>
