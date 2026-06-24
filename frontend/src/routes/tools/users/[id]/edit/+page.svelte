<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { UserForm, breadcrumbs } from '$lib';
	import { UpdateUserRequest } from '$lib/api/apiClient';
	import type { CreateUserRequest, UserDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let user = $state<UserDto | null>(null);
	let error = $state(false);

	onMount(async () => {
		try {
			user = await apiClient.getUser(params.id);
		} catch {
			error = true;
		}
	});

	async function handleEdit(request: CreateUserRequest | UpdateUserRequest) {
		if (!(request instanceof UpdateUserRequest)) return;
		await apiClient.updateUser(params.id, request);
		goto(resolve('/tools/users/[id]', { id: params.id }));
	}

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Gebruikers', href: '/tools/users' },
			{ label: 'Gebruiker bewerken' }
		]);
		return () => breadcrumbs.clear();
	});
</script>

<h1 class="text-2xl font-bold text-gray-900">Gebruiker bewerken</h1>
<div class="mt-6">
	{#if error}
		<p class="text-sm text-gray-600">Gebruiker kon niet worden geladen.</p>
	{:else if user}
		<UserForm {user} mode="edit" submitLabel="Opslaan" onSubmit={handleEdit} />
	{/if}
</div>
