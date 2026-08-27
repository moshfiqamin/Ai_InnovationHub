// ============================================================
// FILE   : main.jsx
// LAYER  : View (MVC: V) — application entry point
// PURPOSE: Mounts React, wraps the app in the router and the
//          authentication context so every page can read the
//          logged-in user.
// ============================================================
import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import App from './App'
import './index.css'

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    {/* BrowserRouter enables client-side routing (React Router) */}
    <BrowserRouter>
      {/* AuthProvider = M1, makes auth state available app-wide */}
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
)
