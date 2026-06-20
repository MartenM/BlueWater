export interface ContactDetails {
	address?: string;
	email?: string;
	phone?: string;
}

// Hardcoded for now; shaped to match what a planned club-info API endpoint will return,
// so swapping this for a fetched value later shouldn't require changing Footer.
export const contactDetails: ContactDetails = {
	address: 'Example Street 1, 0000 AB Example City',
	email: 'info@example.org',
	phone: '+31 00 000 0000'
};
