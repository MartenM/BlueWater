<script lang="ts">
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { UserForm, breadcrumbs } from '$lib';
	import { CreateUserRequest } from '$lib/api/apiClient';
	import type { CreateUserResponse, UpdateUserRequest } from '$lib/api/apiClient';

	let createdResult = $state<CreateUserResponse | null>(null);

	async function handleCreate(request: CreateUserRequest | UpdateUserRequest) {
		if (!(request instanceof CreateUserRequest)) return;
		createdResult = await apiClient.createUser(request);
	}

	$effect(() => {
		breadcrumbs.set([{ label: 'Gebruikers', href: '/tools/users' }, { label: 'Nieuwe gebruiker' }]);
		return () => breadcrumbs.clear();
	});
</script>

<h1 class="text-2xl font-bold text-gray-900">Nieuwe gebruiker</h1>
<div class="mt-6">
	{#if createdResult}
		<div class="rounded-md border border-gray-200 bg-white p-6 shadow-sm">
			<p class="text-sm text-gray-700">
				Gebruiker <span class="font-medium">{createdResult.user.fullname}</span> is aangemaakt.
			</p>
			<p class="mt-3 text-sm text-gray-700">
				Tijdelijk wachtwoord (eenmalig getoond, deel dit met de gebruiker):
			</p>
			<p class="mt-1 rounded-md bg-gray-100 px-3 py-2 font-mono text-sm text-gray-900">
				{createdResult.generatedPassword}
			</p>
			<a
				href={resolve('/tools/users/[id]', { id: createdResult.user.id })}
				class="mt-4 inline-block text-sm font-medium text-primary-hover hover:underline"
			>
				Naar gebruiker
			</a>
		</div>
	{:else}
		<UserForm mode="create" submitLabel="Aanmaken" onSubmit={handleCreate} />
	{/if}
</div>
