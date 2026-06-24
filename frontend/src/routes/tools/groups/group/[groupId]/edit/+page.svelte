<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { GroupForm, Spinner, breadcrumbs } from '$lib';
	import type { UpsertUserGroupRequest, UserGroupDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let group = $state<UserGroupDto | null>(null);
	let loading = $state(true);
	let error = $state(false);

	onMount(async () => {
		try {
			group = await apiClient.userGroupsGET(params.groupId);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	async function handleEdit(request: UpsertUserGroupRequest) {
		await apiClient.userGroupsPUT(params.groupId, request);
		goto(resolve('/tools/groups/group/[groupId]', { groupId: params.groupId }));
	}

	$effect(() => {
		breadcrumbs.set([{ label: 'Groepen', href: '/tools/groups' }, { label: 'Groep bewerken' }]);
		return () => breadcrumbs.clear();
	});
</script>

<h1 class="text-2xl font-bold text-gray-900">Groep bewerken</h1>
<div class="mt-6 max-w-md">
	{#if loading}
		<Spinner />
	{:else if error}
		<p class="text-sm text-gray-600">Groep kon niet worden geladen.</p>
	{:else if group}
		<GroupForm
			{group}
			categoryId={group.userGroupCategoryId}
			categoryName={group.userGroupCategoryName}
			submitLabel="Opslaan"
			onSubmit={handleEdit}
		/>
	{/if}
</div>
