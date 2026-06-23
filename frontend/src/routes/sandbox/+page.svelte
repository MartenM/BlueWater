<script lang="ts">
	import { session } from '$lib/auth/session.svelte';
	import { BluePermission } from '$lib/api/apiClient';
	import HasPermission from '$lib/components/HasPermission.svelte';
</script>

<svelte:head><title>Sandbox</title></svelte:head>

<div class="mx-auto max-w-5xl space-y-6 px-4 py-10">
	<div>
		<h1 class="text-xl font-semibold">Sandbox</h1>
		<p class="text-sm text-gray-500">
			Dev-only inspector for client auth state. Not available in production builds. Access/refresh
			tokens now live in HttpOnly cookies, so they're not inspectable from here anymore.
		</p>
	</div>

	<section class="rounded-md border border-gray-200 bg-white p-4">
		<h2 class="text-sm font-semibold text-gray-700">Auth status</h2>
		<dl class="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-sm sm:grid-cols-4">
			<dt class="text-gray-500">Authenticated</dt>
			<dd class="font-mono">{session.user ? 'true' : 'false'}</dd>
			<dt class="text-gray-500">User id</dt>
			<dd class="font-mono">{session.user?.id ?? '—'}</dd>
			<dt class="text-gray-500">Email</dt>
			<dd class="font-mono">{session.user?.email ?? '—'}</dd>
			<dt class="text-gray-500">Permissions</dt>
			<dd class="col-span-3 font-mono">{session.user?.permissions.join(', ') || '—'}</dd>
		</dl>
		<button
			type="button"
			class="mt-3 rounded-md border border-gray-300 px-3 py-1 text-sm text-gray-700 hover:bg-gray-50"
			onclick={() => session.clear()}
		>
			Clear session
		</button>
	</section>

	<section class="rounded-md border border-gray-200 bg-white p-4">
		<h2 class="text-sm font-semibold text-gray-700">HasPermission</h2>
		<ul class="mt-2 space-y-1 text-sm">
			{#each Object.values(BluePermission) as permission (permission)}
				<li class="flex items-center gap-2">
					<span class="w-48 font-mono text-xs text-gray-500">{permission}</span>
					<HasPermission {permission}>
						<span class="font-mono text-green-600">authorized</span>
						{#snippet fallback()}
							<span class="font-mono text-gray-400">unauthorized</span>
						{/snippet}
					</HasPermission>
				</li>
			{/each}
		</ul>
	</section>
</div>
