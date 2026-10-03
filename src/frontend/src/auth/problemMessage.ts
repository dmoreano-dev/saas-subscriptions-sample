import type { ApiProblem } from '../api/client'

/** User-facing text for a failed auth form submission; branches on `code`, never on title/detail. */
export function formProblemMessage(problem: ApiProblem): string {
  switch (problem.code) {
    case 'invalid_credentials':
      return 'Email or password is incorrect.'
    case 'rate_limited':
      return `Too many attempts. Try again in ${problem.retryAfterSeconds ?? 'a few'} seconds.`
    case 'validation_failed':
    case 'bad_request':
    case 'registration_failed':
      return 'Check the email and password you entered and try again.'
    case 'service_unavailable':
    case 'network_error':
      return 'The service is temporarily unavailable. Try again shortly.'
    default:
      return 'Something went wrong. Try again.'
  }
}
