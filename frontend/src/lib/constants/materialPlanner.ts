import { SNAP_MINUTES, minutesToTimeString } from '$lib/constants/availability';

export function timeOptionsFor(startMinutes: number, endMinutes: number): string[] {
	const options: string[] = [];
	for (let m = startMinutes; m <= endMinutes; m += SNAP_MINUTES) {
		options.push(minutesToTimeString(m));
	}
	return options;
}
