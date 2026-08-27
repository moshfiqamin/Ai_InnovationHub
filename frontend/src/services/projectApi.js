// ============================================================
// MODULE : M6 — Project Collaboration
// LAYER  : View support — API client
// FEATURES: F7 Team · F8 Workspace · F9 Tasks · F10 Files
// ============================================================
import api from './api'

export const projectApi = {
  // ---- F8: projects and workspace ----
  mine: () => api.get('/projects').then(r => r.data),
  create: (payload) => api.post('/projects', payload).then(r => r.data),
  workspace: (id) => api.get(`/projects/${id}`).then(r => r.data),

  // ---- F7: team formation ----
  invite: (id, email, projectRole) =>
    api.post(`/projects/${id}/invite`, { email, projectRole }).then(r => r.data),
  accept: (id) => api.post(`/projects/${id}/accept`).then(r => r.data),
  changeRole: (id, userId, projectRole) =>
    api.put(`/projects/${id}/members/${userId}`, { projectRole }).then(r => r.data),
  removeMember: (id, userId) =>
    api.delete(`/projects/${id}/members/${userId}`).then(r => r.data),

  // ---- F9: tasks ----
  createTask: (id, payload) => api.post(`/projects/${id}/tasks`, payload).then(r => r.data),
  setTaskStatus: (taskId, status) =>
    api.put(`/projects/tasks/${taskId}/status`, { status }).then(r => r.data),
  deleteTask: (taskId) => api.delete(`/projects/tasks/${taskId}`).then(r => r.data),

  // ---- F8: milestones ----
  createMilestone: (id, payload) =>
    api.post(`/projects/${id}/milestones`, payload).then(r => r.data),
  toggleMilestone: (msId) =>
    api.put(`/projects/milestones/${msId}/toggle`).then(r => r.data),

  // ---- F10: files ----
  // Sent as multipart/form-data rather than JSON because it carries bytes.
  uploadFile: (id, file) => {
    const form = new FormData()
    form.append('file', file)
    return api.post(`/projects/${id}/files`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data)
  },
  // Download needs the JWT, so it goes through axios as a blob rather
  // than a plain <a href> which would arrive unauthenticated.
  downloadFile: (fileId, fileName) =>
    api.get(`/projects/files/${fileId}`, { responseType: 'blob' }).then((r) => {
      const url = URL.createObjectURL(new Blob([r.data]))
      const a = document.createElement('a')
      a.href = url; a.download = fileName
      document.body.appendChild(a); a.click()
      a.remove(); URL.revokeObjectURL(url)
    }),
  deleteFile: (fileId) => api.delete(`/projects/files/${fileId}`).then(r => r.data),
}
