// ============================================================
// MODULE : M5 — Idea Management
// LAYER  : View (MVC: V)
// FEATURE: F1 — Idea Submission System
// IMPLEMENTS: create idea with problem, solution, category and tags;
//             save as draft OR publish straight to the M4 feed.
// NFR    : NFR5 Input Validation, NFR6 Error Handling
// ============================================================
import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import Navbar from '../components/Navbar'
import { ideaApi } from '../services/ideaApi'
import { describeError } from '../services/api'
import Banner from '../components/Banner'

const CATEGORIES = [
  'Sustainability', 'Education', 'Climate', 'HealthTech', 'AgriTech',
  'FinTech', 'Transport', 'Social Impact', 'Productivity', 'Other',
]

export default function IdeaNew() {
  const navigate = useNavigate()
  const [form, setForm] = useState({
    title: '', problem: '', solution: '', category: 'Sustainability', tags: '',
  })
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  function handleChange(e) {
    setForm({ ...form, [e.target.name]: e.target.value })
    setError('')
  }

  // ---- CLIENT-SIDE VALIDATION (NFR5) ----
  // Mirrors the DataAnnotations on IdeaRequest so the user gets
  // feedback without a round trip. The server still re-checks.
  function validate() {
    if (form.title.trim().length < 5)     return 'Title must be at least 5 characters.'
    if (form.problem.trim().length < 10)  return 'Please describe the problem in at least 10 characters.'
    if (form.solution.trim().length < 10) return 'Please describe your solution in at least 10 characters.'
    return ''
  }

  // publish=false saves a private draft, publish=true pushes it live.
  async function submit(publish) {
    const problem = validate()
    if (problem) { setError(problem); return }

    setSaving(true)
    try {
      const res = await ideaApi.create({ ...form, publish })
      navigate(`/ideas/${res.id}`)
    } catch (err) {
      setError(describeError(err, 'Could not save your idea. Please try again.'))
    } finally { setSaving(false) }
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar />
      <div className="max-w-3xl mx-auto px-6 py-10">

        <Link to="/feed" className="text-sm text-brand-600 font-medium hover:underline mb-4 inline-block">
          ← Back to feed
        </Link>

        <h1 className="text-3xl font-extrabold text-slate-900 mb-1">Submit an idea</h1>
        <p className="text-slate-500 mb-8">
          Drafts stay private until you publish them.
        </p>

        <Banner>{error}</Banner>

        <form onSubmit={(e) => { e.preventDefault(); submit(true) }} className="card space-y-5">

          <div>
            <label htmlFor="title" className="label-text">Title</label>
            <input id="title" name="title" value={form.title} onChange={handleChange}
              className="input-field" placeholder="A short, clear name for your idea" />
          </div>

          {/* ---- The structured fields requirements.pdf asks for ---- */}
          <div>
            <label htmlFor="problem" className="label-text">
              The problem <span className="font-normal text-slate-400">— what is broken today?</span>
            </label>
            <textarea id="problem" name="problem" rows={4} value={form.problem} onChange={handleChange}
              className="input-field resize-y" placeholder="Describe the problem and who it affects." />
            <div className="text-xs text-slate-400 mt-1">{form.problem.length} characters</div>
          </div>

          <div>
            <label htmlFor="solution" className="label-text">
              Your solution <span className="font-normal text-slate-400">— what do you propose?</span>
            </label>
            <textarea id="solution" name="solution" rows={4} value={form.solution} onChange={handleChange}
              className="input-field resize-y" placeholder="Explain how your idea addresses the problem." />
            <div className="text-xs text-slate-400 mt-1">{form.solution.length} characters</div>
          </div>

          <div className="grid sm:grid-cols-2 gap-5">
            <div>
              <label htmlFor="category" className="label-text">Category</label>
              <select id="category" name="category" value={form.category} onChange={handleChange}
                className="input-field">
                {CATEGORIES.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
            <div>
              <label htmlFor="tags" className="label-text">
                Tags <span className="font-normal text-slate-400">— comma separated</span>
              </label>
              <input id="tags" name="tags" value={form.tags} onChange={handleChange}
                className="input-field" placeholder="ai, recycling, campus" />
            </div>
          </div>

          {/* ---- DRAFT vs PUBLISH ---- */}
          <div className="flex flex-wrap gap-3 pt-2">
            <button type="submit" disabled={saving} className="btn-primary">
              {saving ? 'Saving…' : 'Publish to feed'}
            </button>
            <button type="button" disabled={saving} onClick={() => submit(false)} className="btn-ghost">
              Save as draft
            </button>
          </div>

          <p className="text-xs text-slate-400">
            After saving you can run AI analysis, generate a SWOT, and see similar ideas.
          </p>
        </form>
      </div>
    </div>
  )
}
