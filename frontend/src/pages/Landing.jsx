// ============================================================
// MODULE : M2 — Landing Module
// LAYER  : View (MVC: V)
// FEATURE: No core feature ID — M2 supports adoption and navigation.
// IMPLEMENTS (per requirements.pdf M2):
//   1. Hero section and value proposition        -> #top
//   2. Feature overview                          -> #features
//   3. How AI supports innovation                -> #ai
//   4. Platform/community overview (+ roles)     -> #roles
//   5. Call-to-action navigation                 -> throughout
//   6. Footer / contact / FAQ                    -> #faq, #contact
// NOTE   : Section ids above are the anchor targets used by the
//          navbar links in components/Navbar.jsx.
// NFR    : NFR3 Responsive, NFR9 Usability, NFR12 Accessibility
// ============================================================
import { useState } from 'react'
import { Link } from 'react-router-dom'
import Navbar from '../components/Navbar'
import Logo from '../components/Logo'
import { SELECTABLE_ROLES, ASSIGNED_ROLES } from '../constants/roles'

const PLATFORM_FEATURES = [
  { icon: '💡', title: 'Idea Submission',     desc: 'Capture ideas with problem, solution, category and tags.' },
  { icon: '🤝', title: 'Team Collaboration',  desc: 'Turn ideas into projects with teams, tasks and milestones.' },
  { icon: '💬', title: 'Communities',         desc: 'Discuss innovation with people who share your problem space.' },
  { icon: '🏆', title: 'Challenges',          desc: 'Compete in innovation challenges and climb the leaderboard.' },
  { icon: '🎓', title: 'Mentors & Investors', desc: 'Connect with experienced mentors and funding partners.' },
  { icon: '📊', title: 'Analytics',           desc: 'Track engagement, reputation and project performance.' },
]

const AI_CAPABILITIES = [
  { title: 'Idea Analysis',            desc: 'Instant strengths, gaps and improvement suggestions on any idea.' },
  { title: 'Similar Idea Detection',   desc: 'Semantic matching surfaces related work so effort is not duplicated.' },
  { title: 'SWOT Analysis',            desc: 'Strengths, weaknesses, opportunities and threats, generated automatically.' },
  { title: 'Business Model Generator', desc: 'Turn a raw concept into a structured, reviewable business model.' },
  { title: 'Smart Search',             desc: 'Find ideas and people by meaning, not just exact keywords.' },
  { title: 'Personalized Suggestions', desc: 'Recommendations tuned to your role, skills and activity.' },
]

const STEPS = [
  { n: '01', title: 'Share the idea',    desc: 'Describe the problem and your proposed solution. Drafts stay private until you publish.' },
  { n: '02', title: 'Let AI sharpen it', desc: 'Get analysis, a SWOT breakdown and similar work already on the platform.' },
  { n: '03', title: 'Build the team',    desc: 'Invite collaborators, assign tasks and track milestones in a shared workspace.' },
  { n: '04', title: 'Find backing',      desc: 'Connect with mentors for guidance and investors for funding.' },
]

const FAQS = [
  { q: 'Who is AI Innovation Hub for?',    a: 'Innovators, researchers, entrepreneurs, mentors, investors and organizations — anyone who would rather develop ideas collaboratively than alone.' },
  { q: 'How does the AI assistance work?', a: 'AI analyses the ideas you submit and returns summaries, SWOT breakdowns, business models and recommendations. It assists your decisions rather than replacing them.' },
  { q: 'Does it cost anything to join?',   a: 'No. Creating an account, posting ideas and joining communities are all free.' },
  { q: 'Can I keep an idea private?',      a: 'Yes. Ideas stay as drafts until you explicitly publish them to the feed.' },
  { q: 'Can I change my role later?',      a: 'Yes. Your role shapes recommendations and available tools, and it can be updated from your profile at any time.' },
]

export default function Landing() {
  const [openFaq, setOpenFaq] = useState(null)
  // Set to true when /public/hero.jpg is missing so the SVG shows instead.
  const [heroMissing, setHeroMissing] = useState(false)

  return (
    <div className="min-h-screen bg-white">
      <Navbar showLinks />

      {/* ================================================== */}
      {/* SECTION 1 — HERO (full-bleed photograph + scrim)   */}
      {/* ================================================== */}
      <section id="top" className="relative isolate min-h-[calc(100vh-4rem)] flex items-center overflow-hidden">

        {/* --- BACKGROUND LAYER --- */}
        {/* The SVG sits underneath as the guaranteed fallback. The photo
            is layered on top and hides itself if /public/hero.jpg is absent. */}
        <div aria-hidden="true" className="absolute inset-0 -z-20">
          <img src="/hero-fallback.svg" alt="" className="w-full h-full object-cover" />
        </div>
        {!heroMissing && (
          <img
            src="/hero.jpg" alt=""
            aria-hidden="true"
            onError={() => setHeroMissing(true)}
            className="absolute inset-0 -z-10 w-full h-full object-cover"
          />
        )}

        {/* --- SCRIM --- */}
        {/* Dark gradient, heaviest on the left where the text sits, so the
            headline keeps AA contrast over any photograph (NFR12). */}
        <div aria-hidden="true"
          className="absolute inset-0 -z-[5] bg-gradient-to-r from-slate-950/85 via-slate-950/65 to-slate-950/25" />
        <div aria-hidden="true"
          className="absolute inset-0 -z-[5] bg-gradient-to-t from-slate-950/60 via-transparent to-slate-950/30" />

        {/* --- CONTENT (left aligned) --- */}
        <div className="relative w-full max-w-7xl mx-auto px-6 py-24">
          <div className="max-w-2xl">
            <p className="text-[11px] sm:text-xs font-semibold tracking-[0.2em] uppercase text-white/85 mb-6 animate-fade-up">
              Collaborative Social Platform for Innovation
            </p>

            <h1 className="text-5xl sm:text-6xl lg:text-7xl font-extrabold text-white leading-[1.02] tracking-tight mb-7 animate-fade-up"
                style={{ animationDelay: '.05s' }}>
              AI Innovation Hub
            </h1>

            <p className="text-lg text-white/90 leading-relaxed max-w-xl mb-10 animate-fade-up"
               style={{ animationDelay: '.1s' }}>
              A public gateway for innovators to understand the mission, explore
              AI-powered collaboration, and move confidently from first idea to
              real-world project.
            </p>

            <div className="flex flex-wrap gap-4 animate-fade-up" style={{ animationDelay: '.15s' }}>
              <Link to="/register" className="btn-primary">Start Innovating →</Link>
              <Link to="/login" className="btn-dark">Login</Link>
            </div>
          </div>
        </div>
      </section>

      {/* ================================================== */}
      {/* SECTION 2 — FEATURE OVERVIEW                       */}
      {/* ================================================== */}
      <section id="features" className="scroll-mt-20 max-w-7xl mx-auto px-6 py-24">
        <div className="max-w-2xl mb-14">
          <span className="eyebrow text-brand-600 mb-3">What you get</span>
          <h2 className="text-4xl font-extrabold text-slate-900 mb-4">Everything innovation needs</h2>
          <p className="text-slate-500 text-lg">From first spark to funded project.</p>
        </div>

        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {PLATFORM_FEATURES.map((f) => (
            <article key={f.title} className="card card-hover group">
              <div className="w-12 h-12 rounded-xl bg-brand-50 border border-brand-100 grid place-items-center text-2xl mb-4 group-hover:scale-110 transition-transform duration-300">
                {f.icon}
              </div>
              <h3 className="font-bold text-slate-900 mb-2">{f.title}</h3>
              <p className="text-sm text-slate-600 leading-relaxed">{f.desc}</p>
            </article>
          ))}
        </div>
      </section>

      {/* ================================================== */}
      {/* SECTION 3 — HOW AI SUPPORTS INNOVATION (dark)      */}
      {/* ================================================== */}
      <section id="ai" className="scroll-mt-20 bg-slate-950 text-white">
        <div className="max-w-7xl mx-auto px-6 py-24">
          <div className="max-w-2xl mb-14">
            <span className="eyebrow text-brand-400 mb-3">Intelligence built in</span>
            <h2 className="text-4xl font-extrabold mb-4">How AI supports your innovation</h2>
            <p className="text-slate-400 text-lg">
              AI assists decisions and content across the platform — it is not a generic chatbot.
            </p>
          </div>

          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {AI_CAPABILITIES.map((c, i) => (
              <article key={c.title}
                className="rounded-xl bg-white/5 border border-white/10 p-6 hover:bg-white/10 hover:border-brand-500/40 transition-all duration-300">
                <div className="stat-number text-sm text-brand-400 mb-3">{String(i + 1).padStart(2, '0')}</div>
                <h3 className="font-bold mb-2">{c.title}</h3>
                <p className="text-sm text-slate-400 leading-relaxed">{c.desc}</p>
              </article>
            ))}
          </div>
        </div>
      </section>

      {/* ================================================== */}
      {/* SECTION 4 — WORKFLOW                               */}
      {/* ================================================== */}
      <section id="workflow" className="scroll-mt-20 max-w-7xl mx-auto px-6 py-24">
        <div className="max-w-2xl mb-14">
          <span className="eyebrow text-brand-600 mb-3">The journey</span>
          <h2 className="text-4xl font-extrabold text-slate-900">From idea to impact</h2>
        </div>

        <div className="grid gap-8 md:grid-cols-4">
          {STEPS.map((s, i) => (
            <div key={s.n} className="relative">
              {i < STEPS.length - 1 && (
                <div aria-hidden="true"
                  className="hidden md:block absolute top-6 left-[calc(50%+2.5rem)] right-[-2rem] h-px bg-gradient-to-r from-brand-300 to-transparent" />
              )}
              <div className="relative w-12 h-12 rounded-xl bg-brand-600 text-white grid place-items-center stat-number text-sm shadow-lift mb-4">
                {s.n}
              </div>
              <h3 className="font-bold text-slate-900 mb-2">{s.title}</h3>
              <p className="text-sm text-slate-600 leading-relaxed">{s.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* ================================================== */}
      {/* SECTION 5 — ROLES                                  */}
      {/* ================================================== */}
      <section id="roles" className="scroll-mt-20 bg-slate-50 border-y border-slate-200">
        <div className="max-w-7xl mx-auto px-6 py-24">
          <div className="max-w-2xl mb-14">
            <span className="eyebrow text-brand-600 mb-3">Built around community</span>
            <h2 className="text-4xl font-extrabold text-slate-900 mb-4">A place for every kind of innovator</h2>
            <p className="text-slate-500 text-lg">
              Pick the role that fits how you work — the platform adapts around it.
            </p>
          </div>

          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 mb-10">
            {SELECTABLE_ROLES.map((r) => (
              <article key={r.value} className="card card-hover flex gap-4">
                <span className="text-3xl shrink-0" aria-hidden="true">{r.icon}</span>
                <div>
                  <h3 className="font-bold text-slate-900">{r.label}</h3>
                  <div className="text-xs text-brand-600 font-semibold mb-1.5">{r.tagline}</div>
                  <p className="text-sm text-slate-600 leading-relaxed">{r.desc}</p>
                </div>
              </article>
            ))}
          </div>

          <div className="rounded-xl bg-white border border-slate-200 p-6">
            <div className="text-xs font-bold uppercase tracking-widest text-slate-500 mb-4">
              Granted by an administrator
            </div>
            <div className="grid gap-4 sm:grid-cols-3">
              {ASSIGNED_ROLES.map((r) => (
                <div key={r.value} className="flex gap-3">
                  <span className="text-xl shrink-0" aria-hidden="true">{r.icon}</span>
                  <div>
                    <div className="font-bold text-slate-800 text-sm">{r.label}</div>
                    <p className="text-xs text-slate-500 leading-relaxed">{r.desc}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* ================================================== */}
      {/* SECTION 6 — FAQ                                    */}
      {/* ================================================== */}
      <section id="faq" className="scroll-mt-20 max-w-3xl mx-auto px-6 py-24">
        <div className="mb-12">
          <span className="eyebrow text-brand-600 mb-3">Questions</span>
          <h2 className="text-4xl font-extrabold text-slate-900">Frequently asked</h2>
        </div>

        <div className="space-y-3">
          {FAQS.map((f, i) => (
            <div key={f.q} className={`rounded-xl border transition-all duration-200 overflow-hidden ${
              openFaq === i ? 'border-brand-300 bg-brand-50/40' : 'border-slate-200 bg-white hover:border-slate-300'
            }`}>
              <button onClick={() => setOpenFaq(openFaq === i ? null : i)}
                aria-expanded={openFaq === i}
                className="w-full text-left px-6 py-5 flex items-center justify-between gap-4">
                <span className="font-semibold text-slate-900">{f.q}</span>
                <span aria-hidden="true"
                  className={`shrink-0 w-7 h-7 rounded-full grid place-items-center text-lg leading-none transition-all duration-300 ${
                    openFaq === i ? 'bg-brand-600 text-white rotate-45' : 'bg-slate-100 text-slate-500'
                  }`}>+</span>
              </button>
              {openFaq === i && (
                <p className="px-6 pb-5 text-sm text-slate-600 leading-relaxed animate-fade-up">{f.a}</p>
              )}
            </div>
          ))}
        </div>
      </section>

      {/* ================================================== */}
      {/* SECTION 7 — FINAL CALL TO ACTION                   */}
      {/* ================================================== */}
      <section className="max-w-7xl mx-auto px-6 pb-24">
        <div className="rounded-2xl bg-brand-600 px-8 py-16 text-center text-white">
          <h2 className="text-4xl font-extrabold mb-4">Ready to start building?</h2>
          <p className="text-brand-50 mb-9 max-w-lg mx-auto">
            Join free, submit your first idea, and let AI help you sharpen it.
          </p>
          <Link to="/register"
            className="inline-flex items-center gap-2 rounded-lg bg-white text-brand-700 px-6 py-3 font-semibold hover:bg-brand-50 transition-colors">
            Create your free account →
          </Link>
        </div>
      </section>

      {/* ================================================== */}
      {/* SECTION 8 — FOOTER / CONTACT                       */}
      {/* ================================================== */}
      <footer id="contact" className="scroll-mt-20 bg-slate-950 text-slate-400">
        <div className="max-w-7xl mx-auto px-6 py-14">
          <div className="flex flex-col md:flex-row justify-between gap-10">
            <div className="max-w-sm">
              <Logo light className="mb-3" />
              <p className="text-sm leading-relaxed">
                Where great ideas find great minds. A collaborative platform for
                innovation and problem solving.
              </p>
            </div>

            <div className="grid grid-cols-2 gap-12 text-sm">
              <div>
                <div className="font-bold text-white mb-3">Platform</div>
                <ul className="space-y-2">
                  <li><a href="#features" className="hover:text-brand-400">Features</a></li>
                  <li><a href="#workflow" className="hover:text-brand-400">Workflow</a></li>
                  <li><a href="#roles" className="hover:text-brand-400">Roles</a></li>
                  <li><Link to="/register" className="hover:text-brand-400">Create account</Link></li>
                </ul>
              </div>
              <div>
                <div className="font-bold text-white mb-3">Contact</div>
                <ul className="space-y-2">
                  <li>CSE470 Software Engineering</li>
                  <li>BRAC University</li>
                </ul>
              </div>
            </div>
          </div>

          <div className="border-t border-white/10 mt-10 pt-6 text-xs text-slate-500">
            © {new Date().getFullYear()} AI Innovation Hub — CSE470 course project.
          </div>
        </div>
      </footer>
    </div>
  )
}
