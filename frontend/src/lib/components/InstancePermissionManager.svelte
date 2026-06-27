<script lang="ts">
	import { resolve } from '$app/paths';
	import type { UserGroupPermissionDto } from '$lib/api/apiClient';

	let { userGroupId, permissions }: { userGroupId: string; permissions: UserGroupPermissionDto[] } =
		$props();
</script>

<div>
	<div class="flex items-center justify-between">
		<h2 class="text-sm font-semibold text-gray-700">Permissies</h2>
		<a
			href={resolve('/tools/groups/group/[groupId]', { groupId: userGroupId })}
			class="text-sm font-medium text-primary-hover hover:underline"
		>
			Bewerken
		</a>
	</div>

	<div class="mt-2 flex flex-wrap gap-2">
		{#each permissions as p (p.permission + (p.userGroupCategoryRoleId ?? ''))}
			<span
				class="inline-flex items-center gap-1.5 rounded-md border border-gray-200 bg-gray-50 px-2 py-1 text-sm text-gray-900"
			>
				{p.permission}
				{#if p.userGroupCategoryRoleId}
					<span class="text-xs text-gray-500">(rol)</span>
				{/if}
			</span>
		{:else}
			<p class="text-sm text-gray-500">Geen permissies.</p>
		{/each}
	</div>
</div>
