// ============================================================
// MODULE : M5 — Idea Management
// LAYER  : View (MVC: V)
// FEATURES:
//   F1  publish a draft, delete
//   F2  AI Idea Analysis          ("Run AI analysis")
//   F3  AI Similar Idea Detection ("Find similar")
//   F11 AI SWOT Analysis          ("Generate SWOT")
//   F4  comments, like, bookmark  (shared with the feed)
// ============================================================
import { useEffect, useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import Navbar from '../components/Navbar'
import { ideaApi, feedApi } from '../services/ideaApi'
import { describeError } from '../services/api'
import { projectApi } from '../services/projectApi'
import Banner from '../components/Banner'

export default function IdeaDetail() {
  const { id } = useParams()
  const navigate = useNavigate()

  const [idea, setIdea] = useState(null)
  const [error, setError] = useState('')
  const [comment, setComment] = useState('')

  // Each AI feature tracks its own loading + error so one failing
  // does not block the others (NFR10).
  const [analysis, setAnalysis] = useState(null)
  const [analysisState, setAnalysisState] = useState('idle')   // idle|loading|error
  const [swot, setSwot] = useState(null)
  const [swotState, setSwotState] = useState('idle')
  const [similar, setSimilar] = useState(null)
  const [similarState, setSimilarState] = useState('idle')
  const [bizModel, setBizModel] = useState(null)
  const [bizState, setBizState] = useState('idle')

  // ---- LOAD ----
  useEffect(() => {
    ideaApi.get(id)
      .then((d) => {
        setIdea(d)
        setAnalysis(d.aiAnalysis || null)   // show cached AI results
        setSwot(d.swot || null)
      })
      .catch((err) => setError(describeError(err, 'That idea could not be loaded.')))
  }, [id])

  // ---- F2: AI ANALYSIS ----
  async function runAnalysis() {
    setAnalysisState('loading')
    try {
      const res = await ideaApi.analyze(id)
      setAnalysis(res.analysis); setAnalysisState('idle')
    } catch { setAnalysisState('error') }
  }

  // ---- F11: SWOT ----
  async function runSwot() {
    setSwotState('loading')
    try { setSwot(await ideaApi.swot(id)); setSwotState('idle') }
    catch { setSwotState('error') }
  }

  // ---- F3: SIMILAR IDEAS ----
  async function runSimilar() {
    setSimilarState('loading')
    try { setSimilar(await ideaApi.similar(id)); setSimilarState('idle') }
    catch { setSimilarState('error') }
  }

  // ---- F12: AI BUSINESS MODEL GENERATOR (M8) ----
  async function runBusinessModel() {
    setBizState('loading')
    try { setBizModel(await ideaApi.businessModel(id)); setBizState('idle') }
    catch { setBizState('error') }
  }

  // ---- F1: PUBLISH A DRAFT ----
  async function publish() {
    try { await ideaApi.publish(id); setIdea({ ...idea, isPublished: true }) }
    catch (err) { setError(describeError(err, 'Could not publish this idea.')) }
  }

  // ---- F1: DELETE ----
  async function remove() {
    if (!window.confirm('Delete this idea permanently? This cannot be undone.')) return
    try { await ideaApi.remove(id); navigate('/feed') }
    catch (err) { setError(describeError(err, 'Could not delete this idea.')) }
  }

  // ---- F8: PROMOTE TO A PROJECT (bridges M5 -> M6) ----
  async function createProject() {
    try {
      const res = await projectApi.create({
        title: idea.title, description: idea.solution, sourceIdeaId: idea.id,
      })
      navigate(`/projects/${res.id}`)
    } catch (err) { setError(describeError(err, 'Could not create a project from this idea.')) }
  }

  // ---- F4: LIKE / BOOKMARK / COMMENT ----
  async function toggleLike() {
    const res = await feedApi.toggleLike(id)
    setIdea({ ...idea, likedByMe: res.active, upvotes: res.count })
  }
  async function toggleBookmark() {
    const res = await feedApi.toggleBookmark(id)
    setIdea({ ...idea, bookmarkedByMe: res.active })
  }
  async function postComment(e) {
    e.preventDefault()
    if (!comment.trim()) return
    try {
      const created = await feedApi.addComment(id, comment.trim())
      setIdea({ ...idea, comments: [...idea.comments, created], commentCount: idea.commentCount + 1 })
      setComment('')
    } catch (err) { setError(describeError(err, 'Could not post your comment.')) }
  }

  if (error && !idea) {
    return (
      <div className="min-h-screen bg-slate-50">
        <Navbar />
        <div className="max-w-3xl mx-auto px-6 py-16 text-center">
          <p className="text-slate-600 mb-4">{error}</p>
          <Link to="/feed" className="btn-primary">Back to feed</Link>
        </div>
      </div>
    )
  }

  if (!idea) {
    return (
      <div className="min-h-screen bg-slate-50">
        <Navbar />
        <div className="max-w-3xl mx-auto px-6 py-16 text-slate-400">Loading…</div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar />
      <div className="max-w-3xl mx-auto px-6 py-10">

        <Link to="/feed" className="text-sm text-brand-600 font-medium hover:underline mb-4 inline-block">
          ← Back to feed
        </Link>

        <Banner>{error}</Banner>

        {/* ================= HEADER ================= */}
        <div className="card mb-6">
          <div className="flex items-start justify-between gap-4 mb-3">
            <div>
              {!idea.isPublished && (
                <span className="inline-block text-[11px] font-bold uppercase tracking-wide text-amber-700 bg-amber-50 border border-amber-200 px-2 py-0.5 rounded-full mb-2">
                  Draft — only you can see this
                </span>
              )}
              <h1 className="text-3xl font-extrabold text-slate-900 leading-tight">{idea.title}</h1>
              <p className="text-sm text-slate-500 mt-1.5">
                {idea.authorName} · {idea.category} · {idea.views} views
              </p>
            </div>
          </div>

          <div className="flex flex-wrap gap-1.5 mb-4">
            {idea.tags?.map(t => (
              <span key={t} className="text-[11px] text-slate-500 bg-slate-100 px-2 py-0.5 rounded-full">#{t}</span>
            ))}
          </div>

          {/* ---- F4 actions ---- */}
          <div className="flex flex-wrap items-center gap-2 pt-4 border-t border-slate-100">
            <button onClick={toggleLike} aria-pressed={idea.likedByMe}
              className={`flex items-center gap-1.5 text-sm font-semibold px-3 py-2 rounded-lg transition ${
                idea.likedByMe ? 'text-emerald-600 bg-emerald-50' : 'text-slate-600 hover:bg-slate-100'}`}>
              <span aria-hidden="true">▲</span> {idea.upvotes}
            </button>
            <button onClick={toggleBookmark} aria-pressed={idea.bookmarkedByMe}
              className={`text-sm font-semibold px-3 py-2 rounded-lg transition ${
                idea.bookmarkedByMe ? 'text-amber-600 bg-amber-50' : 'text-slate-600 hover:bg-slate-100'}`}>
              {idea.bookmarkedByMe ? '★ Saved' : '☆ Save'}
            </button>

            {/* ---- Author-only controls ---- */}
            {idea.isMine && (
              <div className="ml-auto flex gap-2">
                {!idea.isPublished && (
                  <button onClick={publish} className="btn-primary !py-2 !px-4 text-sm">Publish</button>
                )}
                <button onClick={createProject} className="btn-ghost !py-2 !px-4 text-sm">
                  Start a project
                </button>
                <button onClick={remove}
                  className="text-sm font-semibold px-3 py-2 rounded-lg text-red-600 hover:bg-red-50 transition">
                  Delete
                </button>
              </div>
            )}
          </div>
        </div>

        {/* ================= PROBLEM + SOLUTION ================= */}
        <div className="grid md:grid-cols-2 gap-4 mb-6">
          <section className="card">
            <h2 className="text-xs font-bold uppercase tracking-widest text-slate-500 mb-3">The problem</h2>
            <p className="text-slate-700 leading-relaxed whitespace-pre-line">{idea.problem}</p>
          </section>
          <section className="card">
            <h2 className="text-xs font-bold uppercase tracking-widest text-slate-500 mb-3">The solution</h2>
            <p className="text-slate-700 leading-relaxed whitespace-pre-line">{idea.solution}</p>
          </section>
        </div>

        {/* ================= AI TOOLS (F2, F11, F3) ================= */}
        <section className="card mb-6">
          <h2 className="font-bold text-slate-900 mb-1 flex items-center gap-2">
            <span className="w-7 h-7 rounded-lg bg-brand-600 text-white grid place-items-center text-xs">✨</span>
            AI tools
          </h2>
          <p className="text-xs text-slate-500 mb-4">
            Results are cached after the first run, so re-opening this page is instant.
          </p>

          <div className="flex flex-wrap gap-2">
            <button onClick={runAnalysis} disabled={analysisState === 'loading'} className="btn-ghost !py-2 !px-4 text-sm">
              {analysisState === 'loading' ? 'Analysing…' : analysis ? 'Show analysis' : 'Run AI analysis'}
            </button>
            <button onClick={runSwot} disabled={swotState === 'loading'} className="btn-ghost !py-2 !px-4 text-sm">
              {swotState === 'loading' ? 'Generating…' : swot ? 'Show SWOT' : 'Generate SWOT'}
            </button>
            <button onClick={runSimilar} disabled={similarState === 'loading'} className="btn-ghost !py-2 !px-4 text-sm">
              {similarState === 'loading' ? 'Searching…' : 'Find similar ideas'}
            </button>
            <button onClick={runBusinessModel} disabled={bizState === 'loading'} className="btn-ghost !py-2 !px-4 text-sm">
              {bizState === 'loading' ? 'Generating…' : bizModel ? 'Show business model' : 'Generate business model'}
            </button>
          </div>

          {/* ---- F2 RESULT ---- */}
          {analysisState === 'error' && (
            <p className="mt-4 text-sm text-amber-700 bg-amber-50 border border-amber-200 rounded-lg px-4 py-3">
              AI analysis is unavailable right now (quota or connectivity). Try again shortly.
            </p>
          )}
          {analysis && (
            <div className="mt-5 pt-5 border-t border-slate-100">
              <h3 className="text-xs font-bold uppercase tracking-widest text-slate-500 mb-3">AI analysis</h3>
              {/* Rendered as pre-wrap: the model returns Markdown and we
                  deliberately do not inject HTML (avoids an XSS path). */}
              <div className="text-sm text-slate-700 leading-relaxed whitespace-pre-wrap">{analysis}</div>
            </div>
          )}

          {/* ---- F11 RESULT ---- */}
          {swotState === 'error' && (
            <p className="mt-4 text-sm text-amber-700 bg-amber-50 border border-amber-200 rounded-lg px-4 py-3">
              SWOT generation is unavailable right now. Try again shortly.
            </p>
          )}
          {swot && (
            <div className="mt-5 pt-5 border-t border-slate-100">
              <h3 className="text-xs font-bold uppercase tracking-widest text-slate-500 mb-3">SWOT analysis</h3>
              <div className="grid sm:grid-cols-2 gap-3">
                {/* NOTE: full class strings, not `bg-${tone}-50`. Tailwind scans
                    source text at build time and cannot see interpolated names,
                    so dynamically built classes are never generated. */}
                {[
                  { label: 'Strengths',     items: swot.strengths,     box: 'bg-emerald-50/60 border-emerald-200', text: 'text-emerald-700' },
                  { label: 'Weaknesses',    items: swot.weaknesses,    box: 'bg-red-50/60 border-red-200',         text: 'text-red-700' },
                  { label: 'Opportunities', items: swot.opportunities, box: 'bg-sky-50/60 border-sky-200',         text: 'text-sky-700' },
                  { label: 'Threats',       items: swot.threats,       box: 'bg-amber-50/60 border-amber-200',     text: 'text-amber-700' },
                ].map(({ label, items, box, text }) => (
                  <div key={label} className={`rounded-xl border p-4 ${box}`}>
                    <div className={`text-xs font-bold uppercase tracking-wide ${text} mb-2`}>{label}</div>
                    <ul className="space-y-1.5">
                      {items?.map((s, i) => (
                        <li key={i} className="text-sm text-slate-700 leading-snug flex gap-2">
                          <span className="text-slate-400" aria-hidden="true">•</span>{s}
                        </li>
                      ))}
                    </ul>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* ---- F12 RESULT ---- */}
          {bizState === 'error' && (
            <p className="mt-4 text-sm text-amber-700 bg-amber-50 border border-amber-200 rounded-lg px-4 py-3">
              Business model generation is unavailable right now. Try again shortly.
            </p>
          )}
          {bizModel && (
            <div className="mt-5 pt-5 border-t border-slate-100">
              <h3 className="text-xs font-bold uppercase tracking-widest text-slate-500 mb-3">Business model canvas</h3>
              <div className="rounded-xl border border-brand-200 bg-brand-50/40 p-4 mb-3">
                <div className="text-xs font-bold uppercase text-brand-700 mb-1.5">Value proposition</div>
                <p className="text-sm text-slate-700 leading-relaxed">{bizModel.valueProposition}</p>
              </div>
              <div className="grid sm:grid-cols-2 gap-3">
                {/* Static class strings — Tailwind cannot see interpolated names */}
                {[
                  { label: 'Customer segments', items: bizModel.customerSegments },
                  { label: 'Revenue streams',   items: bizModel.revenueStreams },
                  { label: 'Key resources',     items: bizModel.keyResources },
                  { label: 'Key partners',      items: bizModel.keyPartners },
                  { label: 'Channels',          items: bizModel.channels },
                  { label: 'Cost structure',    items: bizModel.costStructure },
                ].map(({ label, items }) => (
                  <div key={label} className="rounded-xl border border-slate-200 bg-white p-4">
                    <div className="text-xs font-bold uppercase tracking-wide text-slate-600 mb-2">{label}</div>
                    <ul className="space-y-1.5">
                      {items?.map((x, i) => (
                        <li key={i} className="text-sm text-slate-700 leading-snug flex gap-2">
                          <span className="text-slate-400" aria-hidden="true">•</span>{x}
                        </li>
                      ))}
                    </ul>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* ---- F3 RESULT ---- */}
          {similar && (
            <div className="mt-5 pt-5 border-t border-slate-100">
              <h3 className="text-xs font-bold uppercase tracking-widest text-slate-500 mb-3">
                Similar ideas ({similar.length})
              </h3>
              {similar.length === 0 ? (
                <p className="text-sm text-slate-500">
                  No semantically similar ideas found. This looks like an original direction.
                </p>
              ) : (
                <ul className="space-y-2">
                  {similar.map(s => (
                    <li key={s.id}>
                      <Link to={`/ideas/${s.id}`}
                        className="flex items-center justify-between gap-4 p-3 rounded-lg border border-slate-200 hover:border-brand-300 transition">
                        <div>
                          <div className="text-sm font-semibold text-slate-900">{s.title}</div>
                          <div className="text-xs text-slate-500">{s.category} · {s.authorName}</div>
                        </div>
                        {/* Cosine similarity, shown so the user can judge relevance */}
                        <span className="stat-number text-xs text-brand-700 bg-brand-50 px-2 py-1 rounded-full shrink-0">
                          {Math.round(s.similarity * 100)}% match
                        </span>
                      </Link>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </section>

        {/* ================= COMMENTS (F4) ================= */}
        <section className="card">
          <h2 className="font-bold text-slate-900 mb-4">
            Discussion ({idea.comments?.length || 0})
          </h2>

          {idea.isPublished ? (
            <form onSubmit={postComment} className="flex gap-2 mb-6">
              <input value={comment} onChange={(e) => setComment(e.target.value)}
                aria-label="Write a comment"
                placeholder="Share feedback or ask a question…" className="input-field flex-1" />
              <button type="submit" disabled={!comment.trim()} className="btn-primary !px-5 disabled:opacity-50">
                Post
              </button>
            </form>
          ) : (
            <p className="text-sm text-slate-500 mb-6">Publish this idea to open it for discussion.</p>
          )}

          {idea.comments?.length ? (
            <ul className="space-y-4">
              {idea.comments.map(c => (
                <li key={c.id} className="flex gap-3">
                  <span className="w-8 h-8 shrink-0 rounded-full bg-slate-200 text-slate-600 grid place-items-center text-xs font-bold">
                    {c.authorName?.split(' ').map(p => p[0]).slice(0, 2).join('')}
                  </span>
                  <div>
                    <div className="text-sm">
                      <span className="font-semibold text-slate-800">{c.authorName}</span>
                      <span className="text-slate-400 text-xs ml-2">{c.timeAgo}</span>
                    </div>
                    <p className="text-sm text-slate-700 leading-relaxed mt-0.5">{c.content}</p>
                  </div>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-slate-500">No comments yet.</p>
          )}
        </section>
      </div>
    </div>
  )
}
