<script lang="ts">
	import { onMount } from 'svelte';
	import { HasPermission, MaterialReservationTimeline, Spinner, breadcrumbs } from '$lib';
	import {
		BluePermission,
		CreateMaterialReservationRequest,
		SetMaterialReservationLabelRequest,
		UpdateMaterialReservationRequest,
		type MaterialPlannerDayDto,
		type MaterialPlannerSettingsDto
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { session } from '$lib/auth/session.svelte';
	import { addDays, dateKey, toApiTime } from '$lib/constants/availability';
	import { extractApiError } from '$lib/forms/apiError';

	let selectedDate = $state(new Date());
	let data = $state<MaterialPlannerDayDto | null>(null);
	let settings = $state<MaterialPlannerSettingsDto | null>(null);
	let loading = $state(true);
	let error = $state(false);

	async function loadDay(showSpinner = true) {
		if (showSpinner) loading = true;
		try {
			data = await apiClient.plannerDay(dateKey(selectedDate));
			error = false;
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	}

	async function loadSettings() {
		try {
			settings = await apiClient.materialPlanner();
		} catch {
			settings = null;
		}
	}

	function prevDay() {
		selectedDate = addDays(selectedDate, -1);
	}

	function nextDay() {
		selectedDate = addDays(selectedDate, 1);
	}

	function onDateInputChange(e: Event) {
		const value = (e.target as HTMLInputElement).value;
		if (!value) return;
		const [y, m, d] = value.split('-').map(Number);
		selectedDate = new Date(y, m - 1, d);
	}

	async function createReservation(
		equipmentId: string,
		startTime: string,
		endTime: string
	): Promise<string | null> {
		try {
			await apiClient.materialReservationPOST(
				new CreateMaterialReservationRequest({
					equipmentId,
					date: selectedDate,
					startTime: toApiTime(startTime),
					endTime: toApiTime(endTime)
				})
			);
			await loadDay(false);
			return null;
		} catch (e) {
			return extractApiError(e).formError;
		}
	}

	async function moveReservation(
		id: string,
		startTime: string,
		endTime: string
	): Promise<string | null> {
		try {
			await apiClient.materialReservationPATCH(
				id,
				new UpdateMaterialReservationRequest({
					startTime: toApiTime(startTime),
					endTime: toApiTime(endTime)
				})
			);
			await loadDay(false);
			return null;
		} catch (e) {
			return extractApiError(e).formError;
		}
	}

	async function deleteReservation(id: string): Promise<string | null> {
		try {
			await apiClient.materialReservationDELETE(id);
			await loadDay(false);
			return null;
		} catch (e) {
			return extractApiError(e).formError;
		}
	}

	async function setLabel(id: string, label: string | null): Promise<string | null> {
		try {
			await apiClient.label(
				id,
				new SetMaterialReservationLabelRequest({ customLabel: label ?? undefined })
			);
			await loadDay(false);
			return null;
		} catch (e) {
			return extractApiError(e).formError;
		}
	}

	$effect(() => {
		void selectedDate;
		loadDay();
	});

	onMount(() => {
		loadSettings();
	});

	$effect(() => {
		breadcrumbs.set([{ label: 'Materiaalplanner' }]);
		return () => breadcrumbs.clear();
	});

	const canCreate = $derived(session.hasPermission(BluePermission.MaterialPlannerUse));
</script>

<HasPermission permission={BluePermission.MaterialPlannerUse}>
	<div class="flex items-center justify-between">
		<h1 class="text-2xl font-bold text-gray-900">Materiaalplanner</h1>
		<div class="flex items-center gap-2">
			<button
				type="button"
				onclick={prevDay}
				class="rounded-md border border-gray-300 px-3 py-1.5 text-sm hover:bg-gray-50"
			>
				← Vorige dag
			</button>
			<input
				type="date"
				value={dateKey(selectedDate)}
				onchange={onDateInputChange}
				class="rounded-md border-gray-300 text-sm"
			/>
			<button
				type="button"
				onclick={nextDay}
				class="rounded-md border border-gray-300 px-3 py-1.5 text-sm hover:bg-gray-50"
			>
				Volgende dag →
			</button>
		</div>
	</div>

	<div class="mt-6">
		{#if loading}
			<Spinner />
		{:else if error}
			<p class="text-sm text-gray-600">Materiaalplanner kon niet worden geladen.</p>
		{:else if data}
			<div class="space-y-8">
				{#each data.boatTypeGroups as group, groupIndex (group.equipmentTypeId ?? group.typeLabel)}
					<div>
						<h3 class="text-xs font-semibold tracking-wide text-gray-500 uppercase">
							{group.typeLabel}
						</h3>
						<div class="mt-1 space-y-1.5">
							{#each group.boats as boat, boatIndex (boat.equipmentId)}
								<div class="flex items-center gap-3">
									<span class="w-40 shrink-0 truncate text-sm text-gray-700">
										{boat.name}
									</span>
									<div class="flex-1">
										<MaterialReservationTimeline
											reservations={boat.reservations}
											dayStartMinutes={(settings?.startHour ?? 6) * 60}
											dayEndMinutes={(settings?.endHour ?? 23) * 60}
											{canCreate}
											currentUserId={session.user?.id}
											showHourLabels={groupIndex === 0 && boatIndex === 0}
											oncreate={(startTime, endTime) =>
												createReservation(boat.equipmentId, startTime, endTime)}
											onmove={moveReservation}
											ondelete={deleteReservation}
											onsetlabel={setLabel}
										/>
									</div>
								</div>
							{/each}
						</div>
					</div>
				{/each}
			</div>
		{/if}
	</div>
	{#snippet fallback()}
		<p class="mt-6 text-sm text-gray-600">Je hebt geen toegang tot de materiaalplanner.</p>
	{/snippet}
</HasPermission>
