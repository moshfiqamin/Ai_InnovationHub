// ============================================================
// MODULE : M7 — Community
// LAYER  : View (MVC: V)
// FEATURE: F5 — Community Discussion & Comments
// IMPLEMENTS: create/join communities, topic discovery, posts,
//             comments and replies, upvotes, member list.
// ============================================================
import { useEffect, useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import Navbar from '../components/Navbar'
import { communityApi } from '../services/moduleApi'
import { moderationApi } from '../services/moduleApi'
import { describeError } from '../services/api'
import Banner from '../components/Banner'
import Avatar from '../components/Avatar'
import PageShell from '../components/PageShell'
import EmptyState from '../components/EmptyState'
import Skeleton from '../components/Skeleton'

// ---------- LIST VIEW ----------
export function Communities() {
  const [items, setItems] = useState([])
  const [categories, setCategories] = useState([])
  const [category, setCategory] = useState('All')
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ name: '', description: '', category: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  async function load() {
    setLoading(true)
    try { setItems(await communityApi.list(category)) }
    catch (err) { setError(describeError(err, 'Could not load communities.')) }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, [category])
  useEffect(() => { communityApi.categories().then(setCategories).catch(() => {}) }, [])

  async function create(e) {
    e.preventDefault()
    if (form.name.trim().length < 3) { setError('Name must be at least 3 characters.'); return }
    try {
      await communityApi.create(form)
      setForm({ name: '', description: '', category: '' }); setShowForm(false); setError('')
      await load()
    } catch (err) { setError(describeError(err, 'Could not create that community.')) }
  }

  async function toggleJoin(id) {
    try { await communityApi.toggleJoin(id); await load() }
    catch (err) { setError(describeError(err, 'Could not update membership.')) }
  }

  return (
    <PageShell
      title="Communities"
      subtitle="Find people working on the problems you care about."
      action={
<button onClick={() => setShowForm(!showForm)} className="btn-primary !py-2.5">
            {showForm ? 'Cancel' : '+ New community'}
          </button>
      }
    >
        <Banner>{error}</Banner>

        {showForm && (
          <form onSubmit={create} className="card mb-6 space-y-4 animate-fade-up">
            <div>
              <label htmlFor="cname" className="label-text">Name</label>
              <input id="cname" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="input-field" placeholder="e.g. Climate Innovators" />
            </div>
            <div className="grid sm:grid-cols-2 gap-4">
              <div>
                <label htmlFor="ccat" className="label-text">Category</label>
                <input id="ccat" value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })}
                  className="input-field" placeholder="Sustainability" />
              </div>
              <div>
                <label htmlFor="cdesc" className="label-text">Description</label>
                <input id="cdesc" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })}
                  className="input-field" placeholder="What is this community for?" />
              </div>
            </div>
            <button type="submit" className="btn-primary">Create community</button>
          </form>
        )}

        {/* ---- Topic discovery ---- */}
        <div className="flex flex-wrap gap-2 mb-6">
          {['All', ...categories].map(c => (
            <button key={c} onClick={() => setCategory(c)}
              className={`px-3.5 py-1.5 rounded-full text-sm font-semibold border transition ${
                category === c ? 'bg-brand-600 text-white border-brand-600'
                               : 'bg-white text-slate-600 border-slate-200 hover:border-brand-300'}`}>
              {c}
            </button>
          ))}
        </div>

        {loading ? (
          <Skeleton count={4} height="h-36" cols="grid gap-4 sm:grid-cols-2" />
        ) : items.length === 0 ? (
          <EmptyState icon="💬" title="No communities yet"
              message="Create the first one." />
        ) : (
          <div className="grid gap-4 sm:grid-cols-2">
            {items.map(c => (
              <div key={c.id} className="card card-hover">
                <div className="flex items-start justify-between gap-3 mb-2">
                  <Link to={`/communities/${c.id}`} className="font-bold text-slate-900 hover:text-brand-600">
                    {c.name}
                  </Link>
                  {c.category && (
                    <span className="text-[11px] font-semibold text-brand-700 bg-brand-50 border border-brand-100 px-2 py-0.5 rounded-full shrink-0">
                      {c.category}
                    </span>
                  )}
                </div>
                <p className="text-sm text-slate-600 mb-4 line-clamp-2">{c.description || 'No description.'}</p>
                <div className="flex items-center justify-between text-xs text-slate-500">
                  <span>👥 {c.memberCount} · 📝 {c.postCount}</span>
                  <button onClick={() => toggleJoin(c.id)}
                    className={`font-semibold px-3 py-1.5 rounded-lg transition ${
                      c.joinedByMe ? 'text-slate-600 bg-slate-100 hover:bg-slate-200'
                                   : 'text-white bg-brand-600 hover:bg-brand-700'}`}>
                    {c.joinedByMe ? 'Leave' : 'Join'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
    </PageShell>
  )
}

// ---------- DETAIL VIEW ----------
export function CommunityDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const [community, setCommunity] = useState(null)
  const [posts, setPosts] = useState([])
  const [members, setMembers] = useState([])
  const [form, setForm] = useState({ title: '', content: '' })
  const [replyTo, setReplyTo] = useState(null)
  const [replyText, setReplyText] = useState('')
  const [error, setError] = useState('')

  async function load() {
    try {
      const [c, p, m] = await Promise.all([
        communityApi.get(id), communityApi.posts(id), communityApi.members(id),
      ])
      setCommunity(c); setPosts(p); setMembers(m)
    } catch (err) { setError(describeError(err, 'Could not load this community.')) }
  }
  useEffect(() => { load() }, [id])

  async function createPost(e) {
    e.preventDefault()
    if (form.title.trim().length < 3 || form.content.trim().length < 5) {
      setError('Give your post a title (3+ chars) and some content (5+ chars).'); return
    }
    try {
      await communityApi.createPost(id, form)
      setForm({ title: '', content: '' }); setError(''); await load()
    } catch (err) { setError(describeError(err, 'Could not create that post.')) }
  }

  async function upvote(postId) {
    try { await communityApi.upvotePost(postId); await load() }
    catch (err) { setError(describeError(err, 'Could not upvote.')) }
  }

  async function comment(postId, e) {
    e.preventDefault()
    if (!replyText.trim()) return
    try {
      await communityApi.commentPost(postId, replyText.trim())
      setReplyText(''); setReplyTo(null); await load()
    } catch (err) { setError(describeError(err, 'Could not post that comment.')) }
  }

  async function report(postId, title) {
    const reason = window.prompt(`Report "${title}" — why?`)
    if (!reason || reason.trim().length < 5) return
    try {
      await moderationApi.report('Post', postId, reason.trim())
      alert('Reported. An administrator will review it.')
    } catch (err) { setError(describeError(err, 'Could not submit that report.')) }
  }

  if (!community) {
    return (
      <div className="min-h-screen bg-slate-50"><Navbar />
        <div className="max-w-4xl mx-auto px-6 py-16 text-slate-400">{error || 'Loading…'}</div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar />
      <div className="max-w-4xl mx-auto px-6 py-10">
        <Link to="/communities" className="text-sm text-brand-600 font-medium hover:underline mb-4 inline-block">
          ← All communities
        </Link>

        <header className="card mb-6">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h1 className="text-3xl font-extrabold text-slate-900">{community.name}</h1>
              <p className="text-slate-600 mt-1.5">{community.description}</p>
              <p className="text-sm text-slate-500 mt-2">
                👥 {community.memberCount} members · 📝 {community.postCount} posts
              </p>
            </div>
            <button onClick={async () => { await communityApi.toggleJoin(id); await load() }}
              className={community.joinedByMe ? 'btn-ghost !py-2 !px-4 text-sm' : 'btn-primary !py-2 !px-4 text-sm'}>
              {community.joinedByMe ? 'Leave' : 'Join community'}
            </button>
          </div>
        </header>

        <Banner>{error}</Banner>

        {/* ---- New post (members only) ---- */}
        {community.joinedByMe ? (
          <form onSubmit={createPost} className="card mb-6 space-y-3">
            <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })}
              aria-label="Post title" className="input-field" placeholder="Post title" />
            <textarea rows={3} value={form.content} onChange={(e) => setForm({ ...form, content: e.target.value })}
              aria-label="Post content" className="input-field resize-y" placeholder="Share something with this community…" />
            <button type="submit" className="btn-primary">Post</button>
          </form>
        ) : (
          <div className="card mb-6 text-center text-sm text-slate-500 py-6">
            Join this community to start posting.
          </div>
        )}

        {/* ---- Posts ---- */}
        {posts.length === 0 ? (
          <div className="card text-center py-12 text-sm text-slate-500">No posts yet.</div>
        ) : (
          <div className="space-y-4">
            {posts.map(p => (
              <article key={p.id} className={`card ${p.isFlagged ? 'border-amber-300 bg-amber-50/30' : ''}`}>
                {p.isFlagged && (
                  <div className="text-[11px] font-bold uppercase tracking-wide text-amber-700 mb-2">
                    ⚠ Flagged by AI moderation — awaiting review
                  </div>
                )}
                <h3 className="font-bold text-slate-900 mb-1">{p.title}</h3>
                <p className="text-xs text-slate-500 mb-3">{p.authorName} · {p.timeAgo}</p>
                <p className="text-sm text-slate-700 leading-relaxed whitespace-pre-line mb-4">{p.content}</p>

                <div className="flex items-center gap-1 pt-3 border-t border-slate-100">
                  <button onClick={() => upvote(p.id)} aria-pressed={p.upvotedByMe}
                    className={`flex items-center gap-1.5 text-sm font-medium px-2.5 py-1.5 rounded-lg transition ${
                      p.upvotedByMe ? 'text-emerald-600 bg-emerald-50' : 'text-slate-500 hover:bg-slate-100'}`}>
                    ▲ {p.upvotes}
                  </button>
                  <button onClick={() => { setReplyTo(replyTo === p.id ? null : p.id); setReplyText('') }}
                    className="text-sm text-slate-500 hover:bg-slate-100 px-2.5 py-1.5 rounded-lg transition">
                    💬 {p.commentCount}
                  </button>
                  <button onClick={() => report(p.id, p.title)}
                    className="ml-auto text-xs text-slate-400 hover:text-red-600 px-2 py-1.5 rounded transition">
                    Report
                  </button>
                </div>

                {replyTo === p.id && (
                  <form onSubmit={(e) => comment(p.id, e)} className="flex gap-2 mt-3">
                    <input value={replyText} onChange={(e) => setReplyText(e.target.value)}
                      aria-label="Your comment" className="input-field flex-1" placeholder="Write a comment…" autoFocus />
                    <button type="submit" className="btn-primary !px-4">Send</button>
                  </form>
                )}

                {p.comments?.length > 0 && (
                  <ul className="mt-4 space-y-3 pl-4 border-l-2 border-slate-100">
                    {p.comments.map(c => (
                      <li key={c.id} className="text-sm">
                        <span className="font-semibold text-slate-800">{c.authorName}</span>
                        <span className="text-slate-400 text-xs ml-2">{c.timeAgo}</span>
                        <p className="text-slate-700 mt-0.5">{c.content}</p>
                      </li>
                    ))}
                  </ul>
                )}
              </article>
            ))}
          </div>
        )}

        {/* ---- Member list ---- */}
        <section className="card mt-6">
          <h2 className="font-bold text-slate-900 mb-4">Members ({members.length})</h2>
          <div className="flex flex-wrap gap-2">
            {members.map(m => (
              <Link key={m.userId} to={`/profile/${m.userId}`}
                className="flex items-center gap-2 bg-slate-50 border border-slate-200 rounded-full pl-1 pr-3 py-1 hover:border-brand-300 transition">
                <Avatar name={m.fullName} size="xs" />
                <span className="text-xs font-medium text-slate-700">{m.fullName}</span>
              </Link>
            ))}
          </div>
        </section>
      </div>
    </div>
  )
}
