<script lang="ts">
	import { apiClient } from '$lib/api/client';
	import type { UserProfileDto } from '$lib/api/apiClient';
	import ProfilePicture from './ProfilePicture.svelte';

	interface GroupRow {
		groupId: string;
		category: string;
		groupName: string;
		showCategory: boolean;
	}

	interface SeasonSection {
		seasonDisplayName: string;
		rows: GroupRow[];
	}

	let { profile }: { profile: UserProfileDto } = $props();

	const seasons = $derived.by<SeasonSection[]>(() => {
		const sections: SeasonSection[] = [];
		const bySeason: Record<string, SeasonSection> = {};
		for (const group of profile.groups) {
			let section = bySeason[group.seasonDisplayName];
			if (!section) {
				section = { seasonDisplayName: group.seasonDisplayName, rows: [] };
				bySeason[group.seasonDisplayName] = section;
				sections.push(section);
			}
			const previous = section.rows.at(-1);
			section.rows.push({
				groupId: group.groupId,
				category: group.groupCategoryName,
				groupName: group.groupName,
				showCategory: previous?.category !== group.groupCategoryName
			});
		}
		return sections;
	});

	// Placeholder until the API exposes a real email field for the profile.
	const mockEmail = $derived(`${profile.firstname}.${profile.surname}@example.org`.toLowerCase());
</script>

<div class="mx-auto grid max-w-5xl gap-8 px-4 py-10 sm:px-6 lg:grid-cols-2 lg:px-8">
	<section class="h-fit rounded-md border border-gray-200 bg-white p-6 shadow-sm">
		<div class="flex items-start gap-4">
			<ProfilePicture load={() => apiClient.getMyProfilePicture()} />
			<div>
				<h1 class="text-xl font-bold text-gray-900">{profile.fullname}</h1>
				<dl class="mt-4 space-y-3 text-sm">
					<div>
						<dt class="text-gray-500">E-mailadres</dt>
						<dd class="text-gray-900">{mockEmail}</dd>
					</div>
				</dl>
			</div>
		</div>
	</section>

	<section>
		<h2 class="text-lg font-semibold text-gray-900">Historie</h2>
		{#if seasons.length === 0}
			<p class="mt-2 text-sm text-gray-500">Geen groepen gevonden.</p>
		{:else}
			<div class="mt-4 space-y-8">
				{#each seasons as season (season.seasonDisplayName)}
					<div>
						<h3 class="text-sm font-semibold tracking-wide text-primary-hover uppercase">
							{season.seasonDisplayName}
						</h3>
						<table class="mt-2 w-full text-sm">
							<tbody>
								{#each season.rows as row (row.groupId)}
									<tr class="border-b border-gray-100 last:border-0">
										<td class="w-1/3 py-2 pr-4 align-top text-black font-medium">
											{row.showCategory ? row.category : ''}
										</td>
										<td class="py-2 text-gray-900">{row.groupName}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/each}
			</div>
		{/if}
	</section>
</div>
