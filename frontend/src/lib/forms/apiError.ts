import { ValidationProblemDetails } from '$lib/api/apiClient';

export interface FormErrors {
	formError: string | null;
	fieldErrors: Record<string, string[]>;
}

// The generated API client's throwException() throws the parsed response body directly
// (e.g. a bare ValidationProblemDetails) rather than wrapping it in an ApiException whenever
// the response has a body - it only throws ApiException itself for bodiless error statuses.
export function extractApiError(e: unknown): FormErrors {
	if (e instanceof ValidationProblemDetails && e.errors) {
		return {
			formError: e.detail ?? 'Controleer de gemarkeerde velden.',
			fieldErrors: e.errors
		};
	}
	return { formError: 'Er ging iets mis. Probeer het later opnieuw.', fieldErrors: {} };
}

export function fieldErrors(errors: Record<string, string[]>, field: string): string[] {
	const key = Object.keys(errors).find((k) => k.toLowerCase() === field.toLowerCase());
	return key ? errors[key] : [];
}
