<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { ConfirmDialog, Button, Spinner, FormField, BlueForm, breadcrumbs } from '$lib';
	import { UpsertMailLayoutRequest } from '$lib/api/apiClient';
	import type { MailLayoutDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let layout = $state<MailLayoutDto | null>(null);
	let loading = $state(true);
	let error = $state(false);
	let deleteDialog = $state<HTMLDialogElement>();

	let name = $state('');
	let headerHtml = $state('');
	let footerHtml = $state('');
	let isDefault = $state(false);
	const form = new FormState();

	onMount(async () => {
		try {
			layout = await apiClient.mailLayoutsGET(params.id);
			name = layout.name;
			headerHtml = layout.headerHtml;
			footerHtml = layout.footerHtml;
			isDefault = layout.isDefault;
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (layout) {
			breadcrumbs.set([
				{ label: 'Mail', href: '/tools/mail' },
				{ label: 'Layouts', href: '/tools/mail/layouts' },
				{ label: layout.name }
			]);
		}
		return () => breadcrumbs.clear();
	});

	async function handleEdit() {
		await form.submit(async () => {
			layout = await apiClient.mailLayoutsPUT(
				params.id,
				new UpsertMailLayoutRequest({ name, headerHtml, footerHtml, isDefault })
			);
		});
	}

	async function handleDelete() {
		await apiClient.mailLayoutsDELETE(params.id);
		goto(resolve('/tools/mail/layouts'));
	}
</script>

{#if loading}
	<Spinner />
{:else if error || !layout}
	<p class="text-sm text-gray-600">Layout kon niet worden geladen.</p>
{:else}
	<div class="flex items-start justify-between">
		<h1 class="text-2xl font-bold text-gray-900">{layout.name}</h1>
		<Button variant="danger" size="sm" onclick={() => deleteDialog?.showModal()}>
			Verwijderen
		</Button>
	</div>

	<ConfirmDialog
		bind:dialog={deleteDialog}
		message="Weet je zeker dat je '{layout.name}' wil verwijderen?"
		confirmLabel="Verwijderen"
		onConfirm={handleDelete}
	/>

	<div class="mt-6 max-w-2xl">
		<BlueForm {form} submitLabel="Opslaan" onsubmit={handleEdit}>
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
			<FormField label="Header HTML" errors={form.errorsFor('headerHtml')}>
				{#snippet children(invalid)}
					<textarea
						rows="6"
						bind:value={headerHtml}
						class="font-mono text-sm rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"></textarea>
				{/snippet}
			</FormField>
			<FormField label="Footer HTML" errors={form.errorsFor('footerHtml')}>
				{#snippet children(invalid)}
					<textarea
						rows="6"
						bind:value={footerHtml}
						class="font-mono text-sm rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"></textarea>
				{/snippet}
			</FormField>
			<label class="flex items-center gap-2">
				<input
					type="checkbox"
					bind:checked={isDefault}
					class="rounded border-gray-300 text-primary focus:ring-primary"
				/>
				<span class="text-sm font-medium text-gray-700">Standaard layout</span>
			</label>
		</BlueForm>
	</div>
{/if}
