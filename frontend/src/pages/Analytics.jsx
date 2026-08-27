// ============================================================
// MODULE : M11 — Analytics
// LAYER  : View (MVC: V)
// FEATURE: F19 — Analytics Dashboard (detailed view)
// IMPLEMENTS: idea performance, project progress, engagement metrics,
//   reputation/activity trends, challenge statistics, charts + cards.
// ============================================================
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Chart as ChartJS, CategoryScale, LinearScale, PointElement, LineElement,
  BarElement, ArcElement, Tooltip, Legend, Filler,
} from 'chart.js'
import { Line, Bar, Doughnut } from 'react-chartjs-2'
import Navbar from '../components/Navbar'
import { analyticsApi } from '../services/moduleApi'
import { describeError } from '../services/api'
import PageShell from '../components/PageShell'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement,
                BarElement, ArcElement, Tooltip, Legend, Filler)

export default function Analytics() {
  const [data, setData] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    analyticsApi.get().then(setData)
      .catch(err => setError(describeError(err, 'Could not load analytics.')))
  }, [])

  if (error) {
    return <div className="min-h-screen bg-slate-50"><Navbar />
      <div className="max-w-5xl mx-auto px-6 py-16 text-slate-600">{error}</div></div>
  }
  if (!data) {
    return <div className="min-h-screen bg-slate-50"><Navbar />
      <div className="max-w-5xl mx-auto px-6 py-16 text-slate-400">Loading analytics…</div></div>
  }

  return (
    <PageShell title="Analytics" subtitle="Platform activity and your own performance.">
        {/* ---- PLATFORM TOTALS ---- */}
        <h2 className="text-xs font-bold uppercase tracking-widest text-slate-500 mb-3">Platform</h2>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4 mb-8">
          {[
            { label: 'Published ideas', value: data.totalIdeas, icon: '💡' },
            { label: 'Projects',        value: data.totalProjects, icon: '🚀' },
            { label: 'Communities',     value: data.totalCommunities, icon: '💬' },
            { label: 'Challenges',      value: data.totalChallenges, icon: '🏆' },
          ].map(s => (
            <div key={s.label} className="card">
              <div className="flex items-start justify-between">
                <div>
                  <div className="text-[11px] uppercase tracking-widest text-slate-500 mb-2 font-semibold">{s.label}</div>
                  <div className="stat-number text-4xl text-slate-900">{s.value}</div>
                </div>
                <span className="text-2xl" aria-hidden="true">{s.icon}</span>
              </div>
            </div>
          ))}
        </div>

        {/* ---- MY PERFORMANCE ---- */}
        <h2 className="text-xs font-bold uppercase tracking-widest text-slate-500 mb-3">Your performance</h2>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4 mb-8">
          {[
            { label: 'Your ideas',      value: data.myIdeas },
            { label: 'Upvotes received',value: data.myUpvotesReceived },
            { label: 'Comments written',value: data.myComments },
            { label: 'Reputation',      value: data.myReputation },
          ].map(s => (
            <div key={s.label} className="card">
              <div className="text-[11px] uppercase tracking-widest text-slate-500 mb-2 font-semibold">{s.label}</div>
              <div className="stat-number text-3xl text-brand-700">{s.value}</div>
            </div>
          ))}
        </div>

        <div className="grid lg:grid-cols-3 gap-6 mb-8">
          {/* ---- Ideas over time ---- */}
          <div className="lg:col-span-2 card">
            <h3 className="font-bold text-slate-900 mb-1">Ideas published</h3>
            <p className="text-xs text-slate-500 mb-5">Across the platform, last 14 days.</p>
            <Line
              data={{
                labels: data.ideasOverTime.labels,
                datasets: [{
                  label: 'Ideas', data: data.ideasOverTime.values,
                  borderColor: '#0d9488', backgroundColor: 'rgba(13,148,136,0.12)',
                  fill: true, tension: 0.35, pointRadius: 3,
                }],
              }}
              options={{
                responsive: true, plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
              }}
              height={110}
            />
          </div>

          {/* ---- Category breakdown ---- */}
          <div className="card">
            <h3 className="font-bold text-slate-900 mb-1">By category</h3>
            <p className="text-xs text-slate-500 mb-5">Where ideas cluster.</p>
            <Doughnut
              data={{
                labels: data.categoryBreakdown.labels,
                datasets: [{
                  data: data.categoryBreakdown.values,
                  backgroundColor: ['#0d9488','#f59e0b','#0ea5e9','#8b5cf6','#ef4444','#84cc16'],
                  borderWidth: 0,
                }],
              }}
              options={{
                responsive: true, cutout: '60%',
                plugins: { legend: { position: 'bottom', labels: { boxWidth: 12, font: { size: 11 } } } },
              }}
            />
          </div>
        </div>

        <div className="grid md:grid-cols-2 gap-6">
          {/* ---- Engagement mix ---- */}
          <div className="card">
            <h3 className="font-bold text-slate-900 mb-1">Engagement by type</h3>
            <p className="text-xs text-slate-500 mb-5">How people interact platform-wide.</p>
            <Bar
              data={{
                labels: data.engagementByType.labels,
                datasets: [{
                  label: 'Count', data: data.engagementByType.values,
                  backgroundColor: ['#0d9488','#0ea5e9','#f59e0b','#8b5cf6'],
                  borderRadius: 6,
                }],
              }}
              options={{
                responsive: true, plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
              }}
              height={140}
            />
          </div>

          {/* ---- Top ideas ---- */}
          <div className="card">
            <h3 className="font-bold text-slate-900 mb-4">Top ideas</h3>
            {data.topIdeas.length === 0 ? (
              <p className="text-sm text-slate-500 py-6 text-center">No published ideas yet.</p>
            ) : (
              <ul className="space-y-3">
                {data.topIdeas.map((i, idx) => (
                  <li key={i.id} className="flex items-start gap-3">
                    <span className={`stat-number w-6 h-6 shrink-0 rounded-lg grid place-items-center text-[11px] ${
                      idx === 0 ? 'bg-amber-100 text-amber-700'
                      : idx === 1 ? 'bg-slate-200 text-slate-600'
                      : idx === 2 ? 'bg-orange-100 text-orange-700'
                      : 'bg-slate-100 text-slate-400'}`}>{idx + 1}</span>
                    <div className="flex-1 min-w-0">
                      <Link to={`/ideas/${i.id}`} className="text-sm font-semibold text-slate-900 hover:text-brand-600 truncate block">
                        {i.title}
                      </Link>
                      <div className="text-xs text-slate-500">{i.category}</div>
                    </div>
                    <span className="text-xs font-bold text-emerald-600 shrink-0">▲ {i.upvotes}</span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
    </PageShell>
  )
}
