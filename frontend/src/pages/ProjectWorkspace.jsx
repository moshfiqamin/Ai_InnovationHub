// ============================================================
// MODULE : M6 — Project Collaboration
// LAYER  : View (MVC: V)
// FEATURES:
//   F7  Team Formation        — invite, change role, remove
//   F8  Project Workspace     — overview + milestones
//   F9  Task Management       — create, move between columns, delete
//   F10 File & Resource Share — upload, download, delete
// ============================================================
import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import Navbar from '../components/Navbar'
import { projectApi } from '../services/projectApi'
import { describeError } from '../services/api'
import Banner from '../components/Banner'
import Avatar from '../components/Avatar'

const COLUMNS = [
  { key: 'Todo',       label: 'To do',       tone: 'bg-slate-100 text-slate-600' },
  { key: 'InProgress', label: 'In progress', tone: 'bg-sky-100 text-sky-700' },
  { key: 'Done',       label: 'Done',        tone: 'bg-emerald-100 text-emerald-700' },
]
const TABS = ['Tasks', 'Team', 'Milestones', 'Files']

export default function ProjectWorkspace() {
  const { id } = useParams()
  const [ws, setWs] = useState(null)
  const [tab, setTab] = useState('Tasks')
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  // form state per feature
  const [task, setTask] = useState({ title: '', assigneeId: '', priority: 'Medium', dueDate: '' })
  const [invite, setInvite] = useState({ email: '', projectRole: 'Contributor' })
  const [milestone, setMilestone] = useState({ title: '', dueDate: '' })
  const [uploading, setUploading] = useState(false)

  async function load() {
    try { setWs(await projectApi.workspace(id)); setError('') }
    catch (err) { setError(describeError(err, 'This project could not be loaded.')) }
  }
  useEffect(() => { load() }, [id])

  function flash(msg) { setNotice(msg); setTimeout(() => setNotice(''), 2500) }

  // ---- F9: create a task ----
  async function createTask(e) {
    e.preventDefault()
    if (task.title.trim().length < 2) { setError('Task title must be at least 2 characters.'); return }
    try {
      await projectApi.createTask(id, {
        title: task.title, description: '',
        assigneeId: task.assigneeId || null,
        priority: task.priority,
        dueDate: task.dueDate || null,
      })
      setTask({ title: '', assigneeId: '', priority: 'Medium', dueDate: '' })
      await load(); flash('Task created')
    } catch (err) { setError(describeError(err, 'Could not create the task.')) }
  }

  // ---- F9: move a task between columns ----
  async function moveTask(taskId, status) {
    try { await projectApi.setTaskStatus(taskId, status); await load() }
    catch (err) { setError(describeError(err, 'Could not move that task.')) }
  }

  async function deleteTask(taskId) {
    if (!window.confirm('Delete this task?')) return
    try { await projectApi.deleteTask(taskId); await load() }
    catch (err) { setError(describeError(err, 'Could not delete that task.')) }
  }

  // ---- F7: invite a team member ----
  async function sendInvite(e) {
    e.preventDefault()
    try {
      await projectApi.invite(id, invite.email.trim(), invite.projectRole)
      setInvite({ email: '', projectRole: 'Contributor' })
      await load(); flash('Invitation sent')
    } catch (err) { setError(describeError(err, 'Could not send that invitation.')) }
  }

  async function changeRole(userId, role) {
    try { await projectApi.changeRole(id, userId, role); await load(); flash('Role updated') }
    catch (err) { setError(describeError(err, 'Could not change that role.')) }
  }

  async function removeMember(userId) {
    if (!window.confirm('Remove this member from the project?')) return
    try { await projectApi.removeMember(id, userId); await load() }
    catch (err) { setError(describeError(err, 'Could not remove that member.')) }
  }

  // ---- F8: milestones ----
  async function addMilestone(e) {
    e.preventDefault()
    if (milestone.title.trim().length < 2) { setError('Milestone title is too short.'); return }
    try {
      await projectApi.createMilestone(id, { title: milestone.title, dueDate: milestone.dueDate || null })
      setMilestone({ title: '', dueDate: '' }); await load(); flash('Milestone added')
    } catch (err) { setError(describeError(err, 'Could not add that milestone.')) }
  }

  async function toggleMilestone(msId) {
    try { await projectApi.toggleMilestone(msId); await load() }
    catch (err) { setError(describeError(err, 'Could not update that milestone.')) }
  }

  // ---- F10: files ----
  async function upload(e) {
    const file = e.target.files?.[0]
    if (!file) return
    setUploading(true)
    try { await projectApi.uploadFile(id, file); await load(); flash(`Uploaded ${file.name}`) }
    catch (err) { setError(describeError(err, 'Upload failed. Check the file type and size.')) }
    finally { setUploading(false); e.target.value = '' }   // allow re-picking the same file
  }

  async function download(f) {
    try { await projectApi.downloadFile(f.id, f.fileName) }
    catch (err) { setError(describeError(err, 'Could not download that file.')) }
  }

  async function deleteFile(fileId) {
    if (!window.confirm('Delete this file permanently?')) return
    try { await projectApi.deleteFile(fileId); await load() }
    catch (err) { setError(describeError(err, 'Could not delete that file.')) }
  }

  if (error && !ws) {
    return (
      <div className="min-h-screen bg-slate-50">
        <Navbar />
        <div className="max-w-3xl mx-auto px-6 py-16 text-center">
          <p className="text-slate-600 mb-4">{error}</p>
          <Link to="/projects" className="btn-primary">Back to projects</Link>
        </div>
      </div>
    )
  }
  if (!ws) {
    return (
      <div className="min-h-screen bg-slate-50">
        <Navbar />
        <div className="max-w-5xl mx-auto px-6 py-16 text-slate-400">Loading workspace…</div>
      </div>
    )
  }

  const activeMembers = ws.members.filter(m => m.status === 'Active')

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar />
      <div className="max-w-5xl mx-auto px-6 py-10">

        <Link to="/projects" className="text-sm text-brand-600 font-medium hover:underline mb-4 inline-block">
          ← All projects
        </Link>

        {/* ================= HEADER ================= */}
        <header className="card mb-6">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h1 className="text-3xl font-extrabold text-slate-900">{ws.title}</h1>
              <p className="text-slate-600 mt-1.5 max-w-2xl">{ws.description || 'No description yet.'}</p>
              <p className="text-sm text-slate-500 mt-2">
                Owner: {ws.ownerName} · Your role: <span className="font-semibold text-brand-700">{ws.myRole}</span>
              </p>
              {/* Link back to the idea this project grew from (M5 -> M6) */}
              {ws.sourceIdeaId && (
                <Link to={`/ideas/${ws.sourceIdeaId}`}
                  className="inline-block mt-2 text-xs text-brand-600 hover:underline">
                  ↩ Grew from the idea "{ws.sourceIdeaTitle}"
                </Link>
              )}
            </div>
            <span className="text-xs font-bold uppercase tracking-wide text-slate-600 bg-slate-100 px-3 py-1 rounded-full">
              {ws.status}
            </span>
          </div>
        </header>

        {notice && (
          <div className="mb-4 bg-emerald-50 border border-emerald-200 text-emerald-700 text-sm rounded-xl px-4 py-2.5">
            {notice}
          </div>
        )}
        <Banner>{error}</Banner>

        {/* ================= TABS ================= */}
        <div className="flex gap-1 bg-slate-100 p-1 rounded-lg mb-6 w-fit">
          {TABS.map(t => (
            <button key={t} onClick={() => setTab(t)}
              className={`px-4 py-2 rounded-md text-sm font-semibold transition ${
                tab === t ? 'bg-white text-brand-700 shadow-sm' : 'text-slate-500 hover:text-slate-700'}`}>
              {t}
              {t === 'Tasks' && ` (${ws.tasks.length})`}
              {t === 'Team' && ` (${ws.members.length})`}
              {t === 'Files' && ` (${ws.files.length})`}
            </button>
          ))}
        </div>

        {/* ================= F9 : TASKS ================= */}
        {tab === 'Tasks' && (
          <>
            {ws.myRole !== 'Viewer' && (
              <form onSubmit={createTask} className="card mb-6 flex flex-wrap gap-3 items-end">
                <div className="flex-1 min-w-[200px]">
                  <label htmlFor="tt" className="label-text">New task</label>
                  <input id="tt" value={task.title} onChange={(e) => setTask({ ...task, title: e.target.value })}
                    className="input-field" placeholder="What needs doing?" />
                </div>
                <div>
                  <label htmlFor="ta" className="label-text">Assignee</label>
                  <select id="ta" value={task.assigneeId}
                    onChange={(e) => setTask({ ...task, assigneeId: e.target.value })} className="input-field">
                    <option value="">Unassigned</option>
                    {activeMembers.map(m => <option key={m.userId} value={m.userId}>{m.fullName}</option>)}
                  </select>
                </div>
                <div>
                  <label htmlFor="tp" className="label-text">Priority</label>
                  <select id="tp" value={task.priority}
                    onChange={(e) => setTask({ ...task, priority: e.target.value })} className="input-field">
                    <option>Low</option><option>Medium</option><option>High</option>
                  </select>
                </div>
                <div>
                  <label htmlFor="td" className="label-text">Due</label>
                  <input id="td" type="date" value={task.dueDate}
                    onChange={(e) => setTask({ ...task, dueDate: e.target.value })} className="input-field" />
                </div>
                <button type="submit" className="btn-primary !py-3">Add</button>
              </form>
            )}

            {/* Three-column board */}
            <div className="grid md:grid-cols-3 gap-4">
              {COLUMNS.map(col => {
                const items = ws.tasks.filter(t => t.status === col.key)
                return (
                  <div key={col.key} className="bg-white rounded-2xl border border-slate-200 p-4">
                    <div className="flex items-center justify-between mb-4">
                      <h3 className="font-bold text-slate-900 text-sm">{col.label}</h3>
                      <span className={`text-xs font-bold px-2 py-0.5 rounded-full ${col.tone}`}>{items.length}</span>
                    </div>

                    {items.length === 0 ? (
                      <p className="text-xs text-slate-400 py-6 text-center">Nothing here</p>
                    ) : (
                      <ul className="space-y-2.5">
                        {items.map(t => (
                          <li key={t.id} className="rounded-xl border border-slate-200 p-3 hover:border-brand-300 transition">
                            <div className="text-sm font-semibold text-slate-900 mb-1.5">{t.title}</div>
                            <div className="flex flex-wrap items-center gap-1.5 mb-2">
                              <span className={`text-[10px] font-bold uppercase px-1.5 py-0.5 rounded ${
                                t.priority === 'High' ? 'bg-red-100 text-red-700'
                                : t.priority === 'Low' ? 'bg-slate-100 text-slate-500'
                                : 'bg-amber-100 text-amber-700'}`}>{t.priority}</span>
                              {t.assigneeName && (
                                <span className="text-[11px] text-slate-500">👤 {t.assigneeName}</span>
                              )}
                              {t.dueDate && (
                                <span className={`text-[11px] ${t.isOverdue ? 'text-red-600 font-semibold' : 'text-slate-500'}`}>
                                  {t.isOverdue ? '⚠ overdue' : '📅'} {new Date(t.dueDate).toLocaleDateString()}
                                </span>
                              )}
                            </div>

                            {ws.myRole !== 'Viewer' && (
                              <div className="flex gap-1 flex-wrap">
                                {COLUMNS.filter(c => c.key !== t.status).map(c => (
                                  <button key={c.key} onClick={() => moveTask(t.id, c.key)}
                                    className="text-[11px] font-medium text-brand-600 hover:bg-brand-50 px-2 py-1 rounded transition">
                                    → {c.label}
                                  </button>
                                ))}
                                {ws.canManage && (
                                  <button onClick={() => deleteTask(t.id)}
                                    className="text-[11px] font-medium text-red-500 hover:bg-red-50 px-2 py-1 rounded ml-auto transition">
                                    Delete
                                  </button>
                                )}
                              </div>
                            )}
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                )
              })}
            </div>
          </>
        )}

        {/* ================= F7 : TEAM ================= */}
        {tab === 'Team' && (
          <>
            {ws.canManage && (
              <form onSubmit={sendInvite} className="card mb-6 flex flex-wrap gap-3 items-end">
                <div className="flex-1 min-w-[220px]">
                  <label htmlFor="ie" className="label-text">Invite by email</label>
                  <input id="ie" type="email" value={invite.email}
                    onChange={(e) => setInvite({ ...invite, email: e.target.value })}
                    className="input-field" placeholder="teammate@example.com" />
                </div>
                <div>
                  <label htmlFor="ir" className="label-text">Role</label>
                  <select id="ir" value={invite.projectRole}
                    onChange={(e) => setInvite({ ...invite, projectRole: e.target.value })} className="input-field">
                    <option>Maintainer</option><option>Contributor</option><option>Viewer</option>
                  </select>
                </div>
                <button type="submit" className="btn-primary !py-3">Send invite</button>
              </form>
            )}

            <div className="card">
              <ul className="divide-y divide-slate-100">
                {ws.members.map(m => (
                  <li key={m.userId} className="flex flex-wrap items-center gap-3 py-3 first:pt-0 last:pb-0">
                    <Avatar name={m.fullName} size="md" />
                    <div className="flex-1 min-w-[140px]">
                      <div className="text-sm font-semibold text-slate-900">{m.fullName}</div>
                      <div className="text-xs text-slate-500">{m.email}</div>
                    </div>

                    {m.status === 'Invited' && (
                      <span className="text-[10px] font-bold uppercase text-amber-700 bg-amber-50 border border-amber-200 px-2 py-0.5 rounded-full">
                        Pending
                      </span>
                    )}

                    {/* The owner's role is fixed and cannot be edited */}
                    {ws.canManage && m.projectRole !== 'Owner' ? (
                      <>
                        <select value={m.projectRole} onChange={(e) => changeRole(m.userId, e.target.value)}
                          aria-label={`Role for ${m.fullName}`}
                          className="text-xs border border-slate-200 rounded-lg px-2 py-1.5 bg-white">
                          <option>Maintainer</option><option>Contributor</option><option>Viewer</option>
                        </select>
                        <button onClick={() => removeMember(m.userId)}
                          className="text-xs font-medium text-red-500 hover:bg-red-50 px-2 py-1.5 rounded transition">
                          Remove
                        </button>
                      </>
                    ) : (
                      <span className="text-xs font-bold text-brand-700 bg-brand-50 px-2.5 py-1 rounded-full">
                        {m.projectRole}
                      </span>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          </>
        )}

        {/* ================= F8 : MILESTONES ================= */}
        {tab === 'Milestones' && (
          <>
            {ws.canManage && (
              <form onSubmit={addMilestone} className="card mb-6 flex flex-wrap gap-3 items-end">
                <div className="flex-1 min-w-[200px]">
                  <label htmlFor="mt" className="label-text">New milestone</label>
                  <input id="mt" value={milestone.title}
                    onChange={(e) => setMilestone({ ...milestone, title: e.target.value })}
                    className="input-field" placeholder="e.g. Working prototype" />
                </div>
                <div>
                  <label htmlFor="md" className="label-text">Target date</label>
                  <input id="md" type="date" value={milestone.dueDate}
                    onChange={(e) => setMilestone({ ...milestone, dueDate: e.target.value })} className="input-field" />
                </div>
                <button type="submit" className="btn-primary !py-3">Add</button>
              </form>
            )}

            <div className="card">
              {ws.milestones.length === 0 ? (
                <p className="text-sm text-slate-500 py-6 text-center">No milestones yet.</p>
              ) : (
                <ul className="space-y-3">
                  {ws.milestones.map(m => (
                    <li key={m.id} className="flex items-center gap-3">
                      <button onClick={() => ws.canManage && toggleMilestone(m.id)}
                        disabled={!ws.canManage}
                        aria-label={m.isCompleted ? 'Mark incomplete' : 'Mark complete'}
                        className={`w-5 h-5 rounded-md border-2 grid place-items-center text-xs transition ${
                          m.isCompleted ? 'bg-brand-600 border-brand-600 text-white'
                                        : 'border-slate-300 hover:border-brand-400'}`}>
                        {m.isCompleted && '✓'}
                      </button>
                      <span className={`flex-1 text-sm ${m.isCompleted ? 'line-through text-slate-400' : 'text-slate-800 font-medium'}`}>
                        {m.title}
                      </span>
                      {m.dueDate && (
                        <span className="text-xs text-slate-500">{new Date(m.dueDate).toLocaleDateString()}</span>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </>
        )}

        {/* ================= F10 : FILES ================= */}
        {tab === 'Files' && (
          <>
            {ws.myRole !== 'Viewer' && (
              <div className="card mb-6">
                <label htmlFor="fu" className="label-text">Upload a resource</label>
                <input id="fu" type="file" onChange={upload} disabled={uploading}
                  className="block w-full text-sm text-slate-600
                             file:mr-4 file:py-2.5 file:px-5 file:rounded-lg file:border-0
                             file:text-sm file:font-semibold file:bg-brand-600 file:text-white
                             hover:file:bg-brand-700 file:cursor-pointer" />
                <p className="text-xs text-slate-400 mt-2">
                  Max 10 MB. Documents, images, spreadsheets, archives and text files.
                </p>
                {uploading && <p className="text-sm text-brand-600 mt-2">Uploading…</p>}
              </div>
            )}

            <div className="card">
              {ws.files.length === 0 ? (
                <p className="text-sm text-slate-500 py-6 text-center">No files shared yet.</p>
              ) : (
                <ul className="divide-y divide-slate-100">
                  {ws.files.map(f => (
                    <li key={f.id} className="flex items-center gap-3 py-3 first:pt-0 last:pb-0">
                      <span className="text-2xl shrink-0" aria-hidden="true">📄</span>
                      <div className="flex-1 min-w-0">
                        <div className="text-sm font-semibold text-slate-900 truncate">{f.fileName}</div>
                        <div className="text-xs text-slate-500">
                          {f.sizeLabel} · {f.uploadedByName} · {new Date(f.uploadedAt).toLocaleDateString()}
                        </div>
                      </div>
                      <button onClick={() => download(f)}
                        className="text-xs font-semibold text-brand-600 hover:bg-brand-50 px-3 py-1.5 rounded transition">
                        Download
                      </button>
                      <button onClick={() => deleteFile(f.id)}
                        className="text-xs font-medium text-red-500 hover:bg-red-50 px-2 py-1.5 rounded transition">
                        Delete
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  )
}
