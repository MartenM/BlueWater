<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import { apiClient } from '$lib/api/client';
	import { session } from '$lib/auth/session.svelte';
	import { ApiException, BluePermission } from '$lib/api/apiClient';
	import type { NewsIconDto } from '$lib/api/apiClient';
	import { AlertLevel } from '$lib/alert';
	import BlueAlert from './BlueAlert.svelte';

	let { iconId = $bindable() }: { iconId: string | undefined } = $props();

	let icons = $state<NewsIconDto[]>([]);
	let iconUrls = $state<Record<string, string>>({});
	let loading = $state(true);
	let loadError = $state(false);

	let uploadName = $state('');
	let uploadFile = $state<File | null>(null);
	let uploading = $state(false);
	let uploadError = $state<string | null>(null);

	const canUpload = $derived(session.hasPermission(BluePermission.NewsIconCreate));

	onMount(async () => {
		try {
			icons = await apiClient.newsIconsAll();
			await Promise.all(icons.map(loadThumbnail));
		} catch {
			loadError = true;
		} finally {
			loading = false;
		}
	});

	onDestroy(() => {
		for (const url of Object.values(iconUrls)) URL.revokeObjectURL(url);
	});

	async function loadThumbnail(icon: NewsIconDto) {
		try {
			const content = await apiClient.content(icon.id);
			iconUrls[icon.id] = URL.createObjectURL(content.data);
		} catch {
			// Thumbnail failed to load; the icon stays selectable without a preview.
		}
	}

	function handleFileChange(event: Event) {
		uploadFile = (event.currentTarget as HTMLInputElement).files?.[0] ?? null;
	}

	async function handleUpload(event: SubmitEvent) {
		event.preventDefault();
		if (!uploadFile) return;
		uploading = true;
		uploadError = null;
		try {
			const icon = await apiClient.newsIconsPOST(uploadName, {
				data: uploadFile,
				fileName: uploadFile.name
			});
			icons = [...icons, icon];
			await loadThumbnail(icon);
			iconId = icon.id;
			uploadName = '';
			uploadFile = null;
		} catch (e) {
			uploadError =
				e instanceof ApiException && e.result?.errors
					? Object.values(e.result.errors as Record<string, string[]>)
							.flat()
							.join(' ')
					: 'Uploaden is mislukt. Probeer het later opnieuw.';
		} finally {
			uploading = false;
		}
	}
</script>

<div class="flex flex-col gap-3">
	<span class="text-sm font-medium text-gray-700">Icoon</span>

	{#if loading}
		<p class="text-sm text-gray-500">Icoonlijst laden…</p>
	{:else if loadError}
		<p class="text-sm text-gray-500">Icoonlijst kon niet worden geladen.</p>
	{:else}
		<div class="flex flex-wrap gap-3">
			<button
				type="button"
				onclick={() => (iconId = undefined)}
				class="flex h-16 w-16 items-center justify-center rounded-md border-2 text-xs text-gray-500 {iconId ===
				undefined
					? 'border-primary'
					: 'border-gray-200'}"
			>
				Geen
			</button>
			{#each icons as icon (icon.id)}
				<button
					type="button"
					onclick={() => (iconId = icon.id)}
					title={icon.name}
					class="h-16 w-16 overflow-hidden rounded-md border-2 {iconId === icon.id
						? 'border-primary'
						: 'border-gray-200'}"
				>
					{#if iconUrls[icon.id]}
						<img src={iconUrls[icon.id]} alt={icon.name} class="h-full w-full object-cover" />
					{/if}
				</button>
			{/each}
		</div>
	{/if}

	{#if canUpload}
		<form
			class="flex flex-wrap items-end gap-2 border-t border-gray-200 pt-3"
			onsubmit={handleUpload}
		>
			<label class="flex flex-col gap-1">
				<span class="text-xs font-medium text-gray-700">Naam</span>
				<input
					type="text"
					required
					bind:value={uploadName}
					class="rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
				/>
			</label>
			<label class="flex flex-col gap-1">
				<span class="text-xs font-medium text-gray-700">Nieuw icoon (PNG, 100x100)</span>
				<input
					type="file"
					accept="image/png"
					required
					onchange={handleFileChange}
					class="text-sm"
				/>
			</label>
			<button
				type="submit"
				disabled={uploading || !uploadFile}
				class="rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-content hover:bg-primary-hover disabled:opacity-60"
			>
				Uploaden
			</button>
		</form>
		{#if uploadError}
			<BlueAlert level={AlertLevel.Danger}>{uploadError}</BlueAlert>
		{/if}
	{/if}
</div>
