// ============================================================
// MODULE : M9 — Innovation Challenges
// LAYER  : View (MVC: V)
// FEATURE: F14 — Innovation Challenges
// IMPLEMENTS: challenge list/detail, join, submission form, submission
//   status, judging/score view, leaderboard, deadlines.
// ============================================================
import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import Navbar from '../components/Navbar'
import { challengeApi } from '../services/moduleApi'
import { ideaApi } from '../services/ideaApi'
import { useAuth } from '../context/AuthContext'
import { describeError } from '../services/api'
import Banner from '../components/Banner'
import PageShell from '../components/PageShell'
import EmptyState from '../components/EmptyState'
import Tabs from '../components/Tabs'

const ORGANISER_ROLES = ['Organization', 'Admin']

// ---------- LIST ----------
export function Challenges() {
  const { user } = useAuth()
  const [items, setItems] = useState([])
  const [status, setStatus] = useState('All')
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ title: '', description: '', category: '', prize: '', deadline: '' })
  const [error, setError] = useState('')

  const canCreate = ORGANISER_ROLES.includes(user?.role)

  async function load() {
    try { setItems(await challengeApi.list(status)) }
    catch (err) { setError(describeError(err, 'Could not load challenges.')) }
  }
  useEffect(() => { load() }, [status])

  async function create(e) {
    e.preventDefault()
    try {
      await challengeApi.create({ ...form, deadline: new Date(form.deadline).toISOString() })
      setForm({ title: '', description: '', category: '', prize: '', deadline: '' })
      setShowForm(false); setError(''); await load()
    } catch (err) { setError(describeError(err, 'Could not create that challenge.')) }
  }

  return (
    <PageShell
      title="Innovation Challenges"
      subtitle="Compete, submit ideas, climb the leaderboard."
      // Only Organization and Admin accounts may run challenges (M9)
      action={canCreate && (
        <button onClick={() => setShowForm(!showForm)} className="btn-primary !py-2.5">
          {showForm ? 'Cancel' : '+ New challenge'}
        </button>
      )}
    >
        <Banner>{error}</Banner>

        {!canCreate && (
          <p className="text-xs text-slate-500 mb-6">
            Only <strong>Organization</strong> and <strong>Admin</strong> accounts can create challenges.
            Your role is <strong>{user?.role}</strong>.
          </p>
        )}

        {showForm && canCreate && (
          <form onSubmit={create} className="card mb-6 space-y-4 animate-fade-up">
            <input value={form.title} onChange={e => setForm({ ...form, title: e.target.value })}
              aria-label="Challenge title" className="input-field" placeholder="Challenge title" />
            <textarea rows={3} value={form.description} onChange={e => setForm({ ...form, description: e.target.value })}
              aria-label="Description" className="input-field resize-y" placeholder="What are you asking people to solve?" />
            <div className="grid sm:grid-cols-3 gap-4">
              <input value={form.category} onChange={e => setForm({ ...form, category: e.target.value })}
                aria-label="Category" className="input-field" placeholder="Category" />
              <input value={form.prize} onChange={e => setForm({ ...form, prize: e.target.value })}
                aria-label="Prize" className="input-field" placeholder="Prize" />
              <input type="date" value={form.deadline} onChange={e => setForm({ ...form, deadline: e.target.value })}
                aria-label="Deadline" className="input-field" />
            </div>
            <button type="submit" className="btn-primary">Create challenge</button>
          </form>
        )}

        <Tabs items={['All', 'Open', 'Closed']} value={status} onChange={setStatus} className="mb-6" />
        {items.length === 0 ? (
          <EmptyState icon="🏆" title="No challenges yet"
              message="Check back soon." />
        ) : (
          <div className="grid gap-4 sm:grid-cols-2">
            {items.map(c => (
              <Link key={c.id} to={`/challenges/${c.id}`} className="card card-hover block">
                <div className="flex items-start justify-between gap-3 mb-2">
                  <h3 className="font-bold text-slate-900 leading-snug">{c.title}</h3>
                  <span className={`text-[10px] font-bold uppercase px-2 py-0.5 rounded-full shrink-0 ${
                    c.status === 'Open' ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-200 text-slate-600'}`}>
                    {c.status}
                  </span>
                </div>
                <p className="text-sm text-slate-600 mb-3 line-clamp-2">{c.description}</p>
                <div className="flex flex-wrap items-center gap-3 text-xs text-slate-500">
                  {c.prize && <span>🎁 {c.prize}</span>}
                  <span>📝 {c.submissionCount} entries</span>
                  <span className={c.daysLeft <= 3 && c.status === 'Open' ? 'text-red-600 font-semibold' : ''}>
                    ⏳ {c.status === 'Open' ? `${c.daysLeft} days left` : 'closed'}
                  </span>
                  {c.joinedByMe && <span className="text-brand-600 font-semibold">✓ entered</span>}
                </div>
              </Link>
            ))}
          </div>
        )}
    </PageShell>
  )
}

// ---------- DETAIL + LEADERBOARD ----------
export function ChallengeDetail() {
  const { id } = useParams()
  const [challenge, setChallenge] = useState(null)
  const [submissions, setSubmissions] = useState([])
  const [myIdeas, setMyIdeas] = useState([])
  const [chosenIdea, setChosenIdea] = useState('')
  const [scoring, setScoring] = useState(null)
  const [scoreForm, setScoreForm] = useState({ score: 80, feedback: '' })
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  async function load() {
    try {
      const [c, s] = await Promise.all([challengeApi.get(id), challengeApi.submissions(id)])
      setChallenge(c); setSubmissions(s)
    } catch (err) { setError(describeError(err, 'Could not load this challenge.')) }
  }
  useEffect(() => { load(); ideaApi.mine().then(setMyIdeas).catch(() => {}) }, [id])

  async function submit(e) {
    e.preventDefault()
    if (!chosenIdea) { setError('Pick one of your ideas to submit.'); return }
    try {
      await challengeApi.submit(id, chosenIdea)
      setChosenIdea(''); setError(''); setNotice('Entry submitted.')
      setTimeout(() => setNotice(''), 2500); await load()
    } catch (err) { setError(describeError(err, 'Could not submit that entry.')) }
  }

  async function saveScore(e) {
    e.preventDefault()
    try {
      await challengeApi.score(scoring, scoreForm)
      setScoring(null); setError(''); await load()
    } catch (err) { setError(describeError(err, 'Could not save that score.')) }
  }

  if (!challenge) {
    return <div className="min-h-screen bg-slate-50"><Navbar />
      <div className="max-w-4xl mx-auto px-6 py-16 text-slate-400">{error || 'Loading…'}</div></div>
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar />
      <div className="max-w-4xl mx-auto px-6 py-10">
        <Link to="/challenges" className="text-sm text-brand-600 font-medium hover:underline mb-4 inline-block">
          ← All challenges
        </Link>

        <header className="card mb-6">
          <div className="flex flex-wrap items-start justify-between gap-4 mb-3">
            <h1 className="text-3xl font-extrabold text-slate-900">{challenge.title}</h1>
            <span className={`text-xs font-bold uppercase px-3 py-1 rounded-full ${
              challenge.status === 'Open' ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-200 text-slate-600'}`}>
              {challenge.status}
            </span>
          </div>
          <p className="text-slate-700 leading-relaxed mb-4 whitespace-pre-line">{challenge.description}</p>
          <div className="flex flex-wrap gap-4 text-sm text-slate-500">
            <span>🎁 {challenge.prize || 'No prize listed'}</span>
            <span>📅 Deadline {new Date(challenge.deadline).toLocaleDateString()}</span>
            <span>👤 {challenge.createdByName}</span>
            <span>📝 {challenge.submissionCount} entries</span>
          </div>
        </header>

        <Banner tone="success">{notice}</Banner>
        <Banner>{error}</Banner>

        {/* ---- Submission form ---- */}
        {challenge.status === 'Open' && (
          <form onSubmit={submit} className="card mb-6 flex flex-wrap gap-3 items-end">
            <div className="flex-1 min-w-[240px]">
              <label htmlFor="pick" className="label-text">Submit one of your ideas</label>
              <select id="pick" value={chosenIdea} onChange={e => setChosenIdea(e.target.value)} className="input-field">
                <option value="">Choose an idea…</option>
                {myIdeas.map(i => <option key={i.id} value={i.id}>{i.title}</option>)}
              </select>
            </div>
            <button type="submit" className="btn-primary !py-3">Enter challenge</button>
          </form>
        )}

        {/* ---- Leaderboard ---- */}
        <section className="card">
          <h2 className="font-bold text-slate-900 mb-4">Leaderboard ({submissions.length})</h2>
          {submissions.length === 0 ? (
            <p className="text-sm text-slate-500 py-6 text-center">No entries yet. Be the first.</p>
          ) : (
            <ul className="space-y-3">
              {submissions.map(s => (
                <li key={s.id} className="flex flex-wrap items-center gap-3 pb-3 border-b border-slate-100 last:border-0 last:pb-0">
                  <span className={`stat-number w-7 h-7 shrink-0 rounded-lg grid place-items-center text-xs ${
                    s.rank === 1 ? 'bg-amber-100 text-amber-700'
                    : s.rank === 2 ? 'bg-slate-200 text-slate-600'
                    : s.rank === 3 ? 'bg-orange-100 text-orange-700'
                    : 'bg-slate-100 text-slate-400'}`}>
                    {s.rank}
                  </span>
                  <div className="flex-1 min-w-[160px]">
                    <Link to={`/ideas/${s.ideaId}`} className="text-sm font-semibold text-slate-900 hover:text-brand-600">
                      {s.ideaTitle}
                    </Link>
                    <div className="text-xs text-slate-500">{s.userName}</div>
                    {s.feedback && <p className="text-xs text-slate-600 mt-1 italic">"{s.feedback}"</p>}
                  </div>

                  {s.score != null ? (
                    <span className="stat-number text-sm text-brand-700 bg-brand-50 px-2.5 py-1 rounded-full">
                      {s.score}/100
                    </span>
                  ) : (
                    <span className="text-xs text-slate-400">unscored</span>
                  )}

                  {/* Judging is organiser-only (enforced again server-side) */}
                  {challenge.canManage && (
                    <button onClick={() => { setScoring(s.id); setScoreForm({ score: s.score ?? 80, feedback: s.feedback ?? '' }) }}
                      className="text-xs font-semibold text-brand-600 hover:bg-brand-50 px-2.5 py-1.5 rounded transition">
                      {s.score != null ? 'Re-score' : 'Score'}
                    </button>
                  )}
                </li>
              ))}
            </ul>
          )}

          {scoring && (
            <form onSubmit={saveScore} className="mt-5 pt-5 border-t border-slate-200 flex flex-wrap gap-3 items-end animate-fade-up">
              <div>
                <label htmlFor="sc" className="label-text">Score (0-100)</label>
                <input id="sc" type="number" min={0} max={100} value={scoreForm.score}
                  onChange={e => setScoreForm({ ...scoreForm, score: Number(e.target.value) })}
                  className="input-field w-28" />
              </div>
              <div className="flex-1 min-w-[200px]">
                <label htmlFor="fb" className="label-text">Feedback</label>
                <input id="fb" value={scoreForm.feedback}
                  onChange={e => setScoreForm({ ...scoreForm, feedback: e.target.value })}
                  className="input-field" placeholder="Short comment for the entrant" />
              </div>
              <button type="submit" className="btn-primary !py-3">Save</button>
              <button type="button" onClick={() => setScoring(null)} className="btn-ghost !py-3">Cancel</button>
            </form>
          )}
        </section>
      </div>
    </div>
  )
}
