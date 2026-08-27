// ============================================================
// MODULE : M12 — Notifications
// LAYER  : View (MVC: V)
// FEATURE: F17 — Notification System
// IMPLEMENTS: likes/comments/replies alerts, team invitations, task
//   assignments, mentor requests, investor interest, challenge updates,
//   AI recommendation alerts, read/unread management.
// ============================================================
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Navbar from '../components/Navbar'
import { notificationApi } from '../services/moduleApi'
import { describeError } from '../services/api'
import Banner from '../components/Banner'
import EmptyState from '../components/EmptyState'

// Each notification type gets its own glyph so the list scans quickly.
const ICONS = {
  Like: '👍', Comment: '💬', TeamInvite: '🤝', TaskAssigned: '✅',
  Mentorship: '🎓', Investment: '💰', Challenge: '🏆',
  Badge: '🏅', Moderation: '🛡️', General: '🔔',
}

export default function Notifications() {
  const navigate = useNavigate()
  const [items, setItems] = useState([])
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [error, setError] = useState('')

  async function load() {
    try { setItems(await notificationApi.list(unreadOnly)) }
    catch (err) { setError(describeError(err, 'Could not load notifications.')) }
  }
  useEffect(() => { load() }, [unreadOnly])

  async function open(n) {
    // Mark read first so the badge count is right even if we navigate away.
    if (!n.isRead) { try { await notificationApi.markRead(n.id) } catch {} }
    if (n.link) navigate(n.link)
    else await load()
  }

  async function markAll() {
    try { await notificationApi.markAllRead(); await load() }
    catch (err) { setError(describeError(err, 'Could not mark all as read.')) }
  }

  async function remove(id, e) {
    e.stopPropagation()
    try { await notificationApi.remove(id); await load() }
    catch (err) { setError(describeError(err, 'Could not dismiss that notification.')) }
  }

  const unreadCount = items.filter(n => !n.isRead).length

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar />
      <div className="max-w-3xl mx-auto px-6 py-10">
        <header className="flex flex-wrap items-end justify-between gap-4 mb-6">
          <div>
            <h1 className="text-3xl font-extrabold text-slate-900">Notifications</h1>
            <p className="text-slate-500 mt-1">
              {unreadCount > 0 ? `${unreadCount} unread` : 'You are all caught up.'}
            </p>
          </div>
          {unreadCount > 0 && (
            <button onClick={markAll} className="btn-ghost !py-2 !px-4 text-sm">Mark all read</button>
          )}
        </header>

        <Banner>{error}</Banner>

        <div className="flex gap-1 bg-slate-100 p-1 rounded-lg mb-6 w-fit">
          {[['All', false], ['Unread', true]].map(([label, val]) => (
            <button key={label} onClick={() => setUnreadOnly(val)}
              className={`px-4 py-2 rounded-md text-sm font-semibold transition ${
                unreadOnly === val ? 'bg-white text-brand-700 shadow-sm' : 'text-slate-500 hover:text-slate-700'}`}>
              {label}
            </button>
          ))}
        </div>

        {items.length === 0 ? (
          <EmptyState icon="🔔" title="Nothing here"
              message="Alerts appear when people interact with your work." />
        ) : (
          <ul className="space-y-2">
            {items.map(n => (
              <li key={n.id}>
                <button onClick={() => open(n)}
                  className={`w-full text-left flex items-start gap-3 rounded-xl border p-4 transition ${
                    n.isRead ? 'bg-white border-slate-200 hover:border-slate-300'
                             : 'bg-brand-50/40 border-brand-200 hover:border-brand-300'}`}>
                  <span className="text-xl shrink-0" aria-hidden="true">{ICONS[n.type] || ICONS.General}</span>
                  <div className="flex-1 min-w-0">
                    <p className={`text-sm ${n.isRead ? 'text-slate-600' : 'text-slate-900 font-medium'}`}>
                      {n.message}
                    </p>
                    <div className="text-xs text-slate-400 mt-1">{n.timeAgo}</div>
                  </div>
                  {!n.isRead && <span className="w-2 h-2 rounded-full bg-brand-600 shrink-0 mt-1.5" aria-label="unread" />}
                  <span onClick={(e) => remove(n.id, e)} role="button" tabIndex={0}
                    aria-label="Dismiss"
                    className="text-slate-300 hover:text-red-500 px-1 shrink-0 transition">✕</span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
