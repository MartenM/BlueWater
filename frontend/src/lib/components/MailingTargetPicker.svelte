<script lang="ts">
	import ClusterPicker from './ClusterPicker.svelte';
	import type { MemberClusterDto, UserGroupInstanceDto } from '$lib/api/apiClient';

	let {
		clusters,
		groupInstances,
		selectedClusterIds = $bindable(),
		selectedGroupInstanceIds = $bindable()
	}: {
		clusters: MemberClusterDto[];
		groupInstances: UserGroupInstanceDto[];
		selectedClusterIds: string[];
		selectedGroupInstanceIds: string[];
	} = $props();

	const groupInstanceOptions = $derived(
		groupInstances.map((i) => ({ id: i.id, name: `${i.userGroupName} (${i.seasonName})` }))
	);
</script>

<div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
	<label class="flex flex-col gap-1">
		<span class="text-sm font-medium text-gray-700">Clusters</span>
		<ClusterPicker {clusters} bind:selectedIds={selectedClusterIds} />
	</label>

	<label class="flex flex-col gap-1">
		<span class="text-sm font-medium text-gray-700">Groepen (dit seizoen)</span>
		<ClusterPicker clusters={groupInstanceOptions} bind:selectedIds={selectedGroupInstanceIds} />
	</label>
</div>
