// ============================================================
// MODULE : Shared UI (M2 Landing, and every signed-in page)
// LAYER  : View (MVC: V)
// PURPOSE: Light navigation bar. Shows the landing page's section
//          anchors to visitors, and the module links to signed-in
//          users. Collapses into a menu button on narrow screens so
//          nothing is ever unreachable.
// NFR    : NFR3 Responsive, NFR9 Usability, NFR12 Accessibility
// ============================================================
import { useEffect, useState } from 'react'
import { Link, useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { notificationApi } from '../services/moduleApi'
import Logo from './Logo'

// Anchor targets on the landing page (visitors only).
const SECTION_LINKS = [
  { label: 'Features', href: '#features' },
  { label: 'Workflow', href: '#workflow' },
  { label: 'Roles',    href: '#roles' },
  { label: 'FAQ',      href: '#faq' },
  { label: 'Contact',  href: '#contact' },
]

// The modules a signed-in user can reach. Every role sees all of these —
// each page enforces its own rules, and a role that cannot act on a page
// still benefits from seeing it exists.
const MODULE_LINKS = [
  { to: '/feed',        label: 'Feed' },
  { to: '/communities', label: 'Community' },
  { to: '/challenges',  label: 'Challenges' },
  { to: '/projects',    label: 'Projects' },
  { to: '/mentors',     label: 'Network' },
  { to: '/analytics',   label: 'Analytics' },
]

// The administration area admits Admin and Moderator. A Moderator sees
// only the moderation queue once inside; the API enforces that split.
const ADMIN_ROLES = ['Admin', 'Moderator']

export default function Navbar({ showLinks = false }) {
  const { isAuthenticated, user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [menuOpen, setMenuOpen] = useState(false)
  const [unread, setUnread] = useState(0)

  const canSeeAdmin = ADMIN_ROLES.includes(user?.role)

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

  // Close the menu whenever the route changes, so it never lingers
  // over the page the user just navigated to.
  useEffect(() => { setMenuOpen(false) }, [location.pathname])

  function handleLogout() {
    logout()
    navigate('/')
  }

  // Highlight the module you are currently looking at.
  const isCurrent = (to) => location.pathname.startsWith(to)

  return (
    <header className="sticky top-0 z-50 bg-white border-b border-slate-200/80">
      <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between gap-4">

        <Link to="/" aria-label="AI Innovation Hub home" className="shrink-0">
          <Logo />
        </Link>

        {/* ---- LANDING PAGE SECTION LINKS (visitors) ---- */}
        {showLinks && !isAuthenticated && (
          <nav className="hidden lg:flex items-center gap-8" aria-label="Page sections">
            {SECTION_LINKS.map((l) => (
              <a key={l.href} href={l.href}
                className="text-[15px] text-slate-600 hover:text-brand-600 transition-colors">
                {l.label}
              </a>
            ))}
          </nav>
        )}

        {/* ---- MODULE LINKS (signed in) ---- */}
        {/* Visible from the medium breakpoint up. Below that they move
            into the menu button, so they are never simply unreachable. */}
        {isAuthenticated && (
          <nav className="hidden md:flex items-center gap-4 lg:gap-5 flex-1 justify-center"
               aria-label="Modules">
            {MODULE_LINKS.map(({ to, label }) => (
              <Link key={to} to={to}
                aria-current={isCurrent(to) ? 'page' : undefined}
                className={`text-sm font-medium whitespace-nowrap transition-colors ${
                  isCurrent(to) ? 'text-brand-700 font-semibold' : 'text-slate-600 hover:text-brand-600'
                }`}>
                {label}
              </Link>
            ))}
            {canSeeAdmin && (
              <Link to="/admin"
                aria-current={isCurrent('/admin') ? 'page' : undefined}
                className={`text-sm font-medium whitespace-nowrap transition-colors ${
                  isCurrent('/admin') ? 'text-brand-700 font-semibold' : 'text-slate-600 hover:text-brand-600'
                }`}>
                Admin
              </Link>
            )}
          </nav>
        )}

        {/* ---- RIGHT-HAND ACTIONS ---- */}
        <div className="flex items-center gap-1.5 sm:gap-2 shrink-0">
          {isAuthenticated ? (
            <>
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

              {/* Identity — the role is shown so the current account is never in doubt */}
              <Link to="/profile" className="hidden lg:flex items-center gap-2.5 px-1 group">
                <span className="w-8 h-8 shrink-0 rounded-full bg-brand-600 text-white grid place-items-center text-xs font-bold">
                  {user?.fullName?.split(' ').map((p) => p[0]).slice(0, 2).join('')}
                </span>
                <span className="leading-tight">
                  <span className="block text-xs font-bold text-slate-800 group-hover:text-brand-600">
                    {user?.fullName}
                  </span>
                  <span className="block text-[11px] text-brand-600 font-medium">{user?.role}</span>
                </span>
              </Link>

              <Link to="/dashboard" className="btn-primary !px-4 !py-2.5 text-sm whitespace-nowrap">
                Dashboard
              </Link>

              <button onClick={handleLogout}
                className="hidden sm:block text-sm font-medium text-slate-600 hover:text-slate-900 px-2 py-2">
                Logout
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="text-[15px] font-medium text-slate-600 hover:text-brand-600 px-3 py-2">
                Login
              </Link>
              <Link to="/register" className="btn-primary !px-5 !py-2.5 text-sm">Get Started</Link>
            </>
          )}

          {/* ---- MENU BUTTON ---- */}
          {/* Shown to signed-in users below md, and to visitors on the
              landing page below lg — exactly when links are hidden. */}
          <button onClick={() => setMenuOpen(!menuOpen)}
            aria-expanded={menuOpen}
            aria-label={menuOpen ? 'Close navigation menu' : 'Open navigation menu'}
            className={`w-9 h-9 grid place-items-center rounded-lg hover:bg-slate-100 transition ${
              isAuthenticated ? 'md:hidden' : showLinks ? 'lg:hidden' : 'hidden'
            }`}>
            <span className="text-xl leading-none" aria-hidden="true">{menuOpen ? '✕' : '☰'}</span>
          </button>
        </div>
      </div>

      {/* ---- DROPDOWN ---- */}
      {menuOpen && (
        <nav className={`border-t border-slate-200 bg-white px-6 py-3 ${
              isAuthenticated ? 'md:hidden' : 'lg:hidden'}`}
             aria-label="Navigation">
          {isAuthenticated ? (
            <>
              {MODULE_LINKS.map(({ to, label }) => (
                <Link key={to} to={to}
                  className={`block py-2.5 font-medium border-b border-slate-100 last:border-0 ${
                    isCurrent(to) ? 'text-brand-700' : 'text-slate-700 hover:text-brand-600'}`}>
                  {label}
                </Link>
              ))}
              {canSeeAdmin && (
                <Link to="/admin"
                  className={`block py-2.5 font-medium border-b border-slate-100 ${
                    isCurrent('/admin') ? 'text-brand-700' : 'text-slate-700 hover:text-brand-600'}`}>
                  Admin
                </Link>
              )}
              <Link to="/profile" className="block py-2.5 font-medium text-slate-700 hover:text-brand-600">
                Profile — {user?.fullName}
                <span className="block text-xs text-brand-600 font-normal">{user?.role}</span>
              </Link>
              <button onClick={handleLogout}
                className="block w-full text-left py-2.5 font-medium text-slate-700 hover:text-brand-600">
                Logout
              </button>
            </>
          ) : (
            SECTION_LINKS.map((l) => (
              <a key={l.href} href={l.href} onClick={() => setMenuOpen(false)}
                className="block py-2.5 text-slate-700 hover:text-brand-600 font-medium">
                {l.label}
              </a>
            ))
          )}
        </nav>
      )}
    </header>
  )
}
