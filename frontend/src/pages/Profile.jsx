// ============================================================
// MODULE : M13 — Profile
// LAYER  : View (MVC: V)
// FEATURE: F16 — Reputation & Badge System
// IMPLEMENTS: bio and profile info, skills/interests, portfolio,
//   achievements, reputation points, badges/levels, activity history.
// ============================================================
import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import Navbar from '../components/Navbar'
import { profileApi } from '../services/moduleApi'
import { describeError } from '../services/api'
import Banner from '../components/Banner'
import Avatar from '../components/Avatar'

export default function Profile() {
  const { id } = useParams()           // absent = my own profile
  const [p, setP] = useState(null)
  const [editing, setEditing] = useState(false)
  const [form, setForm] = useState(null)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  async function load() {
    try {
      const data = id ? await profileApi.get(id) : await profileApi.mine()
      setP(data)
      setForm({
        fullName: data.fullName, bio: data.bio, headline: data.headline,
        location: data.location, website: data.website,
        skills: data.skills, interests: data.interests,
        expertise: data.expertise, investmentFocus: data.investmentFocus,
        isAvailableForMentoring: data.isAvailableForMentoring,
      })
    } catch (err) { setError(describeError(err, 'Could not load this profile.')) }
  }
  useEffect(() => { load() }, [id])

  async function save(e) {
    e.preventDefault()
    try {
      await profileApi.update(form)
      setEditing(false); setError(''); setNotice('Profile saved.')
      setTimeout(() => setNotice(''), 2500)
      await load()
    } catch (err) { setError(describeError(err, 'Could not save your profile.')) }
  }

  if (!p) {
    return <div className="min-h-screen bg-slate-50"><Navbar />
      <div className="max-w-4xl mx-auto px-6 py-16 text-slate-400">{error || 'Loading…'}</div></div>
  }

  const earned = p.badges.filter(b => b.earned)

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar />
      <div className="max-w-4xl mx-auto px-6 py-10">

        <Banner tone="success">{notice}</Banner>
        <Banner>{error}</Banner>

        {/* ================= HEADER ================= */}
        <header className="card mb-6">
          <div className="flex flex-wrap items-start gap-5">
            <Avatar name={p.fullName} size="xl" />
            <div className="flex-1 min-w-[220px]">
              <div className="flex flex-wrap items-center gap-2 mb-1">
                <h1 className="text-3xl font-extrabold text-slate-900">{p.fullName}</h1>
                <span className="text-xs font-bold text-brand-700 bg-brand-50 border border-brand-200 px-2.5 py-1 rounded-full">
                  {p.role}
                </span>
              </div>
              {p.headline && <p className="text-slate-600">{p.headline}</p>}
              <div className="flex flex-wrap gap-3 text-sm text-slate-500 mt-2">
                {p.location && <span>📍 {p.location}</span>}
                {p.website && <a href={p.website} target="_blank" rel="noreferrer"
                  className="text-brand-600 hover:underline">🔗 Website</a>}
              </div>
            </div>

            {/* ---- Reputation + level (F16) ---- */}
            <div className="text-center">
              <div className="stat-number text-3xl text-brand-700">{p.reputationPoints}</div>
              <div className="text-[11px] uppercase tracking-widest text-slate-500">Reputation</div>
              <div className="text-xs font-bold text-amber-600 mt-1">{p.level}</div>
            </div>
          </div>

          {p.bio && <p className="text-slate-700 leading-relaxed mt-5 pt-5 border-t border-slate-100">{p.bio}</p>}

          <div className="flex flex-wrap gap-6 mt-5 pt-5 border-t border-slate-100 text-sm">
            <span><strong className="stat-number">{p.ideaCount}</strong> <span className="text-slate-500">ideas</span></span>
            <span><strong className="stat-number">{p.projectCount}</strong> <span className="text-slate-500">projects</span></span>
            <span><strong className="stat-number">{p.commentCount}</strong> <span className="text-slate-500">comments</span></span>
            <span><strong className="stat-number">{earned.length}</strong> <span className="text-slate-500">badges</span></span>
            {p.isMe && (
              <button onClick={() => setEditing(!editing)} className="ml-auto btn-ghost !py-2 !px-4 text-sm">
                {editing ? 'Cancel' : 'Edit profile'}
              </button>
            )}
          </div>
        </header>

        {/* ================= EDIT FORM ================= */}
        {editing && p.isMe && (
          <form onSubmit={save} className="card mb-6 space-y-4 animate-fade-up">
            <h2 className="font-bold text-slate-900">Edit profile</h2>
            <div className="grid sm:grid-cols-2 gap-4">
              <div>
                <label htmlFor="fn" className="label-text">Full name</label>
                <input id="fn" value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })} className="input-field" />
              </div>
              <div>
                <label htmlFor="hl" className="label-text">Headline</label>
                <input id="hl" value={form.headline} onChange={e => setForm({ ...form, headline: e.target.value })}
                  className="input-field" placeholder="e.g. IoT engineer" />
              </div>
            </div>
            <div>
              <label htmlFor="bio" className="label-text">Bio</label>
              <textarea id="bio" rows={3} value={form.bio} onChange={e => setForm({ ...form, bio: e.target.value })}
                className="input-field resize-y" placeholder="Tell people what you work on." />
            </div>
            <div className="grid sm:grid-cols-2 gap-4">
              <div>
                <label htmlFor="loc" className="label-text">Location</label>
                <input id="loc" value={form.location} onChange={e => setForm({ ...form, location: e.target.value })} className="input-field" />
              </div>
              <div>
                <label htmlFor="web" className="label-text">Website</label>
                <input id="web" value={form.website} onChange={e => setForm({ ...form, website: e.target.value })} className="input-field" />
              </div>
              <div>
                <label htmlFor="sk" className="label-text">Skills</label>
                <input id="sk" value={form.skills} onChange={e => setForm({ ...form, skills: e.target.value })}
                  className="input-field" placeholder="React, IoT, ML" />
              </div>
              <div>
                <label htmlFor="int" className="label-text">Interests</label>
                <input id="int" value={form.interests} onChange={e => setForm({ ...form, interests: e.target.value })}
                  className="input-field" placeholder="Sustainability, EdTech" />
              </div>
            </div>

            {/* Role-specific fields */}
            {p.role === 'Mentor' && (
              <div className="grid sm:grid-cols-2 gap-4 items-end">
                <div>
                  <label htmlFor="exp" className="label-text">Expertise</label>
                  <input id="exp" value={form.expertise} onChange={e => setForm({ ...form, expertise: e.target.value })}
                    className="input-field" placeholder="Hardware prototyping, fundraising" />
                </div>
                <label className="flex items-center gap-2 pb-3">
                  <input type="checkbox" checked={form.isAvailableForMentoring}
                    onChange={e => setForm({ ...form, isAvailableForMentoring: e.target.checked })}
                    className="w-4 h-4 accent-brand-600" />
                  <span className="text-sm text-slate-700">Available for mentoring</span>
                </label>
              </div>
            )}
            {p.role === 'Investor' && (
              <div>
                <label htmlFor="inv" className="label-text">Investment focus</label>
                <input id="inv" value={form.investmentFocus} onChange={e => setForm({ ...form, investmentFocus: e.target.value })}
                  className="input-field" placeholder="Seed-stage climate hardware" />
              </div>
            )}

            <button type="submit" className="btn-primary">Save profile</button>
          </form>
        )}

        {/* ================= SKILLS ================= */}
        {(p.skills || p.interests || p.expertise) && (
          <section className="card mb-6">
            <h2 className="font-bold text-slate-900 mb-3">Skills & interests</h2>
            <div className="flex flex-wrap gap-2">
              {[...p.skills.split(','), ...p.interests.split(','), ...p.expertise.split(',')]
                .map(s => s.trim()).filter(Boolean)
                .map((s, i) => (
                  <span key={i} className="text-xs font-medium text-slate-700 bg-slate-100 px-3 py-1.5 rounded-full">{s}</span>
                ))}
            </div>
          </section>
        )}

        {/* ================= BADGES (F16) ================= */}
        <section className="card mb-6">
          <h2 className="font-bold text-slate-900 mb-1">Achievements</h2>
          <p className="text-xs text-slate-500 mb-5">{earned.length} of {p.badges.length} badges earned.</p>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {p.badges.map(b => (
              <div key={b.code}
                className={`rounded-xl border p-4 transition ${
                  b.earned ? 'border-brand-200 bg-brand-50/40' : 'border-slate-200 bg-slate-50/60 opacity-70'}`}>
                <div className="flex items-start gap-3">
                  <span className={`text-2xl ${b.earned ? '' : 'grayscale'}`} aria-hidden="true">{b.icon}</span>
                  <div className="flex-1 min-w-0">
                    <div className="font-bold text-sm text-slate-900">{b.name}</div>
                    <p className="text-xs text-slate-500 leading-snug mb-2">{b.description}</p>
                    {/* Progress bar toward the threshold */}
                    {!b.earned && (
                      <>
                        <div className="h-1.5 bg-slate-200 rounded-full overflow-hidden">
                          <div className="h-full bg-brand-500 rounded-full"
                               style={{ width: `${Math.min(100, (b.progress / b.threshold) * 100)}%` }} />
                        </div>
                        <div className="text-[10px] text-slate-400 mt-1">{b.progress} / {b.threshold}</div>
                      </>
                    )}
                    {b.earned && <span className="text-[10px] font-bold uppercase text-brand-700">Earned</span>}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* ================= PORTFOLIO + ACTIVITY ================= */}
        <div className="grid md:grid-cols-2 gap-6">
          <section className="card">
            <h2 className="font-bold text-slate-900 mb-4">Portfolio</h2>
            {p.recentIdeas.length === 0 ? (
              <p className="text-sm text-slate-500 py-6 text-center">No ideas yet.</p>
            ) : (
              <ul className="space-y-3">
                {p.recentIdeas.map(i => (
                  <li key={i.id} className="pb-3 border-b border-slate-100 last:border-0 last:pb-0">
                    <Link to={`/ideas/${i.id}`} className="text-sm font-semibold text-slate-900 hover:text-brand-600">
                      {i.title}
                    </Link>
                    <div className="text-xs text-slate-500 mt-0.5">{i.category} · ▲ {i.upvotes}</div>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className="card">
            <h2 className="font-bold text-slate-900 mb-4">Activity history</h2>
            {p.recentActivity.length === 0 ? (
              <p className="text-sm text-slate-500 py-6 text-center">No activity recorded.</p>
            ) : (
              <ul className="space-y-3">
                {p.recentActivity.map((a, i) => (
                  <li key={i} className="flex gap-3 text-sm">
                    <span className="text-slate-400" aria-hidden="true">•</span>
                    <div>
                      <span className="text-slate-700">{a.description}</span>
                      <span className="block text-xs text-slate-400 mt-0.5">{a.timeAgo}</span>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>
      </div>
    </div>
  )
}
