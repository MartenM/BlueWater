<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { apiClient } from '$lib/api/client';
	import { Button } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import {
		AssignGroupPermissionRequest,
		BluePermission,
		UserGroupPermissionDto,
		type UserGroupCategoryRoleDto
	} from '$lib/api/apiClient';
	import ConfirmDialog from './common/ConfirmDialog.svelte';
	import BlueAlert from './BlueAlert.svelte';

	let {
		groupId,
		categoryId,
		permissions: initialPermissions
	}: {
		groupId: string;
		categoryId: string;
		permissions: UserGroupPermissionDto[];
	} = $props();

	let permissions = $state<UserGroupPermissionDto[]>(untrack(() => [...initialPermissions]));
	let roles = $state<UserGroupCategoryRoleDto[]>([]);
	let availablePermissions = $state<BluePermission[]>([]);
	let selectedPermission = $state<BluePermission | ''>('');
	let selectedRoleId = $state<string>('');
	let actionError = $state<string | null>(null);
	let busy = $state(false);

	let confirmDialog = $state<HTMLDialogElement>();
	let confirmMessage = $state('');
	let confirmAction = $state<() => void>(() => {});

	function roleLabel(roleId: string | undefined): string {
		if (!roleId) return 'Alle leden';
		return roles.find((r) => r.id === roleId)?.namePlural ?? roleId;
	}

	onMount(async () => {
		try {
			availablePermissions = await apiClient.permissionsAll();
		} catch {
			availablePermissions = [];
		}
		try {
			roles = await apiClient.rolesAll(categoryId);
		} catch {
			roles = [];
		}
	});

	async function addPermission(event: SubmitEvent) {
		event.preventDefault();
		if (!selectedPermission) return;
		const permission = selectedPermission;
		const roleId = selectedRoleId || undefined;
		busy = true;
		actionError = null;
		try {
			await apiClient.permissionsPOST(
				groupId,
				new AssignGroupPermissionRequest({ permission, userGroupCategoryRoleId: roleId })
			);
			permissions = [
				...permissions,
				new UserGroupPermissionDto({ permission, userGroupCategoryRoleId: roleId })
			];
			selectedPermission = '';
			selectedRoleId = '';
		} catch {
			actionError = 'Toevoegen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	function removePermission(p: UserGroupPermissionDto) {
		confirmMessage = `Permissie "${p.permission}" verwijderen?`;
		confirmAction = () => doRemovePermission(p);
		confirmDialog?.showModal();
	}

	async function doRemovePermission(p: UserGroupPermissionDto) {
		busy = true;
		actionError = null;
		try {
			await apiClient.permissionsDELETE(groupId, p.permission, p.userGroupCategoryRoleId);
			permissions = permissions.filter(
				(x) =>
					!(
						x.permission === p.permission && x.userGroupCategoryRoleId === p.userGroupCategoryRoleId
					)
			);
		} catch {
			actionError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}
</script>

<ConfirmDialog bind:dialog={confirmDialog} message={confirmMessage} onConfirm={confirmAction} />

<div>
	<h2 class="text-sm font-semibold text-gray-700">Permissies</h2>

	<div class="mt-2 divide-y divide-gray-200 border-t border-gray-200">
		{#each permissions as p (p.permission + (p.userGroupCategoryRoleId ?? ''))}
			<div class="flex items-center justify-between py-2">
				<div>
					<span class="text-sm text-gray-900">{p.permission}</span>
					<span class="ml-2 text-xs text-gray-500">{roleLabel(p.userGroupCategoryRoleId)}</span>
				</div>
				<button
					type="button"
					disabled={busy}
					onclick={() => removePermission(p)}
					class="text-sm font-medium text-red-600 hover:underline disabled:opacity-60"
				>
					Verwijderen
				</button>
			</div>
		{:else}
			<p class="py-2 text-sm text-gray-500">Geen permissies.</p>
		{/each}
	</div>

	<form class="mt-4 flex flex-wrap gap-2" onsubmit={addPermission}>
		<select
			bind:value={selectedPermission}
			class="flex-1 rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
		>
			<option value="">Selecteer een permissie</option>
			{#each availablePermissions as permission (permission)}
				<option value={permission}>{permission}</option>
			{/each}
		</select>
		{#if roles.length > 0}
			<select
				bind:value={selectedRoleId}
				class="rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
			>
				<option value="">Alle leden</option>
				{#each roles as role (role.id)}
					<option value={role.id}>{role.namePlural}</option>
				{/each}
			</select>
		{/if}
		<Button type="submit" variant="secondary" size="sm" disabled={!selectedPermission || busy}>
			Toevoegen
		</Button>
	</form>

	{#if actionError}
		<div class="mt-3">
			<BlueAlert level={AlertLevel.Danger}>{actionError}</BlueAlert>
		</div>
	{/if}
</div>
