<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { HasPermission, BlueAlert, Spinner, breadcrumbs } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import { BluePermission } from '$lib/api/apiClient';
	import type { UserGroupDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let group = $state<UserGroupDto | null>(null);
	let instances = $state<
		{ id: string; seasonName: string; memberCount: number; permissionCount: number }[]
	>([]);
	let loading = $state(true);
	let error = $state(false);
	let deleteError = $state<string | null>(null);
	let deleting = $state(false);

	onMount(async () => {
		try {
			const [groupResult, allInstances] = await Promise.all([
				apiClient.userGroupsGET(params.groupId),
				apiClient.userGroupInstancesAll()
			]);
			group = groupResult;
			instances = allInstances
				.filter((i) => i.userGroupId === params.groupId)
				.map((i) => ({
					id: i.id,
					seasonName: i.seasonName,
					memberCount: i.memberUserIds.length,
					permissionCount: i.permissions.length
				}));
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (!group) return;
		breadcrumbs.set([{ label: 'Groepen', href: '/tools/groups' }, { label: group.name }]);
		return () => breadcrumbs.clear();
	});

	async function handleDelete() {
		if (!group || !confirm(`Groep "${group.name}" verwijderen?`)) return;
		deleting = true;
		deleteError = null;
		try {
			await apiClient.userGroupsDELETE(group.id);
			goto(resolve('/tools/groups'));
		} catch {
			deleteError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
			deleting = false;
		}
	}
</script>

{#if loading}
	<Spinner />
{:else if error}
	<p class="text-sm text-gray-600">Groep kon niet worden geladen.</p>
{:else if group}
	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-2xl font-bold text-gray-900">{group.name}</h1>
			<p class="text-sm text-gray-500">
				{group.userGroupCategoryName} &middot; {group.description}
			</p>
		</div>
		<HasPermission permission={BluePermission.AdminModifyGroups}>
			<div class="flex gap-3">
				<a
					href={resolve('/tools/groups/group/[groupId]/edit', { groupId: group.id })}
					class="text-sm font-medium text-primary-hover hover:underline"
				>
					Bewerken
				</a>
				<button
					type="button"
					onclick={handleDelete}
					disabled={deleting}
					class="text-sm font-medium text-red-600 hover:underline disabled:opacity-60"
				>
					Verwijderen
				</button>
			</div>
		</HasPermission>
	</div>

	{#if deleteError}
		<div class="mt-4">
			<BlueAlert level={AlertLevel.Danger}>{deleteError}</BlueAlert>
		</div>
	{/if}

	<div class="mt-6 flex items-center justify-between">
		<h2 class="text-sm font-semibold text-gray-700">Instanties per seizoen</h2>
		<HasPermission permission={BluePermission.AdminModifyGroups}>
			<!-- eslint-disable svelte/no-navigation-without-resolve -- resolve() result with an appended query string, not a static route literal -->
			<a
				href={resolve('/tools/groups/instances/new') + `?groupId=${group.id}`}
				class="text-sm font-medium text-primary-hover hover:underline"
			>
				<!-- eslint-enable svelte/no-navigation-without-resolve -->
				Nieuwe instantie
			</a>
		</HasPermission>
	</div>

	<div class="mt-2 divide-y divide-gray-200 border-t border-gray-200">
		{#each instances as instance (instance.id)}
			<a
				href={resolve('/tools/groups/instance/[instanceId]', { instanceId: instance.id })}
				class="flex items-center justify-between py-2 hover:bg-gray-50"
			>
				<span class="font-medium text-gray-900">{instance.seasonName}</span>
				<span class="text-sm text-gray-500">
					{instance.memberCount} leden &middot; {instance.permissionCount} permissies
				</span>
			</a>
		{:else}
			<p class="py-6 text-sm text-gray-500">Geen instanties voor deze groep.</p>
		{/each}
	</div>
{/if}
