export interface NavItem {
	label: string;
	href?: string;
	children?: NavItem[];
}

// Hardcoded for now; shaped to match what the planned navigation API endpoint will return,
// so swapping this for a fetched value later shouldn't require changing NavBar/NavMenuItem.
export const navItems: NavItem[] = [
	{
		label: 'Actueel',
		href: '/',
		children: [
			{ label: 'Nieuws', href: '/news' },
			{ label: 'Agenda', href: '/agenda' },
			{ label: 'Inschrijvingen', href: '/signup' }
		]
	},
	{
		label: 'Over',
		href: '/about',
		children: [
			{ label: 'De Club', href: '/about' },
			{ label: 'Board', href: '/about/board' },
			{ label: 'History', href: '/about/history' }
		]
	},
	{
		label: 'Lidmaatschap',
		href: '/membership/join',
		children: [
			{ label: 'Join us', href: '/membership/join' },
			{ label: 'Fees', href: '/membership/fees' }
		]
	},
	{
		label: 'Tools',
		href: '/tools',
		children: [
			{ label: 'Outing Planner', href: '/tools/outing-planner' },
			{ label: 'Beschikbaarheid', href: '/tools/availability-planner' },
			{ label: 'Materiaalplanner', href: '/tools/material-planner' },
			{ label: 'Materiaal / Vloot Beheer', href: '/tools/fleet' },
			{ label: 'Gebruikers', href: '/tools/users' },
			{ label: 'Groepen', href: '/tools/groups' },
			{ label: 'Examens', href: '/tools/exams' },
			{ label: 'Clusters', href: '/tools/clusters' },
			{ label: 'Mail', href: '/tools/mail' },
			{ label: 'Inschrijvingen', href: '/tools/signup' }
		]
	},
	{ label: 'Contact', href: '/contact' }
];
