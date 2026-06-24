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
			{ label: 'Agenda', href: '/agenda' }
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
			{ label: 'Botenboek', href: '/tools/fleet' },
			{ label: 'Gebruikers', href: '/tools/users' },
			{ label: 'Groepen', href: '/tools/groups' }
		]
	},
	{ label: 'Contact', href: '/contact' }
];
