/**
 * Pull a user-readable message out of an unknown error.
 *
 * Handles the ASP.NET Identity / ProblemDetails shapes the backend returns:
 *   - `error.error` is a plain string
 *   - `error.error.message` is a string
 *   - `error.error.errorMessage` is a string OR an array of strings (joined)
 *   - `error.error.errors` is a `Record<string, string[]>` (flattened and joined)
 *
 * Returns `fallback` when no recognised shape matches.
 */
export function extractErrorMessage(error: unknown, fallback: string): string {
  const payload = (error as { error?: unknown })?.error;

  if (typeof payload === 'string') {
    return payload;
  }

  if (payload && typeof payload === 'object' && 'message' in payload) {
    return String((payload as { message: unknown }).message);
  }

  if (payload && typeof payload === 'object' && 'errorMessage' in payload) {
    const messages = (payload as { errorMessage: unknown }).errorMessage;
    return Array.isArray(messages) ? messages.join(' ') : String(messages);
  }

  if (payload && typeof payload === 'object' && 'errors' in payload) {
    const errors = (payload as { errors: Record<string, string[]> }).errors;
    const messages = Object.values(errors).flat();
    return messages.length ? messages.join(' ') : fallback;
  }

  return fallback;
}
