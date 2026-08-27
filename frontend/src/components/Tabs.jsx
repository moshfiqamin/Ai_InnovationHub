// ============================================================
// FILE   : components/Tabs.jsx
// LAYER  : View (MVC: V)
// PURPOSE: Six pages hand-rolled the same segmented control. One
//          component keeps them visually identical and keyboard
//          accessible in a single place.
// items: array of strings, or { value, label, badge }
// ============================================================
export default function Tabs({ items, value, onChange, className = '' }) {
  const normalised = items.map(i => (typeof i === 'string' ? { value: i, label: i } : i))

  return (
    <div role="tablist" className={`flex gap-1 bg-slate-100 p-1 rounded-lg w-fit ${className}`}>
      {normalised.map(t => (
        <button
          key={t.value}
          role="tab"
          aria-selected={value === t.value}
          onClick={() => onChange(t.value)}
          className={`px-4 py-2 rounded-md text-sm font-semibold transition ${
            value === t.value ? 'bg-white text-brand-700 shadow-sm' : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          {t.label}
          {t.badge != null && t.badge !== '' && (
            <span className="ml-1.5 text-[10px] bg-red-500 text-white px-1.5 py-0.5 rounded-full">
              {t.badge}
            </span>
          )}
        </button>
      ))}
    </div>
  )
}
