import { useEffect, useState } from 'react'
import type { ApiProblem } from '../api/client'
import type { UserDto } from '../api/contract'
import { useAuth } from '../auth/AuthContext'
import { formProblemMessage } from '../auth/problemMessage'

type State = { status: 'loading' } | { status: 'ready'; user: UserDto } | { status: 'error'; problem: ApiProblem }

export function MePage() {
  const { session, authedRequest, logout } = useAuth()
  const [state, setState] = useState<State>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false
    void authedRequest<UserDto>('/api/me').then((result) => {
      if (cancelled) return
      setState(result.ok ? { status: 'ready', user: result.data } : { status: 'error', problem: result.problem })
    })
    return () => {
      cancelled = true
    }
  }, [authedRequest])

  return (
    <main>
      <h1>Your profile</h1>
      {state.status === 'loading' && <p>Loading…</p>}
      {state.status === 'error' && <p role="alert">{formProblemMessage(state.problem)}</p>}
      {state.status === 'ready' && (
        <dl>
          <dt>Email</dt>
          <dd data-testid="me-email">{state.user.email}</dd>
          <dt>User id</dt>
          <dd>{state.user.id}</dd>
        </dl>
      )}
      <p className="hint">Session ends at {session?.expiresAt.toLocaleTimeString()}.</p>
      <button type="button" onClick={logout}>
        Sign out
      </button>
      <p className="hint">
        Signing out only discards the token in this browser. A copy of the token stays valid until it expires
        (about 10 minutes): there is no server-side revocation until P06.
      </p>
    </main>
  )
}
