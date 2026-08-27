// ============================================================
// MODULE : Shared UI (M2 Landing, M3 Dashboard)
// LAYER  : View (MVC: V)
// PURPOSE: Light navigation bar with in-page section links, matching
//          the reference design. Section links only make sense on the
//          landing page, so they are hidden elsewhere via `showLinks`.
// NFR    : NFR9 Usability, NFR12 Accessibility, NFR19 Consistency
// ============================================================
import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import Logo from './Logo'
import { notificationApi } from '../services/moduleApi'

// Anchor targets correspond to the id="" attributes on Landing.jsx sections.
const SECTION_LINKS = [
  { label: 'Features', href: '#features' },
  { label: 'Workflow', href: '#workflow' },
  { label: 'Roles',    href: '#roles' },
  { label: 'FAQ',      href: '#faq' },
  { label: 'Contact',  href: '#contact' },
]

export default function Navbar({ showLinks = false }) {
  const { isAuthenticated, user, logout } = useAuth()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false)
  const [unread, setUnread] = useState(0)

  // ---- F17: keep the bell badge current ----
  // Polls every 30s. SignalR would push instead, but polling keeps the
  // dependency surface small for a course project.
  useEffect(() => {
    if (!isAuthenticated) { setUnread(0); return }
    let alive = true
    const tick = () => notificationApi.count()
      .then(r => { if (alive) setUnread(r.unread) })
      .catch(() => {})
    tick()
    const timer = setInterval(tick, 30000)
    return () => { alive = false; clearInterval(timer) }
  }, [isAuthenticated])

  function handleLogout() {
    logout()
    navigate('/')
  }

  return (
    <header className="sticky top-0 z-50 bg-white border-b border-slate-200/80">
      <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between gap-6">

        <Link to="/" aria-label="AI Innovation Hub home">
          <Logo />
        </Link>

        {/* ---- DESKTOP SECTION LINKS (landing page only) ---- */}
        {showLinks && (
          <nav className="hidden lg:flex items-center gap-8" aria-label="Page sections">
            {SECTION_LINKS.map((l) => (
              <a key={l.href} href={l.href}
                className="text-[15px] text-slate-600 hover:text-brand-600 transition-colors">
                {l.label}
              </a>
            ))}
          </nav>
        )}

        {/* ---- RIGHT-HAND ACTIONS ---- */}
        <div className="flex items-center gap-3">
          {isAuthenticated ? (
            <>
              <div className="hidden sm:flex items-center gap-2.5 mr-1">
                <span className="w-8 h-8 rounded-full bg-brand-600 text-white grid place-items-center text-xs font-bold">
                  {user?.fullName?.split(' ').map((p) => p[0]).slice(0, 2).join('')}
                </span>
                <Link to="/profile" className="leading-tight group">
                  <span className="block text-xs font-bold text-slate-800 group-hover:text-brand-600">{user?.fullName}</span>
                  <span className="block text-[11px] text-brand-600 font-medium">{user?.role}</span>
                </Link>
              </div>
              {/* Module navigation for signed-in users */}
              <nav className="hidden xl:flex items-center gap-4 mr-1" aria-label="Modules">
                {[
                  ['/feed', 'Feed'], ['/communities', 'Community'], ['/challenges', 'Challenges'],
                  ['/projects', 'Projects'], ['/mentors', 'Network'], ['/analytics', 'Analytics'],
                ].map(([to, label]) => (
                  <Link key={to} to={to} className="text-sm font-medium text-slate-600 hover:text-brand-600">
                    {label}
                  </Link>
                ))}
              </nav>

              {/* F6 smart search */}
              <Link to="/search" aria-label="Smart search"
                className="w-9 h-9 grid place-items-center rounded-lg text-slate-500 hover:bg-slate-100 transition">
                <span aria-hidden="true">⌕</span>
              </Link>

              {/* F17 notification bell with unread badge */}
              <Link to="/notifications" aria-label={`Notifications${unread ? `, ${unread} unread` : ''}`}
                className="relative w-9 h-9 grid place-items-center rounded-lg text-slate-500 hover:bg-slate-100 transition">
                <span aria-hidden="true">🔔</span>
                {unread > 0 && (
                  <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] px-1 rounded-full bg-red-500 text-white text-[10px] font-bold grid place-items-center">
                    {unread > 9 ? '9+' : unread}
                  </span>
                )}
              </Link>

              <Link to="/dashboard" className="btn-primary !px-5 !py-2.5 text-sm">Dashboard</Link>
              {/* M14 is only advertised to administrators */}
              {user?.role === 'Admin' && (
                <Link to="/admin" className="text-sm font-medium text-slate-600 hover:text-brand-600 px-2">
                  Admin
                </Link>
              )}
              <button onClick={handleLogout}
                className="text-sm font-medium text-slate-600 hover:text-slate-900 px-3 py-2">
                Logout
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="text-[15px] font-medium text-slate-600 hover:text-brand-600 px-3 py-2">
                Login
              </Link>
              <Link to="/register" className="btn-primary !px-5 !py-2.5 text-sm">Dashboard</Link>
            </>
          )}

          {/* ---- MOBILE MENU TOGGLE ---- */}
          {showLinks && (
            <button onClick={() => setMenuOpen(!menuOpen)}
              aria-expanded={menuOpen} aria-label="Toggle navigation menu"
              className="lg:hidden w-9 h-9 grid place-items-center rounded-lg hover:bg-slate-100">
              <span className="text-xl leading-none">{menuOpen ? '✕' : '☰'}</span>
            </button>
          )}
        </div>
      </div>

      {/* ---- MOBILE DROPDOWN ---- */}
      {showLinks && menuOpen && (
        <nav className="lg:hidden border-t border-slate-200 bg-white px-6 py-4 space-y-1" aria-label="Page sections">
          {SECTION_LINKS.map((l) => (
            <a key={l.href} href={l.href} onClick={() => setMenuOpen(false)}
              className="block py-2.5 text-slate-700 hover:text-brand-600 font-medium">
              {l.label}
            </a>
          ))}
        </nav>
      )}
    </header>
  )
}
