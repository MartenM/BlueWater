<script lang="ts">
	import { onMount } from 'svelte';
	import {
		DataTable,
		HasPermission,
		Modal,
		Button,
		FormField,
		ConfirmDialog,
		breadcrumbs
	} from '$lib';
	import { BluePermission, UpsertSignupCategoryRequest } from '$lib/api/apiClient';
	import type { SignupCategoryDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { FormState } from '$lib/forms/formState.svelte';
	import { resolve } from '$app/paths';

	let items = $state<SignupCategoryDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	let createModal = $state<HTMLDialogElement>();
	let editModal = $state<HTMLDialogElement>();
	let deleteDialog = $state<HTMLDialogElement>();

	const createForm = new FormState();
	const editForm = new FormState();
	const deleteForm = new FormState();

	let newTitle = $state('');
	let newHidden = $state(false);
	let newSortOrder = $state('0');

	let editing = $state<SignupCategoryDto | null>(null);
	let editTitle = $state('');
	let editHidden = $state(false);
	let editSortOrder = $state('0');

	let deleting = $state<SignupCategoryDto | null>(null);

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Inschrijvingen', href: resolve('/tools/signup') },
			{ label: 'Categorieën' }
		]);
		return () => breadcrumbs.clear();
	});

	onMount(async () => {
		try {
			items = await apiClient.signupCategoriesAll();
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	async function handleCreate() {
		await createForm.submit(async () => {
			const created = await apiClient.signupCategoriesPOST(
				new UpsertSignupCategoryRequest({
					title: newTitle,
					hidden: newHidden,
					sortOrder: parseInt(newSortOrder) || 0
				})
			);
			items = [...items, created];
			createModal?.close();
			newTitle = '';
			newHidden = false;
			newSortOrder = '0';
		});
	}

	function startEdit(item: SignupCategoryDto) {
		editing = item;
		editTitle = item.title;
		editHidden = item.hidden;
		editSortOrder = String(item.sortOrder);
		editModal?.showModal();
	}

	async function handleEdit() {
		if (!editing) return;
		await editForm.submit(async () => {
			const updated = await apiClient.signupCategoriesPUT(
				editing!.id,
				new UpsertSignupCategoryRequest({
					title: editTitle,
					hidden: editHidden,
					sortOrder: parseInt(editSortOrder) || 0
				})
			);
			items = items.map((i) => (i.id === updated.id ? updated : i));
			editModal?.close();
		});
	}

	function startDelete(item: SignupCategoryDto) {
		deleting = item;
		deleteDialog?.showModal();
	}

	async function handleDelete() {
		if (!deleting) return;
		await deleteForm.submit(async () => {
			await apiClient.signupCategoriesDELETE(deleting!.id);
			items = items.filter((i) => i.id !== deleting!.id);
			deleting = null;
		});
	}
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Categorieën</h1>
	<HasPermission permission={BluePermission.AdminSignupModify}>
		<Button variant="primary" size="sm" onclick={() => createModal?.showModal()}>
			Nieuwe categorie
		</Button>
	</HasPermission>
</div>

{#snippet titleCell(item: SignupCategoryDto)}
	<span class="font-medium text-gray-900">{item.title}</span>
{/snippet}

{#snippet hiddenCell(item: SignupCategoryDto)}
	<span class="text-gray-500">{item.hidden ? 'Verborgen' : 'Zichtbaar'}</span>
{/snippet}

{#snippet sortOrderCell(item: SignupCategoryDto)}
	<span class="text-gray-500">{item.sortOrder}</span>
{/snippet}

{#snippet actionsCell(item: SignupCategoryDto)}
	<HasPermission permission={BluePermission.AdminSignupModify}>
		<div class="flex gap-2">
			<button
				class="text-sm text-blue-600 hover:underline"
				onclick={(e) => { e.stopPropagation(); startEdit(item); }}
			>
				Bewerken
			</button>
			<button
				class="text-sm text-red-600 hover:underline"
				onclick={(e) => { e.stopPropagation(); startDelete(item); }}
			>
				Verwijderen
			</button>
		</div>
	</HasPermission>
{/snippet}

<DataTable
	columns={[
		{ header: 'Naam', cell: titleCell },
		{ header: 'Zichtbaarheid', cell: hiddenCell },
		{ header: 'Volgorde', cell: sortOrderCell },
		{ header: '', cell: actionsCell }
	]}
	{items}
	{loading}
	error={error ? 'Categorieën konden niet worden geladen.' : undefined}
	emptyMessage="Geen categorieën gevonden."
/>

<!-- Create modal -->
<Modal bind:dialog={createModal}>
	<div class="p-6">
		<h2 class="text-lg font-semibold text-gray-900">Nieuwe categorie</h2>
		<form
			onsubmit={(e) => { e.preventDefault(); handleCreate(); }}
			class="mt-4 flex flex-col gap-4"
		>
			<FormField label="Naam" errors={createForm.errorsFor('title')}>
				{#snippet children(invalid)}
					<input
						type="text"
						required
						bind:value={newTitle}
						class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>
			<FormField label="Volgorde" errors={createForm.errorsFor('sortOrder')}>
				{#snippet children(invalid)}
					<input
						type="number"
						bind:value={newSortOrder}
						class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>
			<label class="flex items-center gap-2 text-sm">
				<input type="checkbox" bind:checked={newHidden} class="h-4 w-4 rounded" />
				Verborgen
			</label>
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

<!-- Edit modal -->
<Modal bind:dialog={editModal}>
	<div class="p-6">
		<h2 class="text-lg font-semibold text-gray-900">Categorie bewerken</h2>
		<form
			onsubmit={(e) => { e.preventDefault(); handleEdit(); }}
			class="mt-4 flex flex-col gap-4"
		>
			<FormField label="Naam" errors={editForm.errorsFor('title')}>
				{#snippet children(invalid)}
					<input
						type="text"
						required
						bind:value={editTitle}
						class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>
			<FormField label="Volgorde" errors={editForm.errorsFor('sortOrder')}>
				{#snippet children(invalid)}
					<input
						type="number"
						bind:value={editSortOrder}
						class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>
			<label class="flex items-center gap-2 text-sm">
				<input type="checkbox" bind:checked={editHidden} class="h-4 w-4 rounded" />
				Verborgen
			</label>
			{#if editForm.formError}
				<p class="text-sm text-red-600">{editForm.formError}</p>
			{/if}
			<div class="flex justify-end gap-3">
				<Button variant="secondary" size="sm" onclick={() => editModal?.close()} type="button">
					Annuleren
				</Button>
				<Button variant="primary" size="sm" type="submit" loading={editForm.submitting}>
					Opslaan
				</Button>
			</div>
		</form>
	</div>
</Modal>

<ConfirmDialog
	bind:dialog={deleteDialog}
	message="Weet je zeker dat je deze categorie wilt verwijderen?"
	onConfirm={handleDelete}
/>
