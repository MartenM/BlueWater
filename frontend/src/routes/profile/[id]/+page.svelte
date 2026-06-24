<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient } from '$lib/api/client';
	import { ProfileView, breadcrumbs } from '$lib';
	import type { UserProfileDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let profile = $state<UserProfileDto | null>(null);
	let error = $state(false);

	onMount(async () => {
		try {
			profile = await apiClient.userProfiles(params.id);
		} catch {
			error = true;
		}
	});

	$effect(() => {
		if (!profile) return;
		breadcrumbs.set([{ label: profile.fullname }]);
		return () => breadcrumbs.clear();
	});
</script>

{#if error}
	<p class="mx-auto max-w-5xl px-4 py-10 text-sm text-red-600">Profiel kon niet worden geladen.</p>
{:else if profile}
	<ProfileView {profile} />
{/if}
