import { timeStringToMinutes, minutesToTimeString } from '$lib/constants/availability';

export interface TimeBlock {
	startTime: string;
	endTime: string;
}

export function mergeBlocks(blocks: TimeBlock[]): TimeBlock[] {
	const sorted = [...blocks]
		.map((b) => ({ start: timeStringToMinutes(b.startTime), end: timeStringToMinutes(b.endTime) }))
		.filter((b) => b.end > b.start)
		.sort((a, b) => a.start - b.start);

	const merged: { start: number; end: number }[] = [];
	for (const block of sorted) {
		const last = merged[merged.length - 1];
		if (last && block.start <= last.end) {
			last.end = Math.max(last.end, block.end);
		} else {
			merged.push({ ...block });
		}
	}

	return merged.map((b) => ({
		startTime: minutesToTimeString(b.start),
		endTime: minutesToTimeString(b.end)
	}));
}
