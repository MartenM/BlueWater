<script lang="ts">
	import { apiClient } from '$lib/api/client';
	import {
		InviteParticipantRequest,
		OutingParticipantRole,
		SetParticipantRoleRequest
	} from '$lib/api/apiClient';
	import type { OutingDetailDto, OutingParticipantDto } from '$lib/api/apiClient';
	import { session } from '$lib/auth/session.svelte';
	import BlueAlert from './BlueAlert.svelte';
	import ProfilePicture from './ProfilePicture.svelte';
	import MemberPicker from './MemberPicker.svelte';
	import ConfirmDialog from './common/ConfirmDialog.svelte';
	import Button from './common/Button.svelte';
	import { AlertLevel } from '$lib/alert';

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

	let inviteUserId = $state<string | null>(null);
	let busy = $state(false);
	let actionError = $state<string | null>(null);

	let removeDialog = $state<HTMLDialogElement>();
	let removeTarget = $state<{ userId: string; fullname: string } | null>(null);

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

	async function invite() {
		if (!inviteUserId) return;
		busy = true;
		actionError = null;
		try {
			await apiClient.invite(outing.id, new InviteParticipantRequest({ userId: inviteUserId }));
			inviteUserId = null;
			onChanged();
		} catch {
			actionError = 'Uitnodigen is mislukt. Probeer het later opnieuw.';
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

<div>
	<h2 class="text-sm font-semibold text-gray-700">Bemanning</h2>

	{#if !outing.confirmed && myUserId}
		<div class="mt-3 flex items-center gap-2">
			<label for="my-role" class="text-sm font-medium text-gray-700">Mijn rol</label>
			<select
				id="my-role"
				value={myRole}
				disabled={busy}
				onchange={(e) =>
					changeRole(
						myUserId,
						(e.currentTarget as HTMLSelectElement).value as OutingParticipantRole
					)}
				class="rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary disabled:opacity-60"
			>
				{#each Object.values(OutingParticipantRole) as role (role)}
					<option value={role}>{roleLabels[role]}</option>
				{/each}
			</select>
		</div>
	{/if}

	<div class="mt-4 space-y-5">
		{#each roleRowOrder as role (role)}
			<div>
				<div class="flex items-center justify-between">
					<h3 class="text-sm font-medium text-gray-700">{roleLabels[role]}</h3>
					{#if role === OutingParticipantRole.Rower && outing.rowerCapacity}
						<span class="text-sm text-gray-500">Roeiers {rowerCount}/{outing.rowerCapacity}</span>
					{/if}
				</div>

				<div class="mt-2 flex flex-wrap gap-2">
					{#each participantsByRole[role] as participant (participant.userId)}
						<div class="flex items-center gap-2 rounded-md border border-gray-200 py-1 pr-2 pl-1">
							{#if participant.hasProfilePicture}
								<ProfilePicture
									load={() => apiClient.getProfilePicture(participant.userId)}
									class="h-8 w-6 shrink-0 rounded object-cover"
								/>
							{:else}
								<div
									class="flex h-8 w-6 shrink-0 items-center justify-center rounded bg-gray-100 text-gray-400"
								>
									<svg viewBox="0 0 24 24" fill="currentColor" class="h-4 w-4">
										<path
											d="M12 12c2.7 0 4.875-2.175 4.875-4.875S14.7 2.25 12 2.25 7.125 4.425 7.125 7.125 9.3 12 12 12Zm0 2.25c-3.45 0-9 1.5-9 5.25v1.5h18v-1.5c0-3.75-5.55-5.25-9-5.25Z"
										/>
									</svg>
								</div>
							{/if}

							<span class="truncate text-sm text-gray-900">
								{participant.fullname}
								{#if participant.invited}
									<span class="ml-1 text-xs text-gray-400">(uitgenodigd)</span>
								{/if}
								{#if participant.checkedIn}
									<span class="ml-1 text-green-600" title="Ingecheckt">✅</span>
								{/if}
							</span>

							{#if !outing.confirmed}
								<select
									value={participant.role}
									disabled={busy}
									onchange={(e) =>
										changeRole(
											participant.userId,
											(e.currentTarget as HTMLSelectElement).value as OutingParticipantRole
										)}
									class="rounded-md border-gray-300 py-1 pl-1.5 text-xs focus:border-primary focus:ring-primary disabled:opacity-60"
								>
									{#each Object.values(OutingParticipantRole) as optionRole (optionRole)}
										<option value={optionRole}>{roleLabels[optionRole]}</option>
									{/each}
								</select>

								<button
									type="button"
									disabled={busy}
									onclick={() => askRemove(participant.userId, participant.fullname)}
									class="text-xs font-medium text-red-600 hover:underline disabled:opacity-60"
								>
									Verwijderen
								</button>
							{/if}
						</div>
					{:else}
						<p class="text-sm text-gray-400">Niemand</p>
					{/each}
				</div>
			</div>
		{/each}
	</div>

	{#if !outing.confirmed}
		<div class="mt-4 flex items-end gap-2">
			<div class="flex-1">
				<span class="mb-1 block text-sm font-medium text-gray-700">Iemand uitnodigen</span>
				<MemberPicker bind:value={inviteUserId} disabled={busy} />
			</div>
			<Button
				type="button"
				variant="secondary"
				size="sm"
				disabled={!inviteUserId || busy}
				onclick={invite}
			>
				Uitnodigen
			</Button>
		</div>
	{/if}

	{#if actionError}
		<div class="mt-3">
			<BlueAlert level={AlertLevel.Danger}>{actionError}</BlueAlert>
		</div>
	{/if}
</div>
