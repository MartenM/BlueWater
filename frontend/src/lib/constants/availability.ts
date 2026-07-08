export const DAY_START_MINUTES = 6 * 60;
export const DAY_END_MINUTES = 23 * 60;
export const SNAP_MINUTES = 15;

export function minutesToTimeString(minutes: number): string {
	const h = Math.floor(minutes / 60);
	const m = minutes % 60;
	return `${h < 10 ? '0' : ''}${h}:${m < 10 ? '0' : ''}${m}`;
}

export function timeStringToMinutes(time: string): number {
	const [h, m] = time.split(':').map(Number);
	return h * 60 + m;
}

// Backend TimeOnly values (de)serialize as "HH:mm:ss" — trim/append seconds at the
// boundary so the rest of the frontend can work with plain "HH:mm" strings.
export function fromApiTime(time: string): string {
	return time.slice(0, 5);
}

export function toApiTime(time: string): string {
	return `${time}:00`;
}

export function snapMinutes(minutes: number): number {
	return Math.round(minutes / SNAP_MINUTES) * SNAP_MINUTES;
}

export const TIME_OPTIONS: string[] = (() => {
	const options: string[] = [];
	for (let m = DAY_START_MINUTES; m <= DAY_END_MINUTES; m += SNAP_MINUTES) {
		options.push(minutesToTimeString(m));
	}
	return options;
})();

export function dateKey(d: Date): string {
	const y = d.getFullYear();
	const mo = d.getMonth() + 1;
	const day = d.getDate();
	return `${y}-${mo < 10 ? '0' : ''}${mo}-${day < 10 ? '0' : ''}${day}`;
}

export function addDays(d: Date, days: number): Date {
	const next = new Date(d);
	next.setDate(next.getDate() + days);
	return next;
}

export function startOfWeek(d: Date): Date {
	const day = d.getDay();
	const diff = day === 0 ? -6 : 1 - day;
	const monday = addDays(d, diff);
	monday.setHours(0, 0, 0, 0);
	return monday;
}
