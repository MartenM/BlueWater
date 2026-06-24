export interface BreadcrumbItem {
	label: string;
	href?: string;
}

class BreadcrumbStore {
	trail = $state<BreadcrumbItem[] | null>(null);

	set(trail: BreadcrumbItem[]) {
		this.trail = trail;
	}

	clear() {
		this.trail = null;
	}
}

export const breadcrumbs = new BreadcrumbStore();
