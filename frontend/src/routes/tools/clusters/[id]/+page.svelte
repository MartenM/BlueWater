<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import {
		HasPermission,
		ConfirmDialog,
		Button,
		Spinner,
		FormField,
		DataTable,
		breadcrumbs
	} from '$lib';
	import {
		AddClusterCriterionRequest,
		BluePermission,
		ClusterCriterionType,
		UpsertMemberClusterRequest
	} from '$lib/api/apiClient';
	import type {
		ClusterMemberDto,
		MemberClusterCriterionDto,
		MemberClusterDto,
		UserGroupCategoryDto,
		UserGroupCategoryRoleDto,
		UserGroupDto
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let cluster = $state<MemberClusterDto | null>(null);
	let loading = $state(true);
	let error = $state(false);
	let deleteDialog = $state<HTMLDialogElement>();

	// Edit name/description
	let editingMeta = $state(false);
	let editName = $state('');
	let editDescription = $state('');
	const metaForm = new FormState();

	// Add criterion
	let addingCriterion = $state(false);
	let newCriterionType = $state<ClusterCriterionType>(ClusterCriterionType.GroupCategory);
	let newCategoryId = $state('');
	let newRoleId = $state('');
	let newGroupId = $state('');
	const criterionForm = new FormState();

	// Dropdowns for criterion form
	let categories = $state<UserGroupCategoryDto[]>([]);
	let roles = $state<UserGroupCategoryRoleDto[]>([]);
	let groups = $state<UserGroupDto[]>([]);
	let loadingDropdowns = $state(false);

	// Members
	let members = $state<ClusterMemberDto[]>([]);
	let loadingMembers = $state(false);
	let membersError = $state<string | undefined>(undefined);

	onMount(async () => {
		try {
			cluster = await apiClient.memberClustersGET(params.id);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (cluster) {
			breadcrumbs.set([{ label: 'Clusters', href: '/tools/clusters' }, { label: cluster.name }]);
		}
		return () => breadcrumbs.clear();
	});

	$effect(() => {
		if (addingCriterion && categories.length === 0 && groups.length === 0) {
			loadDropdowns();
		}
	});

	$effect(() => {
		// When category selection changes, reload roles
		if (newCriterionType === ClusterCriterionType.GroupCategory && newCategoryId) {
			newRoleId = '';
			loadRoles(newCategoryId);
		} else {
			roles = [];
		}
	});

	async function loadDropdowns() {
		loadingDropdowns = true;
		try {
			[categories, groups] = await Promise.all([
				apiClient.userGroupCategoriesAll(),
				apiClient.userGroupsAll()
			]);
		} finally {
			loadingDropdowns = false;
		}
	}

	async function loadRoles(categoryId: string) {
		try {
			roles = await apiClient.rolesAll(categoryId);
		} catch {
			roles = [];
		}
	}

	function startEdit() {
		if (!cluster) return;
		editName = cluster.name;
		editDescription = cluster.description;
		editingMeta = true;
	}

	async function handleUpdateMeta() {
		await metaForm.submit(async () => {
			cluster = await apiClient.memberClustersPUT(
				params.id,
				new UpsertMemberClusterRequest({ name: editName, description: editDescription })
			);
			editingMeta = false;
		});
	}

	async function handleDelete() {
		await apiClient.memberClustersDELETE(params.id);
		goto(resolve('/tools/clusters'));
	}

	async function handleAddCriterion() {
		await criterionForm.submit(async () => {
			const req = new AddClusterCriterionRequest({
				type: newCriterionType,
				userGroupCategoryId:
					newCriterionType === ClusterCriterionType.GroupCategory && newCategoryId
						? newCategoryId
						: undefined,
				userGroupCategoryRoleId:
					newCriterionType === ClusterCriterionType.GroupCategory && newRoleId
						? newRoleId
						: undefined,
				userGroupId:
					newCriterionType === ClusterCriterionType.Group && newGroupId ? newGroupId : undefined
			});

			const criterion = await apiClient.criteriaPOST(params.id, req);
			cluster = { ...cluster!, criteria: [...cluster!.criteria, criterion] } as MemberClusterDto;
			addingCriterion = false;
			newCategoryId = '';
			newRoleId = '';
			newGroupId = '';
		});
	}

	async function handleRemoveCriterion(criterion: MemberClusterCriterionDto) {
		await apiClient.criteriaDELETE(params.id, criterion.id);
		cluster = { ...cluster!, criteria: cluster!.criteria.filter((c) => c.id !== criterion.id) } as MemberClusterDto;
	}

	$effect(() => {
		// Re-fetch members whenever criteria change (track length + ids)
		void cluster?.criteria.map((c) => c.id);
		if (!cluster) return;
		fetchMembers();
	});

	async function fetchMembers() {
		loadingMembers = true;
		membersError = undefined;
		try {
			members = await apiClient.membersAll(params.id);
		} catch {
			membersError = 'Leden konden niet worden geladen.';
		} finally {
			loadingMembers = false;
		}
	}

	function criterionLabel(c: MemberClusterCriterionDto): string {
		if (c.type === ClusterCriterionType.GroupCategory) {
			const cat = c.categoryName ?? c.categoryId ?? '?';
			return c.roleName ? `${c.roleName} in ${cat}` : `Alle leden van ${cat}`;
		}
		return `Leden van ${c.groupName ?? c.groupId ?? '?'}`;
	}
</script>

{#if loading}
	<Spinner />
{:else if error || !cluster}
	<p class="text-sm text-gray-600">Cluster kon niet worden geladen.</p>
{:else}
	<!-- Header -->
	<div class="flex items-start justify-between">
		<div class="flex-1">
			{#if editingMeta}
				<form
					onsubmit={(e) => {
						e.preventDefault();
						handleUpdateMeta();
					}}
					class="flex flex-col gap-3"
				>
					<FormField label="Naam" errors={metaForm.errorsFor('name')}>
						{#snippet children(invalid)}
							<input
								type="text"
								required
								bind:value={editName}
								class="rounded-md focus:border-primary focus:ring-primary {invalid
									? 'border-red-400'
									: 'border-gray-300'}"
							/>
						{/snippet}
					</FormField>
					<FormField label="Omschrijving" errors={metaForm.errorsFor('description')}>
						{#snippet children(invalid)}
							<textarea
								bind:value={editDescription}
								rows="2"
								class="rounded-md focus:border-primary focus:ring-primary {invalid
									? 'border-red-400'
									: 'border-gray-300'}"></textarea>
						{/snippet}
					</FormField>
					{#if metaForm.formError}
						<p class="text-sm text-red-600">{metaForm.formError}</p>
					{/if}
					<div class="flex gap-2">
						<Button variant="primary" size="sm" type="submit" loading={metaForm.submitting}>
							Opslaan
						</Button>
						<Button
							variant="secondary"
							size="sm"
							type="button"
							onclick={() => (editingMeta = false)}
						>
							Annuleren
						</Button>
					</div>
				</form>
			{:else}
				<h1 class="text-2xl font-bold text-gray-900">{cluster.name}</h1>
				{#if cluster.description}
					<p class="mt-1 text-sm text-gray-500">{cluster.description}</p>
				{/if}
			{/if}
		</div>
		<HasPermission permission={BluePermission.ClustersModify}>
			{#if !editingMeta}
				<div class="ml-4 flex gap-3">
					<Button variant="secondary" size="sm" onclick={startEdit}>Bewerken</Button>
					<Button variant="danger" size="sm" onclick={() => deleteDialog?.showModal()}>
						Verwijderen
					</Button>
				</div>
			{/if}
		</HasPermission>
	</div>

	<ConfirmDialog
		bind:dialog={deleteDialog}
		message="Weet je zeker dat je '{cluster.name}' wil verwijderen?"
		confirmLabel="Verwijderen"
		onConfirm={handleDelete}
	/>

	<!-- Criteria -->
	<div class="mt-8">
		<div class="flex items-center justify-between">
			<h2 class="text-lg font-semibold text-gray-900">Criteria</h2>
			<HasPermission permission={BluePermission.ClustersModify}>
				{#if !addingCriterion}
					<Button variant="secondary" size="sm" onclick={() => (addingCriterion = true)}>
						Criterium toevoegen
					</Button>
				{/if}
			</HasPermission>
		</div>

		{#if cluster.criteria.length === 0 && !addingCriterion}
			<p class="mt-3 text-sm text-gray-500">
				Geen criteria. Voeg een criterium toe om te beginnen.
			</p>
		{:else}
			<ul class="mt-3 divide-y divide-gray-200 rounded-md border border-gray-200">
				{#each cluster.criteria as criterion (criterion.id)}
					<li class="flex items-center justify-between px-4 py-3">
						<span class="text-sm text-gray-800">{criterionLabel(criterion)}</span>
						<HasPermission permission={BluePermission.ClustersModify}>
							<button
								type="button"
								onclick={() => handleRemoveCriterion(criterion)}
								class="text-sm font-medium text-red-600 hover:underline"
							>
								Verwijderen
							</button>
						</HasPermission>
					</li>
				{/each}
			</ul>
		{/if}

		<!-- Add criterion form -->
		{#if addingCriterion}
			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleAddCriterion();
				}}
				class="mt-4 rounded-md border border-gray-200 p-4"
			>
				<h3 class="mb-3 text-sm font-semibold text-gray-700">Nieuw criterium</h3>

				{#if loadingDropdowns}
					<Spinner />
				{:else}
					<div class="flex flex-col gap-3">
						<label class="flex flex-col gap-1">
							<span class="text-sm font-medium text-gray-700">Type</span>
							<select
								bind:value={newCriterionType}
								class="rounded-md border-gray-300 focus:border-primary focus:ring-primary"
							>
								<option value={ClusterCriterionType.GroupCategory}>Groepscategorie</option>
								<option value={ClusterCriterionType.Group}>Specifieke groep</option>
							</select>
						</label>

						{#if newCriterionType === ClusterCriterionType.GroupCategory}
							<FormField label="Categorie" errors={criterionForm.errorsFor('userGroupCategoryId')}>
								{#snippet children(invalid)}
									<select
										bind:value={newCategoryId}
										required
										class="rounded-md focus:border-primary focus:ring-primary {invalid
											? 'border-red-400'
											: 'border-gray-300'}"
									>
										<option value="">— Selecteer categorie —</option>
										{#each categories as cat (cat.id)}
											<option value={cat.id}>{cat.name}</option>
										{/each}
									</select>
								{/snippet}
							</FormField>

							{#if newCategoryId}
								<label class="flex flex-col gap-1">
									<span class="text-sm font-medium text-gray-700">Rol (optioneel)</span>
									<select
										bind:value={newRoleId}
										disabled={roles.length === 0}
										class="rounded-md border-gray-300 focus:border-primary focus:ring-primary disabled:bg-gray-50 disabled:text-gray-400"
									>
										<option value=""
											>{roles.length === 0
												? '— Geen rollen gedefinieerd —'
												: '— Alle rollen —'}</option
										>
										{#each roles as role (role.id)}
											<option value={role.id}>{role.namePlural}</option>
										{/each}
									</select>
								</label>
							{/if}
						{:else}
							<FormField label="Groep" errors={criterionForm.errorsFor('userGroupId')}>
								{#snippet children(invalid)}
									<select
										bind:value={newGroupId}
										required
										class="rounded-md focus:border-primary focus:ring-primary {invalid
											? 'border-red-400'
											: 'border-gray-300'}"
									>
										<option value="">— Selecteer groep —</option>
										{#each groups as group (group.id)}
											<option value={group.id}>{group.name} ({group.userGroupCategoryName})</option>
										{/each}
									</select>
								{/snippet}
							</FormField>
						{/if}
					</div>

					{#if criterionForm.formError}
						<p class="mt-2 text-sm text-red-600">{criterionForm.formError}</p>
					{/if}

					<div class="mt-4 flex gap-2">
						<Button variant="primary" size="sm" type="submit" loading={criterionForm.submitting}>
							Toevoegen
						</Button>
						<Button
							variant="secondary"
							size="sm"
							type="button"
							onclick={() => (addingCriterion = false)}
						>
							Annuleren
						</Button>
					</div>
				{/if}
			</form>
		{/if}
	</div>

	<!-- Members -->
	{#snippet memberNameCell(member: ClusterMemberDto)}
		<span class="font-medium text-gray-900">{member.fullname}</span>
	{/snippet}
	{#snippet memberEmailCell(member: ClusterMemberDto)}
		<span class="text-gray-600">{member.email}</span>
	{/snippet}

	<div class="mt-8">
		<h2 class="text-lg font-semibold text-gray-900">
			Leden{members.length > 0 ? ` (${members.length})` : ''}
		</h2>
		<DataTable
			items={members}
			loading={loadingMembers}
			error={membersError}
			emptyMessage="Geen leden gevonden voor de huidige criteria."
			columns={[
				{
					header: 'Naam',
					cell: memberNameCell
				},
				{
					header: 'E-mailadres',
					cell: memberEmailCell
				}
			]}
		/>
	</div>
{/if}
