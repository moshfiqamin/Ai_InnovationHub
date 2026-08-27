// ============================================================
// MODULE : M14 — Administration
// LAYER  : View (MVC: V)
// FEATURE: F20 — Admin & AI Content Moderation
// IMPLEMENTS: user management, idea/content review, community
//   moderation, reports handling, challenge management, platform
//   analytics, AI-assisted content flagging, admin audit visibility.
// NOTE   : The API also enforces Admin-only access; this page hiding
//   itself is convenience, not the security boundary.
// ============================================================
import { useEffect, useState } from 'react'
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, ArcElement, Tooltip, Legend } from 'chart.js'
import { Bar, Doughnut } from 'react-chartjs-2'
import Navbar from '../components/Navbar'
import { adminApi } from '../services/moduleApi'
import { useAuth } from '../context/AuthContext'
import { describeError } from '../services/api'
import Banner from '../components/Banner'
import Avatar from '../components/Avatar'
import EmptyState from '../components/EmptyState'
import PageShell from '../components/PageShell'
import Tabs from '../components/Tabs'

ChartJS.register(CategoryScale, LinearScale, BarElement, ArcElement, Tooltip, Legend)

const ALL_ROLES = ['Innovator','Researcher','Entrepreneur','Mentor','Investor','Organization','Judge','Moderator','Admin']

export default function Admin() {
  const { user } = useAuth()
  const [tab, setTab] = useState(user?.role === 'Moderator' ? 'Moderation' : 'Overview')
  const [stats, setStats] = useState(null)
  const [users, setUsers] = useState([])
  const [reports, setReports] = useState([])
  const [reportFilter, setReportFilter] = useState('Pending')
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  // Admins see everything. Moderators see the moderation queue only —
  // the API enforces the same split independently.
  const isAdmin = user?.role === 'Admin'
  const isModerator = user?.role === 'Moderator'
  const canEnter = isAdmin || isModerator

  function flash(m) { setNotice(m); setTimeout(() => setNotice(''), 2500) }

  async function load() {
    if (!canEnter) return
    try {
      // A Moderator is not permitted to read stats or the user list,
      // so only an Admin requests them.
      if (isAdmin) {
        const [s, u, r] = await Promise.all([
          adminApi.stats(), adminApi.users(search || undefined), adminApi.reports(reportFilter),
        ])
        setStats(s); setUsers(u); setReports(r)
      } else {
        setReports(await adminApi.reports(reportFilter))
      }
    } catch (err) { setError(describeError(err, 'Could not load this page.')) }
  }
  useEffect(() => { load() }, [search, reportFilter, isAdmin, isModerator])

  async function setRole(id, role) {
    try { await adminApi.setRole(id, role); await load(); flash('Role updated.') }
    catch (err) { setError(describeError(err, 'Could not change that role.')) }
  }

  async function resolve(id, action) {
    if (action === 'remove' && !window.confirm('Permanently delete this content?')) return
    try { await adminApi.resolve(id, action); await load(); flash(`Report ${action === 'remove' ? 'actioned' : 'dismissed'}.`) }
    catch (err) { setError(describeError(err, 'Could not resolve that report.')) }
  }

  // ---- Non-admins get a clear explanation, not a broken page ----
  if (!canEnter) {
    return (
      <div className="min-h-screen bg-slate-50">
        <Navbar />
        <div className="max-w-2xl mx-auto px-6 py-20 text-center">
          <div className="text-5xl mb-4" aria-hidden="true">🛡️</div>
          <h1 className="text-2xl font-extrabold text-slate-900 mb-2">Administrators only</h1>
          <p className="text-slate-600">
            This area is restricted to <strong>Admin</strong> and <strong>Moderator</strong> accounts.
            Your role is <strong>{user?.role}</strong>.
          </p>
        </div>
      </div>
    )
  }

  return (
    <PageShell title="Administration" subtitle="Platform management, users and moderation.">
        <Banner tone="success">{notice}</Banner>
        <Banner>{error}</Banner>

        <Tabs items={['Overview', 'Users', 'Moderation']} value={tab} onChange={setTab} className="mb-6" />
        {/* ================= OVERVIEW ================= */}
        {tab === 'Overview' && stats && (
          <>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4 mb-6">
              {[
                { label: 'Users', value: stats.totalUsers, icon: '👥' },
                { label: 'Ideas', value: stats.totalIdeas, icon: '💡' },
                { label: 'Projects', value: stats.totalProjects, icon: '🚀' },
                { label: 'Communities', value: stats.totalCommunities, icon: '💬' },
                { label: 'Challenges', value: stats.totalChallenges, icon: '🏆' },
                { label: 'Pending reports', value: stats.pendingReports, icon: '⚠️' },
                { label: 'Flagged content', value: stats.flaggedContent, icon: '🛡️' },
              ].map(s => (
                <div key={s.label} className="card">
                  <div className="flex items-start justify-between">
                    <div>
                      <div className="text-[11px] uppercase tracking-widest text-slate-500 mb-2 font-semibold">{s.label}</div>
                      <div className="stat-number text-3xl text-slate-900">{s.value}</div>
                    </div>
                    <span className="text-xl" aria-hidden="true">{s.icon}</span>
                  </div>
                </div>
              ))}
            </div>

            <div className="grid md:grid-cols-2 gap-6">
              <div className="card">
                <h3 className="font-bold text-slate-900 mb-4">Users by role</h3>
                <Doughnut
                  data={{
                    labels: stats.usersByRole.labels,
                    datasets: [{
                      data: stats.usersByRole.values,
                      backgroundColor: ['#0d9488','#f59e0b','#0ea5e9','#8b5cf6','#ef4444','#84cc16','#ec4899','#6366f1','#14b8a6'],
                      borderWidth: 0,
                    }],
                  }}
                  options={{ responsive: true, cutout: '58%',
                    plugins: { legend: { position: 'bottom', labels: { boxWidth: 12, font: { size: 11 } } } } }}
                />
              </div>
              <div className="card">
                <h3 className="font-bold text-slate-900 mb-4">Sign-ups this week</h3>
                <Bar
                  data={{
                    labels: stats.signupsOverTime.labels,
                    datasets: [{ label: 'Sign-ups', data: stats.signupsOverTime.values,
                                 backgroundColor: '#0d9488', borderRadius: 6 }],
                  }}
                  options={{ responsive: true, plugins: { legend: { display: false } },
                             scales: { y: { beginAtZero: true, ticks: { precision: 0 } } } }}
                  height={160}
                />
              </div>
            </div>
          </>
        )}

        {/* ================= USERS ================= */}
        {tab === 'Users' && (
          <>
            <input value={search} onChange={e => setSearch(e.target.value)}
              aria-label="Search users" className="input-field mb-4" placeholder="Search by name or email…" />
            <div className="card">
              <ul className="divide-y divide-slate-100">
                {users.map(u => (
                  <li key={u.id} className="flex flex-wrap items-center gap-3 py-3 first:pt-0 last:pb-0">
                    <Avatar name={u.fullName} size="md" />
                    <div className="flex-1 min-w-[160px]">
                      <div className="text-sm font-semibold text-slate-900">{u.fullName}</div>
                      <div className="text-xs text-slate-500">{u.email}</div>
                    </div>
                    <span className="text-xs text-slate-500">⭐ {u.reputationPoints} · 💡 {u.ideaCount}</span>
                    {/* The only route by which a privileged role is granted */}
                    <select value={u.role} onChange={e => setRole(u.id, e.target.value)}
                      aria-label={`Role for ${u.fullName}`}
                      className="text-xs border border-slate-200 rounded-lg px-2 py-1.5 bg-white">
                      {ALL_ROLES.map(r => <option key={r} value={r}>{r}</option>)}
                    </select>
                  </li>
                ))}
              </ul>
            </div>
          </>
        )}

        {/* ================= MODERATION (F20) ================= */}
        {tab === 'Moderation' && (
          <>
            <div className="flex gap-1 bg-slate-100 p-1 rounded-lg mb-4 w-fit">
              {['Pending', 'Dismissed', 'ActionTaken', 'All'].map(s => (
                <button key={s} onClick={() => setReportFilter(s)}
                  className={`px-3 py-1.5 rounded-md text-xs font-semibold transition ${
                    reportFilter === s ? 'bg-white text-brand-700 shadow-sm' : 'text-slate-500'}`}>
                  {s === 'ActionTaken' ? 'Actioned' : s}
                </button>
              ))}
            </div>

            {reports.length === 0 ? (
              <EmptyState icon="✅" title="Queue is clear"
              message="No reports with this status." />
            ) : (
              <ul className="space-y-3">
                {reports.map(r => (
                  <li key={r.id} className="card">
                    <div className="flex flex-wrap items-start gap-3 mb-3">
                      <span className="text-[10px] font-bold uppercase bg-slate-100 text-slate-600 px-2 py-0.5 rounded-full">
                        {r.targetType}
                      </span>
                      {/* AI verdict shown alongside the human report */}
                      {r.aiVerdict && (
                        <span className={`text-[10px] font-bold uppercase px-2 py-0.5 rounded-full ${
                          r.aiVerdict === 'Unsafe' ? 'bg-red-100 text-red-700'
                          : r.aiVerdict === 'Review' ? 'bg-amber-100 text-amber-700'
                          : 'bg-emerald-100 text-emerald-700'}`}>
                          AI: {r.aiVerdict}
                        </span>
                      )}
                      <span className={`text-[10px] font-bold uppercase px-2 py-0.5 rounded-full ml-auto ${
                        r.status === 'Pending' ? 'bg-amber-100 text-amber-700'
                        : r.status === 'ActionTaken' ? 'bg-red-100 text-red-700'
                        : 'bg-slate-100 text-slate-600'}`}>
                        {r.status}
                      </span>
                    </div>

                    <p className="text-sm font-semibold text-slate-900 mb-1">{r.targetPreview}</p>
                    <p className="text-sm text-slate-600 mb-1">Reason: {r.reason}</p>
                    {r.aiReason && <p className="text-xs text-slate-500 italic mb-1">AI note: {r.aiReason}</p>}
                    <p className="text-xs text-slate-400 mb-3">Raised by {r.reporterName} · {r.timeAgo}</p>

                    {r.status === 'Pending' && (
                      <div className="flex gap-2">
                        <button onClick={() => resolve(r.id, 'dismiss')} className="btn-ghost !py-2 !px-4 text-xs">
                          Dismiss
                        </button>
                        <button onClick={() => resolve(r.id, 'remove')}
                          className="text-xs font-semibold text-white bg-red-600 hover:bg-red-700 px-4 py-2 rounded-lg transition">
                          Remove content
                        </button>
                      </div>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
    </PageShell>
  )
}
