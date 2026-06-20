<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient } from '$lib/api/client';
	import { requireAuth } from '$lib/auth/requireAuth';
	import { ProfileView } from '$lib';
	import type { UserProfileDto } from '$lib/api/apiClient';

	let profile = $state<UserProfileDto | null>(null);
	let error = $state(false);

	$effect(requireAuth);

	onMount(async () => {
		try {
			profile = await apiClient.me();
		} catch {
			error = true;
		}
	});
</script>

{#if error}
	<p class="mx-auto max-w-5xl px-4 py-10 text-sm text-red-600">Profiel kon niet worden geladen.</p>
{:else if profile}
	<ProfileView {profile} />
{/if}
