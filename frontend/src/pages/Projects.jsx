// ============================================================
// MODULE : M6 — Project Collaboration
// LAYER  : View (MVC: V)
// FEATURE: F8 — Project Workspace (the project list + creation)
//          F7 — accepting a pending team invitation
// ============================================================
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import Navbar from '../components/Navbar'
import { projectApi } from '../services/projectApi'
import { describeError } from '../services/api'
import Banner from '../components/Banner'
import PageShell from '../components/PageShell'
import EmptyState from '../components/EmptyState'
import Skeleton from '../components/Skeleton'

export default function Projects() {
  const [projects, setProjects] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ title: '', description: '' })
  const [saving, setSaving] = useState(false)

  async function load() {
    setLoading(true)
    try { setProjects(await projectApi.mine()) }
    catch (err) { setError(describeError(err, 'Could not load your projects.')) }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, [])

  // ---- F8: create a project ----
  async function create(e) {
    e.preventDefault()
    if (form.title.trim().length < 3) { setError('Project title must be at least 3 characters.'); return }
    setSaving(true)
    try {
      await projectApi.create(form)
      setForm({ title: '', description: '' }); setShowForm(false); setError('')
      await load()
    } catch (err) { setError(describeError(err, 'Could not create the project.')) }
    finally { setSaving(false) }
  }

  // ---- F7: accept a pending invitation ----
  async function accept(id) {
    try { await projectApi.accept(id); await load() }
    catch (err) { setError(describeError(err, 'Could not accept the invitation.')) }
  }

  return (
    <PageShell
      title="Projects"
      subtitle="Turn ideas into work with a team behind them."
      action={
<button onClick={() => setShowForm(!showForm)} className="btn-primary !py-2.5">
            {showForm ? 'Cancel' : '+ New project'}
          </button>
      }
    >
        <Banner>{error}</Banner>

        {/* ---- CREATE FORM ---- */}
        {showForm && (
          <form onSubmit={create} className="card mb-6 space-y-4 animate-fade-up">
            <div>
              <label htmlFor="ptitle" className="label-text">Project title</label>
              <input id="ptitle" value={form.title}
                onChange={(e) => setForm({ ...form, title: e.target.value })}
                className="input-field" placeholder="What are you building?" />
            </div>
            <div>
              <label htmlFor="pdesc" className="label-text">Description</label>
              <textarea id="pdesc" rows={3} value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                className="input-field resize-y" placeholder="A short summary of the goal." />
            </div>
            <button type="submit" disabled={saving} className="btn-primary">
              {saving ? 'Creating…' : 'Create project'}
            </button>
            <p className="text-xs text-slate-400">
              Tip: you can also start a project directly from one of your ideas.
            </p>
          </form>
        )}

        {/* ---- PROJECT LIST ---- */}
        {loading ? (
          <Skeleton count={4} height="h-40" cols="grid gap-4 sm:grid-cols-2" />
        ) : projects.length === 0 ? (
          <EmptyState icon="🚀" title="No projects yet"
              message={'Create one here, or open one of your ideas and choose \"Start a project\".'}
              action={<Link to="/feed" className="btn-ghost">Browse ideas</Link>} />
        ) : (
          <div className="grid gap-4 sm:grid-cols-2">
            {projects.map(p => {
              const pending = p.myRole === 'Invited'
              const pct = p.taskCount ? Math.round((p.completedTaskCount / p.taskCount) * 100) : 0
              return (
                <div key={p.id} className={`card ${pending ? 'border-amber-300 bg-amber-50/40' : 'card-hover'}`}>
                  <div className="flex items-start justify-between gap-3 mb-2">
                    <h3 className="font-bold text-slate-900 leading-snug">{p.title}</h3>
                    <span className="text-[10px] font-bold uppercase tracking-wide text-brand-700 bg-brand-50 border border-brand-100 px-2 py-0.5 rounded-full shrink-0">
                      {p.myRole}
                    </span>
                  </div>

                  <p className="text-sm text-slate-600 leading-relaxed mb-4 line-clamp-2">
                    {p.description || 'No description yet.'}
                  </p>

                  {/* ---- F7: pending invitation ---- */}
                  {pending ? (
                    <button onClick={() => accept(p.id)} className="btn-primary !py-2 !px-4 text-sm w-full">
                      Accept invitation
                    </button>
                  ) : (
                    <>
                      {/* ---- F9: task progress ---- */}
                      <div className="mb-3">
                        <div className="flex justify-between text-xs text-slate-500 mb-1.5">
                          <span>{p.completedTaskCount} of {p.taskCount} tasks done</span>
                          <span className="stat-number">{pct}%</span>
                        </div>
                        <div className="h-1.5 bg-slate-100 rounded-full overflow-hidden">
                          <div className="h-full bg-brand-600 rounded-full transition-all"
                               style={{ width: `${pct}%` }} />
                        </div>
                      </div>

                      <div className="flex items-center justify-between text-xs text-slate-500">
                        <span>👥 {p.memberCount} member{p.memberCount === 1 ? '' : 's'}</span>
                        <Link to={`/projects/${p.id}`} className="text-brand-600 font-semibold hover:underline">
                          Open workspace →
                        </Link>
                      </div>
                    </>
                  )}
                </div>
              )
            })}
          </div>
        )}
    </PageShell>
  )
}
