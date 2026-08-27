// ============================================================
// MODULE : M4 — Innovation Feed
// LAYER  : View (MVC: V)
// FEATURE: F4 — the idea card, with like, bookmark, comment and share
// PURPOSE: One card in the feed. Interactions update optimistically so
//          the UI feels instant, then reconcile with the server reply.
// ============================================================
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { feedApi } from '../services/ideaApi'
import Avatar from './Avatar'

export default function IdeaCard({ idea, onChange }) {
  const [liked, setLiked] = useState(idea.likedByMe)
  const [likes, setLikes] = useState(idea.upvotes)
  const [saved, setSaved] = useState(idea.bookmarkedByMe)
  const [busy, setBusy] = useState(false)
  const [shared, setShared] = useState(false)

  // ---- F4: LIKE / UPVOTE ----
  async function handleLike(e) {
    e.preventDefault()          // the card is wrapped in a <Link>
    if (busy) return
    setBusy(true)

    // Optimistic update, reverted if the request fails.
    const prevLiked = liked, prevLikes = likes
    setLiked(!liked); setLikes(liked ? likes - 1 : likes + 1)

    try {
      const res = await feedApi.toggleLike(idea.id)
      setLiked(res.active); setLikes(res.count)
      onChange?.()
    } catch {
      setLiked(prevLiked); setLikes(prevLikes)
    } finally { setBusy(false) }
  }

  // ---- F4: BOOKMARK / SAVE ----
  async function handleBookmark(e) {
    e.preventDefault()
    if (busy) return
    setBusy(true)
    const prev = saved
    setSaved(!saved)
    try { const res = await feedApi.toggleBookmark(idea.id); setSaved(res.active) }
    catch { setSaved(prev) }
    finally { setBusy(false) }
  }

  // ---- F4: SHARE ----
  // Copies a deep link to the clipboard and confirms inline.
  async function handleShare(e) {
    e.preventDefault()
    const url = `${window.location.origin}/ideas/${idea.id}`
    try {
      await navigator.clipboard.writeText(url)
      setShared(true)
      setTimeout(() => setShared(false), 2000)
    } catch {
      window.prompt('Copy this link:', url)   // clipboard blocked
    }
  }

  return (
    <Link to={`/ideas/${idea.id}`} className="card card-hover block">
      {/* ---- AUTHOR ---- */}
      <div className="flex items-center gap-2.5 mb-3">
        <Avatar name={idea.authorName} size="sm" />
        <div className="leading-tight">
          <div className="text-sm font-semibold text-slate-800">{idea.authorName}</div>
          <div className="text-[11px] text-slate-500">{idea.authorRole} · {idea.timeAgo}</div>
        </div>
      </div>

      {/* ---- BODY ---- */}
      <h3 className="font-bold text-slate-900 mb-1.5 leading-snug">{idea.title}</h3>
      <p className="text-sm text-slate-600 leading-relaxed mb-3 line-clamp-2">{idea.summary}</p>

      {/* ---- CATEGORY + TAGS ---- */}
      <div className="flex flex-wrap items-center gap-1.5 mb-4">
        {idea.category && (
          <span className="text-[11px] font-semibold text-brand-700 bg-brand-50 border border-brand-100 px-2 py-0.5 rounded-full">
            {idea.category}
          </span>
        )}
        {idea.tags?.slice(0, 3).map(t => (
          <span key={t} className="text-[11px] text-slate-500 bg-slate-100 px-2 py-0.5 rounded-full">#{t}</span>
        ))}
      </div>

      {/* ---- ACTION BAR ---- */}
      <div className="flex items-center gap-1 pt-3 border-t border-slate-100">
        <button onClick={handleLike} disabled={busy}
          aria-pressed={liked} aria-label={liked ? 'Remove upvote' : 'Upvote this idea'}
          className={`flex items-center gap-1.5 text-sm font-medium px-2.5 py-1.5 rounded-lg transition ${
            liked ? 'text-emerald-600 bg-emerald-50' : 'text-slate-500 hover:bg-slate-100'}`}>
          <span aria-hidden="true">▲</span>{likes}
        </button>

        <span className="flex items-center gap-1.5 text-sm text-slate-500 px-2.5 py-1.5">
          <span aria-hidden="true">💬</span>{idea.commentCount}
        </span>

        <span className="flex items-center gap-1.5 text-sm text-slate-400 px-2.5 py-1.5">
          <span aria-hidden="true">👁</span>{idea.views}
        </span>

        <div className="ml-auto flex items-center gap-1">
          <button onClick={handleShare} aria-label="Copy link to this idea"
            className="text-sm px-2.5 py-1.5 rounded-lg text-slate-500 hover:bg-slate-100 transition">
            {shared ? <span className="text-emerald-600 font-medium">Copied</span> : '🔗'}
          </button>
          <button onClick={handleBookmark} disabled={busy}
            aria-pressed={saved} aria-label={saved ? 'Remove bookmark' : 'Save this idea'}
            className={`text-sm px-2.5 py-1.5 rounded-lg transition ${
              saved ? 'text-amber-600 bg-amber-50' : 'text-slate-500 hover:bg-slate-100'}`}>
            {saved ? '★' : '☆'}
          </button>
        </div>
      </div>
    </Link>
  )
}
