// ============================================================
// FILE   : components/Banner.jsx
// LAYER  : View (MVC: V)
// PURPOSE: Error and confirmation messages were styled separately on
//          every page. One component keeps the wording and colour
//          consistent, and always gives errors the right ARIA role so
//          screen readers announce them.
// ============================================================
const TONES = {
  error:   'bg-red-50 border-red-200 text-red-700',
  success: 'bg-emerald-50 border-emerald-200 text-emerald-700',
  warn:    'bg-amber-50 border-amber-200 text-amber-700',
}

export default function Banner({ tone = 'error', children, className = '' }) {
  if (!children) return null
  return (
    <div
      role={tone === 'error' ? 'alert' : 'status'}
      className={`mb-4 border text-sm rounded-xl px-4 py-3 ${TONES[tone]} ${className}`}
    >
      {children}
    </div>
  )
}
