<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient } from '$lib/api/client';
	import { Button } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import {
		AssignMemberRoleRequest,
		InstanceMemberDto,
		type UserDto,
		type UserGroupCategoryRoleDto
	} from '$lib/api/apiClient';
	import BlueAlert from './BlueAlert.svelte';
	import ConfirmDialog from './common/ConfirmDialog.svelte';

	let {
		instanceId,
		categoryId,
		members: initialMembers,
		readonly = false
	}: {
		instanceId: string;
		categoryId: string;
		members: InstanceMemberDto[];
		readonly?: boolean;
	} = $props();

	type MemberWithUser = { dto: InstanceMemberDto; user: UserDto };

	let members = $state<MemberWithUser[]>([]);
	let roles = $state<UserGroupCategoryRoleDto[]>([]);
	let loadError = $state(false);
	let search = $state('');
	let searchResults = $state<UserDto[]>([]);
	let actionError = $state<string | null>(null);
	let busy = $state(false);

	let confirmDialog = $state<HTMLDialogElement>();
	let confirmMessage = $state('');
	let confirmAction = $state<() => void>(() => {});

	function displayName(role: UserGroupCategoryRoleDto | undefined): string {
		if (!role) return 'Geen rol';
		return role.namePlural;
	}

	async function loadMembers() {
		try {
			const users = await Promise.all(initialMembers.map((m) => apiClient.getUser(m.userId)));
			members = initialMembers.map((m, i) => ({ dto: m, user: users[i] }));
			loadError = false;
		} catch {
			loadError = true;
		}
	}

	async function loadRoles() {
		try {
			roles = await apiClient.rolesAll(categoryId);
		} catch {
			roles = [];
		}
	}

	onMount(() => {
		loadMembers();
		loadRoles();
	});

	async function handleSearchSubmit(event: SubmitEvent) {
		event.preventDefault();
		if (!search.trim()) {
			searchResults = [];
			return;
		}
		try {
			const result = await apiClient.listUsers(1, 10, search);
			searchResults = result.items.filter((u) => !members.some((m) => m.user.id === u.id));
		} catch {
			actionError = 'Zoeken is mislukt. Probeer het later opnieuw.';
		}
	}

	async function addMember(user: UserDto) {
		busy = true;
		actionError = null;
		try {
			await apiClient.usersPOST(instanceId, user.id);
			members = [
				...members,
				{
					dto: new InstanceMemberDto({ userId: user.id, userGroupCategoryRoleId: undefined }),
					user
				}
			];
			searchResults = searchResults.filter((u) => u.id !== user.id);
		} catch {
			actionError = 'Toevoegen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	function removeMember(item: MemberWithUser) {
		confirmMessage = `${item.user.fullname} uit deze groep verwijderen?`;
		confirmAction = () => doRemoveMember(item);
		confirmDialog?.showModal();
	}

	async function doRemoveMember(item: MemberWithUser) {
		busy = true;
		actionError = null;
		try {
			await apiClient.usersDELETE(instanceId, item.user.id);
			members = members.filter((m) => m.user.id !== item.user.id);
		} catch {
			actionError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	async function assignRole(item: MemberWithUser, roleId: string | undefined) {
		busy = true;
		actionError = null;
		try {
			await apiClient.role(
				instanceId,
				item.user.id,
				new AssignMemberRoleRequest({ userGroupCategoryRoleId: roleId })
			);
			members = members.map((m) =>
				m.user.id === item.user.id
					? {
							...m,
							dto: new InstanceMemberDto({ userId: m.dto.userId, userGroupCategoryRoleId: roleId })
						}
					: m
			);
		} catch {
			actionError = 'Rol toewijzen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}
</script>

<ConfirmDialog bind:dialog={confirmDialog} message={confirmMessage} onConfirm={confirmAction} />

<div>
	<h2 class="text-sm font-semibold text-gray-700">Leden</h2>

	{#if loadError}
		<p class="mt-2 text-sm text-gray-600">Leden konden niet worden geladen.</p>
	{:else}
		<div class="mt-2 divide-y divide-gray-200 border-t border-gray-200">
			{#each members as item (item.user.id)}
				<div class="flex items-center justify-between gap-4 py-2">
					<span class="min-w-0 flex-1 text-sm text-gray-900">{item.user.fullname}</span>
					{#if roles.length > 0 && !readonly}
						<select
							value={item.dto.userGroupCategoryRoleId ?? ''}
							onchange={(e) =>
								assignRole(item, (e.currentTarget as HTMLSelectElement).value || undefined)}
							disabled={busy}
							class="rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary disabled:opacity-60"
						>
							<option value="">Geen rol</option>
							{#each roles as role (role.id)}
								<option value={role.id}>{displayName(role)}</option>
							{/each}
						</select>
					{:else if item.dto.userGroupCategoryRoleId}
						<span class="text-sm text-gray-500">
							{displayName(roles.find((r) => r.id === item.dto.userGroupCategoryRoleId))}
						</span>
					{/if}
					{#if !readonly}
						<button
							type="button"
							disabled={busy}
							onclick={() => removeMember(item)}
							class="text-sm font-medium text-red-600 hover:underline disabled:opacity-60"
						>
							Verwijderen
						</button>
					{/if}
				</div>
			{:else}
				<p class="py-2 text-sm text-gray-500">Geen leden.</p>
			{/each}
		</div>
	{/if}

	{#if !readonly}
		<form class="mt-4 flex gap-2" onsubmit={handleSearchSubmit}>
			<input
				type="search"
				placeholder="Zoek gebruiker om toe te voegen"
				bind:value={search}
				class="flex-1 rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
			/>
			<Button type="submit" variant="secondary" size="sm">Zoeken</Button>
		</form>

		{#if searchResults.length > 0}
			<div class="mt-2 divide-y divide-gray-200 border-t border-gray-200">
				{#each searchResults as result (result.id)}
					<div class="flex items-center justify-between py-2">
						<span class="text-sm text-gray-900">{result.fullname}</span>
						<button
							type="button"
							disabled={busy}
							onclick={() => addMember(result)}
							class="text-sm font-medium text-primary-hover hover:underline disabled:opacity-60"
						>
							Toevoegen
						</button>
					</div>
				{/each}
			</div>
		{/if}
	{/if}

	{#if actionError}
		<div class="mt-3">
			<BlueAlert level={AlertLevel.Danger}>{actionError}</BlueAlert>
		</div>
	{/if}
</div>
