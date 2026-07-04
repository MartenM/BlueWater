<script lang="ts">
	import { apiClient } from '$lib/api/client';
	import { OutingParticipantRole, SetParticipantRoleRequest } from '$lib/api/apiClient';
	import type { ActiveMemberDto, OutingDetailDto, OutingParticipantDto } from '$lib/api/apiClient';
	import { session } from '$lib/auth/session.svelte';
	import BlueAlert from './BlueAlert.svelte';
	import ProfilePicture from './ProfilePicture.svelte';
	import Modal from './common/Modal.svelte';
	import ConfirmDialog from './common/ConfirmDialog.svelte';
	import { AlertLevel } from '$lib/alert';
	import { Plus } from '@lucide/svelte';

	let {
		outing,
		onChanged
	}: {
		outing: OutingDetailDto;
		onChanged: () => void;
	} = $props();

	const roleLabels: Record<OutingParticipantRole, string> = {
		[OutingParticipantRole.None]: 'Niet ingedeeld',
		[OutingParticipantRole.Rower]: 'Roeier',
		[OutingParticipantRole.Cox]: 'Stuurman/-vrouw',
		[OutingParticipantRole.Coach]: 'Coach',
		[OutingParticipantRole.Reserve]: 'Reserve',
		[OutingParticipantRole.Unavailable]: 'Niet beschikbaar'
	};

	const roleRowOrder: OutingParticipantRole[] = [
		OutingParticipantRole.Rower,
		OutingParticipantRole.Cox,
		OutingParticipantRole.Coach,
		OutingParticipantRole.Reserve,
		OutingParticipantRole.Unavailable,
		OutingParticipantRole.None
	];

	let busy = $state(false);
	let actionError = $state<string | null>(null);

	let removeDialog = $state<HTMLDialogElement>();
	let removeTarget = $state<{ userId: string; fullname: string } | null>(null);

	let pickerDialog = $state<HTMLDialogElement>();
	let pickerRole = $state<OutingParticipantRole | null>(null);
	let pickerSearch = $state('');
	let pickerResults = $state<ActiveMemberDto[]>([]);
	let pickerSearching = $state(false);
	let pickerDebounceTimer: ReturnType<typeof setTimeout>;

	const rowerCount = $derived(
		outing.participants.filter((p) => p.role === OutingParticipantRole.Rower).length
	);

	const participantsByRole = $derived(
		Object.fromEntries(
			roleRowOrder.map((role) => [role, outing.participants.filter((p) => p.role === role)])
		) as Record<OutingParticipantRole, OutingParticipantDto[]>
	);

	const myUserId = $derived(session.user?.id ?? null);
	const myParticipant = $derived(
		myUserId ? (outing.participants.find((p) => p.userId === myUserId) ?? null) : null
	);
	const myRole = $derived(myParticipant?.role ?? OutingParticipantRole.None);

	async function changeRole(userId: string, role: OutingParticipantRole) {
		busy = true;
		actionError = null;
		try {
			await apiClient.assignRole(outing.id, userId, new SetParticipantRoleRequest({ role }));
			onChanged();
		} catch {
			actionError = 'Rol wijzigen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	function openPicker(role: OutingParticipantRole) {
		pickerRole = role;
		pickerSearch = '';
		pickerResults = [];
		loadPickerCandidates('');
		pickerDialog?.showModal();
	}

	async function loadPickerCandidates(term: string) {
		pickerSearching = true;
		try {
			pickerResults = await apiClient.candidates(outing.id, term.trim() || undefined);
		} catch {
			pickerResults = [];
		} finally {
			pickerSearching = false;
		}
	}

	function handlePickerSearch() {
		clearTimeout(pickerDebounceTimer);
		pickerDebounceTimer = setTimeout(() => loadPickerCandidates(pickerSearch), 300);
	}

	async function addToRole(member: ActiveMemberDto) {
		if (!pickerRole) return;
		const role = pickerRole;
		busy = true;
		actionError = null;
		try {
			await apiClient.assignRole(outing.id, member.id, new SetParticipantRoleRequest({ role }));
			pickerDialog?.close();
			onChanged();
		} catch {
			actionError = 'Toevoegen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	function askRemove(userId: string, fullname: string) {
		removeTarget = { userId, fullname };
		removeDialog?.showModal();
	}

	async function doRemove() {
		if (!removeTarget) return;
		busy = true;
		actionError = null;
		try {
			await apiClient.participants(outing.id, removeTarget.userId);
			onChanged();
		} catch {
			actionError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}
</script>

<ConfirmDialog
	bind:dialog={removeDialog}
	message={removeTarget ? `${removeTarget.fullname} van deze outing verwijderen?` : ''}
	onConfirm={doRemove}
/>

<Modal bind:dialog={pickerDialog} maxWidth="max-w-md">
	<div class="p-6">
		<h3 class="text-sm font-medium text-gray-700">
			{pickerRole ? roleLabels[pickerRole] : ''} toevoegen
		</h3>
		<div class="relative mt-3">
			<input
				type="search"
				placeholder="Lid zoeken..."
				bind:value={pickerSearch}
				oninput={handlePickerSearch}
				disabled={busy}
				class="w-full rounded-md border border-gray-300 py-2 pr-8 pl-3 text-sm focus:border-primary focus:ring-primary disabled:bg-gray-50 disabled:text-gray-500"
			/>
			{#if pickerSearching}
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

		<ul class="mt-3 max-h-72 overflow-auto rounded-md border border-gray-200">
			{#each pickerResults as member (member.id)}
				<li>
					<button
						type="button"
						disabled={busy}
						onclick={() => addToRole(member)}
						class="flex w-full items-center gap-3 px-3 py-1 text-left text-sm hover:bg-gray-50 focus:bg-gray-50 focus:outline-none disabled:opacity-60"
					>
						<ProfilePicture userId={member.id} class="h-10 shrink-0 rounded object-cover" />
						<span class="text-gray-900">{member.fullname}</span>
					</button>
				</li>
			{:else}
				{#if !pickerSearching}
					<li class="px-3 py-2 text-sm text-gray-400">Geen leden gevonden.</li>
				{/if}
			{/each}
		</ul>
	</div>
</Modal>

<div>
	<div class="mt-4 space-y-5">
		{#each roleRowOrder as role (role)}
			<div>
				<div class="flex items-center justify-between">
					<div class="flex items-center gap-1.5">
						<h3 class="text-sm font-medium text-gray-700">{roleLabels[role]}</h3>
						{#if !outing.confirmed}
							<button
								type="button"
								disabled={busy}
								onclick={() => openPicker(role)}
								class="text-gray-400 hover:text-primary disabled:opacity-60"
								aria-label={`Lid toevoegen aan ${roleLabels[role]}`}
								title="Lid toevoegen"
							>
								<Plus class="h-4 w-4" />
							</button>
						{/if}
					</div>
					{#if role === OutingParticipantRole.Rower && outing.rowerCapacity}
						<span class="text-sm text-gray-500">Roeiers {rowerCount}/{outing.rowerCapacity}</span>
					{/if}
				</div>

				<div class="mt-2 flex flex-wrap gap-2">
					{#each participantsByRole[role] as participant (participant.userId)}
						<div
							class="flex w-40 flex-col items-center gap-1 rounded-md border border-gray-200 p-2 text-center"
						>
							<ProfilePicture
								userId={participant.userId}
								class="h-20 shrink-0 rounded object-cover"
							/>

							<span class="w-full truncate text-xs text-gray-900">
								{participant.fullname}
							</span>
							{#if participant.invited}
								<span class="text-[10px] text-gray-400">(uitgenodigd)</span>
							{/if}
							{#if participant.checkedIn}
								<span class="text-green-600" title="Ingecheckt">✅</span>
							{/if}

							{#if !outing.confirmed}
								<div class="flex flex-row items-center gap-1">
									<select
										value={participant.role}
										disabled={busy}
										onchange={(e) =>
											changeRole(
												participant.userId,
												(e.currentTarget as HTMLSelectElement).value as OutingParticipantRole
											)}
										class="w-full rounded-md border-gray-300 py-1 pl-1.5 text-xs focus:border-primary focus:ring-primary disabled:opacity-60"
									>
										{#each Object.values(OutingParticipantRole) as optionRole (optionRole)}
											<option value={optionRole}>{roleLabels[optionRole]}</option>
										{/each}
									</select>

									<button
										type="button"
										disabled={busy}
										onclick={() => askRemove(participant.userId, participant.fullname)}
										class="text-gray-400 hover:text-red-600 disabled:opacity-60"
										aria-label={`${participant.fullname} verwijderen`}
										title="Verwijderen"
									>
										<svg viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4">
											<path
												fill-rule="evenodd"
												d="M8.75 1A2.75 2.75 0 0 0 6 3.75v.25H3.5a.75.75 0 0 0 0 1.5h.325l.616 10.474A2.75 2.75 0 0 0 7.184 18.5h5.632a2.75 2.75 0 0 0 2.743-2.526L16.175 5.5H16.5a.75.75 0 0 0 0-1.5H14v-.25A2.75 2.75 0 0 0 11.25 1h-2.5ZM10 4.5h2.5v-.25a1.25 1.25 0 0 0-1.25-1.25h-2.5A1.25 1.25 0 0 0 7.5 4.25v.25H10Zm-2 3.25a.75.75 0 0 1 .75.75v6a.75.75 0 0 1-1.5 0v-6a.75.75 0 0 1 .75-.75Zm4.5.75a.75.75 0 0 0-1.5 0v6a.75.75 0 0 0 1.5 0v-6Z"
												clip-rule="evenodd"
											/>
										</svg>
									</button>
								</div>
							{/if}
						</div>
					{:else}
						<p class="text-sm text-gray-400">Niemand</p>
					{/each}
				</div>
			</div>
		{/each}
	</div>

	{#if actionError}
		<div class="mt-3">
			<BlueAlert level={AlertLevel.Danger}>{actionError}</BlueAlert>
		</div>
	{/if}
</div>
