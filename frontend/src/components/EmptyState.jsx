// ============================================================
// FILE   : components/EmptyState.jsx
// LAYER  : View (MVC: V)
// PURPOSE: Eight pages wrote their own "nothing here yet" block. An
//          empty screen is where a user most needs guidance, so it is
//          worth having one consistent, well-written version.
// ============================================================
export default function EmptyState({ icon = '📭', title, message, action }) {
  return (
    <div className="card text-center py-16">
      <div className="text-4xl mb-3" aria-hidden="true">{icon}</div>
      <h2 className="font-bold text-slate-900 mb-1">{title}</h2>
      {message && <p className="text-sm text-slate-500 mb-5 max-w-sm mx-auto">{message}</p>}
      {action}
    </div>
  )
}
