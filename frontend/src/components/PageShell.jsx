// ============================================================
// FILE   : components/PageShell.jsx
// LAYER  : View (MVC: V) — shared page frame
// PURPOSE: Thirteen pages opened with the same wrapper, navbar and
//          heading block. This owns that frame so every module page
//          has identical spacing and structure.
// ============================================================
import Navbar from './Navbar'

export default function PageShell({ title, subtitle, action, width = 'max-w-5xl', children }) {
  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar />
      <div className={`${width} mx-auto px-6 py-10`}>
        {title && (
          <header className="flex flex-wrap items-end justify-between gap-4 mb-8">
            <div>
              <h1 className="text-3xl font-extrabold text-slate-900">{title}</h1>
              {subtitle && <p className="text-slate-500 mt-1">{subtitle}</p>}
            </div>
            {action}
          </header>
        )}
        {children}
      </div>
    </div>
  )
}
