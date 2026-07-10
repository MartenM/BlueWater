<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import Sailboat from '@lucide/svelte/icons/sailboat';
	import {
		SNAP_MINUTES,
		fromApiTime,
		minutesToTimeString,
		snapMinutes,
		timeStringToMinutes
	} from '$lib/constants/availability';
	import type { MaterialReservationDto } from '$lib/api/apiClient';

	let {
		reservations,
		dayStartMinutes,
		dayEndMinutes,
		canCreate,
		currentUserId,
		showHourLabels = true,
		oncreate,
		onmove,
		ondelete,
		onsetlabel
	}: {
		reservations: MaterialReservationDto[];
		dayStartMinutes: number;
		dayEndMinutes: number;
		canCreate: boolean;
		currentUserId: string | undefined;
		showHourLabels?: boolean;
		oncreate?: (startTime: string, endTime: string) => Promise<string | null>;
		onmove?: (id: string, startTime: string, endTime: string) => Promise<string | null>;
		ondelete?: (id: string) => Promise<string | null>;
		onsetlabel?: (id: string, label: string | null) => Promise<string | null>;
	} = $props();

	const EDGE_HANDLE_PX = 8;
	const TAP_MAX_MS = 300;
	const TAP_MAX_MOVE_PX = 5;

	const totalMinutes = $derived(dayEndMinutes - dayStartMinutes);

	let containerEl = $state<HTMLDivElement>();
	let containerWidth = $state(0);

	let busy = $state(false);
	let errorMessage = $state<string | null>(null);

	// Live preview shown while a drag is active. id === null means a brand-new box being drawn;
	// otherwise it's the reservation id currently being resized/moved.
	let dragPreview = $state<{ id: string | null; startMin: number; endMin: number } | null>(null);

	let popupId = $state<string | null>(null);
	let popupX = $state(0);
	let popupLabel = $state('');

	const popupReservation = $derived(reservations.find((r) => r.id === popupId) ?? null);

	type DragState = {
		mode: 'draw' | 'resize-start' | 'resize-end' | 'move';
		id: string | null;
		editable: boolean;
		anchorMin: number;
		blockStartMin: number;
		blockEndMin: number;
		pointerId: number;
		startClientX: number;
		startClientY: number;
		startTime: number;
		moved: boolean;
	};

	let dragState: DragState | null = $state(null);

	function xToMinutes(x: number): number {
		const ratio = containerWidth > 0 ? x / containerWidth : 0;
		const min = dayStartMinutes + ratio * totalMinutes;
		return Math.min(dayEndMinutes, Math.max(dayStartMinutes, min));
	}

	function minutesToPercent(min: number): number {
		return ((min - dayStartMinutes) / totalMinutes) * 100;
	}

	function clientXToLocal(clientX: number): number {
		if (!containerEl) return 0;
		const rect = containerEl.getBoundingClientRect();
		return clientX - rect.left;
	}

	function reservationMinutes(r: MaterialReservationDto): { startMin: number; endMin: number } {
		return {
			startMin: timeStringToMinutes(fromApiTime(r.startTime)),
			endMin: timeStringToMinutes(fromApiTime(r.endTime))
		};
	}

	function onContainerPointerDown(e: PointerEvent) {
		if (busy || !canCreate) return;
		if (e.target !== containerEl) return;
		e.preventDefault();
		const min = snapMinutes(xToMinutes(clientXToLocal(e.clientX)));
		dragState = {
			mode: 'draw',
			id: null,
			editable: true,
			anchorMin: min,
			blockStartMin: min,
			blockEndMin: min,
			pointerId: e.pointerId,
			startClientX: e.clientX,
			startClientY: e.clientY,
			startTime: Date.now(),
			moved: false
		};
		dragPreview = { id: null, startMin: min, endMin: min };
		window.addEventListener('pointermove', onWindowPointerMove);
		window.addEventListener('pointerup', onWindowPointerUp);
	}

	function onBlockPointerDown(e: PointerEvent, reservation: MaterialReservationDto) {
		if (busy) return;
		e.stopPropagation();
		e.preventDefault();

		const { startMin, endMin } = reservationMinutes(reservation);
		const localX = clientXToLocal(e.clientX);
		const startX = (containerWidth * (startMin - dayStartMinutes)) / totalMinutes;
		const endX = (containerWidth * (endMin - dayStartMinutes)) / totalMinutes;

		let mode: DragState['mode'] = 'move';
		if (Math.abs(localX - startX) <= EDGE_HANDLE_PX) mode = 'resize-start';
		else if (Math.abs(localX - endX) <= EDGE_HANDLE_PX) mode = 'resize-end';

		dragState = {
			mode,
			id: reservation.id,
			editable: reservation.canEdit,
			anchorMin: snapMinutes(xToMinutes(localX)),
			blockStartMin: startMin,
			blockEndMin: endMin,
			pointerId: e.pointerId,
			startClientX: e.clientX,
			startClientY: e.clientY,
			startTime: Date.now(),
			moved: false
		};
		dragPreview = { id: reservation.id, startMin, endMin };
		window.addEventListener('pointermove', onWindowPointerMove);
		window.addEventListener('pointerup', onWindowPointerUp);
	}

	function onWindowPointerMove(e: PointerEvent) {
		if (!dragState) return;
		const dx = e.clientX - dragState.startClientX;
		const dy = e.clientY - dragState.startClientY;
		if (Math.abs(dx) > TAP_MAX_MOVE_PX || Math.abs(dy) > TAP_MAX_MOVE_PX) dragState.moved = true;

		if (!dragState.editable) return; // view-only block: track tap detection only, no preview

		const localX = clientXToLocal(e.clientX);
		const min = snapMinutes(xToMinutes(localX));

		if (dragState.mode === 'draw') {
			const start = Math.min(dragState.anchorMin, min);
			const end = Math.max(dragState.anchorMin, min);
			dragPreview = { id: null, startMin: start, endMin: end };
		} else if (dragState.mode === 'resize-start') {
			const start = Math.min(min, dragState.blockEndMin - SNAP_MINUTES);
			dragPreview = { id: dragState.id, startMin: start, endMin: dragState.blockEndMin };
		} else if (dragState.mode === 'resize-end') {
			const end = Math.max(min, dragState.blockStartMin + SNAP_MINUTES);
			dragPreview = { id: dragState.id, startMin: dragState.blockStartMin, endMin: end };
		} else if (dragState.mode === 'move') {
			const deltaMin = min - dragState.anchorMin;
			const duration = dragState.blockEndMin - dragState.blockStartMin;
			let start = dragState.blockStartMin + deltaMin;
			start = Math.min(dayEndMinutes - duration, Math.max(dayStartMinutes, start));
			dragPreview = { id: dragState.id, startMin: start, endMin: start + duration };
		}
	}

	async function onWindowPointerUp(e: PointerEvent) {
		window.removeEventListener('pointermove', onWindowPointerMove);
		window.removeEventListener('pointerup', onWindowPointerUp);
		if (!dragState) return;

		const state = dragState;
		const preview = dragPreview;
		dragState = null;

		const elapsed = Date.now() - state.startTime;
		const isTap = !state.moved && elapsed < TAP_MAX_MS;

		if (isTap && state.id !== null) {
			dragPreview = null;
			const reservation = reservations.find((r) => r.id === state.id);
			if (reservation?.outingId) {
				goto(resolve('/tools/outing-planner/[id]', { id: reservation.outingId }));
			} else {
				openPopup(state.id, e.clientX);
			}
			return;
		}

		if (!state.editable || !preview) {
			dragPreview = null;
			return;
		}

		if (state.mode === 'draw') {
			if (preview.endMin - preview.startMin >= SNAP_MINUTES) {
				const startMin = preview.startMin;
				const endMin = preview.endMin;
				await commitCreate(startMin, endMin);
				dragPreview = null;
			} else {
				dragPreview = null;
			}
		} else if (state.id !== null) {
			const id = state.id;
			const startMin = preview.startMin;
			const endMin = preview.endMin;
			await commitMove(id, startMin, endMin);
			dragPreview = null;
		} else {
			dragPreview = null;
		}
	}

	async function commitCreate(startMin: number, endMin: number) {
		busy = true;
		errorMessage = null;
		const err = await oncreate?.(minutesToTimeString(startMin), minutesToTimeString(endMin));
		busy = false;
		if (err) errorMessage = err;
	}

	async function commitMove(id: string, startMin: number, endMin: number) {
		busy = true;
		errorMessage = null;
		const err = await onmove?.(id, minutesToTimeString(startMin), minutesToTimeString(endMin));
		busy = false;
		if (err) errorMessage = err;
	}

	function openPopup(id: string, clientX: number) {
		const reservation = reservations.find((r) => r.id === id);
		if (!reservation) return;
		popupId = id;
		popupLabel = reservation.customLabel ?? '';
		popupX = containerEl ? clientXToLocal(clientX) : 0;
	}

	function closePopup() {
		popupId = null;
		popupLabel = '';
	}

	async function deletePopupReservation() {
		if (!popupId) return;
		busy = true;
		errorMessage = null;
		const err = await ondelete?.(popupId);
		busy = false;
		if (!err) closePopup();
		else errorMessage = err;
	}

	async function saveLabel() {
		if (!popupId) return;
		busy = true;
		errorMessage = null;
		const trimmed = popupLabel.trim();
		const err = await onsetlabel?.(popupId, trimmed === '' ? null : trimmed);
		busy = false;
		if (!err) closePopup();
		else errorMessage = err;
	}

	function onPopupContainerPointerDown(e: PointerEvent) {
		const target = e.target as HTMLElement;
		if (target.closest('[data-material-reservation-popup]')) return;
		closePopup();
	}

	$effect(() => {
		if (popupId !== null) {
			document.addEventListener('pointerdown', onPopupContainerPointerDown);
			return () => document.removeEventListener('pointerdown', onPopupContainerPointerDown);
		}
	});

	const hourMarks = $derived(
		Array.from({ length: Math.floor(totalMinutes / 60) + 1 }, (_, i) => dayStartMinutes + i * 60)
	);

	// Below this width, there isn't room to render the label inside the box.
	const MIN_MINUTES_FOR_LABEL = 45;
</script>

<div class="relative">
	{#if showHourLabels}
		<div class="sticky top-0 z-10 h-4 select-none bg-white text-[10px] text-gray-400">
			{#each hourMarks as mark (mark)}
				<span class="absolute -translate-x-1/2" style="left: {minutesToPercent(mark)}%">
					{minutesToTimeString(mark)}
				</span>
			{/each}
		</div>
	{/if}

	<div
		class="relative h-8 select-none rounded border border-gray-200 bg-gray-50"
		class:opacity-60={busy}
		bind:this={containerEl}
		bind:clientWidth={containerWidth}
		onpointerdown={onContainerPointerDown}
		role="presentation"
		data-testid="material-reservation-timeline"
		data-can-create={canCreate}
	>
		{#each hourMarks as mark (mark)}
			<div
				class="pointer-events-none absolute inset-y-0 border-l border-gray-200"
				style="left: {minutesToPercent(mark)}%"
			></div>
		{/each}

		{#each reservations as reservation (reservation.id)}
			{@const preview = dragPreview && dragPreview.id === reservation.id ? dragPreview : null}
			{@const base = reservationMinutes(reservation)}
			{@const startMin = preview ? preview.startMin : base.startMin}
			{@const endMin = preview ? preview.endMin : base.endMin}
			{@const isOwn = reservation.ownerUserId === currentUserId}
			{@const isOuting = !!reservation.outingId}
			<div
				class="absolute inset-y-0.5 flex items-center justify-center gap-1 overflow-hidden rounded {isOuting
					? 'cursor-pointer bg-blue-600/80 hover:bg-blue-600'
					: reservation.canEdit
						? 'cursor-grab bg-primary/80 hover:bg-primary'
						: 'cursor-pointer bg-primary/50 hover:bg-primary/60'} {isOwn && !isOuting
					? 'ring-2 ring-primary'
					: ''}"
				style="left: {minutesToPercent(startMin)}%; width: {minutesToPercent(endMin) -
					minutesToPercent(startMin)}%"
				onpointerdown={(e) => onBlockPointerDown(e, reservation)}
				role="presentation"
				title="{isOuting ? 'Outing: ' : ''}{reservation.customLabel ??
					reservation.ownerFullname}{isOwn && !isOuting
					? ' (jouw reservering)'
					: ''} · {fromApiTime(reservation.startTime)} - {fromApiTime(reservation.endTime)}"
				data-testid="material-reservation-block"
				data-outing-linked={isOuting}
			>
				{#if isOuting}
					<Sailboat class="size-3 shrink-0 text-white" />
				{/if}
				{#if endMin - startMin >= MIN_MINUTES_FOR_LABEL}
					<span
						class="truncate px-1 text-[10px] whitespace-nowrap {isOuting
							? 'text-white'
							: 'text-primary-content'}"
					>
						{reservation.customLabel ?? reservation.ownerFullname}
					</span>
				{/if}
			</div>
		{/each}

		{#if dragPreview && dragPreview.id === null}
			<div
				class="pointer-events-none absolute inset-y-0.5 rounded bg-primary/40"
				style="left: {minutesToPercent(dragPreview.startMin)}%; width: {minutesToPercent(
					dragPreview.endMin
				) - minutesToPercent(dragPreview.startMin)}%"
			></div>
		{/if}
	</div>

	{#if errorMessage}
		<p class="mt-0.5 text-xs text-red-600">{errorMessage}</p>
	{/if}

	{#if popupId !== null && popupReservation}
		<div
			data-material-reservation-popup
			class="absolute z-20 w-64 rounded-md border border-gray-200 bg-white p-3 shadow-lg"
			style="top: 100%; margin-top: 0.25rem; left: {Math.max(0, popupX - 128)}px"
		>
			<p class="text-sm font-medium text-gray-900">{popupReservation.ownerFullname}</p>
			<p class="text-xs text-gray-500">
				{fromApiTime(popupReservation.startTime)} - {fromApiTime(popupReservation.endTime)}
			</p>

			{#if popupReservation.canEdit}
				<div class="mt-2">
					<label
						class="block text-[10px] font-semibold tracking-wide text-gray-500 uppercase"
						for="material-reservation-label-{popupId}"
					>
						Eigen label
					</label>
					<input
						id="material-reservation-label-{popupId}"
						type="text"
						bind:value={popupLabel}
						maxlength="200"
						placeholder={popupReservation.ownerFullname}
						class="mt-1 w-full rounded border-gray-300 text-sm"
					/>
				</div>
				<div class="mt-2 flex items-center justify-between">
					<button
						type="button"
						class="text-xs text-red-600 hover:underline"
						onclick={deletePopupReservation}
					>
						Verwijderen
					</button>
					<button
						type="button"
						class="rounded-md bg-primary px-2 py-1 text-xs text-primary-content hover:bg-primary-hover"
						onclick={saveLabel}
					>
						Opslaan
					</button>
				</div>
			{/if}
		</div>
	{/if}
</div>
