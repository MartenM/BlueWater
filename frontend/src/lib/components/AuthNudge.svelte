<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { logout } from '$lib/auth/auth';
	import { session } from '$lib/auth/session.svelte';
	import { Button, Modal } from '$lib';

	let dialog: HTMLDialogElement | undefined = $state();

	async function handleLogout() {
		dialog?.close();
		await logout();
		goto(resolve('/'));
	}
</script>

<div class="absolute top-0 right-4 z-10 sm:right-6 lg:right-8">
	{#if session.user}
		<button
			type="button"
			onclick={() => dialog?.showModal()}
			class="rounded-b-md border border-t-0 border-gray-200 bg-white px-3 py-1 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-100 hover:text-primary-hover"
		>
			{session.user.email}
		</button>
		<Modal bind:dialog>
			<div class="p-6">
				<h2 class="text-lg font-medium text-black">{session.user.email}</h2>
				<div class="mt-4 flex justify-end gap-2">
					<Button variant="secondary" href={resolve('/profile')} onclick={() => dialog?.close()}>
						Mijn profiel
					</Button>
					<Button variant="secondary" onclick={handleLogout}>Uitloggen</Button>
				</div>
			</div>
		</Modal>
	{:else}
		<a
			href={resolve('/login')}
			class="block rounded-b-md border border-t-0 border-gray-200 bg-white px-3 py-1 text-sm font-medium text-black shadow-sm hover:bg-gray-100 hover:text-primary-hover"
		>
			Inloggen
		</a>
	{/if}
</div>
