<script lang="ts">
	import { OutingChangelogType, type OutingChangelogEntryDto } from '$lib/api/apiClient';

	let { entries }: { entries: OutingChangelogEntryDto[] } = $props();

	function parseFields(fields: string): Record<string, unknown> {
		try {
			return JSON.parse(fields);
		} catch {
			return {};
		}
	}

	function formatDate(d: Date): string {
		return d.toLocaleString('nl-NL', { dateStyle: 'medium', timeStyle: 'short' });
	}

	function describe(entry: OutingChangelogEntryDto): string {
		const f = parseFields(entry.fields);
		const actor = entry.actorFullname || 'Iemand';

		switch (entry.type) {
			case OutingChangelogType.FirstSignUp:
				return `${actor} heeft ${f.userFullname ?? 'een lid'} aangemeld als ${f.appliedRole ?? f.requestedRole}${f.reason === 'capacity' ? ' (roeiplaatsen vol)' : f.reason === 'not_coxed' ? ' (boot heeft geen stuurplaats)' : ''}.`;
			case OutingChangelogType.RoleChanged:
				return `${actor} heeft de rol van ${f.userFullname ?? 'een lid'} gewijzigd van ${f.from} naar ${f.appliedRole ?? f.requestedRole}${f.reason === 'capacity' ? ' (roeiplaatsen vol)' : f.reason === 'not_coxed' ? ' (boot heeft geen stuurplaats)' : ''}.`;
			case OutingChangelogType.Invited:
				return `${actor} heeft ${f.userFullname ?? 'iemand'} uitgenodigd voor deze outing.`;
			case OutingChangelogType.DateChanged:
				return `${actor} heeft de datum gewijzigd.`;
			case OutingChangelogType.BoatChanged:
				return `${actor} heeft de boot gewijzigd naar ${f.toBoatName ?? '—'}.`;
			case OutingChangelogType.BoatTypeChanged:
				return `${actor} heeft het boottype gewijzigd naar ${f.toBoatTypeName ?? '—'}.`;
			case OutingChangelogType.Confirmed:
				return `${actor} heeft deze outing bevestigd.`;
			default:
				return `${actor} heeft een wijziging aangebracht.`;
		}
	}
</script>

<div>
	<h2 class="text-sm font-semibold text-gray-700">Geschiedenis</h2>
	{#if entries.length === 0}
		<p class="mt-2 text-sm text-gray-500">Nog geen wijzigingen.</p>
	{:else}
		<ul class="mt-2 space-y-2 border-t border-gray-200 pt-2">
			{#each entries as entry (entry.id)}
				<li class="text-sm text-gray-700">
					<span class="text-gray-400">{formatDate(entry.createdAt)}</span>
					— {describe(entry)}
				</li>
			{/each}
		</ul>
	{/if}
</div>
