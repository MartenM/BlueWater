<script lang="ts">
	import { untrack } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { HasPermission, BlueAlert, breadcrumbs } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import { BluePermission, BlueUserSex } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	const dateFormatter = new Intl.DateTimeFormat('nl-NL', {
		day: 'numeric',
		month: 'long',
		year: 'numeric'
	});

	const genderLabels: Record<BlueUserSex, string> = {
		[BlueUserSex.Male]: 'Man',
		[BlueUserSex.Female]: 'Vrouw',
		[BlueUserSex.Unknown]: 'Onbekend'
	};

	let user = $state(untrack(() => data.user));
	let error = $state(untrack(() => data.error));
	let deleteError = $state<string | null>(null);
	let deleting = $state(false);

	$effect(() => {
		if (!user) return;
		breadcrumbs.set([{ label: 'Gebruikers', href: '/tools/users' }, { label: user.fullname }]);
		return () => breadcrumbs.clear();
	});

	async function handleDelete() {
		if (!user || !confirm(`Weet je zeker dat je ${user.fullname} wilt verwijderen?`)) return;
		deleting = true;
		deleteError = null;
		try {
			await apiClient.deleteUser(user.id);
			goto(resolve('/tools/users'));
		} catch {
			deleteError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
			deleting = false;
		}
	}
</script>

{#if error}
	<p class="text-sm text-gray-600">Gebruiker kon niet worden geladen.</p>
{:else if user}
	<h1 class="text-2xl font-bold text-gray-900">{user.fullname}</h1>

	<dl class="mt-6 grid grid-cols-2 gap-4 text-sm">
		<div>
			<dt class="text-gray-500">Gebruikersnaam</dt>
			<dd class="text-gray-900">{user.userName}</dd>
		</div>
		<div>
			<dt class="text-gray-500">E-mailadres</dt>
			<dd class="text-gray-900">{user.email}</dd>
		</div>
		<div>
			<dt class="text-gray-500">Telefoonnummer</dt>
			<dd class="text-gray-900">{user.phoneNumber || '-'}</dd>
		</div>
		<div>
			<dt class="text-gray-500">Geboortedatum</dt>
			<dd class="text-gray-900">{dateFormatter.format(user.dateOfBirth)}</dd>
		</div>
		<div>
			<dt class="text-gray-500">Geslacht</dt>
			<dd class="text-gray-900">{genderLabels[user.gender]}</dd>
		</div>
		<div>
			<dt class="text-gray-500">Adres</dt>
			<dd class="text-gray-900">
				{user.address.address}<br />
				{user.address.zip}
				{user.address.city}
			</dd>
		</div>
		<div>
			<dt class="text-gray-500">Noodadres</dt>
			<dd class="text-gray-900">
				{user.emergencyAddress.address}<br />
				{user.emergencyAddress.zip}
				{user.emergencyAddress.city}
			</dd>
		</div>
		<div>
			<dt class="text-gray-500">Noodtelefoonnummer</dt>
			<dd class="text-gray-900">{user.emergencyPhoneNumber}</dd>
		</div>
	</dl>

	<HasPermission permission={BluePermission.AdminUsersModify}>
		<div class="mt-8 flex gap-3 border-t border-gray-200 pt-6">
			<a
				href={resolve('/tools/users/[id]/edit', { id: user.id })}
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
		{#if deleteError}
			<div class="mt-4">
				<BlueAlert level={AlertLevel.Danger}>{deleteError}</BlueAlert>
			</div>
		{/if}
	</HasPermission>
{/if}
