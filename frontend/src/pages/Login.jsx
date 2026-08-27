// ============================================================
// MODULE : M1 — Authentication
// LAYER  : View (MVC: V)
// PURPOSE: Sign-in screen. Mirrors the Register layout so the two
//          auth pages feel like one flow (NFR19 Consistency).
// NFR    : NFR5 Validation, NFR6 Error Handling
// ============================================================
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { describeError } from '../services/api'
import Logo from '../components/Logo'
import { SELECTABLE_ROLES } from '../constants/roles'
import Banner from '../components/Banner'

export default function Login() {
  const { login } = useAuth()
  const navigate = useNavigate()

  const [form, setForm] = useState({ email: '', password: '' })
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [showPassword, setShowPassword] = useState(false)

  function handleChange(e) {
    setForm({ ...form, [e.target.name]: e.target.value })
    setError('')
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')

    if (!form.email.trim() || !form.password) {
      setError('Please enter both your email and password.')
      return
    }

    setSubmitting(true)
    try {
      await login(form.email.trim(), form.password)
      navigate('/dashboard')
    } catch (err) {
      setError(describeError(err, 'Login failed. Please check your credentials.'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="min-h-screen lg:grid lg:grid-cols-[1.05fr_1fr]">

      {/* ---- LEFT PANEL — brand story ---- */}
      <aside className="relative hidden lg:flex flex-col justify-between overflow-hidden bg-slate-950 p-12 text-white">
        <div aria-hidden="true">
          <div className="blob w-[30rem] h-[30rem] bg-brand-700 -top-32 -left-24" />
          <div className="blob w-[24rem] h-[24rem] bg-brand-500 bottom-0 -right-16 opacity-30" />
        </div>

        <Link to="/" className="relative"><Logo light /></Link>

        <div className="relative max-w-md">
          <h2 className="text-4xl font-extrabold leading-tight mb-5">
            Your ideas are <span className="gradient-text">waiting for you</span>.
          </h2>
          <p className="text-slate-300 leading-relaxed mb-8">
            Pick up where you left off — your projects, recommendations and
            community activity are exactly as you left them.
          </p>

          {/* Roles the platform supports, shown as small pills */}
          <div className="flex flex-wrap gap-2">
            {SELECTABLE_ROLES.map((r) => (
              <span key={r.value}
                className="inline-flex items-center gap-1.5 rounded-full bg-white/10 border border-white/15 px-3 py-1.5 text-xs font-medium text-slate-200">
                <span aria-hidden="true">{r.icon}</span>{r.label}
              </span>
            ))}
          </div>
        </div>

        <p className="relative text-xs text-slate-500">
          CSE470 Software Engineering · BRAC University
        </p>
      </aside>

      {/* ---- RIGHT PANEL — the form ---- */}
      <main className="flex items-center justify-center bg-slate-50 px-6 py-12">
        <div className="w-full max-w-md animate-fade-up">

          <Link to="/" className="lg:hidden inline-block mb-8"><Logo /></Link>

          <h1 className="text-3xl font-extrabold text-slate-900 mb-1">Welcome back</h1>
          <p className="text-slate-500 mb-8">Sign in to continue to your dashboard.</p>

          <Banner>{error}</Banner>

          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label htmlFor="email" className="label-text">Email</label>
              <input id="email" name="email" type="email" autoComplete="email"
                value={form.email} onChange={handleChange}
                className="input-field" placeholder="you@example.com" />
            </div>

            <div>
              <label htmlFor="password" className="label-text">Password</label>
              <div className="relative">
                {/* Type flips between password/text so the user can reveal it */}
                <input id="password" name="password"
                  type={showPassword ? 'text' : 'password'} autoComplete="current-password"
                  value={form.password} onChange={handleChange}
                  className="input-field pr-16" placeholder="••••••••" />
                <button type="button" onClick={() => setShowPassword(!showPassword)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-xs font-semibold text-slate-500 hover:text-brand-600 px-2 py-1">
                  {showPassword ? 'Hide' : 'Show'}
                </button>
              </div>
            </div>

            <button type="submit" disabled={submitting} className="btn-primary w-full">
              {submitting ? 'Signing in…' : 'Sign In'}
            </button>
          </form>

          <p className="text-sm text-slate-500 mt-6 text-center">
            No account yet?{' '}
            <Link to="/register" className="text-brand-600 font-semibold hover:underline">Create one</Link>
          </p>
        </div>
      </main>
    </div>
  )
}
