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
		children: [
			{ label: 'Nieuws', href: '/about' },
			{ label: 'Agenda', href: '/about/board' },
		]
	},
	{
		label: 'Over',
		children: [
			{ label: 'De Club', href: '/about' },
			{ label: 'Board', href: '/about/board' },
			{ label: 'History', href: '/about/history' }
		]
	},
	{
		label: 'Lidmaatschap',
		children: [
			{ label: 'Join us', href: '/membership/join' },
			{ label: 'Fees', href: '/membership/fees' }
		]
	},
	{
		label: 'Activiteiten',
		children: [
			{ label: 'Training schema', href: '/activities/schedule' },
			{ label: 'Vloot', href: '/activities/fleet' }
		]
	},
	{ label: 'Contact', href: '/contact' }
];
