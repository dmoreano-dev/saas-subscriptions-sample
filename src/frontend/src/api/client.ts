import type { ProblemCode, ProblemDetails } from './contract'

/** Failure of any API call. `code` comes from the ProblemDetails body; `network_error` means no usable response. */
export interface ApiProblem {
  status: number
  code: ProblemCode | 'network_error'
  /** Seconds from the `Retry-After` header (429 only). */
  retryAfterSeconds?: number
}

export type ApiResult<T> = { ok: true; data: T } | { ok: false; problem: ApiProblem }

interface RequestOptions {
  method?: 'GET' | 'POST'
  body?: unknown
  /** Access token held in memory by the auth provider; sent as `Authorization: Bearer`. */
  accessToken?: string
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<ApiResult<T>> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  if (options.accessToken) headers.Authorization = `Bearer ${options.accessToken}`

  let response: Response
  try {
    response = await fetch(path, {
      method: options.method ?? 'GET',
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      // Never send or accept cookies: the only credential is the bearer header.
      credentials: 'omit',
      cache: 'no-store',
    })
  } catch {
    return { ok: false, problem: { status: 0, code: 'network_error' } }
  }

  if (response.ok) {
    return { ok: true, data: (await response.json()) as T }
  }

  const problem = (await response.json().catch(() => null)) as Partial<ProblemDetails> | null
  const retryAfter = Number(response.headers.get('Retry-After'))
  return {
    ok: false,
    problem: {
      status: response.status,
      code: problem?.code ?? 'internal_error',
      retryAfterSeconds: response.status === 429 && Number.isFinite(retryAfter) ? retryAfter : undefined,
    },
  }
}
