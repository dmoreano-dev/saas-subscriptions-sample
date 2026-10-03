import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { apiRequest } from '../api/client'
import type { LoginRequest, LoginResponse } from '../api/contract'
import { AuthContext, type AuthContextValue, type AuthNotice, type Session } from './AuthContext'

// setTimeout stores its delay in a signed 32-bit integer; longer delays fire immediately.
const MAX_TIMER_MS = 2_147_483_647

/**
 * Holds the access token in memory only (no localStorage, sessionStorage or cookies; plan §5A).
 * The client closes the session at `expiresAt`, but the server's 401 remains the authority:
 * the client clock can drift and the API applies its own clock skew.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(null)
  const [notice, setNotice] = useState<AuthNotice | null>(null)
  const sessionRef = useRef<Session | null>(null)

  const endSession = useCallback((reason: AuthNotice) => {
    sessionRef.current = null
    setSession(null)
    setNotice(reason)
  }, [])

  const login = useCallback<AuthContextValue['login']>(async (request: LoginRequest) => {
    const result = await apiRequest<LoginResponse>('/api/auth/login', { method: 'POST', body: request })
    if (!result.ok) return result
    const next: Session = {
      accessToken: result.data.accessToken,
      expiresAt: new Date(result.data.expiresAt),
      user: result.data.user,
    }
    sessionRef.current = next
    setSession(next)
    setNotice(null)
    return { ok: true, data: next }
  }, [])

  const logout = useCallback(() => endSession('loggedOut'), [endSession])

  const authedRequest = useCallback<AuthContextValue['authedRequest']>(
    async (path) => {
      const token = sessionRef.current?.accessToken
      const result = await apiRequest<never>(path, { accessToken: token })
      if (!result.ok && result.problem.code === 'unauthenticated' && token && sessionRef.current?.accessToken === token) {
        endSession('expired')
      }
      return result
    },
    [endSession],
  )

  const announceRegistered = useCallback(() => setNotice('registered'), [])

  useEffect(() => {
    if (!session) return
    const remaining = session.expiresAt.getTime() - Date.now()
    const timer = setTimeout(() => endSession('expired'), Math.min(Math.max(remaining, 0), MAX_TIMER_MS))
    return () => clearTimeout(timer)
  }, [session, endSession])

  const value = useMemo(
    () => ({ session, notice, login, logout, authedRequest, announceRegistered }),
    [session, notice, login, logout, authedRequest, announceRegistered],
  )
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
