<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import {
		DataTable,
		HasPermission,
		Modal,
		Button,
		FormField,
		ConfirmDialog,
		ClusterPicker,
		breadcrumbs
	} from '$lib';
	import {
		BluePermission,
		UpsertSignupRequest
	} from '$lib/api/apiClient';
	import type { SignupListItemDto, SignupCategoryDto, SignupArchiveSeasonDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { FormState } from '$lib/forms/formState.svelte';

	let items = $state<SignupListItemDto[]>([]);
	let categories = $state<SignupCategoryDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	let showArchived = $state(false);
	let archivedSeasons = $state<SignupArchiveSeasonDto[]>([]);
	let archivedLoading = $state(false);
	let archivedError = $state(false);

	let createModal = $state<HTMLDialogElement>();
	const createForm = new FormState();

	// Form fields
	let newTitle = $state('');
	let newDescription = $state('');
	let newCategoryId = $state('');
	let newEndDate = $state('');
	let newMaxSignups = $state('');
	let newMaxWaitlist = $state('');
	let newAllowMultiple = $state(false);
	let newAllowDelete = $state(true);
	let newAllowUpdate = $state(true);
	let newHideSignups = $state(false);
	let newAnonymous = $state(false);
	let newClusterIds = $state<string[]>([]);

	let clusters = $state<Array<{ id: string; name: string }>>([]);

	$effect(() => {
		breadcrumbs.set([{ label: 'Inschrijvingen' }]);
		return () => breadcrumbs.clear();
	});

	onMount(async () => {
		try {
			const [itemsData, categoriesData, clustersData] = await Promise.all([
				apiClient.signupsAll(),
				apiClient.signupCategoriesAll(),
				apiClient.memberClustersAll()
			]);
			items = itemsData;
			categories = categoriesData;
			clusters = clustersData;
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	async function toggleArchived() {
		if (showArchived) {
			showArchived = false;
			return;
		}
		showArchived = true;
		if (archivedSeasons.length > 0) return;
		archivedLoading = true;
		archivedError = false;
		try {
			archivedSeasons = await apiClient.archive();
		} catch {
			archivedError = true;
		} finally {
			archivedLoading = false;
		}
	}

	async function handleCreate() {
		await createForm.submit(async () => {
			const created = await apiClient.signupsPOST(
				new UpsertSignupRequest({
					title: newTitle,
					description: newDescription || undefined,
					categoryId: newCategoryId,
					endDate: newEndDate ? new Date(newEndDate) : undefined,
					maxSignups: newMaxSignups ? parseInt(newMaxSignups) : undefined,
					maxWaitlist: newMaxWaitlist ? parseInt(newMaxWaitlist) : undefined,
					allowMultiple: newAllowMultiple,
					allowDelete: newAllowDelete,
					allowUpdate: newAllowUpdate,
					hideSignups: newHideSignups,
					anonymous: newAnonymous,
					clusterIds: newClusterIds
				})
			);
			createModal?.close();
			goto(resolve('/tools/signup/[id]', { id: created.id }));
		});
	}

	function formatDate(d: Date | undefined) {
		if (!d) return '—';
		return new Date(d).toLocaleDateString('nl-NL', { day: 'numeric', month: 'short', year: 'numeric' });
	}
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Inschrijvingen</h1>
	<div class="flex gap-2">
		<a href={resolve('/tools/signup/categories')} class="self-center text-sm text-gray-500 hover:underline">
			Categorieën
		</a>
		<Button variant="secondary" size="sm" onclick={toggleArchived}>
			{showArchived ? 'Verberg oude' : 'Oude inschrijvingen'}
		</Button>
		<HasPermission permission={BluePermission.AdminSignupModify}>
			<Button variant="primary" size="sm" onclick={() => createModal?.showModal()}>
				Nieuwe inschrijving
			</Button>
		</HasPermission>
	</div>
</div>

{#snippet titleCell(item: SignupListItemDto)}
	<span class="font-medium text-gray-900">{item.title}</span>
{/snippet}

{#snippet categoryCell(item: SignupListItemDto)}
	<span class="text-gray-600">{item.categoryTitle ?? '—'}</span>
{/snippet}

{#snippet endDateCell(item: SignupListItemDto)}
	<span class="text-gray-600">{formatDate(item.endDate)}</span>
{/snippet}

{#snippet responsesCell(item: SignupListItemDto)}
	<span class="text-gray-600">{item.validResponses}{item.maxSignups != null ? `/${item.maxSignups}` : ''}</span>
{/snippet}

<DataTable
	columns={[
		{ header: 'Titel', cell: titleCell },
		{ header: 'Categorie', cell: categoryCell },
		{ header: 'Einddatum', cell: endDateCell },
		{ header: 'Aanmeldingen', cell: responsesCell }
	]}
	{items}
	{loading}
	error={error ? 'Inschrijvingen konden niet worden geladen.' : undefined}
	emptyMessage="Geen inschrijvingen gevonden."
	rowHref={(item) => resolve('/tools/signup/[id]', { id: item.id })}
/>

{#if showArchived}
	<div class="mt-8">
		<h2 class="mb-4 text-lg font-semibold text-gray-700">Oude inschrijvingen</h2>
		{#if archivedLoading}
			<p class="text-sm text-gray-500">Laden…</p>
		{:else if archivedError}
			<p class="text-sm text-red-600">Oude inschrijvingen konden niet worden geladen.</p>
		{:else if archivedSeasons.length === 0}
			<p class="text-sm text-gray-500">Geen oude inschrijvingen gevonden.</p>
		{:else}
			{#each archivedSeasons as season (season.seasonName)}
				<details class="mb-4 rounded-lg border border-gray-200">
					<summary class="cursor-pointer select-none rounded-lg bg-gray-50 px-4 py-3 text-sm font-medium text-gray-700 hover:bg-gray-100">
						{season.seasonName}
						<span class="ml-1 font-normal text-gray-400">({season.signups?.length ?? 0})</span>
					</summary>
					<div class="p-2">
						<DataTable
							columns={[
								{ header: 'Titel', cell: titleCell },
								{ header: 'Categorie', cell: categoryCell },
								{ header: 'Einddatum', cell: endDateCell },
								{ header: 'Aanmeldingen', cell: responsesCell }
							]}
							items={season.signups ?? []}
							loading={false}
							emptyMessage="Geen inschrijvingen."
							rowHref={(item) => resolve('/tools/signup/[id]', { id: item.id })}
						/>
					</div>
				</details>
			{/each}
		{/if}
	</div>
{/if}

<Modal bind:dialog={createModal}>
	<div class="p-6 max-w-lg w-full">
		<h2 class="text-lg font-semibold text-gray-900">Nieuwe inschrijving</h2>
		<form
			onsubmit={(e) => { e.preventDefault(); handleCreate(); }}
			class="mt-4 flex flex-col gap-4"
		>
			<FormField label="Titel" errors={createForm.errorsFor('title')}>
				{#snippet children(invalid)}
					<input
						type="text"
						required
						bind:value={newTitle}
						class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>

			<FormField label="Beschrijving" errors={createForm.errorsFor('description')}>
				{#snippet children(invalid)}
					<textarea
						bind:value={newDescription}
						rows="2"
						class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
					></textarea>
				{/snippet}
			</FormField>

			<FormField label="Categorie" errors={createForm.errorsFor('categoryId')}>
				{#snippet children(invalid)}
					<select
						required
						bind:value={newCategoryId}
						class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
					>
						<option value="" disabled>Kies een categorie</option>
						{#each categories as cat (cat.id)}
							<option value={cat.id}>{cat.title}</option>
						{/each}
					</select>
				{/snippet}
			</FormField>

			<FormField label="Sluitdatum" errors={createForm.errorsFor('endDate')}>
				{#snippet children(invalid)}
					<input
						type="datetime-local"
						bind:value={newEndDate}
						class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>

			<div class="grid grid-cols-2 gap-4">
				<FormField label="Max. aanmeldingen" errors={createForm.errorsFor('maxSignups')}>
					{#snippet children(invalid)}
						<input
							type="number"
							min="1"
							bind:value={newMaxSignups}
							class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
						/>
					{/snippet}
				</FormField>
				<FormField label="Max. wachtlijst" errors={createForm.errorsFor('maxWaitlist')}>
					{#snippet children(invalid)}
						<input
							type="number"
							min="1"
							bind:value={newMaxWaitlist}
							class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
						/>
					{/snippet}
				</FormField>
			</div>

			<FormField label="Clusters" errors={createForm.errorsFor('clusterIds')}>
				{#snippet children(invalid)}
					<ClusterPicker clusters={clusters} bind:selectedIds={newClusterIds} {invalid} />
				{/snippet}
			</FormField>

			<div class="flex flex-wrap gap-4 text-sm">
				<label class="flex items-center gap-1.5">
					<input type="checkbox" bind:checked={newAllowMultiple} class="h-4 w-4 rounded" />
					Meerdere aanmeldingen
				</label>
				<label class="flex items-center gap-1.5">
					<input type="checkbox" bind:checked={newAllowDelete} class="h-4 w-4 rounded" />
					Afmelden toegestaan
				</label>
				<label class="flex items-center gap-1.5">
					<input type="checkbox" bind:checked={newAllowUpdate} class="h-4 w-4 rounded" />
					Wijzigen toegestaan
				</label>
				<label class="flex items-center gap-1.5">
					<input type="checkbox" bind:checked={newHideSignups} class="h-4 w-4 rounded" />
					Aanmeldingen verbergen
				</label>
				<label class="flex items-center gap-1.5">
					<input type="checkbox" bind:checked={newAnonymous} class="h-4 w-4 rounded" />
					Anoniem
				</label>
			</div>

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
