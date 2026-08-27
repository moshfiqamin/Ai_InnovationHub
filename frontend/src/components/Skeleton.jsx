// ============================================================
// FILE   : components/Skeleton.jsx
// LAYER  : View (MVC: V)
// PURPOSE: The pulsing placeholder shown while data loads. Repeated
//          in six pages with slightly different heights and counts.
// ============================================================
export default function Skeleton({ count = 3, height = 'h-40', cols = '' }) {
  return (
    <div className={cols || 'space-y-3'}>
      {Array.from({ length: count }, (_, i) => (
        <div key={i} className={`${height} bg-white rounded-2xl border border-slate-200 animate-pulse`} />
      ))}
    </div>
  )
}
