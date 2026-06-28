<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { DataTable, HasPermission, Modal, Button, FormField, breadcrumbs } from '$lib';
	import { BluePermission, UpsertMemberClusterRequest } from '$lib/api/apiClient';
	import type { MemberClusterDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { FormState } from '$lib/forms/formState.svelte';

	let items = $state<MemberClusterDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	let createModal = $state<HTMLDialogElement>();
	let newName = $state('');
	let newDescription = $state('');
	const createForm = new FormState();

	$effect(() => {
		breadcrumbs.set([{ label: 'Clusters' }]);
		return () => breadcrumbs.clear();
	});

	onMount(async () => {
		try {
			items = await apiClient.memberClustersAll();
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	async function handleCreate() {
		await createForm.submit(async () => {
			const created = await apiClient.memberClustersPOST(
				new UpsertMemberClusterRequest({ name: newName, description: newDescription })
			);
			createModal?.close();
			goto(resolve('/tools/clusters/[id]', { id: created.id }));
		});
	}
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Clusters</h1>
	<HasPermission permission={BluePermission.ClustersModify}>
		<Button variant="primary" size="sm" onclick={() => createModal?.showModal()}>
			Nieuw cluster
		</Button>
	</HasPermission>
</div>

{#snippet nameCell(item: MemberClusterDto)}
	<span class="font-medium text-gray-900">{item.name}</span>
{/snippet}

{#snippet descriptionCell(item: MemberClusterDto)}
	<span class="text-gray-600">{item.description}</span>
{/snippet}

{#snippet criteriaCell(item: MemberClusterDto)}
	<span class="text-gray-500">{item.criteria.length} criteria</span>
{/snippet}

<DataTable
	columns={[
		{ header: 'Naam', cell: nameCell },
		{ header: 'Omschrijving', cell: descriptionCell },
		{ header: 'Criteria', cell: criteriaCell }
	]}
	{items}
	{loading}
	error={error ? 'Clusters konden niet worden geladen.' : undefined}
	emptyMessage="Geen clusters gevonden."
	rowHref={(item) => resolve('/tools/clusters/[id]', { id: item.id })}
/>

<Modal bind:dialog={createModal}>
	<div class="p-6">
		<h2 class="text-lg font-semibold text-gray-900">Nieuw cluster</h2>
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
			<FormField label="Omschrijving" errors={createForm.errorsFor('description')}>
				{#snippet children(invalid)}
					<textarea
						bind:value={newDescription}
						rows="2"
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"></textarea>
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
