<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import {
		Button,
		FormField,
		Modal,
		ConfirmDialog,
		HasPermission,
		ClusterPicker,
		breadcrumbs
	} from '$lib';
	import {
		BluePermission,
		UpsertSignupRequest,
		UpsertSignupInputFieldRequest,
		SetReservationRequest,
		SignupInputFieldType
	} from '$lib/api/apiClient';
	import type {
		SignupAdminDetailDto,
		SignupInputFieldDto,
		SignupResponseDto,
		SignupCategoryDto
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();
	const signupId = params.id;

	let detail = $state<SignupAdminDetailDto | null>(null);
	let categories = $state<SignupCategoryDto[]>([]);
	let clusters = $state<Array<{ id: string; name: string }>>([]);
	let loading = $state(true);
	let error = $state(false);

	let activeTab = $state<'settings' | 'fields' | 'responses'>('settings');

	// Settings form
	const settingsForm = new FormState();
	let settingsTitle = $state('');
	let settingsDescription = $state('');
	let settingsCategoryId = $state('');
	let settingsEndDate = $state('');
	let settingsMaxSignups = $state('');
	let settingsMaxWaitlist = $state('');
	let settingsAllowMultiple = $state(false);
	let settingsAllowDelete = $state(true);
	let settingsAllowUpdate = $state(true);
	let settingsHideSignups = $state(false);
	let settingsAnonymous = $state(false);
	let settingsClusterIds = $state<string[]>([]);

	// Field management
	let addFieldModal = $state<HTMLDialogElement>();
	let editFieldModal = $state<HTMLDialogElement>();
	let deleteFieldDialog = $state<HTMLDialogElement>();
	const addFieldForm = new FormState();
	const editFieldForm = new FormState();

	let newFieldTitle = $state('');
	let newFieldNote = $state('');
	let newFieldType = $state<SignupInputFieldType>(SignupInputFieldType.Textbox);
	let newFieldOptions = $state('');
	let newFieldVisible = $state(true);
	let newFieldSortOrder = $state('0');

	let editingField = $state<SignupInputFieldDto | null>(null);
	let editFieldTitle = $state('');
	let editFieldNote = $state('');
	let editFieldType = $state<SignupInputFieldType>(SignupInputFieldType.Textbox);
	let editFieldOptions = $state('');
	let editFieldVisible = $state(true);
	let editFieldSortOrder = $state('0');

	let deletingField = $state<SignupInputFieldDto | null>(null);

	// Response management
	let deleteResponseDialog = $state<HTMLDialogElement>();
	let deletingResponseId = $state<string | null>(null);
	const deleteResponseForm = new FormState();

	// Delete signup
	let deleteSignupDialog = $state<HTMLDialogElement>();
	const deleteSignupForm = new FormState();

	$effect(() => {
		if (detail) {
			breadcrumbs.set([
				{ label: 'Inschrijvingen', href: resolve('/tools/signup') },
				{ label: detail.title }
			]);
		}
		return () => breadcrumbs.clear();
	});

	async function refreshDetail() {
		detail = await apiClient.signupsGET(signupId);
	}

	onMount(async () => {
		try {
			const [detailData, categoriesData, clustersData] = await Promise.all([
				apiClient.signupsGET(signupId),
				apiClient.signupCategoriesAll(),
				apiClient.memberClustersAll()
			]);
			detail = detailData;
			categories = categoriesData;
			clusters = clustersData;
			loadSettingsFromDetail();
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	function loadSettingsFromDetail() {
		if (!detail) return;
		settingsTitle = detail.title;
		settingsDescription = detail.description ?? '';
		settingsCategoryId = detail.categoryId;
		settingsEndDate = detail.endDate ? new Date(detail.endDate).toISOString().slice(0, 16) : '';
		settingsMaxSignups = detail.maxSignups != null ? String(detail.maxSignups) : '';
		settingsMaxWaitlist = detail.maxWaitlist != null ? String(detail.maxWaitlist) : '';
		settingsAllowMultiple = detail.allowMultiple;
		settingsAllowDelete = detail.allowDelete;
		settingsAllowUpdate = detail.allowUpdate;
		settingsHideSignups = detail.hideSignups;
		settingsAnonymous = detail.anonymous;
		settingsClusterIds = detail.clusterIds ? [...detail.clusterIds] : [];
	}

	async function handleSaveSettings() {
		await settingsForm.submit(async () => {
			await apiClient.signupsPUT(
				signupId,
				new UpsertSignupRequest({
					title: settingsTitle,
					description: settingsDescription || undefined,
					categoryId: settingsCategoryId,
					endDate: settingsEndDate ? new Date(settingsEndDate) : undefined,
					maxSignups: settingsMaxSignups ? parseInt(settingsMaxSignups) : undefined,
					maxWaitlist: settingsMaxWaitlist ? parseInt(settingsMaxWaitlist) : undefined,
					allowMultiple: settingsAllowMultiple,
					allowDelete: settingsAllowDelete,
					allowUpdate: settingsAllowUpdate,
					hideSignups: settingsHideSignups,
					anonymous: settingsAnonymous,
					clusterIds: settingsClusterIds
				})
			);
			await refreshDetail();
		});
	}

	async function handleAddField() {
		await addFieldForm.submit(async () => {
			await apiClient.fieldsPOST(
				signupId,
				new UpsertSignupInputFieldRequest({
					title: newFieldTitle,
					note: newFieldNote || undefined,
					type: newFieldType,
					options: newFieldOptions || undefined,
					visible: newFieldVisible,
					sortOrder: parseInt(newFieldSortOrder) || 0
				})
			);
			await refreshDetail();
			addFieldModal?.close();
			newFieldTitle = '';
			newFieldNote = '';
			newFieldType = SignupInputFieldType.Textbox;
			newFieldOptions = '';
			newFieldVisible = true;
			newFieldSortOrder = '0';
		});
	}

	function startEditField(field: SignupInputFieldDto) {
		editingField = field;
		editFieldTitle = field.title;
		editFieldNote = field.note ?? '';
		editFieldType = field.type;
		editFieldOptions = field.options ?? '';
		editFieldVisible = field.visible;
		editFieldSortOrder = String(field.sortOrder);
		editFieldModal?.showModal();
	}

	async function handleEditField() {
		if (!editingField) return;
		await editFieldForm.submit(async () => {
			await apiClient.fieldsPUT(
				signupId,
				editingField!.id,
				new UpsertSignupInputFieldRequest({
					title: editFieldTitle,
					note: editFieldNote || undefined,
					type: editFieldType,
					options: editFieldOptions || undefined,
					visible: editFieldVisible,
					sortOrder: parseInt(editFieldSortOrder) || 0
				})
			);
			await refreshDetail();
			editFieldModal?.close();
		});
	}

	function startDeleteField(field: SignupInputFieldDto) {
		deletingField = field;
		deleteFieldDialog?.showModal();
	}

	async function handleDeleteField() {
		if (!deletingField) return;
		await apiClient.fieldsDELETE(signupId, deletingField.id);
		deletingField = null;
		await refreshDetail();
	}

	function startDeleteResponse(responseId: string) {
		deletingResponseId = responseId;
		deleteResponseDialog?.showModal();
	}

	async function handleDeleteResponse() {
		if (!deletingResponseId) return;
		await deleteResponseForm.submit(async () => {
			await apiClient.responsesDELETE(signupId, deletingResponseId!);
			deletingResponseId = null;
			await refreshDetail();
		});
	}

	async function toggleReservation(response: SignupResponseDto) {
		await apiClient.reservation(
			signupId,
			response.id,
			new SetReservationRequest({ reservation: !response.reservation })
		);
		await refreshDetail();
	}

	async function handleExport() {
		const response = await fetch(`/api/admin/signups/${signupId}/export`, {
			credentials: 'include'
		});
		if (!response.ok) return;
		const blob = await response.blob();
		const url = URL.createObjectURL(blob);
		const a = document.createElement('a');
		a.href = url;
		a.download = `signup-${signupId}.csv`;
		a.click();
		URL.revokeObjectURL(url);
	}

	async function handleDeleteSignup() {
		await deleteSignupForm.submit(async () => {
			await apiClient.signupsDELETE(signupId);
			goto(resolve('/tools/signup'));
		});
	}

	const fieldTypes = [
		{ value: SignupInputFieldType.Textbox, label: 'Tekstvak' },
		{ value: SignupInputFieldType.Textarea, label: 'Meerdere regels' },
		{ value: SignupInputFieldType.NumberField, label: 'Getal' },
		{ value: SignupInputFieldType.Checkbox, label: 'Vinkje' },
		{ value: SignupInputFieldType.CheckboxList, label: 'Meerdere keuzes' },
		{ value: SignupInputFieldType.RadioList, label: 'Één keuze' },
		{ value: SignupInputFieldType.OtherMember, label: 'Ander lid' }
	];

	function fieldTypeLabel(type: SignupInputFieldType) {
		return fieldTypes.find((t) => t.value === type)?.label ?? type;
	}

	function formatDate(d: Date | undefined) {
		if (!d) return '—';
		return new Date(d).toLocaleDateString('nl-NL', {
			day: 'numeric',
			month: 'short',
			year: 'numeric',
			hour: '2-digit',
			minute: '2-digit'
		});
	}

	const hasOptions = $derived(
		newFieldType === SignupInputFieldType.CheckboxList ||
			newFieldType === SignupInputFieldType.RadioList
	);

	const editHasOptions = $derived(
		editFieldType === SignupInputFieldType.CheckboxList ||
			editFieldType === SignupInputFieldType.RadioList
	);
</script>

{#if loading}
	<p class="text-sm text-gray-500">Laden…</p>
{:else if error || !detail}
	<p class="text-sm text-gray-600">Inschrijving kon niet worden geladen.</p>
{:else}
	<div class="space-y-6">
		<!-- Header -->
		<div class="flex items-start justify-between">
			<div>
				<a href={resolve('/tools/signup')} class="text-sm text-gray-500 hover:underline">
					← Inschrijvingen
				</a>
				<h1 class="mt-1 text-2xl font-bold text-gray-900">{detail.title}</h1>
			</div>
			<HasPermission permission={BluePermission.AdminSignupModify}>
				<Button variant="danger" size="sm" onclick={() => deleteSignupDialog?.showModal()}>
					Verwijderen
				</Button>
			</HasPermission>
		</div>

		<!-- Tabs -->
		<div class="border-b border-gray-200">
			<nav class="-mb-px flex gap-6">
				{#each [['settings', 'Instellingen'], ['fields', 'Velden'], ['responses', 'Aanmeldingen']] as const as [tab, label] (tab)}
					<button
						class="py-2 text-sm font-medium border-b-2 {activeTab === tab
							? 'border-blue-600 text-blue-600'
							: 'border-transparent text-gray-500 hover:text-gray-700'}"
						onclick={() => (activeTab = tab)}
					>
						{label}
						{#if tab === 'responses'}
							({detail.responses?.length ?? 0})
						{/if}
					</button>
				{/each}
			</nav>
		</div>

		<!-- Settings tab -->
		{#if activeTab === 'settings'}
			<HasPermission permission={BluePermission.AdminSignupModify}>
				<form
					onsubmit={(e) => {
						e.preventDefault();
						handleSaveSettings();
					}}
					class="max-w-lg space-y-4"
				>
					<FormField label="Titel" errors={settingsForm.errorsFor('title')}>
						{#snippet children(invalid)}
							<input
								type="text"
								required
								bind:value={settingsTitle}
								class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
							/>
						{/snippet}
					</FormField>

					<FormField label="Beschrijving" errors={settingsForm.errorsFor('description')}>
						{#snippet children(invalid)}
							<textarea
								bind:value={settingsDescription}
								rows="3"
								class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"></textarea>
						{/snippet}
					</FormField>

					<FormField label="Categorie" errors={settingsForm.errorsFor('categoryId')}>
						{#snippet children(invalid)}
							<select
								required
								bind:value={settingsCategoryId}
								class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
							>
								{#each categories as cat (cat.id)}
									<option value={cat.id}>{cat.title}</option>
								{/each}
							</select>
						{/snippet}
					</FormField>

					<FormField label="Sluitdatum" errors={settingsForm.errorsFor('endDate')}>
						{#snippet children(invalid)}
							<input
								type="datetime-local"
								bind:value={settingsEndDate}
								class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
							/>
						{/snippet}
					</FormField>

					<div class="grid grid-cols-2 gap-4">
						<FormField label="Max. aanmeldingen" errors={settingsForm.errorsFor('maxSignups')}>
							{#snippet children(invalid)}
								<input
									type="number"
									min="1"
									bind:value={settingsMaxSignups}
									class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
								/>
							{/snippet}
						</FormField>
						<FormField label="Max. wachtlijst" errors={settingsForm.errorsFor('maxWaitlist')}>
							{#snippet children(invalid)}
								<input
									type="number"
									min="1"
									bind:value={settingsMaxWaitlist}
									class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
								/>
							{/snippet}
						</FormField>
					</div>

					<FormField label="Clusters" errors={settingsForm.errorsFor('clusterIds')}>
						{#snippet children(invalid)}
							<ClusterPicker {clusters} bind:selectedIds={settingsClusterIds} {invalid} />
						{/snippet}
					</FormField>

					<div class="flex flex-wrap gap-4 text-sm">
						<label class="flex items-center gap-1.5">
							<input type="checkbox" bind:checked={settingsAllowMultiple} class="h-4 w-4 rounded" />
							Meerdere aanmeldingen
						</label>
						<label class="flex items-center gap-1.5">
							<input type="checkbox" bind:checked={settingsAllowDelete} class="h-4 w-4 rounded" />
							Afmelden toegestaan
						</label>
						<label class="flex items-center gap-1.5">
							<input type="checkbox" bind:checked={settingsAllowUpdate} class="h-4 w-4 rounded" />
							Wijzigen toegestaan
						</label>
						<label class="flex items-center gap-1.5">
							<input type="checkbox" bind:checked={settingsHideSignups} class="h-4 w-4 rounded" />
							Aanmeldingen verbergen
						</label>
						<label class="flex items-center gap-1.5">
							<input type="checkbox" bind:checked={settingsAnonymous} class="h-4 w-4 rounded" />
							Anoniem
						</label>
					</div>

					{#if settingsForm.formError}
						<p class="text-sm text-red-600">{settingsForm.formError}</p>
					{/if}
					<Button type="submit" variant="primary" size="sm" loading={settingsForm.submitting}>
						Opslaan
					</Button>
				</form>
			</HasPermission>
		{/if}

		<!-- Fields tab -->
		{#if activeTab === 'fields'}
			<div>
				<HasPermission permission={BluePermission.AdminSignupModify}>
					<Button variant="primary" size="sm" onclick={() => addFieldModal?.showModal()}>
						Veld toevoegen
					</Button>
				</HasPermission>

				{#if !detail.fields || detail.fields.length === 0}
					<p class="mt-4 text-sm text-gray-500">Nog geen velden.</p>
				{:else}
					<div class="mt-4 divide-y divide-gray-100 border border-gray-200 rounded-lg">
						{#each [...detail.fields].sort((a, b) => a.sortOrder - b.sortOrder) as field (field.id)}
							<div class="flex items-center justify-between px-4 py-3">
								<div>
									<p class="text-sm font-medium text-gray-900">{field.title}</p>
									<p class="text-xs text-gray-500">
										{fieldTypeLabel(field.type)}{field.visible ? '' : ' · verborgen'}
									</p>
									{#if field.note}
										<p class="text-xs text-gray-400">{field.note}</p>
									{/if}
								</div>
								<HasPermission permission={BluePermission.AdminSignupModify}>
									<div class="flex gap-2 shrink-0">
										<button
											class="text-sm text-blue-600 hover:underline"
											onclick={() => startEditField(field)}
										>
											Bewerken
										</button>
										<button
											class="text-sm text-red-600 hover:underline"
											onclick={() => startDeleteField(field)}
										>
											Verwijderen
										</button>
									</div>
								</HasPermission>
							</div>
						{/each}
					</div>
				{/if}
			</div>
		{/if}

		<!-- Responses tab -->
		{#if activeTab === 'responses'}
			<div>
				<div class="mb-4 flex items-center justify-between">
					<p class="text-sm text-gray-600">
						{detail.responses?.filter((r) => r.status !== 'waitlist').length ?? 0} aangemeld
						{#if detail.maxSignups != null}(max {detail.maxSignups}){/if}
						{#if (detail.responses?.filter((r) => r.status === 'waitlist').length ?? 0) > 0}
							&middot; {detail.responses?.filter((r) => r.status === 'waitlist').length} wachtlijst
						{/if}
					</p>
					<Button variant="secondary" size="sm" onclick={handleExport}>Exporteren (CSV)</Button>
				</div>

				{#if !detail.responses || detail.responses.length === 0}
					<p class="text-sm text-gray-500">Nog geen aanmeldingen.</p>
				{:else}
					<div class="overflow-x-auto">
						<table class="min-w-full text-sm text-left">
							<thead class="text-gray-500 border-b border-gray-200">
								<tr>
									<th class="py-2 pr-4 font-medium">Naam</th>
									<th class="py-2 pr-4 font-medium">Status</th>
									<th class="py-2 pr-4 font-medium">Aangemeld op</th>
									{#each detail.fields ?? [] as field (field.id)}
										<th class="py-2 pr-4 font-medium">{field.title}</th>
									{/each}
									<th class="py-2 font-medium"></th>
								</tr>
							</thead>
							<tbody class="divide-y divide-gray-100">
								{#each detail.responses as response (response.id)}
									<tr>
										<td class="py-2 pr-4 text-gray-900">{response.userFullname ?? '—'}</td>
										<td class="py-2 pr-4">
											<button
												class="text-xs px-2 py-0.5 rounded-full {response.reservation
													? 'bg-blue-100 text-blue-700'
													: response.status === 'waitlist'
														? 'bg-amber-100 text-amber-700'
														: 'bg-green-100 text-green-700'} hover:opacity-80"
												title="Klik om reservering te wisselen"
												onclick={() => toggleReservation(response)}
											>
												{response.reservation
													? 'Reservering'
													: response.status === 'waitlist'
														? 'Wachtlijst'
														: 'Aangemeld'}
											</button>
										</td>
										<td class="py-2 pr-4 text-gray-500">{formatDate(response.createdAt)}</td>
										{#each detail.fields ?? [] as field (field.id)}
											<td class="py-2 pr-4 text-gray-600">
												{response.fieldValues?.find((v) => v.fieldId === field.id)?.value ?? '—'}
											</td>
										{/each}
										<td class="py-2 text-right">
											<HasPermission permission={BluePermission.AdminSignupModify}>
												<button
													class="text-sm text-red-600 hover:underline"
													onclick={() => startDeleteResponse(response.id)}
												>
													Verwijderen
												</button>
											</HasPermission>
										</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}
			</div>
		{/if}
	</div>

	<!-- Add field modal -->
	<Modal bind:dialog={addFieldModal}>
		<div class="p-6 max-w-sm w-full">
			<h2 class="text-lg font-semibold text-gray-900">Veld toevoegen</h2>
			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleAddField();
				}}
				class="mt-4 flex flex-col gap-4"
			>
				<FormField label="Naam" errors={addFieldForm.errorsFor('title')}>
					{#snippet children(invalid)}
						<input
							type="text"
							required
							bind:value={newFieldTitle}
							class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
						/>
					{/snippet}
				</FormField>
				<FormField label="Type" errors={addFieldForm.errorsFor('type')}>
					{#snippet children(invalid)}
						<select
							bind:value={newFieldType}
							class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
						>
							{#each fieldTypes as ft (ft.value)}
								<option value={ft.value}>{ft.label}</option>
							{/each}
						</select>
					{/snippet}
				</FormField>
				{#if hasOptions}
					<FormField label="Opties (kommagescheiden)" errors={addFieldForm.errorsFor('options')}>
						{#snippet children(invalid)}
							<input
								type="text"
								bind:value={newFieldOptions}
								placeholder="Optie A, Optie B, Optie C"
								class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
							/>
						{/snippet}
					</FormField>
				{/if}
				<FormField label="Toelichting" errors={addFieldForm.errorsFor('note')}>
					{#snippet children(invalid)}
						<input
							type="text"
							bind:value={newFieldNote}
							class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
						/>
					{/snippet}
				</FormField>
				<FormField label="Volgorde" errors={addFieldForm.errorsFor('sortOrder')}>
					{#snippet children(invalid)}
						<input
							type="number"
							bind:value={newFieldSortOrder}
							class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
						/>
					{/snippet}
				</FormField>
				<label class="flex items-center gap-2 text-sm">
					<input type="checkbox" bind:checked={newFieldVisible} class="h-4 w-4 rounded" />
					Zichtbaar voor leden
				</label>
				{#if addFieldForm.formError}
					<p class="text-sm text-red-600">{addFieldForm.formError}</p>
				{/if}
				<div class="flex justify-end gap-3">
					<Button
						variant="secondary"
						size="sm"
						onclick={() => addFieldModal?.close()}
						type="button"
					>
						Annuleren
					</Button>
					<Button variant="primary" size="sm" type="submit" loading={addFieldForm.submitting}>
						Toevoegen
					</Button>
				</div>
			</form>
		</div>
	</Modal>

	<!-- Edit field modal -->
	<Modal bind:dialog={editFieldModal}>
		<div class="p-6 max-w-sm w-full">
			<h2 class="text-lg font-semibold text-gray-900">Veld bewerken</h2>
			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleEditField();
				}}
				class="mt-4 flex flex-col gap-4"
			>
				<FormField label="Naam" errors={editFieldForm.errorsFor('title')}>
					{#snippet children(invalid)}
						<input
							type="text"
							required
							bind:value={editFieldTitle}
							class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
						/>
					{/snippet}
				</FormField>
				<FormField label="Type" errors={editFieldForm.errorsFor('type')}>
					{#snippet children(invalid)}
						<select
							bind:value={editFieldType}
							class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
						>
							{#each fieldTypes as ft (ft.value)}
								<option value={ft.value}>{ft.label}</option>
							{/each}
						</select>
					{/snippet}
				</FormField>
				{#if editHasOptions}
					<FormField label="Opties (kommagescheiden)" errors={editFieldForm.errorsFor('options')}>
						{#snippet children(invalid)}
							<input
								type="text"
								bind:value={editFieldOptions}
								placeholder="Optie A, Optie B, Optie C"
								class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
							/>
						{/snippet}
					</FormField>
				{/if}
				<FormField label="Toelichting" errors={editFieldForm.errorsFor('note')}>
					{#snippet children(invalid)}
						<input
							type="text"
							bind:value={editFieldNote}
							class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
						/>
					{/snippet}
				</FormField>
				<FormField label="Volgorde" errors={editFieldForm.errorsFor('sortOrder')}>
					{#snippet children(invalid)}
						<input
							type="number"
							bind:value={editFieldSortOrder}
							class="rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
						/>
					{/snippet}
				</FormField>
				<label class="flex items-center gap-2 text-sm">
					<input type="checkbox" bind:checked={editFieldVisible} class="h-4 w-4 rounded" />
					Zichtbaar voor leden
				</label>
				{#if editFieldForm.formError}
					<p class="text-sm text-red-600">{editFieldForm.formError}</p>
				{/if}
				<div class="flex justify-end gap-3">
					<Button
						variant="secondary"
						size="sm"
						onclick={() => editFieldModal?.close()}
						type="button"
					>
						Annuleren
					</Button>
					<Button variant="primary" size="sm" type="submit" loading={editFieldForm.submitting}>
						Opslaan
					</Button>
				</div>
			</form>
		</div>
	</Modal>

	<ConfirmDialog
		bind:dialog={deleteFieldDialog}
		message="Weet je zeker dat je dit veld wilt verwijderen?"
		onConfirm={handleDeleteField}
	/>

	<ConfirmDialog
		bind:dialog={deleteResponseDialog}
		message="Weet je zeker dat je deze aanmelding wilt verwijderen?"
		onConfirm={handleDeleteResponse}
	/>

	<ConfirmDialog
		bind:dialog={deleteSignupDialog}
		message="Weet je zeker dat je deze inschrijving wilt verwijderen? Alle aanmeldingen worden ook verwijderd."
		onConfirm={handleDeleteSignup}
	/>
{/if}
