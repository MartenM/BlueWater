<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient } from '$lib/api/client';
	import { ProfileView, BlueAlert, AlertLevel } from '$lib';
	import type { UserProfileDto } from '$lib/api/apiClient';

	let profile = $state<UserProfileDto | null>(null);
	let error = $state(false);

	onMount(async () => {
		try {
			profile = await apiClient.me();
		} catch {
			error = true;
		}
	});
</script>

{#if error}
	<div class="mx-auto max-w-5xl px-4 py-10">
		<BlueAlert level={AlertLevel.Danger}>Profiel kon niet worden geladen.</BlueAlert>
	</div>
{:else if profile}
	<ProfileView {profile} />
{/if}
