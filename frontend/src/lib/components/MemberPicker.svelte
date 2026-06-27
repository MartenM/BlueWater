<script lang="ts">
	import { apiClient } from '$lib/api/client';
	import type { ActiveMemberDto } from '$lib/api/apiClient';
	import ProfilePicture from './ProfilePicture.svelte';

	let {
		value = $bindable<string | null>(null),
		invalid = false,
		disabled = false,
		placeholder = 'Zoek een lid...'
	}: {
		value?: string | null;
		invalid?: boolean;
		disabled?: boolean;
		placeholder?: string;
	} = $props();

	let search = $state('');
	let results = $state<ActiveMemberDto[]>([]);
	let searching = $state(false);
	let open = $state(false);
	let selectedName = $state<string | null>(null);
	let container = $state<HTMLDivElement>();
	let debounceTimer: ReturnType<typeof setTimeout>;

	function handleInput() {
		clearTimeout(debounceTimer);
		const term = search.trim();
		if (!term) {
			results = [];
			open = false;
			return;
		}
		searching = true;
		debounceTimer = setTimeout(async () => {
			try {
				results = await apiClient.active(term);
				open = results.length > 0;
			} catch {
				results = [];
				open = false;
			} finally {
				searching = false;
			}
		}, 200);
	}

	function select(member: ActiveMemberDto) {
		value = member.id;
		selectedName = member.fullname;
		search = '';
		results = [];
		open = false;
	}

	function clear() {
		value = null;
		selectedName = null;
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			open = false;
			results = [];
		}
	}

	function handleBlur(e: FocusEvent) {
		// Close only when focus moves outside this component
		if (container && !container.contains(e.relatedTarget as Node | null)) {
			open = false;
		}
	}
</script>

<div bind:this={container} class="relative" onblur={handleBlur}>
	{#if value && selectedName}
		<div
			class="flex items-center gap-2 rounded-md border px-3 py-2 text-sm {invalid
				? 'border-red-400'
				: 'border-gray-300'} bg-white"
		>
			<ProfilePicture
				load={() => apiClient.getProfilePicture(value!)}
				class="h-10 w-[30px] shrink-0 rounded object-cover"
			/>
			<span class="flex-1 text-gray-900">{selectedName}</span>
			{#if !disabled}
				<button
					type="button"
					onclick={clear}
					class="text-gray-400 hover:text-gray-600"
					aria-label="Selectie wissen"
				>
					<svg viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4">
						<path
							d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z"
						/>
					</svg>
				</button>
			{/if}
		</div>
	{:else}
		<div class="relative">
			<input
				type="search"
				{placeholder}
				{disabled}
				bind:value={search}
				oninput={handleInput}
				onkeydown={handleKeydown}
				class="w-full rounded-md border py-2 pr-8 pl-3 text-sm focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'} disabled:bg-gray-50 disabled:text-gray-500"
			/>
			{#if searching}
				<div class="pointer-events-none absolute inset-y-0 right-2 flex items-center">
					<svg class="h-4 w-4 animate-spin text-gray-400" fill="none" viewBox="0 0 24 24">
						<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"
						></circle>
						<path
							class="opacity-75"
							fill="currentColor"
							d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
						></path>
					</svg>
				</div>
			{/if}
		</div>

		{#if open && results.length > 0}
			<ul
				class="absolute z-10 mt-1 max-h-64 w-full overflow-auto rounded-md border border-gray-200 bg-white py-1 shadow-lg"
			>
				{#each results as member (member.id)}
					<li>
						<button
							type="button"
							onclick={() => select(member)}
							class="flex w-full items-center gap-3 px-3 py-2 text-left text-sm hover:bg-gray-50 focus:bg-gray-50 focus:outline-none"
						>
							{#if member.hasProfilePicture}
								<ProfilePicture
									load={() => apiClient.getProfilePicture(member.id)}
									class="h-10 w-[30px] shrink-0 rounded object-cover"
								/>
							{:else}
								<div
									class="flex h-10 w-[30px] shrink-0 items-center justify-center rounded bg-gray-100 text-gray-400"
								>
									<svg viewBox="0 0 24 24" fill="currentColor" class="h-5 w-5">
										<path
											d="M12 12c2.7 0 4.875-2.175 4.875-4.875S14.7 2.25 12 2.25 7.125 4.425 7.125 7.125 9.3 12 12 12Zm0 2.25c-3.45 0-9 1.5-9 5.25v1.5h18v-1.5c0-3.75-5.55-5.25-9-5.25Z"
										/>
									</svg>
								</div>
							{/if}
							<span class="text-gray-900">{member.fullname}</span>
						</button>
					</li>
				{/each}
			</ul>
		{/if}
	{/if}
</div>
