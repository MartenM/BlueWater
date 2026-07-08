<script lang="ts">
	import {
		DAY_START_MINUTES,
		DAY_END_MINUTES,
		SNAP_MINUTES,
		TIME_OPTIONS,
		snapMinutes,
		minutesToTimeString,
		timeStringToMinutes
	} from '$lib/constants/availability';
	import { mergeBlocks, type TimeBlock } from '$lib/utils/availabilityMerge';

	let {
		blocks,
		editable,
		onchange,
		showHourLabels = true
	}: {
		blocks: TimeBlock[];
		editable: boolean;
		onchange?: (blocks: TimeBlock[]) => void;
		showHourLabels?: boolean;
	} = $props();

	const TOTAL_MINUTES = DAY_END_MINUTES - DAY_START_MINUTES;
	const EDGE_HANDLE_PX = 8;
	const TAP_MAX_MS = 300;
	const TAP_MAX_MOVE_PX = 5;

	let containerEl = $state<HTMLDivElement>();
	let containerWidth = $state(0);

	let workingBlocks = $state<TimeBlock[]>([...blocks]);

	// Preview overlay shown while a drag is active. index === null means a brand-new block
	// being drawn; otherwise it's the index into workingBlocks currently being resized/moved.
	let dragPreview = $state<{ index: number | null; startMin: number; endMin: number } | null>(null);

	let popupIndex = $state<number | null>(null);
	let popupStart = $state('');
	let popupEnd = $state('');
	let popupX = $state(0);

	let saveTimer: ReturnType<typeof setTimeout> | undefined;

	type DragState = {
		mode: 'draw' | 'resize-start' | 'resize-end' | 'move';
		index: number | null;
		anchorMin: number; // for draw: fixed anchor; for move: pointer-start minute offset
		blockStartMin: number;
		blockEndMin: number;
		pointerId: number;
		startClientX: number;
		startClientY: number;
		startTime: number;
		moved: boolean;
	};

	let dragState: DragState | null = $state(null);

	// Only resync from the `blocks` prop when its *content* actually changed (e.g. the parent
	// refreshed after a save, or navigated to a different week) - not merely because a drag/popup
	// ended, which would otherwise clobber a just-committed local edit with stale prop data before
	// the debounced save has round-tripped.
	let lastBlocksSignature = '';
	$effect(() => {
		const signature = JSON.stringify(blocks);
		if (signature !== lastBlocksSignature && !dragState && popupIndex === null) {
			lastBlocksSignature = signature;
			workingBlocks = [...blocks];
		}
	});

	function scheduleSave() {
		clearTimeout(saveTimer);
		saveTimer = setTimeout(() => {
			onchange?.(workingBlocks.map((b) => ({ startTime: b.startTime, endTime: b.endTime })));
		}, 300);
	}

	function commit(next: TimeBlock[]) {
		workingBlocks = mergeBlocks(next);
		lastBlocksSignature = JSON.stringify(workingBlocks);
		scheduleSave();
	}

	function xToMinutes(x: number): number {
		const ratio = containerWidth > 0 ? x / containerWidth : 0;
		const min = DAY_START_MINUTES + ratio * TOTAL_MINUTES;
		return Math.min(DAY_END_MINUTES, Math.max(DAY_START_MINUTES, min));
	}

	function minutesToPercent(min: number): number {
		return ((min - DAY_START_MINUTES) / TOTAL_MINUTES) * 100;
	}

	function clientXToLocal(clientX: number): number {
		if (!containerEl) return 0;
		const rect = containerEl.getBoundingClientRect();
		return clientX - rect.left;
	}

	function onContainerPointerDown(e: PointerEvent) {
		if (!editable) return;
		if (e.target !== containerEl) return;
		e.preventDefault();
		const min = snapMinutes(xToMinutes(clientXToLocal(e.clientX)));
		dragState = {
			mode: 'draw',
			index: null,
			anchorMin: min,
			blockStartMin: min,
			blockEndMin: min,
			pointerId: e.pointerId,
			startClientX: e.clientX,
			startClientY: e.clientY,
			startTime: Date.now(),
			moved: false
		};
		dragPreview = { index: null, startMin: min, endMin: min };
		window.addEventListener('pointermove', onWindowPointerMove);
		window.addEventListener('pointerup', onWindowPointerUp);
	}

	function onBlockPointerDown(e: PointerEvent, index: number) {
		if (!editable) return;
		e.stopPropagation();
		e.preventDefault();
		const block = workingBlocks[index];
		const startMin = timeStringToMinutes(block.startTime);
		const endMin = timeStringToMinutes(block.endTime);
		const localX = clientXToLocal(e.clientX);
		const startX = (containerWidth * (startMin - DAY_START_MINUTES)) / TOTAL_MINUTES;
		const endX = (containerWidth * (endMin - DAY_START_MINUTES)) / TOTAL_MINUTES;

		let mode: DragState['mode'] = 'move';
		if (Math.abs(localX - startX) <= EDGE_HANDLE_PX) mode = 'resize-start';
		else if (Math.abs(localX - endX) <= EDGE_HANDLE_PX) mode = 'resize-end';

		dragState = {
			mode,
			index,
			anchorMin: snapMinutes(xToMinutes(localX)),
			blockStartMin: startMin,
			blockEndMin: endMin,
			pointerId: e.pointerId,
			startClientX: e.clientX,
			startClientY: e.clientY,
			startTime: Date.now(),
			moved: false
		};
		dragPreview = { index, startMin, endMin };
		window.addEventListener('pointermove', onWindowPointerMove);
		window.addEventListener('pointerup', onWindowPointerUp);
	}

	function onWindowPointerMove(e: PointerEvent) {
		if (!dragState) return;
		const dx = e.clientX - dragState.startClientX;
		const dy = e.clientY - dragState.startClientY;
		if (Math.abs(dx) > TAP_MAX_MOVE_PX || Math.abs(dy) > TAP_MAX_MOVE_PX) dragState.moved = true;

		const localX = clientXToLocal(e.clientX);
		const min = snapMinutes(xToMinutes(localX));

		if (dragState.mode === 'draw') {
			const start = Math.min(dragState.anchorMin, min);
			const end = Math.max(dragState.anchorMin, min);
			dragPreview = { index: null, startMin: start, endMin: end };
		} else if (dragState.mode === 'resize-start') {
			const start = Math.min(min, dragState.blockEndMin - SNAP_MINUTES);
			dragPreview = { index: dragState.index, startMin: start, endMin: dragState.blockEndMin };
		} else if (dragState.mode === 'resize-end') {
			const end = Math.max(min, dragState.blockStartMin + SNAP_MINUTES);
			dragPreview = { index: dragState.index, startMin: dragState.blockStartMin, endMin: end };
		} else if (dragState.mode === 'move') {
			const deltaMin = min - dragState.anchorMin;
			const duration = dragState.blockEndMin - dragState.blockStartMin;
			let start = dragState.blockStartMin + deltaMin;
			start = Math.min(DAY_END_MINUTES - duration, Math.max(DAY_START_MINUTES, start));
			dragPreview = { index: dragState.index, startMin: start, endMin: start + duration };
		}
	}

	function onWindowPointerUp(e: PointerEvent) {
		window.removeEventListener('pointermove', onWindowPointerMove);
		window.removeEventListener('pointerup', onWindowPointerUp);
		if (!dragState) return;

		const state = dragState;
		const preview = dragPreview;
		dragState = null;

		// Tap detection (mainly for touch): quick, essentially-stationary press on an
		// existing block opens the edit popup instead of committing a move/resize.
		const elapsed = Date.now() - state.startTime;
		const isTap =
			e.pointerType === 'touch' && state.index !== null && !state.moved && elapsed < TAP_MAX_MS;

		if (isTap && state.index !== null) {
			dragPreview = null;
			openPopup(state.index, e.clientX);
			return;
		}

		if (!preview) {
			dragPreview = null;
			return;
		}

		if (state.mode === 'draw') {
			if (preview.endMin - preview.startMin >= SNAP_MINUTES) {
				commit([
					...workingBlocks,
					{
						startTime: minutesToTimeString(preview.startMin),
						endTime: minutesToTimeString(preview.endMin)
					}
				]);
			}
		} else if (state.index !== null) {
			const next = [...workingBlocks];
			next[state.index] = {
				startTime: minutesToTimeString(preview.startMin),
				endTime: minutesToTimeString(preview.endMin)
			};
			commit(next);
		}

		dragPreview = null;
	}

	function openPopup(index: number, clientX: number) {
		if (!editable) return;
		popupIndex = index;
		popupStart = workingBlocks[index].startTime;
		popupEnd = workingBlocks[index].endTime;
		popupX = containerEl ? clientXToLocal(clientX) : 0;
	}

	function onBlockDoubleClick(e: MouseEvent, index: number) {
		if (!editable) return;
		openPopup(index, e.clientX);
	}

	function onBlockContextMenu(e: MouseEvent, index: number) {
		if (!editable) return;
		e.preventDefault();
		commit(workingBlocks.filter((_, i) => i !== index));
	}

	function savePopup() {
		if (popupIndex === null) return;
		if (timeStringToMinutes(popupStart) >= timeStringToMinutes(popupEnd)) return;
		const next = [...workingBlocks];
		next[popupIndex] = { startTime: popupStart, endTime: popupEnd };
		commit(next);
		popupIndex = null;
	}

	function deletePopup() {
		if (popupIndex === null) return;
		commit(workingBlocks.filter((_, i) => i !== popupIndex));
		popupIndex = null;
	}

	function closePopup() {
		popupIndex = null;
	}

	function onPopupContainerPointerDown(e: PointerEvent) {
		const target = e.target as HTMLElement;
		if (target.closest('[data-availability-popup]')) return;
		closePopup();
	}

	$effect(() => {
		if (popupIndex !== null) {
			document.addEventListener('pointerdown', onPopupContainerPointerDown);
			return () => document.removeEventListener('pointerdown', onPopupContainerPointerDown);
		}
	});

	const hourMarks = Array.from(
		{ length: Math.floor(TOTAL_MINUTES / 60) + 1 },
		(_, i) => DAY_START_MINUTES + i * 60
	);

	// Below this width, there isn't room to render the "HH:MM - HH:MM" label inside the block.
	const MIN_MINUTES_FOR_LABEL = 45;
</script>

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
	bind:this={containerEl}
	bind:clientWidth={containerWidth}
	onpointerdown={onContainerPointerDown}
	role="presentation"
	data-testid="availability-timeline"
	data-editable={editable}
>
	{#each hourMarks as mark (mark)}
		<div
			class="pointer-events-none absolute inset-y-0 border-l border-gray-200"
			style="left: {minutesToPercent(mark)}%"
		></div>
	{/each}

	{#each workingBlocks as block, i (i)}
		{@const preview = dragPreview && dragPreview.index === i ? dragPreview : null}
		{@const startMin = preview ? preview.startMin : timeStringToMinutes(block.startTime)}
		{@const endMin = preview ? preview.endMin : timeStringToMinutes(block.endTime)}
		<div
			class="absolute inset-y-0.5 flex items-center justify-center overflow-hidden rounded {editable
				? 'cursor-grab bg-primary/80 hover:bg-primary'
				: 'bg-primary/70'}"
			style="left: {minutesToPercent(startMin)}%; width: {minutesToPercent(endMin) -
				minutesToPercent(startMin)}%"
			onpointerdown={(e) => onBlockPointerDown(e, i)}
			ondblclick={(e) => onBlockDoubleClick(e, i)}
			oncontextmenu={(e) => onBlockContextMenu(e, i)}
			role="presentation"
			title="{block.startTime} - {block.endTime}"
			data-testid="availability-block"
		>
			{#if endMin - startMin >= MIN_MINUTES_FOR_LABEL}
				<span class="truncate px-1 text-[10px] whitespace-nowrap text-primary-content">
					{minutesToTimeString(startMin)} - {minutesToTimeString(endMin)}
				</span>
			{/if}
		</div>
	{/each}

	{#if dragPreview && dragPreview.index === null}
		<div
			class="pointer-events-none absolute inset-y-0.5 rounded bg-primary/40"
			style="left: {minutesToPercent(dragPreview.startMin)}%; width: {minutesToPercent(
				dragPreview.endMin
			) - minutesToPercent(dragPreview.startMin)}%"
		></div>
	{/if}
</div>

{#if popupIndex !== null}
	<div
		data-availability-popup
		class="absolute z-20 mt-1 w-56 rounded-md border border-gray-200 bg-white p-3 shadow-lg"
		style="left: {Math.max(0, popupX - 112)}px"
	>
		<div class="flex items-center gap-2">
			<select bind:value={popupStart} class="w-full rounded border-gray-300 text-sm">
				{#each TIME_OPTIONS as t (t)}
					<option value={t}>{t}</option>
				{/each}
			</select>
			<span class="text-gray-400">–</span>
			<select bind:value={popupEnd} class="w-full rounded border-gray-300 text-sm">
				{#each TIME_OPTIONS as t (t)}
					<option value={t}>{t}</option>
				{/each}
			</select>
		</div>
		{#if timeStringToMinutes(popupStart) >= timeStringToMinutes(popupEnd)}
			<p class="mt-1 text-xs text-red-600">Starttijd moet voor eindtijd liggen.</p>
		{/if}
		<div class="mt-2 flex items-center justify-between">
			<button type="button" class="text-xs text-red-600 hover:underline" onclick={deletePopup}>
				Verwijderen
			</button>
			<button
				type="button"
				class="rounded-md bg-primary px-2 py-1 text-xs text-primary-content hover:bg-primary-hover"
				onclick={savePopup}
			>
				Opslaan
			</button>
		</div>
	</div>
{/if}
