import { createContext, useContext } from 'react'
import type { ApiResult } from '../api/client'
import type { LoginRequest, UserDto } from '../api/contract'

/** The in-memory session. It lives only in React state: a reload discards it. */
export interface Session {
  accessToken: string
  expiresAt: Date
  user: UserDto
}

/** Why the user is looking at the login screen; shown as a banner. */
export type AuthNotice = 'expired' | 'loggedOut' | 'registered'

export interface AuthContextValue {
  session: Session | null
  notice: AuthNotice | null
  login: (request: LoginRequest) => Promise<ApiResult<Session>>
  /** Local logout: discards the token in this browser only. A copy stays valid until `exp`. */
  logout: () => void
  /** Authenticated GET/POST; a 401 `unauthenticated` ends the session with the `expired` notice. */
  authedRequest: <T>(path: string) => Promise<ApiResult<T>>
  announceRegistered: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used inside AuthProvider')
  return value
}
