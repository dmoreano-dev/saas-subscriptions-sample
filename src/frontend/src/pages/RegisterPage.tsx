import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { apiRequest } from '../api/client'
import type { RegisterResponse } from '../api/contract'
import { useAuth } from '../auth/AuthContext'
import { formProblemMessage } from '../auth/problemMessage'

export function RegisterPage() {
  const { announceRegistered } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError(null)
    // 201 carries no token: the user signs in explicitly afterwards.
    const result = await apiRequest<RegisterResponse>('/api/auth/register', {
      method: 'POST',
      body: { email, password },
    })
    setPending(false)
    if (result.ok) {
      announceRegistered()
      navigate('/login', { replace: true })
      return
    }
    setError(formProblemMessage(result.problem))
  }

  return (
    <main>
      <h1>Create account</h1>
      <form onSubmit={onSubmit}>
        <label>
          Email
          <input type="email" autoComplete="username" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete="new-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        {error && <p role="alert">{error}</p>}
        <button type="submit" disabled={pending}>
          Create account
        </button>
      </form>
      <p>
        Already registered? <Link to="/login">Sign in</Link>.
      </p>
    </main>
  )
}
