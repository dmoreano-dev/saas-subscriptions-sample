import { useEffect, useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth, type AuthNotice } from '../auth/AuthContext'
import { formProblemMessage } from '../auth/problemMessage'

const NOTICE_TEXT: Record<AuthNotice, string> = {
  expired: 'Your session expired. Sign in again.',
  loggedOut: 'You signed out of this browser. Sign in again to continue.',
  registered: 'Account created. Sign in to continue.',
}

export function LoginPage() {
  const { session, notice, login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)
  const [retryIn, setRetryIn] = useState(0)

  useEffect(() => {
    if (retryIn <= 0) return
    const timer = setTimeout(() => setRetryIn((seconds) => seconds - 1), 1000)
    return () => clearTimeout(timer)
  }, [retryIn])

  if (session) return <Navigate to="/me" replace />

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError(null)
    const result = await login({ email, password })
    setPending(false)
    if (result.ok) {
      navigate('/me', { replace: true })
      return
    }
    setError(formProblemMessage(result.problem))
    if (result.problem.code === 'rate_limited') setRetryIn(result.problem.retryAfterSeconds ?? 0)
  }

  return (
    <main>
      <h1>Sign in</h1>
      {notice && <p role="status">{NOTICE_TEXT[notice]}</p>}
      <form onSubmit={onSubmit}>
        <label>
          Email
          <input type="email" autoComplete="username" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        {error && <p role="alert">{error}</p>}
        <button type="submit" disabled={pending || retryIn > 0}>
          {retryIn > 0 ? `Wait ${retryIn}s` : 'Sign in'}
        </button>
      </form>
      <p>
        No account? <Link to="/register">Create one</Link>.
      </p>
      <p className="hint">
        The access token is kept in memory only: reloading this page signs you out.
      </p>
    </main>
  )
}
