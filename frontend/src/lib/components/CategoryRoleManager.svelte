<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient } from '$lib/api/client';
	import { Button, ConfirmDialog } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import {
		ReorderRolesRequest,
		UserGroupCategoryRoleDto,
		UpsertUserGroupCategoryRoleRequest
	} from '$lib/api/apiClient';
	import BlueAlert from './BlueAlert.svelte';

	let { categoryId }: { categoryId: string } = $props();

	let roles = $state<UserGroupCategoryRoleDto[]>([]);
	let loadError = $state(false);
	let actionError = $state<string | null>(null);
	let busy = $state(false);

	// create form
	let newPlural = $state('');
	let newMasculine = $state('');
	let newFeminine = $state('');

	// edit state
	let editingId = $state<string | null>(null);
	let editPlural = $state('');
	let editMasculine = $state('');
	let editFeminine = $state('');

	// confirm dialog
	let confirmDialog = $state<HTMLDialogElement>();
	let confirmMessage = $state('');
	let confirmAction = $state<() => void>(() => {});

	function showConfirm(message: string, action: () => void) {
		confirmMessage = message;
		confirmAction = action;
		confirmDialog?.showModal();
	}

	onMount(async () => {
		try {
			roles = await apiClient.rolesAll(categoryId);
		} catch {
			loadError = true;
		}
	});

	function startEdit(role: UserGroupCategoryRoleDto) {
		editingId = role.id;
		editPlural = role.namePlural;
		editMasculine = role.nameMasculine ?? '';
		editFeminine = role.nameFeminine ?? '';
	}

	function cancelEdit() {
		editingId = null;
	}

	async function saveEdit(roleId: string) {
		if (!editPlural.trim()) return;
		busy = true;
		actionError = null;
		try {
			const updated = await apiClient.rolesPUT(
				categoryId,
				roleId,
				new UpsertUserGroupCategoryRoleRequest({
					namePlural: editPlural.trim(),
					nameMasculine: editMasculine.trim() || undefined,
					nameFeminine: editFeminine.trim() || undefined
				})
			);
			roles = roles.map((r) => (r.id === roleId ? updated : r));
			editingId = null;
		} catch {
			actionError = 'Opslaan is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	function deleteRole(role: UserGroupCategoryRoleDto) {
		showConfirm(`Rol "${role.namePlural}" verwijderen?`, () => doDeleteRole(role));
	}

	async function doDeleteRole(role: UserGroupCategoryRoleDto) {
		busy = true;
		actionError = null;
		try {
			await apiClient.rolesDELETE(categoryId, role.id);
			roles = roles.filter((r) => r.id !== role.id);
		} catch {
			actionError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	async function createRole(event: SubmitEvent) {
		event.preventDefault();
		if (!newPlural.trim()) return;
		busy = true;
		actionError = null;
		try {
			const created = await apiClient.rolesPOST(
				categoryId,
				new UpsertUserGroupCategoryRoleRequest({
					namePlural: newPlural.trim(),
					nameMasculine: newMasculine.trim() || undefined,
					nameFeminine: newFeminine.trim() || undefined
				})
			);
			roles = [...roles, created];
			newPlural = '';
			newMasculine = '';
			newFeminine = '';
		} catch {
			actionError = 'Aanmaken is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	async function moveUp(index: number) {
		if (index === 0) return;
		await applyMove(index, index - 1);
	}

	async function moveDown(index: number) {
		if (index === roles.length - 1) return;
		await applyMove(index, index + 1);
	}

	async function applyMove(from: number, to: number) {
		const previous = [...roles];
		const updated = [...roles];
		[updated[to], updated[from]] = [updated[from], updated[to]];
		roles = updated;
		busy = true;
		actionError = null;
		try {
			await apiClient.reorder(
				categoryId,
				new ReorderRolesRequest({ roleIds: updated.map((r) => r.id) })
			);
		} catch {
			roles = previous;
			actionError = 'Volgorde opslaan is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}
</script>

<ConfirmDialog bind:dialog={confirmDialog} message={confirmMessage} onConfirm={confirmAction} />

<div>
	<h2 class="text-sm font-semibold text-gray-700">Rollen</h2>
	<p class="mt-1 text-xs text-gray-500">
		Rollen zijn van toepassing op alle groepen in deze categorie.
	</p>

	{#if loadError}
		<p class="mt-2 text-sm text-gray-600">Rollen konden niet worden geladen.</p>
	{:else}
		<div class="mt-3 divide-y divide-gray-200 border-t border-gray-200">
			{#each roles as role, i (role.id)}
				{#if editingId === role.id}
					<div class="py-3">
						<div class="flex flex-col gap-2 sm:flex-row">
							<input
								bind:value={editPlural}
								placeholder="Meervoud (verplicht)"
								class="flex-1 rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
							/>
							<input
								bind:value={editMasculine}
								placeholder="Mannelijk (optioneel)"
								class="flex-1 rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
							/>
							<input
								bind:value={editFeminine}
								placeholder="Vrouwelijk (optioneel)"
								class="flex-1 rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
							/>
						</div>
						<div class="mt-2 flex gap-2">
							<Button
								type="button"
								variant="primary"
								size="sm"
								disabled={!editPlural.trim() || busy}
								onclick={() => saveEdit(role.id)}
							>
								Opslaan
							</Button>
							<Button type="button" variant="secondary" size="sm" onclick={cancelEdit}>
								Annuleren
							</Button>
						</div>
					</div>
				{:else}
					<div class="flex items-center justify-between py-2">
						<div>
							<span class="text-sm font-medium text-gray-900">{role.namePlural}</span>
							{#if role.nameMasculine || role.nameFeminine}
								<span class="ml-2 text-xs text-gray-500">
									{[role.nameMasculine, role.nameFeminine].filter(Boolean).join(' / ')}
								</span>
							{/if}
						</div>
						<div class="flex items-center gap-3">
							<div class="flex gap-0.5">
								<button
									type="button"
									disabled={i === 0 || busy}
									onclick={() => moveUp(i)}
									aria-label="Omhoog"
									class="rounded px-1 text-gray-400 hover:text-gray-600 disabled:opacity-30"
									>↑</button
								>
								<button
									type="button"
									disabled={i === roles.length - 1 || busy}
									onclick={() => moveDown(i)}
									aria-label="Omlaag"
									class="rounded px-1 text-gray-400 hover:text-gray-600 disabled:opacity-30"
									>↓</button
								>
							</div>
							<button
								type="button"
								onclick={() => startEdit(role)}
								class="text-sm font-medium text-primary-hover hover:underline"
							>
								Bewerken
							</button>
							<button
								type="button"
								disabled={busy}
								onclick={() => deleteRole(role)}
								class="text-sm font-medium text-red-600 hover:underline disabled:opacity-60"
							>
								Verwijderen
							</button>
						</div>
					</div>
				{/if}
			{:else}
				<p class="py-2 text-sm text-gray-500">Geen rollen voor deze categorie.</p>
			{/each}
		</div>

		<form class="mt-4" onsubmit={createRole}>
			<div class="flex flex-col gap-2 sm:flex-row">
				<input
					bind:value={newPlural}
					placeholder="Meervoud (verplicht)"
					class="flex-1 rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
				/>
				<input
					bind:value={newMasculine}
					placeholder="Mannelijk (optioneel)"
					class="flex-1 rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
				/>
				<input
					bind:value={newFeminine}
					placeholder="Vrouwelijk (optioneel)"
					class="flex-1 rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
				/>
				<Button type="submit" variant="secondary" size="sm" disabled={!newPlural.trim() || busy}>
					Rol toevoegen
				</Button>
			</div>
		</form>
	{/if}

	{#if actionError}
		<div class="mt-3">
			<BlueAlert level={AlertLevel.Danger}>{actionError}</BlueAlert>
		</div>
	{/if}
</div>
