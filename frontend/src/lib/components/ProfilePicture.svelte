<script lang="ts">
	import { apiClient } from '$lib/api/client';

	let {
		userId,
		fetch: fetchPicture = (id: string) => apiClient.getProfilePicture(id),
		version = 0,
		alt = 'Profielfoto',
		class: className = 'h-[100px] shrink-0 rounded-lg object-cover'
	}: {
		userId: string;
		fetch?: (userId: string) => Promise<{ data: Blob }>;
		version?: number;
		alt?: string;
		class?: string;
	} = $props();

	let url = $state<string | null>(null);

	$effect(() => {
		void version;
		void userId;
		let active = true;
		let createdUrl: string | null = null;
		url = null;

		(async () => {
			try {
				const content = await fetchPicture(userId);
				if (!active) return;
				createdUrl = URL.createObjectURL(content.data);
				url = createdUrl;
			} catch {
				// No picture set; the placeholder stays visible.
			}
		})();

		return () => {
			active = false;
			if (createdUrl) URL.revokeObjectURL(createdUrl);
		};
	});
</script>

{#if url}
	<img src={url} {alt} class="aspect-[75/100] {className}" />
{:else}
	<div
		class="flex aspect-[75/100] items-center justify-center bg-gray-100 text-gray-400 {className}"
	>
		<svg viewBox="0 0 24 24" fill="currentColor" class="h-1/2 w-1/2">
			<path
				d="M12 12c2.7 0 4.875-2.175 4.875-4.875S14.7 2.25 12 2.25 7.125 4.425 7.125 7.125 9.3 12 12 12Zm0 2.25c-3.45 0-9 1.5-9 5.25v1.5h18v-1.5c0-3.75-5.55-5.25-9-5.25Z"
			/>
		</svg>
	</div>
{/if}
