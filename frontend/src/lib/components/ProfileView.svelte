<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient } from '$lib/api/client';
	import type { UserProfileDto, UserExamDto } from '$lib/api/apiClient';
	import ProfilePicture from './ProfilePicture.svelte';

	interface GroupRow {
		groupId: string;
		category: string;
		groupName: string;
		roleName: string | undefined;
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
				roleName: group.roleName,
				showCategory: previous?.category !== group.groupCategoryName
			});
		}
		return sections;
	});

	// Placeholder until the API exposes a real email field for the profile.
	const mockEmail = $derived(`${profile.firstname}.${profile.surname}@example.org`.toLowerCase());

	let exams = $state<UserExamDto[]>([]);

	const examDateFormatter = new Intl.DateTimeFormat('nl-NL', {
		day: 'numeric',
		month: 'long',
		year: 'numeric'
	});

	onMount(async () => {
		try {
			exams = await apiClient.byUser(profile.id);
		} catch {
			// silently ignore — exams section just stays empty
		}
	});
</script>

<div class="mx-auto grid max-w-5xl gap-8 px-4 py-10 sm:px-6 lg:grid-cols-2 lg:px-8">
	<div class="flex flex-col gap-6">
		<section class="h-fit rounded-md border border-gray-200 bg-white p-6 shadow-sm">
			<div class="flex items-start gap-4">
				<ProfilePicture userId={profile.id} fetch={() => apiClient.getMyProfilePicture()} />
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

		{#if exams.length > 0}
			<section>
				<h2 class="text-base font-semibold text-gray-900">Examens</h2>
				<table class="mt-2 w-full text-sm">
					<tbody>
						{#each exams as exam (exam.id)}
							<tr class="border-b border-gray-100 last:border-0">
								<td class="py-1 pr-4 font-medium text-gray-900">{exam.examTypeName}</td>
								<td class="py-1 text-gray-500">{examDateFormatter.format(exam.obtainedAt)}</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</section>
		{/if}
	</div>

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
										<td class="w-full py-2 text-gray-900">{row.groupName}</td>
										<td class="py-2 pl-4 text-right whitespace-nowrap text-gray-500">
											{row.roleName ?? ''}
										</td>
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
