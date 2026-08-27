// ============================================================
// FILE   : services/api.js
// LAYER  : View support — HTTP client
// PURPOSE: Single axios instance used by every page. Attaches the
//          JWT to outgoing requests and handles 401s centrally.
// NFR    : NFR4 Security, NFR6 Error Handling, NFR14 Session Mgmt
// ============================================================
import axios from 'axios'

// Base URL is '/api' — Vite proxies this to the backend (see vite.config.js)
const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
})

// ---- REQUEST INTERCEPTOR -----------------------------------
// Before every request, attach the stored JWT as a Bearer token.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('aih_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// ---- RESPONSE INTERCEPTOR ----------------------------------
// If the backend says 401 the token is missing/expired, so we clear
// the session and bounce the user to login (NFR14).
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('aih_token')
      localStorage.removeItem('aih_user')
      if (!window.location.pathname.startsWith('/login')) window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

// ============================================================
// ERROR MESSAGE HELPER  (NFR6 Error Handling, NFR10 Reliability)
// ------------------------------------------------------------
// Axios reports a dead server and a rejected form the same way to
// callers: the request throws. Without this helper the UI printed
// "Registration failed. Please try again." when the real problem was
// that the backend was not running — which reads as though the user's
// input was wrong. This distinguishes the two cases.
// ============================================================
export function describeError(err, fallback = 'Something went wrong. Please try again.') {
  // 1. The server answered and gave us a reason -> show that reason.
  if (err.response?.data?.message) return err.response.data.message

  // 2. The request was made but nothing came back -> the API is down.
  if (err.request && !err.response) {
    return 'Cannot reach the server. Is the backend running on port 5099? '
         + 'Start it with:  cd backend && dotnet run --launch-profile http'
  }

  // 3. The server answered but with no usable message -> describe the status.
  if (err.response?.status >= 500) return 'The server hit an error. Please try again shortly.'

  return fallback
}

export default api
