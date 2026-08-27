// ============================================================
// MODULE : Shared UI
// LAYER  : View (MVC: V)
// PURPOSE: The brand mark — a teal/amber four-point sparkle beside
//          the wordmark. Kept in its own component so the navbar,
//          footer and auth pages all render an identical logo
//          (NFR19 Consistency).
// ============================================================
export default function Logo({ light = false, className = '' }) {
  return (
    <span className={`inline-flex items-center gap-2.5 font-bold text-lg tracking-tight ${
      light ? 'text-white' : 'text-slate-900'
    } ${className}`}>
      {/* Two overlapping sparkles: large teal, small amber */}
      <svg width="26" height="26" viewBox="0 0 32 32" fill="none" aria-hidden="true" className="shrink-0">
        <path d="M13 2.5c.4 5.6 2.4 7.6 8 8-5.6.4-7.6 2.4-8 8-.4-5.6-2.4-7.6-8-8 5.6-.4 7.6-2.4 8-8Z"
              fill="#0d9488" />
        <path d="M24 17c.25 3.4 1.45 4.6 4.85 4.85-3.4.25-4.6 1.45-4.85 4.85-.25-3.4-1.45-4.6-4.85-4.85 3.4-.25 4.6-1.45 4.85-4.85Z"
              fill="#f59e0b" />
      </svg>
      AI Innovation Hub
    </span>
  )
}
