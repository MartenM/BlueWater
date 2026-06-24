<script lang="ts">
	import House from '@lucide/svelte/icons/house';
	import ChevronRight from '@lucide/svelte/icons/chevron-right';
	import { page } from '$app/state';
	import { navItems, type NavItem } from '$lib/navigation';
	import { breadcrumbs, type BreadcrumbItem } from '$lib/breadcrumbs.svelte';

	function flattenNavItems(items: NavItem[]): Record<string, string> {
		const map: Record<string, string> = {};
		for (const item of items) {
			if (item.href) map[item.href] = item.label;
			if (item.children) {
				for (const child of item.children) {
					if (child.href) map[child.href] = child.label;
				}
			}
		}
		return map;
	}

	function humanize(segment: string): string {
		return segment
			.split('-')
			.map((word) => word.charAt(0).toUpperCase() + word.slice(1))
			.join(' ');
	}

	function deriveTrail(pathname: string): BreadcrumbItem[] {
		const navLabels = flattenNavItems(navItems);
		const segments = pathname.split('/').filter(Boolean);

		let bestMatch: BreadcrumbItem | null = null;
		let cumulative = '';
		for (const segment of segments) {
			cumulative += `/${segment}`;
			const label = navLabels[cumulative];
			if (label) bestMatch = { label, href: cumulative };
		}

		if (bestMatch) return [bestMatch];

		const lastSegment = segments[segments.length - 1];
		return lastSegment ? [{ label: humanize(lastSegment) }] : [];
	}

	const trail = $derived(breadcrumbs.trail ?? deriveTrail(page.url.pathname));
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -- crumb hrefs are data-driven (from navigation.ts or page-supplied overrides), not static route literals resolve() can check -->
{#if page.url.pathname !== '/'}
	<nav aria-label="Breadcrumb" class="border-b border-gray-200 bg-gray-50">
		<ol class="mx-auto flex max-w-7xl items-center gap-1 px-4 py-2 text-sm sm:px-6 lg:px-8">
			<li class="flex items-center">
				<a href="/" aria-label="Home" class="text-gray-500 hover:text-primary-hover">
					<House class="size-4" />
				</a>
			</li>
			{#each trail as crumb, i (crumb.label)}
				<li class="flex items-center gap-1">
					<ChevronRight class="size-4 text-gray-400" />
					{#if crumb.href && i < trail.length - 1}
						<a href={crumb.href} class="text-gray-500 hover:text-primary-hover">{crumb.label}</a>
					{:else}
						<span class="font-medium text-gray-900">{crumb.label}</span>
					{/if}
				</li>
			{/each}
		</ol>
	</nav>
{/if}
