// ============================================================
// MODULE : M1 — Authentication
// LAYER  : View (MVC: V)
// PURPOSE: Two-panel registration screen. Left panel sells the
//          platform, right panel collects the account details.
// IMPLEMENTS: "Role selection/access control" from M1 — now with the
//          six self-service roles derived from the module actors,
//          plus the three admin-granted roles shown as context.
// NFR    : NFR5 Validation, NFR6 Error Handling, NFR12 Accessibility
// ============================================================
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { describeError } from '../services/api'
import Logo from '../components/Logo'
import { SELECTABLE_ROLES, ASSIGNED_ROLES, DEFAULT_ROLE } from '../constants/roles'
import Banner from '../components/Banner'

export default function Register() {
  const { register } = useAuth()
  const navigate = useNavigate()

  const [form, setForm] = useState({
    fullName: '', email: '', password: '', confirmPassword: '', role: DEFAULT_ROLE,
  })
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [showAssigned, setShowAssigned] = useState(false)

  function handleChange(e) {
    setForm({ ...form, [e.target.name]: e.target.value })
    setError('')
  }

  // ---- PASSWORD STRENGTH METER ----
  // Purely advisory feedback; the real rule is the 6-character minimum
  // enforced in validate() and again by the backend DTO.
  function strengthOf(pw) {
    let score = 0
    if (pw.length >= 6) score++
    if (pw.length >= 10) score++
    if (/[A-Z]/.test(pw) && /[a-z]/.test(pw)) score++
    if (/\d/.test(pw)) score++
    if (/[^A-Za-z0-9]/.test(pw)) score++
    return Math.min(score, 4)
  }
  const strength = strengthOf(form.password)
  const strengthMeta = [
    { label: '', color: '' },
    { label: 'Weak',       color: 'bg-red-500' },
    { label: 'Fair',       color: 'bg-amber-500' },
    { label: 'Good',       color: 'bg-lime-500' },
    { label: 'Strong',     color: 'bg-emerald-500' },
  ][strength]

  // ---- CLIENT-SIDE VALIDATION (NFR5) ----
  function validate() {
    if (!form.fullName.trim())                  return 'Please enter your full name.'
    if (!form.email.trim())                     return 'Please enter your email address.'
    if (!/^\S+@\S+\.\S+$/.test(form.email))     return 'Please enter a valid email address.'
    if (form.password.length < 6)               return 'Password must be at least 6 characters.'
    if (form.password !== form.confirmPassword) return 'Passwords do not match.'
    return ''
  }

  async function handleSubmit(e) {
    e.preventDefault()
    const problem = validate()
    if (problem) { setError(problem); return }

    setSubmitting(true)
    try {
      await register(form.fullName.trim(), form.email.trim(), form.password, form.role)
      navigate('/dashboard')
    } catch (err) {
      setError(describeError(err, 'Registration failed. Please try again.'))
    } finally {
      setSubmitting(false)
    }
  }

  const activeRole = SELECTABLE_ROLES.find((r) => r.value === form.role)

  return (
    <div className="min-h-screen lg:grid lg:grid-cols-[1.05fr_1fr]">

      {/* ==================================================== */}
      {/* LEFT PANEL — brand story (hidden on small screens)   */}
      {/* ==================================================== */}
      <aside className="relative hidden lg:flex flex-col justify-between overflow-hidden bg-slate-950 p-12 text-white">
        {/* Decorative colour orbs */}
        <div aria-hidden="true">
          <div className="blob w-[30rem] h-[30rem] bg-brand-700 -top-32 -left-24" />
          <div className="blob w-[24rem] h-[24rem] bg-brand-500 bottom-0 -right-16 opacity-30" />
        </div>

        <Link to="/" className="relative"><Logo light /></Link>

        <div className="relative max-w-md">
          <h2 className="text-4xl font-extrabold leading-tight mb-5">
            Join the people turning <span className="gradient-text">ideas into impact</span>.
          </h2>
          <p className="text-slate-300 leading-relaxed mb-10">
            Whichever role you pick, the platform adapts around it — the tools,
            recommendations and connections you see are matched to how you work.
          </p>

          {/* Live preview of the currently selected role */}
          <div className="glass !bg-white/10 !border-white/15 rounded-2xl p-5">
            <div className="text-[11px] uppercase tracking-widest text-slate-400 mb-3">You are joining as</div>
            <div className="flex items-start gap-4">
              <span className="text-3xl">{activeRole?.icon}</span>
              <div>
                <div className="font-bold text-white">{activeRole?.label}</div>
                <div className="text-sm text-brand-200 mb-1">{activeRole?.tagline}</div>
                <p className="text-sm text-slate-300 leading-relaxed">{activeRole?.desc}</p>
              </div>
            </div>
          </div>
        </div>

        <div className="relative flex gap-8 text-sm">
          {[['14', 'Modules'], ['20', 'Features'], ['9', 'Roles']].map(([n, l]) => (
            <div key={l}>
              <div className="stat-number text-2xl text-white">{n}</div>
              <div className="text-slate-400 text-xs uppercase tracking-wide">{l}</div>
            </div>
          ))}
        </div>
      </aside>

      {/* ==================================================== */}
      {/* RIGHT PANEL — the actual form                        */}
      {/* ==================================================== */}
      <main className="flex items-center justify-center bg-slate-50 px-6 py-12">
        <div className="w-full max-w-lg animate-fade-up">

          {/* Small-screen brand mark */}
          <Link to="/" className="lg:hidden inline-block mb-8"><Logo /></Link>

          <h1 className="text-3xl font-extrabold text-slate-900 mb-1">Create your account</h1>
          <p className="text-slate-500 mb-8">Free forever. No card required.</p>

          <Banner>{error}</Banner>

          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label htmlFor="fullName" className="label-text">Full name</label>
              <input id="fullName" name="fullName" type="text" autoComplete="name"
                value={form.fullName} onChange={handleChange}
                className="input-field" placeholder="Your name" />
            </div>

            <div>
              <label htmlFor="email" className="label-text">Email</label>
              <input id="email" name="email" type="email" autoComplete="email"
                value={form.email} onChange={handleChange}
                className="input-field" placeholder="you@example.com" />
            </div>

            {/* ---- ROLE SELECTION (M1) ---- */}
            {/* radiogroup semantics so keyboard/screen-reader users get
                the same experience as mouse users (NFR12). */}
            <fieldset>
              <legend className="label-text">I am joining as</legend>
              <div role="radiogroup" aria-label="Account role" className="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
                {SELECTABLE_ROLES.map((r) => {
                  const selected = form.role === r.value
                  return (
                    <button
                      key={r.value} type="button" role="radio" aria-checked={selected}
                      onClick={() => setForm({ ...form, role: r.value })}
                      className={`group relative rounded-xl border-2 p-3 text-left transition-all duration-200 ${
                        selected
                          ? 'border-brand-500 bg-brand-50 shadow-lift'
                          : 'border-slate-200 bg-white hover:border-brand-300 hover:-translate-y-0.5'
                      }`}
                    >
                      <span className="block text-xl mb-1.5">{r.icon}</span>
                      <span className={`block text-sm font-bold ${selected ? 'text-brand-700' : 'text-slate-800'}`}>
                        {r.label}
                      </span>
                      <span className="block text-[11px] text-slate-500 leading-snug mt-0.5">{r.tagline}</span>
                      {selected && (
                        <span className="absolute top-2 right-2 w-4 h-4 rounded-full bg-brand-600 text-white grid place-items-center text-[10px]">✓</span>
                      )}
                    </button>
                  )
                })}
              </div>

              {/* ---- ADMIN-GRANTED ROLES (context only, never selectable) ---- */}
              <button type="button" onClick={() => setShowAssigned(!showAssigned)}
                className="mt-3 text-xs font-semibold text-brand-600 hover:text-brand-700 inline-flex items-center gap-1">
                {showAssigned ? '−' : '+'} Other roles exist — how do I get one?
              </button>
              {showAssigned && (
                <div className="mt-3 rounded-xl bg-slate-100/80 border border-slate-200 p-4 space-y-2.5 animate-fade-up">
                  <p className="text-xs text-slate-600 mb-2">
                    These are granted by an administrator and cannot be chosen at sign-up:
                  </p>
                  {ASSIGNED_ROLES.map((r) => (
                    <div key={r.value} className="flex gap-3 text-xs">
                      <span aria-hidden="true">{r.icon}</span>
                      <div>
                        <span className="font-bold text-slate-700">{r.label}</span>
                        <span className="text-slate-500"> — {r.desc}</span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </fieldset>

            <div>
              <label htmlFor="password" className="label-text">Password</label>
              <input id="password" name="password" type="password" autoComplete="new-password"
                value={form.password} onChange={handleChange}
                className="input-field" placeholder="At least 6 characters" />

              {/* Strength meter appears only once typing starts */}
              {form.password && (
                <div className="flex items-center gap-3 mt-2">
                  <div className="flex-1 flex gap-1">
                    {[1, 2, 3, 4].map((i) => (
                      <span key={i} className={`h-1.5 flex-1 rounded-full transition-colors ${
                        i <= strength ? strengthMeta.color : 'bg-slate-200'}`} />
                    ))}
                  </div>
                  <span className="text-xs font-semibold text-slate-500 w-12">{strengthMeta.label}</span>
                </div>
              )}
            </div>

            <div>
              <label htmlFor="confirmPassword" className="label-text">Confirm password</label>
              <input id="confirmPassword" name="confirmPassword" type="password" autoComplete="new-password"
                value={form.confirmPassword} onChange={handleChange}
                className="input-field" placeholder="Repeat your password" />
            </div>

            <button type="submit" disabled={submitting} className="btn-primary w-full">
              {submitting ? 'Creating account…' : 'Create Account'}
            </button>
          </form>

          <p className="text-sm text-slate-500 mt-6 text-center">
            Already registered?{' '}
            <Link to="/login" className="text-brand-600 font-semibold hover:underline">Sign in</Link>
          </p>
        </div>
      </main>
    </div>
  )
}
