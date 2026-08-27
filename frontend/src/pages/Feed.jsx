// ============================================================
// MODULE : M4 — Innovation Feed
// LAYER  : View (MVC: V)
// FEATURE: F4 — Innovation Feed
// IMPLEMENTS (per requirements.pdf M4):
//   latest/trending feed · idea cards · like/upvote · comment entry
//   points · bookmark/save · share · feed filtering
// ============================================================
import { useEffect, useState, useCallback } from 'react'
import { Link } from 'react-router-dom'
import Navbar from '../components/Navbar'
import IdeaCard from '../components/IdeaCard'
import { feedApi } from '../services/ideaApi'
import Banner from '../components/Banner'
import EmptyState from '../components/EmptyState'
import Skeleton from '../components/Skeleton'

const SORTS = [
  { value: 'latest',    label: 'Latest',    hint: 'Newest first' },
  { value: 'trending',  label: 'Trending',  hint: 'Most upvoted' },
  { value: 'discussed', label: 'Discussed', hint: 'Most comments' },
]

export default function Feed() {
  const [ideas, setIdeas] = useState([])
  const [categories, setCategories] = useState([])
  const [sort, setSort] = useState('latest')
  const [category, setCategory] = useState('All')
  const [search, setSearch] = useState('')
  const [showSaved, setShowSaved] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  // ---- LOAD THE FEED ----
  // useCallback keeps this stable so the effect below does not loop.
  const load = useCallback(async () => {
    setLoading(true); setError('')
    try {
      const data = showSaved
        ? await feedApi.bookmarks()
        : await feedApi.list({ sort, category, search: search || undefined })
      setIdeas(data)
    } catch {
      setError('Could not load the feed. Is the backend running?')
    } finally { setLoading(false) }
  }, [sort, category, search, showSaved])

  // Debounced: waits 300ms after typing stops before querying (NFR7).
  useEffect(() => {
    const t = setTimeout(load, search ? 300 : 0)
    return () => clearTimeout(t)
  }, [load, search])

  useEffect(() => { feedApi.categories().then(setCategories).catch(() => {}) }, [])

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar />
      <div className="max-w-5xl mx-auto px-6 py-10">

        {/* ---- HEADER ---- */}
        <header className="flex flex-wrap items-end justify-between gap-4 mb-8">
          <div>
            <h1 className="text-3xl font-extrabold text-slate-900">Innovation Feed</h1>
            <p className="text-slate-500 mt-1">Discover what the community is building.</p>
          </div>
          <Link to="/ideas/new" className="btn-primary !py-2.5">+ Submit an idea</Link>
        </header>

        {/* ---- FILTER BAR (F4 feed filtering) ---- */}
        <div className="card mb-6 space-y-4">
          <div className="flex flex-wrap gap-3 items-center">
            {/* Search */}
            <div className="relative flex-1 min-w-[220px]">
              <input
                type="search" value={search} onChange={(e) => setSearch(e.target.value)}
                placeholder="Search ideas, problems, tags…"
                aria-label="Search ideas"
                className="input-field pl-10"
              />
              <span aria-hidden="true" className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400">⌕</span>
            </div>

            {/* Saved toggle */}
            <button
              onClick={() => setShowSaved(!showSaved)}
              aria-pressed={showSaved}
              className={`px-4 py-3 rounded-xl text-sm font-semibold border transition ${
                showSaved ? 'bg-amber-50 border-amber-300 text-amber-700'
                          : 'bg-white border-slate-200 text-slate-600 hover:border-slate-300'}`}>
              ★ Saved
            </button>
          </div>

          {/* Sort tabs — hidden while viewing bookmarks, which have their own order */}
          {!showSaved && (
            <div className="flex flex-wrap gap-4 items-center">
              <div className="flex gap-1 bg-slate-100 p-1 rounded-lg">
                {SORTS.map(s => (
                  <button key={s.value} onClick={() => setSort(s.value)} title={s.hint}
                    className={`px-3.5 py-1.5 rounded-md text-sm font-semibold transition ${
                      sort === s.value ? 'bg-white text-brand-700 shadow-sm' : 'text-slate-500 hover:text-slate-700'}`}>
                    {s.label}
                  </button>
                ))}
              </div>

              {/* Category filter */}
              <select value={category} onChange={(e) => setCategory(e.target.value)}
                aria-label="Filter by category"
                className="text-sm border border-slate-200 rounded-lg px-3 py-2 bg-white text-slate-700
                           focus:outline-none focus:ring-2 focus:ring-brand-500/20">
                <option value="All">All categories</option>
                {categories.map(c => <option key={c} value={c}>{c}</option>)}
              </select>

              {(category !== 'All' || search) && (
                <button onClick={() => { setCategory('All'); setSearch('') }}
                  className="text-sm text-brand-600 font-medium hover:underline">
                  Clear filters
                </button>
              )}
            </div>
          )}
        </div>

        {/* ---- RESULTS ---- */}
        <Banner>{error}</Banner>

        {loading ? (
          <Skeleton count={4} height="h-48" cols="grid gap-4 sm:grid-cols-2" />
        ) : ideas.length === 0 ? (
          <EmptyState
            icon={showSaved ? '★' : '💡'}
            title={showSaved ? 'Nothing saved yet' : 'No ideas match your filters'}
            message={showSaved
              ? 'Bookmark ideas from the feed to find them here later.'
              : 'Try a different search, or be the first to publish one.'}
            action={!showSaved && <Link to="/ideas/new" className="btn-primary">Submit an idea</Link>}
          />
        ) : (
          <>
            <p className="text-sm text-slate-500 mb-4">
              {ideas.length} idea{ideas.length === 1 ? '' : 's'}
              {showSaved ? ' saved' : category !== 'All' ? ` in ${category}` : ''}
            </p>
            <div className="grid gap-4 sm:grid-cols-2">
              {ideas.map(i => <IdeaCard key={i.id} idea={i} onChange={load} />)}
            </div>
          </>
        )}
      </div>
    </div>
  )
}
