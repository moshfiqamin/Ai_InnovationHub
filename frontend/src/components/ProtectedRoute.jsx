// ============================================================
// MODULE : M1 — Authentication
// LAYER  : View (MVC: V) — route guard
// PURPOSE: Wraps any page that requires a logged-in user.
//          Implements the "Protected routes/pages" item of M1.
// NFR    : NFR2 Authentication & Authorization
// ============================================================
import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function ProtectedRoute({ children }) {
  const { isAuthenticated, loading } = useAuth()

  // While the saved session is being restored, show nothing.
  // Without this the user would be bounced to /login on every refresh.
  if (loading) {
    return <div className="min-h-screen grid place-items-center text-slate-500">Loading…</div>
  }

  // Not logged in -> send to the login page instead of rendering the page.
  if (!isAuthenticated) return <Navigate to="/login" replace />

  // Logged in -> render the protected page.
  return children
}
