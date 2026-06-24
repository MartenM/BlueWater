import { extractApiError, fieldErrors } from './apiError';

export class FormState {
	submitting = $state(false);
	formError = $state<string | null>(null);
	fieldErrors = $state<Record<string, string[]>>({});

	errorsFor(field: string): string[] {
		return fieldErrors(this.fieldErrors, field);
	}

	async submit(fn: () => Promise<void>) {
		this.formError = null;
		this.fieldErrors = {};
		this.submitting = true;
		try {
			await fn();
		} catch (e) {
			const result = extractApiError(e);
			this.formError = result.formError;
			this.fieldErrors = result.fieldErrors;
		} finally {
			this.submitting = false;
		}
	}
}
