// ============================================================
// MODULE : M10 — Mentor & Investor
// LAYER  : View (MVC: V)
// FEATURES: F13 AI Mentor Recommendation · F15 Investor Connect
// IMPLEMENTS: mentor directory, mentor profiles/expertise, AI mentor
//   suggestions, mentorship request, investor directory, investor/
//   project discovery, funding interest, meeting/request status.
// ============================================================
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import Navbar from '../components/Navbar'
import { engagementApi } from '../services/moduleApi'
import { projectApi } from '../services/projectApi'
import { describeError } from '../services/api'
import Banner from '../components/Banner'
import Avatar from '../components/Avatar'
import EmptyState from '../components/EmptyState'
import PageShell from '../components/PageShell'
import Tabs from '../components/Tabs'

export default function Network({ initialTab = 'Mentors' }) {
  const [tab, setTab] = useState(initialTab)
  const [mentors, setMentors] = useState([])
  const [recommended, setRecommended] = useState(null)
  const [recLoading, setRecLoading] = useState(false)
  const [investors, setInvestors] = useState([])
  const [engagements, setEngagements] = useState([])
  const [projects, setProjects] = useState([])
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [pitch, setPitch] = useState({ projectId: '', message: '', amount: '' })

  function flash(m) { setNotice(m); setTimeout(() => setNotice(''), 2500) }

  async function load() {
    try {
      const [m, i, e] = await Promise.all([
        engagementApi.mentors(search || undefined),
        engagementApi.investors(search || undefined),
        engagementApi.mine(),
      ])
      setMentors(m); setInvestors(i); setEngagements(e)
    } catch (err) { setError(describeError(err, 'Could not load the network.')) }
  }
  useEffect(() => { load() }, [search])
  useEffect(() => { projectApi.mine().then(setProjects).catch(() => {}) }, [])

  // ---- F13: AI mentor recommendations ----
  async function loadRecommendations() {
    setRecLoading(true)
    try { setRecommended(await engagementApi.recommendedMentors()) }
    catch (err) { setError(describeError(err, 'Could not load recommendations.')) }
    finally { setRecLoading(false) }
  }

  async function requestMentorship(mentorId, name) {
    const message = window.prompt(`Message to ${name} — what would you like help with?`)
    if (!message || message.trim().length < 10) {
      if (message !== null) setError('Your message must be at least 10 characters.')
      return
    }
    try { await engagementApi.requestMentorship(mentorId, message.trim()); flash('Request sent.'); await load() }
    catch (err) { setError(describeError(err, 'Could not send that request.')) }
  }

  // ---- F15: register funding interest ----
  async function sendPitch(e) {
    e.preventDefault()
    if (!pitch.projectId) { setError('Choose which project you are pitching.'); return }
    try {
      await engagementApi.expressInterest({
        projectId: pitch.projectId, message: pitch.message,
        amount: pitch.amount ? Number(pitch.amount) : null,
      })
      setPitch({ projectId: '', message: '', amount: '' }); setError(''); flash('Interest registered with investors.')
      await load()
    } catch (err) { setError(describeError(err, 'Could not register that interest.')) }
  }

  async function respond(kind, id, status) {
    try { await engagementApi.respond(kind, id, status); await load(); flash(`Marked ${status.toLowerCase()}.`) }
    catch (err) { setError(describeError(err, 'Could not update that request.')) }
  }

  return (
    <PageShell title="Network" subtitle="Find mentors, connect with investors, track your requests.">
        <Banner tone="success">{notice}</Banner>
        <Banner>{error}</Banner>

        <Tabs items={['Mentors', 'Investors', 'Requests']} value={tab} onChange={setTab} className="mb-6" />
        {/* ================= MENTORS (F13) ================= */}
        {tab === 'Mentors' && (
          <>
            <div className="flex flex-wrap gap-3 mb-6">
              <input value={search} onChange={e => setSearch(e.target.value)}
                aria-label="Search mentors" className="input-field flex-1 min-w-[220px]"
                placeholder="Search by name or expertise…" />
              <button onClick={loadRecommendations} disabled={recLoading} className="btn-primary !py-3">
                {recLoading ? 'Thinking…' : '✨ Suggest mentors for me'}
              </button>
            </div>

            {recommended && (
              <section className="card mb-6 border-brand-200 bg-brand-50/30">
                <h2 className="font-bold text-slate-900 mb-1">Recommended for you</h2>
                <p className="text-xs text-slate-500 mb-4">Matched to your skills, interests and recent ideas.</p>
                {recommended.length === 0 ? (
                  <p className="text-sm text-slate-500">No mentors on the platform yet.</p>
                ) : (
                  <ul className="space-y-3">
                    {recommended.map(m => (
                      <li key={m.userId} className="flex gap-3 bg-white rounded-xl border border-slate-200 p-4">
                        <Avatar name={m.fullName} size="md" />
                        <div className="flex-1">
                          <Link to={`/profile/${m.userId}`} className="font-semibold text-slate-900 hover:text-brand-600">
                            {m.fullName}
                          </Link>
                          <div className="text-xs text-slate-500">{m.expertise || m.headline || 'Mentor'}</div>
                          {m.whyRecommended && (
                            <p className="text-sm text-slate-700 mt-1.5 leading-relaxed">{m.whyRecommended}</p>
                          )}
                        </div>
                        <button onClick={() => requestMentorship(m.userId, m.fullName)}
                          className="btn-ghost !py-2 !px-3 text-xs self-start">Request</button>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            )}

            {mentors.length === 0 ? (
              <EmptyState icon="🎓" title="No mentors yet"
              message="Accounts with the Mentor role appear here." />
            ) : (
              <div className="grid gap-4 sm:grid-cols-2">
                {mentors.map(m => (
                  <div key={m.userId} className="card card-hover">
                    <div className="flex items-start gap-3 mb-3">
                      <Avatar name={m.fullName} size="lg" />
                      <div>
                        <Link to={`/profile/${m.userId}`} className="font-bold text-slate-900 hover:text-brand-600">
                          {m.fullName}
                        </Link>
                        <div className="text-xs text-slate-500">{m.headline || 'Mentor'}</div>
                      </div>
                      {m.isAvailable && (
                        <span className="ml-auto text-[10px] font-bold uppercase text-emerald-700 bg-emerald-50 border border-emerald-200 px-2 py-0.5 rounded-full">
                          Available
                        </span>
                      )}
                    </div>
                    {m.expertise && <p className="text-xs text-brand-700 font-medium mb-1.5">{m.expertise}</p>}
                    <p className="text-sm text-slate-600 line-clamp-2 mb-3">{m.bio || 'No bio yet.'}</p>
                    <div className="flex items-center justify-between">
                      <span className="text-xs text-slate-500">⭐ {m.reputationPoints}</span>
                      <button onClick={() => requestMentorship(m.userId, m.fullName)}
                        className="text-xs font-semibold text-white bg-brand-600 hover:bg-brand-700 px-3 py-1.5 rounded-lg transition">
                        Request mentorship
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </>
        )}

        {/* ================= INVESTORS (F15) ================= */}
        {tab === 'Investors' && (
          <>
            <form onSubmit={sendPitch} className="card mb-6 space-y-4">
              <h2 className="font-bold text-slate-900">Register funding interest</h2>
              <p className="text-xs text-slate-500">
                Pitch one of your projects. Every investor on the platform is notified.
              </p>
              <div className="grid sm:grid-cols-3 gap-3">
                <select value={pitch.projectId} onChange={e => setPitch({ ...pitch, projectId: e.target.value })}
                  aria-label="Project" className="input-field">
                  <option value="">Choose a project…</option>
                  {projects.filter(p => p.myRole === 'Owner').map(p => (
                    <option key={p.id} value={p.id}>{p.title}</option>
                  ))}
                </select>
                <input type="number" min="0" value={pitch.amount}
                  onChange={e => setPitch({ ...pitch, amount: e.target.value })}
                  aria-label="Amount sought" className="input-field" placeholder="Amount sought" />
                <input value={pitch.message} onChange={e => setPitch({ ...pitch, message: e.target.value })}
                  aria-label="Pitch message" className="input-field" placeholder="Short pitch" />
              </div>
              <button type="submit" className="btn-primary">Send to investors</button>
            </form>

            {investors.length === 0 ? (
              <EmptyState icon="💰" title="No investors yet"
              message="Accounts with the Investor role appear here." />
            ) : (
              <div className="grid gap-4 sm:grid-cols-2">
                {investors.map(i => (
                  <div key={i.userId} className="card card-hover">
                    <Link to={`/profile/${i.userId}`} className="font-bold text-slate-900 hover:text-brand-600">
                      {i.fullName}
                    </Link>
                    <div className="text-xs text-slate-500 mb-2">{i.headline || 'Investor'}</div>
                    {i.investmentFocus && (
                      <p className="text-xs text-brand-700 font-medium mb-1.5">Focus: {i.investmentFocus}</p>
                    )}
                    <p className="text-sm text-slate-600 line-clamp-2">{i.bio || 'No bio yet.'}</p>
                  </div>
                ))}
              </div>
            )}
          </>
        )}

        {/* ================= REQUEST STATUS ================= */}
        {tab === 'Requests' && (
          <div className="card">
            {engagements.length === 0 ? (
              <p className="text-sm text-slate-500 py-10 text-center">
                No mentorship or investment requests yet.
              </p>
            ) : (
              <ul className="divide-y divide-slate-100">
                {engagements.map(e => (
                  <li key={e.id} className="py-4 first:pt-0 last:pb-0">
                    <div className="flex flex-wrap items-start gap-3">
                      <span className={`text-[10px] font-bold uppercase px-2 py-0.5 rounded-full shrink-0 ${
                        e.direction === 'Incoming' ? 'bg-sky-100 text-sky-700' : 'bg-slate-100 text-slate-600'}`}>
                        {e.direction}
                      </span>
                      <div className="flex-1 min-w-[180px]">
                        <div className="text-sm font-semibold text-slate-900">
                          {e.subject} · {e.counterpartName}
                        </div>
                        <p className="text-sm text-slate-600 mt-0.5">{e.message}</p>
                        <div className="text-xs text-slate-400 mt-1">
                          {e.timeAgo}{e.amount ? ` · ${e.amount} sought` : ''}
                        </div>
                      </div>
                      <span className={`text-xs font-bold px-2.5 py-1 rounded-full shrink-0 ${
                        e.status === 'Accepted' ? 'bg-emerald-100 text-emerald-700'
                        : e.status === 'Declined' ? 'bg-red-100 text-red-700'
                        : 'bg-amber-100 text-amber-700'}`}>
                        {e.status}
                      </span>

                      {/* Only the receiving side may accept or decline */}
                      {e.direction === 'Incoming' && e.status === 'Pending' && (
                        <div className="flex gap-1">
                          <button onClick={() => respond(e.subject === 'Mentorship' ? 'mentorship' : 'investment', e.id, 'Accepted')}
                            className="text-xs font-semibold text-emerald-700 hover:bg-emerald-50 px-2.5 py-1.5 rounded transition">
                            Accept
                          </button>
                          <button onClick={() => respond(e.subject === 'Mentorship' ? 'mentorship' : 'investment', e.id, 'Declined')}
                            className="text-xs font-semibold text-red-600 hover:bg-red-50 px-2.5 py-1.5 rounded transition">
                            Decline
                          </button>
                        </div>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
    </PageShell>
  )
}
