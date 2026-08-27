// ============================================================
// MODULE : M1 — Authentication
// LAYER  : View support (MVC: V) — shared client state
// PURPOSE: Holds the logged-in user for the whole app. Exposes
//          register / login / logout and persists the session so
//          a page refresh does not log the user out.
// NOTE   : Per the CSE470 guide, login/registration are NOT counted
//          among the 20 features — this is supporting infrastructure.
// ============================================================
import { createContext, useContext, useState, useEffect } from 'react'
import api from '../services/api'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)      // current user object, null = logged out
  const [loading, setLoading] = useState(true) // true while we restore a saved session

  // ---- RESTORE SESSION ON APP START ------------------------
  // Read any previously saved user from localStorage so a refresh
  // keeps the user signed in (NFR14 Session Management).
  useEffect(() => {
    const saved = localStorage.getItem('aih_user')
    if (saved) {
      try { setUser(JSON.parse(saved)) } catch { localStorage.removeItem('aih_user') }
    }
    setLoading(false)
  }, [])

  // ---- HELPER: persist a successful auth response -----------
  function persistSession(data) {
    localStorage.setItem('aih_token', data.token)
    localStorage.setItem('aih_user', JSON.stringify(data.user))
    setUser(data.user)
  }

  // ---- REGISTER --------------------------------------------
  // Calls POST /api/auth/register on the AuthController.
  async function register(fullName, email, password, role) {
    const { data } = await api.post('/auth/register', { fullName, email, password, role })
    persistSession(data)
    return data
  }

  // ---- LOGIN -----------------------------------------------
  // Calls POST /api/auth/login on the AuthController.
  async function login(email, password) {
    const { data } = await api.post('/auth/login', { email, password })
    persistSession(data)
    return data
  }

  // ---- LOGOUT ----------------------------------------------
  // Clears the stored token and user. Nothing server-side to call
  // because JWTs are stateless.
  function logout() {
    localStorage.removeItem('aih_token')
    localStorage.removeItem('aih_user')
    setUser(null)
  }

  const value = { user, loading, register, login, logout, isAuthenticated: !!user }
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// Convenience hook so pages can call useAuth() instead of useContext(AuthContext)
export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>')
  return ctx
}
