<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { UpsertOutingRequest } from '$lib/api/apiClient';
	import { FormState } from '$lib/forms/formState.svelte';
	import type {
		EquipmentDto,
		EquipmentTypeDto,
		MaterialReservationConflictDto,
		OutingDetailDto,
		OutingMyInstanceDto
	} from '$lib/api/apiClient';
	import { BlueForm, apiClient } from '$lib';
	import { dateKey, toApiTime } from '$lib/constants/availability';
	import FormField from './FormField.svelte';

	let {
		outing,
		submitLabel,
		onSubmit
	}: {
		outing?: OutingDetailDto;
		submitLabel: string;
		onSubmit: (request: UpsertOutingRequest) => Promise<void>;
	} = $props();

	const defaultDurationMinutes = 90;

	function toDateTimeInput(d: Date | undefined): string {
		if (!d) return '';
		const pad = (n: number) => String(n).padStart(2, '0');
		return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
	}

	function parseDateTime(s: string): Date | undefined {
		return s ? new Date(s) : undefined;
	}

	function initialDurationMinutes(): string {
		if (outing?.outingDate && outing?.outingDateEnd) {
			const minutes = Math.round(
				(outing.outingDateEnd.getTime() - outing.outingDate.getTime()) / 60000
			);
			return String(minutes);
		}
		return String(defaultDurationMinutes);
	}

	let userGroupInstanceId = $state(untrack(() => outing?.userGroupInstanceId) ?? '');
	let outingDate = $state(untrack(() => toDateTimeInput(outing?.outingDate)));
	let durationMinutes = $state(untrack(() => initialDurationMinutes()));
	let boatTypeId = $state(untrack(() => outing?.boatTypeId) ?? '');
	let boatTypeDifferent = $state(untrack(() => outing?.boatTypeDifferent) ?? '');
	let boatId = $state(untrack(() => outing?.boatId) ?? '');
	let description = $state(untrack(() => outing?.description) ?? '');

	let instances = $state<OutingMyInstanceDto[]>([]);
	let boatTypes = $state<EquipmentTypeDto[]>([]);
	let boats = $state<EquipmentDto[]>([]);

	const availableBoats = $derived(
		boatTypeId ? boats.filter((b) => b.equipmentTypeId === boatTypeId) : boats
	);

	let conflict = $state<MaterialReservationConflictDto | null>(null);
	let conflictChecking = $state(false);

	// The conflict endpoint has no notion of "the outing being edited" — if this outing already
	// booked the boat for this exact slot, the check reports that same reservation as a conflict.
	const isOwnBooking = $derived(
		!!conflict?.conflictingReservation &&
			conflict.conflictingReservation.id === outing?.boatReservationId
	);

	let conflictRequestId = 0;

	$effect(() => {
		const id = boatId;
		const dateInput = outingDate;
		const minutesInput = durationMinutes;

		const requestId = ++conflictRequestId;

		if (!id || !dateInput) {
			conflict = null;
			return;
		}
		const start = parseDateTime(dateInput);
		if (!start) {
			conflict = null;
			return;
		}
		const minutes = parseInt(minutesInput) || 0;
		const end = minutes > 0 ? new Date(start.getTime() + minutes * 60000) : start;
		const pad = (n: number) => String(n).padStart(2, '0');

		conflictChecking = true;
		apiClient
			.conflict(
				id,
				dateKey(start),
				toApiTime(`${pad(start.getHours())}:${pad(start.getMinutes())}`),
				toApiTime(`${pad(end.getHours())}:${pad(end.getMinutes())}`)
			)
			.then((result) => {
				if (requestId === conflictRequestId) conflict = result;
			})
			.catch(() => {
				if (requestId === conflictRequestId) conflict = null;
			})
			.finally(() => {
				if (requestId === conflictRequestId) conflictChecking = false;
			});
	});

	const form = new FormState();

	onMount(async () => {
		try {
			const [myInstances, types, equipmentPage] = await Promise.all([
				apiClient.myInstances(),
				apiClient.typesAll2(),
				apiClient.fleetGET(1, 200, undefined)
			]);
			instances = myInstances;
			boatTypes = types.filter((t) => t.isBoat);
			boats = equipmentPage.items;
		} catch {
			// non-fatal; selects just stay empty
		}
	});
</script>

<BlueForm
	{form}
	{submitLabel}
	onsubmit={() => {
		const start = parseDateTime(outingDate)!;
		const minutes = parseInt(durationMinutes) || 0;
		const end = minutes > 0 ? new Date(start.getTime() + minutes * 60000) : undefined;

		return onSubmit(
			new UpsertOutingRequest({
				userGroupInstanceId,
				outingDate: start,
				outingDateEnd: end,
				boatTypeId: boatTypeId || undefined,
				boatTypeDifferent: boatTypeId ? undefined : boatTypeDifferent || undefined,
				boatId: boatId || undefined,
				description: description || undefined
			})
		);
	}}
>
	<FormField label="Team" errors={form.errorsFor('userGroupInstanceId')}>
		{#snippet children(invalid)}
			<select
				required
				disabled={!!outing}
				bind:value={userGroupInstanceId}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'} disabled:bg-gray-50 disabled:text-gray-500"
			>
				<option value="">— kies een team —</option>
				{#each instances as i (i.id)}
					<option value={i.id}>{i.name}</option>
				{/each}
			</select>
		{/snippet}
	</FormField>

	<div class="grid grid-cols-2 gap-4">
		<FormField label="Datum en tijd" errors={form.errorsFor('outingDate')}>
			{#snippet children(invalid)}
				<input
					type="datetime-local"
					required
					bind:value={outingDate}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="Duur (minuten)" errors={form.errorsFor('outingDateEnd')}>
			{#snippet children(invalid)}
				<input
					type="number"
					min="0"
					bind:value={durationMinutes}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>
	</div>

	<FormField label="Boottype" errors={form.errorsFor('boatTypeId')}>
		{#snippet children(invalid)}
			<select
				bind:value={boatTypeId}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			>
				<option value="">— anders —</option>
				{#each boatTypes as t (t.id)}
					<option value={t.id}>{t.name}</option>
				{/each}
			</select>
		{/snippet}
	</FormField>

	{#if !boatTypeId}
		<FormField label="Omschrijving training" errors={form.errorsFor('boatTypeDifferent')}>
			{#snippet children(invalid)}
				<input
					type="text"
					maxlength="30"
					bind:value={boatTypeDifferent}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>
	{/if}

	<FormField label="Boot" errors={form.errorsFor('boatId')}>
		{#snippet children(invalid)}
			<select
				bind:value={boatId}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			>
				<option value="">— geen —</option>
				{#each availableBoats as b (b.id)}
					<option value={b.id}>{b.name}</option>
				{/each}
			</select>
		{/snippet}
	</FormField>

	{#if boatId}
		<p class="-mt-2 text-xs">
			{#if conflictChecking}
				<span class="text-gray-500">Beschikbaarheid controleren…</span>
			{:else if conflict?.hasConflict && !isOwnBooking}
				<span class="text-red-600">
					Boot is al gereserveerd: {conflict.conflictingReservation?.customLabel ??
						conflict.conflictingReservation?.ownerFullname}
				</span>
			{:else if isOwnBooking}
				<span class="text-green-700">Boot gereserveerd voor deze outing ✓</span>
			{:else if conflict}
				<span class="text-green-700">Boot is beschikbaar</span>
			{/if}
		</p>
	{/if}

	<FormField label="Omschrijving" errors={form.errorsFor('description')}>
		{#snippet children(invalid)}
			<textarea
				rows="3"
				bind:value={description}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"></textarea>
		{/snippet}
	</FormField>
</BlueForm>
