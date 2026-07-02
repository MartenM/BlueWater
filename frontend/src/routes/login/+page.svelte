<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { login } from '$lib/auth/auth';
	import { ApiException } from '$lib/api/apiClient';

	let email = $state('');
	let password = $state('');
	let error = $state<string | null>(null);
	let submitting = $state(false);

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		error = null;
		submitting = true;
		try {
			await login(email, password);
			const redirectTo = page.url.searchParams.get('redirectTo');
			// Only redirect to an internal path - never let an attacker-supplied
			// redirectTo send the user to an external site after login.
			const target = redirectTo?.startsWith('/') && !redirectTo.startsWith('//') ? redirectTo : '/';
			goto(target);
		} catch (e) {
			error =
				e instanceof ApiException && (e.status === 401 || e.status === 400)
					? 'Onjuist e-mailadres of wachtwoord.'
					: 'Er ging iets mis. Probeer het later opnieuw.';
		} finally {
			submitting = false;
		}
	}
</script>

<div class="mx-auto flex max-w-md flex-col px-4 py-16 sm:px-6 lg:px-8">
	<h1 class="text-2xl font-bold text-gray-900">Inloggen</h1>

	<form class="mt-8 flex flex-col gap-4" onsubmit={handleSubmit}>
		<label class="flex flex-col gap-1">
			<span class="text-sm font-medium text-gray-700">E-mailadres</span>
			<input
				type="email"
				autocomplete="email"
				required
				bind:value={email}
				class="rounded-md border-gray-300 focus:border-primary focus:ring-primary"
			/>
		</label>

		<label class="flex flex-col gap-1">
			<span class="text-sm font-medium text-gray-700">Wachtwoord</span>
			<input
				type="password"
				autocomplete="current-password"
				required
				bind:value={password}
				class="rounded-md border-gray-300 focus:border-primary focus:ring-primary"
			/>
		</label>

		{#if error}
			<p class="text-sm text-red-600">{error}</p>
		{/if}

		<button
			type="submit"
			disabled={submitting}
			class="mt-2 rounded-md bg-primary px-4 py-2 font-medium text-primary-content hover:bg-primary-hover disabled:opacity-60"
		>
			{submitting ? 'Bezig met inloggen…' : 'Inloggen'}
		</button>
	</form>
</div>
