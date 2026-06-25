<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import {
		HasPermission,
		BlueAlert,
		ConfirmDialog,
		InstanceMemberManager,
		InstancePermissionManager,
		Spinner,
		breadcrumbs,
		session
	} from '$lib';
	import { AlertLevel } from '$lib/alert';
	import { BluePermission } from '$lib/api/apiClient';
	import type { UserGroupInstanceDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let instance = $state<UserGroupInstanceDto | null>(null);
	let loading = $state(true);
	let error = $state(false);
	let deleteError = $state<string | null>(null);
	let deleting = $state(false);
	let deleteDialog = $state<HTMLDialogElement>();

	const canModify = $derived(session.hasPermission(BluePermission.AdminModifyGroups));

	onMount(async () => {
		try {
			instance = await apiClient.userGroupInstancesGET(params.instanceId);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (!instance) return;
		breadcrumbs.set([
			{ label: 'Groepen', href: '/tools/groups' },
			{ label: instance.userGroupName, href: `/tools/groups/group/${instance.userGroupId}` },
			{ label: instance.seasonName }
		]);
		return () => breadcrumbs.clear();
	});

	async function handleDelete() {
		if (!instance) return;
		deleting = true;
		deleteError = null;
		try {
			await apiClient.userGroupInstancesDELETE(instance.id);
			goto(resolve('/tools/groups/group/[groupId]', { groupId: instance.userGroupId }));
		} catch {
			deleteError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
			deleting = false;
		}
	}
</script>

{#if loading}
	<Spinner />
{:else if error}
	<p class="text-sm text-gray-600">Instantie kon niet worden geladen.</p>
{:else if instance}
	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-2xl font-bold text-gray-900">{instance.userGroupName}</h1>
			<p class="text-sm text-gray-500">{instance.seasonName}</p>
		</div>
		<HasPermission permission={BluePermission.AdminModifyGroups}>
			<button
				type="button"
				onclick={() => deleteDialog?.showModal()}
				disabled={deleting}
				class="text-sm font-medium text-red-600 hover:underline disabled:opacity-60"
			>
				Instantie verwijderen
			</button>
		</HasPermission>
	</div>

	{#if deleteError}
		<div class="mt-4">
			<BlueAlert level={AlertLevel.Danger}>{deleteError}</BlueAlert>
		</div>
	{/if}

	<ConfirmDialog
		bind:dialog={deleteDialog}
		message={`Instantie voor ${instance.seasonName} verwijderen?`}
		onConfirm={handleDelete}
	/>

	<div class="mt-6 grid grid-cols-2 gap-8">
		<InstanceMemberManager
			instanceId={instance.id}
			categoryId={instance.userGroupCategoryId}
			members={instance.members}
			readonly={!canModify}
		/>
		<InstancePermissionManager
			userGroupId={instance.userGroupId}
			permissions={instance.permissions}
		/>
	</div>
{/if}
