// ============================================================
// MODULES: M7 · M8 · M9 · M10 · M11 · M12 · M13 · M14
// LAYER  : View support — API clients
// ============================================================
import api from './api'

// ---------- M7 : COMMUNITY (F5) ----------
export const communityApi = {
  list: (category) => api.get('/communities', { params: { category } }).then(r => r.data),
  categories: () => api.get('/communities/categories').then(r => r.data),
  create: (payload) => api.post('/communities', payload).then(r => r.data),
  get: (id) => api.get(`/communities/${id}`).then(r => r.data),
  toggleJoin: (id) => api.post(`/communities/${id}/join`).then(r => r.data),
  members: (id) => api.get(`/communities/${id}/members`).then(r => r.data),
  posts: (id) => api.get(`/communities/${id}/posts`).then(r => r.data),
  createPost: (id, payload) => api.post(`/communities/${id}/posts`, payload).then(r => r.data),
  upvotePost: (postId) => api.post(`/communities/posts/${postId}/upvote`).then(r => r.data),
  commentPost: (postId, content, parentId = null) =>
    api.post(`/communities/posts/${postId}/comments`, { content, parentId }).then(r => r.data),
}

// ---------- M8 : SMART SEARCH (F6) ----------
export const searchApi = {
  search: (q) => api.get('/search', { params: { q } }).then(r => r.data),
}

// ---------- M9 : CHALLENGES (F14) ----------
export const challengeApi = {
  list: (status) => api.get('/challenges', { params: { status } }).then(r => r.data),
  get: (id) => api.get(`/challenges/${id}`).then(r => r.data),
  create: (payload) => api.post('/challenges', payload).then(r => r.data),
  submit: (id, ideaId) => api.post(`/challenges/${id}/submit`, { ideaId }).then(r => r.data),
  submissions: (id) => api.get(`/challenges/${id}/submissions`).then(r => r.data),
  score: (submissionId, payload) =>
    api.put(`/challenges/submissions/${submissionId}/score`, payload).then(r => r.data),
}

// ---------- M10 : MENTOR & INVESTOR (F13, F15) ----------
export const engagementApi = {
  mentors: (search) => api.get('/mentors', { params: { search } }).then(r => r.data),
  recommendedMentors: () => api.get('/mentors/recommended').then(r => r.data),
  requestMentorship: (mentorId, message) =>
    api.post(`/mentors/${mentorId}/request`, { message }).then(r => r.data),
  investors: (search) => api.get('/investors', { params: { search } }).then(r => r.data),
  expressInterest: (payload) => api.post('/investors/interest', payload).then(r => r.data),
  mine: () => api.get('/engagements').then(r => r.data),
  respond: (kind, id, status) =>
    api.put(`/engagements/${kind}/${id}`, { status }).then(r => r.data),
}

// ---------- M11 : ANALYTICS (F19) ----------
export const analyticsApi = {
  get: () => api.get('/analytics').then(r => r.data),
}

// ---------- M12 : NOTIFICATIONS (F17) ----------
export const notificationApi = {
  list: (unreadOnly = false) => api.get('/notifications', { params: { unreadOnly } }).then(r => r.data),
  count: () => api.get('/notifications/count').then(r => r.data),
  markRead: (id) => api.put(`/notifications/${id}/read`).then(r => r.data),
  markAllRead: () => api.put('/notifications/read-all').then(r => r.data),
  remove: (id) => api.delete(`/notifications/${id}`).then(r => r.data),
}

// ---------- M13 : PROFILE (F16) ----------
export const profileApi = {
  mine: () => api.get('/profile').then(r => r.data),
  get: (id) => api.get(`/profile/${id}`).then(r => r.data),
  update: (payload) => api.put('/profile', payload).then(r => r.data),
}

// ---------- M14 : ADMIN (F20) ----------
export const adminApi = {
  stats: () => api.get('/admin/stats').then(r => r.data),
  users: (search) => api.get('/admin/users', { params: { search } }).then(r => r.data),
  setRole: (id, role) => api.put(`/admin/users/${id}/role`, { role }).then(r => r.data),
  reports: (status) => api.get('/admin/reports', { params: { status } }).then(r => r.data),
  resolve: (id, action) => api.put(`/admin/reports/${id}/resolve`, null, { params: { action } }).then(r => r.data),
}

// Reporting is open to every user, so it lives outside adminApi.
export const moderationApi = {
  report: (targetType, targetId, reason) =>
    api.post('/moderation/report', { targetType, targetId, reason }).then(r => r.data),
}
