<script lang="ts">
	import { untrack } from 'svelte';
	import {
		BlueAddressDto,
		BlueUserSex,
		CreateUserRequest,
		UpdateUserRequest
	} from '$lib/api/apiClient';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { UserDto } from '$lib/api/apiClient';
	import { BlueForm } from '$lib';
	import FormField from './FormField.svelte';

	let {
		user,
		mode,
		submitLabel,
		onSubmit
	}: {
		user?: UserDto;
		mode: 'create' | 'edit';
		submitLabel: string;
		onSubmit: (request: CreateUserRequest | UpdateUserRequest) => Promise<void>;
	} = $props();

	function toDateInputValue(date?: Date): string {
		if (!date) return '';
		return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}-${String(date.getUTCDate()).padStart(2, '0')}`;
	}

	let userName = $state(untrack(() => user?.userName) ?? '');
	let email = $state(untrack(() => user?.email) ?? '');
	let firstname = $state(untrack(() => user?.firstname) ?? '');
	let surnamePrefix = $state(untrack(() => user?.surnamePrefix) ?? '');
	let surname = $state(untrack(() => user?.surname) ?? '');
	let phoneNumber = $state(untrack(() => user?.phoneNumber) ?? '');
	let address = $state(untrack(() => user?.address?.address) ?? '');
	let city = $state(untrack(() => user?.address?.city) ?? '');
	let zip = $state(untrack(() => user?.address?.zip) ?? '');
	let emergencyAddress = $state(untrack(() => user?.emergencyAddress?.address) ?? '');
	let emergencyCity = $state(untrack(() => user?.emergencyAddress?.city) ?? '');
	let emergencyZip = $state(untrack(() => user?.emergencyAddress?.zip) ?? '');
	let emergencyPhoneNumber = $state(untrack(() => user?.emergencyPhoneNumber) ?? '');
	let dateOfBirth = $state(untrack(() => toDateInputValue(user?.dateOfBirth)));
	let gender = $state(untrack(() => user?.gender) ?? BlueUserSex.Unknown);
	const form = new FormState();
</script>

<BlueForm
	{form}
	{submitLabel}
	onsubmit={() => {
		const data = {
			userName,
			email,
			firstname,
			surnamePrefix,
			surname,
			phoneNumber: phoneNumber || undefined,
			address: new BlueAddressDto({ address, city, zip }),
			emergencyAddress: new BlueAddressDto({
				address: emergencyAddress,
				city: emergencyCity,
				zip: emergencyZip
			}),
			emergencyPhoneNumber,
			dateOfBirth: new Date(dateOfBirth),
			gender
		};
		return onSubmit(mode === 'create' ? new CreateUserRequest(data) : new UpdateUserRequest(data));
	}}
>
	<div class="grid grid-cols-2 gap-4">
		<FormField label="Gebruikersnaam" errors={form.errorsFor('userName')}>
			{#snippet children(invalid)}
				<input
					type="text"
					required
					bind:value={userName}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="E-mailadres" errors={form.errorsFor('email')}>
			{#snippet children(invalid)}
				<input
					type="email"
					required
					bind:value={email}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>
	</div>

	<div class="grid grid-cols-3 gap-4">
		<FormField label="Voornaam" errors={form.errorsFor('firstname')}>
			{#snippet children(invalid)}
				<input
					type="text"
					required
					bind:value={firstname}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="Tussenvoegsel" errors={form.errorsFor('surnamePrefix')}>
			{#snippet children(invalid)}
				<input
					type="text"
					bind:value={surnamePrefix}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="Achternaam" errors={form.errorsFor('surname')}>
			{#snippet children(invalid)}
				<input
					type="text"
					required
					bind:value={surname}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>
	</div>

	<div class="grid grid-cols-2 gap-4">
		<FormField label="Telefoonnummer" errors={form.errorsFor('phoneNumber')}>
			{#snippet children(invalid)}
				<input
					type="tel"
					bind:value={phoneNumber}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="Geboortedatum" errors={form.errorsFor('dateOfBirth')}>
			{#snippet children(invalid)}
				<input
					type="date"
					required
					bind:value={dateOfBirth}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>
	</div>

	<FormField label="Geslacht" errors={form.errorsFor('gender')}>
		{#snippet children(invalid)}
			<select
				bind:value={gender}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			>
				<option value={BlueUserSex.Male}>Man</option>
				<option value={BlueUserSex.Female}>Vrouw</option>
				<option value={BlueUserSex.Unknown}>Onbekend</option>
			</select>
		{/snippet}
	</FormField>

	<fieldset class="rounded-md border border-gray-200 p-4">
		<legend class="px-1 text-sm font-semibold text-gray-700">Adres</legend>
		<div class="grid grid-cols-3 gap-4">
			<FormField label="Straat en huisnummer" errors={form.errorsFor('address.address')}>
				{#snippet children(invalid)}
					<input
						type="text"
						required
						bind:value={address}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>

			<FormField label="Plaats" errors={form.errorsFor('address.city')}>
				{#snippet children(invalid)}
					<input
						type="text"
						required
						bind:value={city}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>

			<FormField label="Postcode" errors={form.errorsFor('address.zip')}>
				{#snippet children(invalid)}
					<input
						type="text"
						required
						bind:value={zip}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>
		</div>
	</fieldset>

	<fieldset class="rounded-md border border-gray-200 p-4">
		<legend class="px-1 text-sm font-semibold text-gray-700">Noodadres</legend>
		<div class="grid grid-cols-3 gap-4">
			<FormField label="Straat en huisnummer" errors={form.errorsFor('emergencyAddress.address')}>
				{#snippet children(invalid)}
					<input
						type="text"
						required
						bind:value={emergencyAddress}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>

			<FormField label="Plaats" errors={form.errorsFor('emergencyAddress.city')}>
				{#snippet children(invalid)}
					<input
						type="text"
						required
						bind:value={emergencyCity}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>

			<FormField label="Postcode" errors={form.errorsFor('emergencyAddress.zip')}>
				{#snippet children(invalid)}
					<input
						type="text"
						required
						bind:value={emergencyZip}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>
		</div>

		<div class="mt-4">
			<FormField label="Noodtelefoonnummer" errors={form.errorsFor('emergencyPhoneNumber')}>
				{#snippet children(invalid)}
					<input
						type="tel"
						required
						bind:value={emergencyPhoneNumber}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>
		</div>
	</fieldset>
</BlueForm>
