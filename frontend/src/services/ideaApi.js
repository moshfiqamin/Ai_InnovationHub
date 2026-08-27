// ============================================================
// MODULE : M4 Innovation Feed + M5 Idea Management
// LAYER  : View support — API client
// PURPOSE: Every call the feed and idea pages make, in one place, so
//          components never build URLs themselves (NFR19).
// ============================================================
import api from './api'

// ---------- M4 : FEED (F4) ----------
export const feedApi = {
  // sort: 'latest' | 'trending' | 'discussed'
  list: (params = {}) => api.get('/feed', { params }).then(r => r.data),
  categories: () => api.get('/feed/categories').then(r => r.data),
  bookmarks: () => api.get('/feed/bookmarks').then(r => r.data),
  toggleLike: (ideaId) => api.post(`/feed/${ideaId}/like`).then(r => r.data),
  toggleBookmark: (ideaId) => api.post(`/feed/${ideaId}/bookmark`).then(r => r.data),
  addComment: (ideaId, content, parentId = null) =>
    api.post(`/feed/${ideaId}/comments`, { content, parentId }).then(r => r.data),
}

// ---------- M5 : IDEAS (F1, F2, F3, F11) ----------
export const ideaApi = {
  mine: () => api.get('/ideas/mine').then(r => r.data),
  get: (id) => api.get(`/ideas/${id}`).then(r => r.data),
  create: (payload) => api.post('/ideas', payload).then(r => r.data),        // F1
  update: (id, payload) => api.put(`/ideas/${id}`, payload).then(r => r.data),
  publish: (id) => api.post(`/ideas/${id}/publish`).then(r => r.data),
  remove: (id) => api.delete(`/ideas/${id}`).then(r => r.data),
  analyze: (id) => api.post(`/ideas/${id}/analyze`).then(r => r.data),        // F2
  swot: (id) => api.post(`/ideas/${id}/swot`).then(r => r.data),              // F11
  similar: (id) => api.get(`/ideas/${id}/similar`).then(r => r.data),         // F3
  businessModel: (id) => api.post(`/ideas/${id}/business-model`).then(r => r.data), // F12
}
