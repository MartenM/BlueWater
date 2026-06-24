export interface YearMonth {
	year: number;
	month: number; // 0-based, JS convention
}

const monthLabelFormatter = new Intl.DateTimeFormat('nl-NL', { month: 'long', year: 'numeric' });

export function currentYearMonth(): YearMonth {
	const now = new Date();
	return { year: now.getFullYear(), month: now.getMonth() };
}

export function parseYearMonth(value: string | null): YearMonth | null {
	const match = value?.match(/^(\d{4})-(\d{2})$/);
	if (!match) return null;
	const year = Number(match[1]);
	const month = Number(match[2]) - 1;
	if (month < 0 || month > 11) return null;
	return { year, month };
}

export function formatYearMonth({ year, month }: YearMonth): string {
	return `${year}-${String(month + 1).padStart(2, '0')}`;
}

export function addMonths({ year, month }: YearMonth, delta: number): YearMonth {
	const total = year * 12 + month + delta;
	return { year: Math.floor(total / 12), month: ((total % 12) + 12) % 12 };
}

export function monthRangeDates(
	{ year, month }: YearMonth,
	monthsSpan: number
): { start: Date; end: Date } {
	return {
		start: new Date(Date.UTC(year, month, 1)),
		end: new Date(Date.UTC(year, month + monthsSpan, 0))
	};
}

export function monthLabel(ym: YearMonth): string {
	return monthLabelFormatter.format(new Date(Date.UTC(ym.year, ym.month, 1)));
}
