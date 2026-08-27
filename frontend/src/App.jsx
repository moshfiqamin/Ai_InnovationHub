// ============================================================
// FILE   : App.jsx
// LAYER  : View (MVC: V) — route table
// PURPOSE: Maps URLs to all 14 module pages. Public routes are open;
//          every module page is wrapped in <ProtectedRoute> (M1).
// ============================================================
import { Routes, Route } from 'react-router-dom'
import Landing from './pages/Landing'                       // M2
import Login from './pages/Login'                           // M1
import Register from './pages/Register'                     // M1
import Dashboard from './pages/Dashboard'                   // M3
import Feed from './pages/Feed'                             // M4
import IdeaNew from './pages/IdeaNew'                       // M5
import IdeaDetail from './pages/IdeaDetail'                 // M5
import Projects from './pages/Projects'                     // M6
import ProjectWorkspace from './pages/ProjectWorkspace'     // M6
import { Communities, CommunityDetail } from './pages/Communities'  // M7
import Search from './pages/Search'                         // M8
import { Challenges, ChallengeDetail } from './pages/Challenges'    // M9
import Network from './pages/Network'                       // M10
import Analytics from './pages/Analytics'                   // M11
import Notifications from './pages/Notifications'           // M12
import Profile from './pages/Profile'                       // M13
import Admin from './pages/Admin'                           // M14
import ProtectedRoute from './components/ProtectedRoute'

const Guard = ({ children }) => <ProtectedRoute>{children}</ProtectedRoute>

export default function App() {
  return (
    <Routes>
      {/* ---- PUBLIC ---- */}
      <Route path="/"         element={<Landing />} />                                {/* M2 */}
      <Route path="/login"    element={<Login />} />                                  {/* M1 */}
      <Route path="/register" element={<Register />} />                               {/* M1 */}

      {/* ---- PROTECTED ---- */}
      <Route path="/dashboard"        element={<Guard><Dashboard /></Guard>} />       {/* M3  F18,F19 */}
      <Route path="/feed"             element={<Guard><Feed /></Guard>} />            {/* M4  F4      */}
      <Route path="/ideas/new"        element={<Guard><IdeaNew /></Guard>} />         {/* M5  F1      */}
      <Route path="/ideas/:id"        element={<Guard><IdeaDetail /></Guard>} />      {/* M5  F2,F3,F11,F12 */}
      <Route path="/projects"         element={<Guard><Projects /></Guard>} />        {/* M6  F8      */}
      <Route path="/projects/:id"     element={<Guard><ProjectWorkspace /></Guard>} />{/* M6  F7-F10  */}
      <Route path="/communities"      element={<Guard><Communities /></Guard>} />     {/* M7  F5      */}
      <Route path="/communities/:id"  element={<Guard><CommunityDetail /></Guard>} /> {/* M7  F5      */}
      <Route path="/search"           element={<Guard><Search /></Guard>} />          {/* M8  F6      */}
      <Route path="/challenges"       element={<Guard><Challenges /></Guard>} />      {/* M9  F14     */}
      <Route path="/challenges/:id"   element={<Guard><ChallengeDetail /></Guard>} /> {/* M9  F14     */}
      <Route path="/mentors"          element={<Guard><Network initialTab="Mentors" /></Guard>} />   {/* M10 F13 */}
      <Route path="/investors"        element={<Guard><Network initialTab="Investors" /></Guard>} /> {/* M10 F15 */}
      <Route path="/analytics"        element={<Guard><Analytics /></Guard>} />       {/* M11 F19     */}
      <Route path="/notifications"    element={<Guard><Notifications /></Guard>} />   {/* M12 F17     */}
      <Route path="/profile"          element={<Guard><Profile /></Guard>} />         {/* M13 F16     */}
      <Route path="/profile/:id"      element={<Guard><Profile /></Guard>} />         {/* M13 F16     */}
      <Route path="/admin"            element={<Guard><Admin /></Guard>} />           {/* M14 F20     */}

      {/* ---- FALLBACK ---- */}
      <Route path="*" element={
        <div className="min-h-screen grid place-items-center text-slate-500">
          404 — Page not found
        </div>
      } />
    </Routes>
  )
}
