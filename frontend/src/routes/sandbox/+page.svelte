<script lang="ts">
	import { session } from '$lib/auth/session.svelte';
	import { decodeJwt, decodeJwtHeader, getExpiryMs, isExpiringSoon } from '$lib/auth/jwt';
	import { BluePermission } from '$lib/api/apiClient';
	import HasPermission from '$lib/components/HasPermission.svelte';

	let now = $state(Date.now());

	$effect(() => {
		const interval = setInterval(() => (now = Date.now()), 1000);
		return () => clearInterval(interval);
	});

	function tryDecode<T>(fn: () => T): T | null {
		try {
			return fn();
		} catch {
			return null;
		}
	}

	const accessToken = $derived(session.tokens?.accessToken ?? null);
	const refreshToken = $derived(session.tokens?.refreshToken ?? null);

	const accessHeader = $derived(accessToken && tryDecode(() => decodeJwtHeader(accessToken)));
	const accessClaims = $derived(accessToken && tryDecode(() => decodeJwt(accessToken)));
	const accessExpiryMs = $derived(accessToken ? tryDecode(() => getExpiryMs(accessToken)) : null);
	const accessExpiringSoon = $derived(
		accessToken ? tryDecode(() => isExpiringSoon(accessToken)) : null
	);

	const refreshHeader = $derived(refreshToken && tryDecode(() => decodeJwtHeader(refreshToken)));
	const refreshClaims = $derived(refreshToken && tryDecode(() => decodeJwt(refreshToken)));
	const refreshExpiryMs = $derived(
		refreshToken ? tryDecode(() => getExpiryMs(refreshToken)) : null
	);

	function formatTimestamp(ms: number | null): string {
		if (ms === null) return '—';
		return new Date(ms).toLocaleString();
	}

	function formatCountdown(targetMs: number | null): string {
		if (targetMs === null) return '—';
		const diff = targetMs - now;
		const sign = diff < 0 ? '-' : '';
		const totalSeconds = Math.floor(Math.abs(diff) / 1000);
		const minutes = Math.floor(totalSeconds / 60);
		const seconds = totalSeconds % 60;
		return `${sign}${minutes}m ${seconds}s`;
	}

	async function copy(text: string | null) {
		if (text) await navigator.clipboard.writeText(text);
	}
</script>

<svelte:head><title>Sandbox</title></svelte:head>

<div class="mx-auto max-w-5xl space-y-6 px-4 py-10">
	<div>
		<h1 class="text-xl font-semibold">Sandbox</h1>
		<p class="text-sm text-gray-500">
			Dev-only inspector for client auth state. Not available in production builds.
		</p>
	</div>

	<section class="rounded-md border border-gray-200 bg-white p-4">
		<h2 class="text-sm font-semibold text-gray-700">Auth status</h2>
		<dl class="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-sm sm:grid-cols-4">
			<dt class="text-gray-500">Authenticated</dt>
			<dd class="font-mono">{session.tokens ? 'true' : 'false'}</dd>
			<dt class="text-gray-500">Access token expiring soon</dt>
			<dd class="font-mono">{accessExpiringSoon === null ? '—' : String(accessExpiringSoon)}</dd>
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

	<section class="rounded-md border border-gray-200 bg-white p-4">
		<div class="flex items-center justify-between">
			<h2 class="text-sm font-semibold text-gray-700">Access token</h2>
			<button
				type="button"
				class="rounded-md border border-gray-300 px-2 py-1 text-xs text-gray-700 hover:bg-gray-50"
				disabled={!accessToken}
				onclick={() => copy(accessToken)}
			>
				Copy raw
			</button>
		</div>
		{#if accessToken}
			<p class="mt-2 break-all font-mono text-xs text-gray-500">{accessToken}</p>
			<dl class="mt-3 grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
				<dt class="text-gray-500">Expires at</dt>
				<dd class="font-mono">{formatTimestamp(accessExpiryMs)}</dd>
				<dt class="text-gray-500">Expires in</dt>
				<dd class="font-mono">{formatCountdown(accessExpiryMs)}</dd>
			</dl>
			<h3 class="mt-3 text-xs font-semibold text-gray-500">Header</h3>
			<pre class="mt-1 overflow-x-auto rounded-md bg-gray-50 p-3 text-xs">{JSON.stringify(
					accessHeader,
					null,
					2
				)}</pre>
			<h3 class="mt-3 text-xs font-semibold text-gray-500">Claims</h3>
			<pre class="mt-1 overflow-x-auto rounded-md bg-gray-50 p-3 text-xs">{JSON.stringify(
					accessClaims,
					null,
					2
				)}</pre>
		{:else}
			<p class="mt-2 text-sm text-gray-500">No access token in session.</p>
		{/if}
	</section>

	<section class="rounded-md border border-gray-200 bg-white p-4">
		<div class="flex items-center justify-between">
			<h2 class="text-sm font-semibold text-gray-700">Refresh token</h2>
			<button
				type="button"
				class="rounded-md border border-gray-300 px-2 py-1 text-xs text-gray-700 hover:bg-gray-50"
				disabled={!refreshToken}
				onclick={() => copy(refreshToken)}
			>
				Copy raw
			</button>
		</div>
		{#if refreshToken}
			<p class="mt-2 break-all font-mono text-xs text-gray-500">{refreshToken}</p>
			{#if refreshClaims}
				<dl class="mt-3 grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
					<dt class="text-gray-500">Expires at</dt>
					<dd class="font-mono">{formatTimestamp(refreshExpiryMs)}</dd>
				</dl>
				<h3 class="mt-3 text-xs font-semibold text-gray-500">Header</h3>
				<pre class="mt-1 overflow-x-auto rounded-md bg-gray-50 p-3 text-xs">{JSON.stringify(
						refreshHeader,
						null,
						2
					)}</pre>
				<h3 class="mt-3 text-xs font-semibold text-gray-500">Claims</h3>
				<pre class="mt-1 overflow-x-auto rounded-md bg-gray-50 p-3 text-xs">{JSON.stringify(
						refreshClaims,
						null,
						2
					)}</pre>
			{:else}
				<p class="mt-2 text-sm text-gray-500">Not a decodable JWT (opaque token).</p>
			{/if}
		{:else}
			<p class="mt-2 text-sm text-gray-500">No refresh token in session.</p>
		{/if}
	</section>
</div>
