import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

/** Route guard: without an in-memory session the user goes to login. */
export function RequireAuth() {
  const { session } = useAuth()
  return session ? <Outlet /> : <Navigate to="/login" replace />
}
