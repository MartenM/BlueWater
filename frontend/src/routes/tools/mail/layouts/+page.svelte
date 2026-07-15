<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { DataTable, Modal, Button, FormField, breadcrumbs } from '$lib';
	import { UpsertMailLayoutRequest } from '$lib/api/apiClient';
	import type { MailLayoutDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { FormState } from '$lib/forms/formState.svelte';

	let items = $state<MailLayoutDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	let createModal = $state<HTMLDialogElement>();
	let newName = $state('');
	const createForm = new FormState();

	$effect(() => {
		breadcrumbs.set([{ label: 'Mail', href: '/tools/mail' }, { label: 'Layouts' }]);
		return () => breadcrumbs.clear();
	});

	onMount(async () => {
		try {
			items = await apiClient.mailLayoutsAll();
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	async function handleCreate() {
		await createForm.submit(async () => {
			const created = await apiClient.mailLayoutsPOST(
				new UpsertMailLayoutRequest({
					name: newName,
					headerHtml: '',
					footerHtml: '',
					isDefault: false
				})
			);
			createModal?.close();
			goto(resolve('/tools/mail/layouts/[id]', { id: created.id }));
		});
	}
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Mail layouts</h1>
	<Button variant="primary" size="sm" onclick={() => createModal?.showModal()}>
		Nieuwe layout
	</Button>
</div>

{#snippet nameCell(item: MailLayoutDto)}
	<span class="font-medium text-gray-900">{item.name}</span>
{/snippet}

{#snippet defaultCell(item: MailLayoutDto)}
	<span class="text-gray-500">{item.isDefault ? 'Standaard' : ''}</span>
{/snippet}

<DataTable
	columns={[
		{ header: 'Naam', cell: nameCell },
		{ header: '', cell: defaultCell }
	]}
	{items}
	{loading}
	error={error ? 'Layouts konden niet worden geladen.' : undefined}
	emptyMessage="Geen layouts gevonden."
	rowHref={(item) => resolve('/tools/mail/layouts/[id]', { id: item.id })}
/>

<Modal bind:dialog={createModal}>
	<div class="p-6">
		<h2 class="text-lg font-semibold text-gray-900">Nieuwe layout</h2>
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
