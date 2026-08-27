// ============================================================
// FILE   : components/Avatar.jsx
// LAYER  : View (MVC: V)
// PURPOSE: Initials were derived inline in nine places. One component
//          means one rule for how a name becomes an avatar.
// ============================================================
const SIZES = {
  xs: 'w-6 h-6 text-[10px]',
  sm: 'w-8 h-8 text-xs',
  md: 'w-9 h-9 text-xs',
  lg: 'w-10 h-10 text-sm',
  xl: 'w-20 h-20 text-2xl rounded-2xl',
}

export default function Avatar({ name, size = 'sm', className = '' }) {
  // "S M Moshfiq Ul Amin" -> "SM"
  const initials = (name || '?')
    .split(' ').filter(Boolean).map(p => p[0]).slice(0, 2).join('').toUpperCase()

  return (
    <span
      aria-hidden="true"
      className={`shrink-0 rounded-full bg-brand-600 text-white grid place-items-center font-bold ${SIZES[size]} ${className}`}
    >
      {initials}
    </span>
  )
}
