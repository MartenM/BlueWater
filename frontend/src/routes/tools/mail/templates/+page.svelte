<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { DataTable, Modal, Button, FormField, breadcrumbs } from '$lib';
	import { MailTemplateKind, UpsertMailTemplateRequest } from '$lib/api/apiClient';
	import type { MailTemplateDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { FormState } from '$lib/forms/formState.svelte';

	let items = $state<MailTemplateDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	let createModal = $state<HTMLDialogElement>();
	let newName = $state('');
	const createForm = new FormState();

	$effect(() => {
		breadcrumbs.set([{ label: 'Mail', href: '/tools/mail' }, { label: "Sjabloon's" }]);
		return () => breadcrumbs.clear();
	});

	onMount(async () => {
		try {
			items = await apiClient.mailTemplatesAll(undefined);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	async function handleCreate() {
		await createForm.submit(async () => {
			const created = await apiClient.mailTemplatesPOST(
				new UpsertMailTemplateRequest({
					name: newName,
					kind: MailTemplateKind.Mailing,
					subjectTemplate: 'Onderwerp',
					bodyMarkdown: 'Inhoud van het bericht.',
					defaultLayoutId: undefined,
					defaultSenderKey: undefined
				})
			);
			createModal?.close();
			goto(resolve('/tools/mail/templates/[id]', { id: created.id }));
		});
	}
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Mailsjablonen</h1>
	<Button variant="primary" size="sm" onclick={() => createModal?.showModal()}>
		Nieuw sjabloon
	</Button>
</div>

{#snippet nameCell(item: MailTemplateDto)}
	<span class="font-medium text-gray-900">{item.name}</span>
{/snippet}

{#snippet kindCell(item: MailTemplateDto)}
	<span class="text-gray-600">
		{item.kind === MailTemplateKind.Mailing ? 'Mailing' : 'Transactioneel'}
	</span>
{/snippet}

{#snippet subjectCell(item: MailTemplateDto)}
	<span class="text-gray-500">{item.subjectTemplate}</span>
{/snippet}

<DataTable
	columns={[
		{ header: 'Naam', cell: nameCell },
		{ header: 'Type', cell: kindCell },
		{ header: 'Onderwerp', cell: subjectCell }
	]}
	{items}
	{loading}
	error={error ? 'Sjablonen konden niet worden geladen.' : undefined}
	emptyMessage="Geen sjablonen gevonden."
	rowHref={(item) => resolve('/tools/mail/templates/[id]', { id: item.id })}
/>

<Modal bind:dialog={createModal}>
	<div class="p-6">
		<h2 class="text-lg font-semibold text-gray-900">Nieuw sjabloon</h2>
		<form
			onsubmit={(e) => {
				e.preventDefault();
				handleCreate();
			}}
			class="mt-4 flex flex-col gap-4"
		>
			<FormField label="Naam" errors={createForm.errorsFor('name')}>
				{#snippet children(invalid)}
					<input
						type="text"
						required
						bind:value={newName}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>
			<p class="text-xs text-gray-400">
				Nieuwe sjablonen zijn altijd van het type Mailing. Transactionele sjablonen worden
				automatisch aangemaakt en kunnen hier niet worden toegevoegd.
			</p>
			{#if createForm.formError}
				<p class="text-sm text-red-600">{createForm.formError}</p>
			{/if}
			<div class="flex justify-end gap-3">
				<Button variant="secondary" size="sm" onclick={() => createModal?.close()} type="button">
					Annuleren
				</Button>
				<Button variant="primary" size="sm" type="submit" loading={createForm.submitting}>
					Aanmaken
				</Button>
			</div>
		</form>
	</div>
</Modal>
