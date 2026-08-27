// ============================================================
// MODULE : M8 — AI Intelligence
// LAYER  : View (MVC: V)
// FEATURE: F6 — AI Smart Search
// PURPOSE: One search box across ideas, projects, people and
//          communities. The badge shows whether the backend answered
//          semantically (embeddings) or fell back to keywords.
// ============================================================
import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import Navbar from '../components/Navbar'
import { searchApi } from '../services/moduleApi'
import { describeError } from '../services/api'
import Banner from '../components/Banner'
import EmptyState from '../components/EmptyState'
import Skeleton from '../components/Skeleton'
import PageShell from '../components/PageShell'

const TYPE_STYLE = {
  Idea:      { icon: '💡', tone: 'bg-amber-100 text-amber-700' },
  Project:   { icon: '🚀', tone: 'bg-brand-100 text-brand-700' },
  User:      { icon: '👤', tone: 'bg-sky-100 text-sky-700' },
  Community: { icon: '💬', tone: 'bg-violet-100 text-violet-700' },
}

export default function Search() {
  const [params, setParams] = useSearchParams()
  const [query, setQuery] = useState(params.get('q') || '')
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function run(q) {
    if (!q.trim()) { setData(null); return }
    setLoading(true); setError('')
    try { setData(await searchApi.search(q)) }
    catch (err) { setError(describeError(err, 'Search failed.')) }
    finally { setLoading(false) }
  }

  // Run once on mount if the URL already carries ?q=
  useEffect(() => { if (params.get('q')) run(params.get('q')) }, [])

  function submit(e) {
    e.preventDefault()
    setParams(query ? { q: query } : {})
    run(query)
  }

  return (
    <PageShell width="max-w-3xl" title="Smart Search" subtitle="Find ideas, projects, people and communities by meaning, not just keywords.">
        <form onSubmit={submit} className="flex gap-2 mb-6">
          <input value={query} onChange={e => setQuery(e.target.value)}
            aria-label="Search query" className="input-field flex-1"
            placeholder="e.g. clean water for rural clinics" autoFocus />
          <button type="submit" className="btn-primary !px-6">Search</button>
        </form>

        <Banner>{error}</Banner>

        {loading && (
          <Skeleton count={3} height="h-16" />
        )}

        {data && !loading && (
          <>
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-slate-500">
                {data.results.length} result{data.results.length === 1 ? '' : 's'} for "{data.query}"
              </p>
              {/* Tells the user (and the examiner) which path answered */}
              <span className={`text-[10px] font-bold uppercase tracking-widest px-2.5 py-1 rounded-full border ${
                data.mode === 'semantic'
                  ? 'bg-emerald-50 border-emerald-200 text-emerald-700'
                  : 'bg-slate-100 border-slate-200 text-slate-600'}`}>
                {data.mode} search
              </span>
            </div>

            {data.results.length === 0 ? (
              <EmptyState icon="🔍" title="Nothing matched"
              message="Try different wording or a broader term." />
            ) : (
              <ul className="space-y-2">
                {data.results.map(r => {
                  const style = TYPE_STYLE[r.type] || TYPE_STYLE.Idea
                  return (
                    <li key={`${r.type}-${r.id}`}>
                      <Link to={r.link}
                        className="flex items-center gap-3 bg-white rounded-xl border border-slate-200 p-4 hover:border-brand-300 hover:-translate-y-0.5 transition-all">
                        <span className="text-xl shrink-0" aria-hidden="true">{style.icon}</span>
                        <div className="flex-1 min-w-0">
                          <div className="text-sm font-semibold text-slate-900 truncate">{r.title}</div>
                          <div className="text-xs text-slate-500">{r.subtitle}</div>
                        </div>
                        <span className={`text-[10px] font-bold uppercase px-2 py-0.5 rounded-full shrink-0 ${style.tone}`}>
                          {r.type}
                        </span>
                        {data.mode === 'semantic' && r.type === 'Idea' && (
                          <span className="stat-number text-[11px] text-slate-400 shrink-0">
                            {Math.round(r.score * 100)}%
                          </span>
                        )}
                      </Link>
                    </li>
                  )
                })}
              </ul>
            )}
          </>
        )}
    </PageShell>
  )
}
