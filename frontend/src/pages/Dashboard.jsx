// ============================================================
// MODULE : M3 — Dashboard Module
// LAYER  : View (MVC: V)
// FEATURES:
//   F18 — AI Personalized Recommendation (Gemini-generated)
//   F19 — Analytics Dashboard (Chart.js visualisations)
// IMPLEMENTS (per requirements.pdf M3):
//   1. Quick statistics
//   2. AI personalized recommendations   <- F18
//   3. Trending/recommended ideas
//   4. Recent activity
//   5. Innovation/reputation summary
//   6. Shortcuts to ideas, projects, challenges
// NFR    : NFR3 Responsive, NFR6 Error Handling, NFR10 Reliability
// ============================================================
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Chart as ChartJS, CategoryScale, LinearScale, PointElement,
  LineElement, BarElement, ArcElement, Tooltip, Legend, Filler,
} from 'chart.js'
import { Line, Doughnut } from 'react-chartjs-2'
import api from '../services/api'
import { useAuth } from '../context/AuthContext'
import Navbar from '../components/Navbar'

// Chart.js is modular — every scale/element used must be registered once.
ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement,
                BarElement, ArcElement, Tooltip, Legend, Filler)

export default function Dashboard() {
  const { user } = useAuth()

  // ---- PAGE STATE ------------------------------------------
  const [summary, setSummary] = useState(null)   // stats + activity + trending (F19)
  const [recs, setRecs] = useState(null)         // AI recommendations (F18)
  const [recsLoading, setRecsLoading] = useState(true)
  const [error, setError] = useState('')

  // ---- LOAD DASHBOARD SUMMARY (F19) ------------------------
  // Runs once on mount. Calls DashboardController -> GET /api/dashboard/summary
  useEffect(() => {
    api.get('/dashboard/summary')
      .then((res) => setSummary(res.data))
      .catch(() => setError('Could not load dashboard statistics.'))
  }, [])

  // ---- LOAD AI RECOMMENDATIONS (F18) -----------------------
  // Separate request because the AI call is slower than the stats query.
  // Loading them independently keeps the page responsive (NFR7).
  useEffect(() => {
    api.get('/dashboard/recommendations')
      .then((res) => setRecs(res.data))
      .catch(() => setRecs({ items: [], source: 'unavailable' })) // graceful degradation (NFR10)
      .finally(() => setRecsLoading(false))
  }, [])

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 to-white">
      <Navbar />
      <div className="max-w-6xl mx-auto px-6 py-10">

        {/* ---- GREETING + ROLE BADGE ---- */}
        <header className="mb-10 animate-fade-up">
          <div className="flex flex-wrap items-center gap-3 mb-2">
            <h1 className="text-4xl font-extrabold text-slate-900">
              Welcome back, <span className="gradient-text">{user?.fullName?.split(' ')[0]}</span>
            </h1>
            <span className="wave" aria-hidden="true">👋</span>
          </div>
          <div className="flex items-center gap-3">
            <p className="text-slate-500">
              Here is what is happening across your innovation workspace.
            </p>
            {user?.role && (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-brand-50 border border-brand-200 px-3 py-1 text-xs font-bold text-brand-700">
                {user.role}
              </span>
            )}
          </div>
        </header>

        {error && (
          <div role="alert" className="mb-6 bg-amber-50 border border-amber-200 text-amber-800 text-sm rounded-lg px-4 py-3">
            {error}
          </div>
        )}

        {/* ================================================== */}
        {/* SECTION 1 — QUICK STATISTICS (F19)                 */}
        {/* ================================================== */}
        <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4 mb-8">
          {[
            { label: 'Ideas Submitted', value: summary?.stats?.ideasSubmitted,     icon: '💡', from: 'from-amber-400',   to: 'to-orange-500' },
            { label: 'Active Projects', value: summary?.stats?.activeProjects,     icon: '🚀', from: 'from-brand-400',   to: 'to-brand-600' },
            { label: 'Reputation',      value: summary?.stats?.reputationPoints,   icon: '⭐', from: 'from-accent-400',  to: 'to-accent-600' },
            { label: 'Notifications',   value: summary?.stats?.unreadNotifications,icon: '🔔', from: 'from-emerald-400', to: 'to-teal-600' },
          ].map((s, i) => (
            <div key={s.label}
              className="card card-hover relative overflow-hidden animate-fade-up"
              style={{ animationDelay: `${i * 0.06}s` }}>
              {/* Coloured accent bar along the top edge of each tile */}
              <div aria-hidden="true"
                className={`absolute inset-x-0 top-0 h-1 bg-gradient-to-r ${s.from} ${s.to}`} />
              <div className="flex items-start justify-between">
                <div>
                  <div className="text-[11px] uppercase tracking-widest text-slate-500 mb-2 font-semibold">
                    {s.label}
                  </div>
                  {/* '—' placeholder while the request is still in flight */}
                  <div className="stat-number text-4xl text-slate-900">{s.value ?? '—'}</div>
                </div>
                <span className={`w-11 h-11 rounded-xl bg-gradient-to-br ${s.from} ${s.to} grid place-items-center text-xl shadow-lift`}>
                  {s.icon}
                </span>
              </div>
            </div>
          ))}
        </section>

        <div className="grid lg:grid-cols-3 gap-6 mb-8">

          {/* ============================================== */}
          {/* SECTION 2 — ENGAGEMENT CHART (F19)             */}
          {/* ============================================== */}
          <div className="lg:col-span-2 card">
            <h2 className="font-bold text-slate-900 mb-1">Engagement over time</h2>
            <p className="text-xs text-slate-500 mb-5">Views and interactions across your ideas.</p>
            {summary?.engagement ? (
              <Line
                data={{
                  labels: summary.engagement.labels,
                  datasets: [{
                    label: 'Engagement',
                    data: summary.engagement.values,
                    borderColor: '#0d9488',
                    backgroundColor: 'rgba(13,148,136,0.12)',
                    fill: true,
                    tension: 0.35,
                    pointRadius: 3,
                  }],
                }}
                options={{
                  responsive: true,
                  plugins: { legend: { display: false } },
                  scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
                }}
                height={110}
              />
            ) : (
              <div className="h-40 grid place-items-center text-slate-400 text-sm">Loading chart…</div>
            )}
          </div>

          {/* ============================================== */}
          {/* SECTION 3 — REPUTATION SUMMARY (F19)           */}
          {/* ============================================== */}
          <div className="card">
            <h2 className="font-bold text-slate-900 mb-1">Contribution mix</h2>
            <p className="text-xs text-slate-500 mb-5">Where your reputation comes from.</p>
            {summary?.contributionMix ? (
              <Doughnut
                data={{
                  labels: summary.contributionMix.labels,
                  datasets: [{
                    data: summary.contributionMix.values,
                    backgroundColor: ['#0d9488', '#f59e0b', '#0ea5e9', '#8b5cf6', '#ef4444'],
                    borderWidth: 0,
                  }],
                }}
                options={{
                  responsive: true,
                  plugins: { legend: { position: 'bottom', labels: { boxWidth: 12, font: { size: 11 } } } },
                  cutout: '62%',
                }}
              />
            ) : (
              <div className="h-40 grid place-items-center text-slate-400 text-sm">Loading…</div>
            )}
          </div>
        </div>

        {/* ================================================== */}
        {/* SECTION 4 — AI PERSONALIZED RECOMMENDATIONS (F18)  */}
        {/* ================================================== */}
        <section className="card mb-8">
          <div className="flex items-center justify-between mb-1">
            <h2 className="font-bold text-slate-900 flex items-center gap-2">
              <span className="w-7 h-7 rounded-lg bg-gradient-to-br from-brand-500 to-brand-700 text-white grid place-items-center text-xs">✨</span>
              AI recommendations for you
            </h2>
            {/* Shows whether the text came from Gemini or the offline fallback */}
            {recs?.source && (
              // Green when the live model answered, amber when we fell back.
              <span className={`inline-flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-widest px-2.5 py-1 rounded-full border ${
                recs.source === 'gemini'
                  ? 'bg-emerald-50 border-emerald-200 text-emerald-700'
                  : 'bg-amber-50 border-amber-200 text-amber-700'
              }`}>
                <span className={`w-1.5 h-1.5 rounded-full ${
                  recs.source === 'gemini' ? 'bg-emerald-500 animate-pulse' : 'bg-amber-500'}`} />
                {recs.source}
              </span>
            )}
          </div>
          <p className="text-xs text-slate-500 mb-5">
            Generated from your role, skills and recent activity.
          </p>

          {recsLoading ? (
            <div className="space-y-3">
              {/* Skeleton placeholders while the AI call completes */}
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-16 bg-slate-100 rounded-lg animate-pulse" />
              ))}
            </div>
          ) : recs?.items?.length ? (
            <ul className="space-y-3">
              {recs.items.map((r, i) => (
                <li key={i}
                  className="flex gap-4 p-4 rounded-xl border border-slate-200 hover:border-brand-300 hover:bg-brand-50/30 hover:-translate-y-0.5 transition-all duration-200 animate-fade-up"
                  style={{ animationDelay: `${i * 0.08}s` }}>
                  <span className="w-8 h-8 shrink-0 rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white grid place-items-center text-xs font-bold shadow-lift">
                    {i + 1}
                  </span>
                  <div>
                    <div className="font-medium text-slate-900 text-sm mb-0.5">{r.title}</div>
                    <p className="text-sm text-slate-600 leading-relaxed">{r.reason}</p>
                  </div>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-slate-500">
              Recommendations are unavailable right now. Submit an idea to get personalised suggestions.
            </p>
          )}
        </section>

        <div className="grid md:grid-cols-2 gap-6">

          {/* ============================================== */}
          {/* SECTION 5 — TRENDING IDEAS                     */}
          {/* ============================================== */}
          <section className="card">
            <h2 className="font-bold text-slate-900 mb-4">🔥 Trending ideas</h2>
            {summary?.trendingIdeas?.length ? (
              <ul className="space-y-3">
                {summary.trendingIdeas.map((idea, i) => (
                  <li key={idea.id}
                    className="flex items-start gap-3 pb-3 border-b border-slate-100 last:border-0 last:pb-0 group">
                    {/* Top three get a coloured rank chip, the rest stay neutral */}
                    <span className={`stat-number w-6 h-6 shrink-0 rounded-lg grid place-items-center text-[11px] ${
                      i === 0 ? 'bg-amber-100 text-amber-700'
                      : i === 1 ? 'bg-slate-200 text-slate-600'
                      : i === 2 ? 'bg-orange-100 text-orange-700'
                      : 'bg-slate-100 text-slate-400'}`}>
                      {i + 1}
                    </span>
                    <div className="flex-1 min-w-0">
                      <div className="text-sm font-semibold text-slate-900 group-hover:text-brand-600 transition-colors truncate">
                        {idea.title}
                      </div>
                      <div className="text-xs text-slate-500 mt-0.5">{idea.category}</div>
                    </div>
                    <span className="text-xs font-bold text-emerald-600 shrink-0">▲ {idea.upvotes}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-sm text-slate-500">No ideas yet — the feed fills up in M4/M5.</p>
            )}
          </section>

          {/* ============================================== */}
          {/* SECTION 6 — RECENT ACTIVITY                    */}
          {/* ============================================== */}
          <section className="card">
            <h2 className="font-bold text-slate-900 mb-4">Recent activity</h2>
            {summary?.recentActivity?.length ? (
              <ul className="space-y-3">
                {summary.recentActivity.map((a, i) => (
                  <li key={i} className="flex gap-3 text-sm">
                    <span className="text-slate-400 shrink-0">•</span>
                    <div>
                      <span className="text-slate-700">{a.description}</span>
                      <span className="block text-xs text-slate-400 mt-0.5">{a.timeAgo}</span>
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-sm text-slate-500">No activity recorded yet.</p>
            )}
          </section>
        </div>

        {/* ================================================== */}
        {/* SECTION 7 — SHORTCUTS to future modules            */}
        {/* ================================================== */}
        <section className="mt-8">
          <h2 className="font-bold text-slate-900 mb-4">Quick actions</h2>
          <div className="grid gap-4 sm:grid-cols-3">
            {[
              { label: 'Submit an idea',  desc: 'Share a problem worth solving', to: '/ideas/new', ready: true },
              { label: 'Browse the feed', desc: 'See what others are building', to: '/feed',      ready: true },
              { label: 'Your projects',   desc: 'Turn ideas into real work',   to: '/projects',  ready: true },
            ].map((s) => (
              <Link key={s.label} to={s.to} className="card card-hover group block">
                <div className="flex items-center justify-between mb-1">
                  <div className="font-bold text-slate-900 text-sm">{s.label}</div>
                  <span aria-hidden="true"
                    className="text-brand-500 opacity-0 group-hover:opacity-100 group-hover:translate-x-1 transition-all duration-200">
                    →
                  </span>
                </div>
                <div className="text-xs text-slate-500">{s.desc}</div>
                {/* These routes are placeholders until those modules are built */}

              </Link>
            ))}
          </div>
        </section>
      </div>
    </div>
  )
}
