# AI_InnovationHub — Master Project Reference

> **This is the single living reference for the whole project.**
> Every decision, module, feature and progress update is tracked here.
> Update this file as modules are completed — do not create parallel tracking docs.

**Course:** CSE470 Software Engineering
**Project:** AI_InnovationHub — *"Where Great Ideas Find Great Minds"*
**Concept:** A collaborative social platform for innovation and problem solving.
**Repo:** https://github.com/moshfiqamin/Ai_InnovationHub
**Source documents:** `guide.docx.pdf` (course outline, 4pp) · `requirements.pdf` (module + feature spec, 12pp)

---

## 1. Locked Decisions

Decisions confirmed by the project owner. These override any suggestion in the source PDFs.

| # | Question | Decision |
|---|---|---|
| D1 | File count limit | **No limit.** Proper MVC separation takes priority over a small file count. |
| D2 | Tech stack | **Follow `requirements.pdf` section 5** as specified. |
| D3 | Paid services | **Free tools only.** Every dependency must have a free tier. |
| D4 | Scope | **All 20 features** (not the 5-feature single-member split in `requirements.pdf` §6). |
| D5 | AI assistance | Permitted. AI provider must be a **free model** capable of serving the requirements. |

### Derived decisions (made from D1–D5)

| # | Decision | Reason |
|---|---|---|
| D6 | **Database = local PostgreSQL 16** for development | Already installed and running. Zero setup, no network dependency. |
| D7 | **Supabase = optional cloud host only**, not the app backend | Using the Supabase client SDK from React would bypass the controller layer and break the mandatory MVC requirement. Supabase stays a plain Postgres host if cloud deployment is needed. |
| D8 | **AI provider = Google Gemini API (free tier)** | The only provider that is both listed in `requirements.pdf` §5 *and* genuinely free. Covers generation **and** embeddings for pgvector. |
| D9 | **Fallback provider = Groq** `openai/gpt-oss-120b` | ✅ **Live and verified.** `ResilientAiProvider` tries Gemini, then Groq. ⚠️ Llama models are **not** available on this account — check yours with `curl -H "Authorization: Bearer $KEY" https://api.groq.com/openai/v1/models`. Groq serves no embedding model, so F3/F6 remain Gemini-only. |
| D11 | **AI responses report the provider that actually answered** | The dashboard previously hardcoded `source: "gemini"`, so the UI badge lied whenever Groq answered. `IAiProvider.LastProviderUsed` now surfaces `gemini` / `groq` / `unavailable` honestly. |
| D10 | **Embeddings truncated to 1536 dimensions** | `gemini-embedding-001` returns 3072 dims natively, but **pgvector indexes cap at 2000**. Requesting `outputDimensionality: 1536` keeps vectors indexable (MRL truncation, minimal quality loss). |

---

## 2. Hard Constraints (from `guide.docx.pdf` — non-negotiable)

- ⚠️ **MVC architecture is MANDATORY.** Worth **3 of 15 marks**. Models, Views and Controllers must be clearly separated.
- ⚠️ **Minimum 20 features.** Login/registration do **not** count as features.
- ⚠️ **Libraries may not implement a major feature.** Doing so is *"considered illegal, and the student using the library will be penalized."* → **Faculty approval required for the AI/search/upload/auth packages.**
- ⚠️ **Django and Flask are banned.** FastAPI allowed only for AI systems.
- ⚠️ **GitHub required**, faculty added as collaborator during Sprint 1; push at the end of every sprint even if incomplete.
- ⚠️ **4 sprints**, 7–14 days each.
- ⚠️ **Viva = 10 of 15 marks**, graded on explaining your own code. *All code in this project is commented for exactly this reason.*

### Marking rubric (15 marks total)

| Category | Marks |
|---|---|
| SRS document + version control | 2 |
| MVC architecture + project quality | 3 |
| Project viva (individual Q&A) | 10 |

---

## 3. Confirmed Technology Stack

All entries verified free. ✅ = installed · ⬜ = to install

| Layer | Technology | Cost | Status |
|---|---|---|---|
| **Frontend** | React 18 + Vite 6.4 | Free (MIT) | ✅ installed |
| Frontend language | JavaScript / JSX | Free | — |
| UI styling | Tailwind CSS 3.4 | Free (MIT) | ✅ installed |
| Routing | React Router 6 | Free (MIT) | ✅ installed |
| State | React Context API | Free (built in) | — |
| HTTP client | Axios | Free (MIT) | ✅ installed |
| **Backend** | ASP.NET Core **10** Web API | Free (MIT) | ✅ SDK 10.0.400 installed |
| **Architecture** | MVC pattern | — | — |
| **Database** | PostgreSQL 16.14 | Free (open source) | ✅ running on `:5432` |
| **ORM** | EF Core 10 + Npgsql 10.0.3 | Free (MIT) | ✅ installed, migration applied |
| **Auth** | JWT bearer + PBKDF2 hashing | Free | ✅ working |
| **Real-time** | SignalR | Free (in ASP.NET Core) | ⬜ Sprint 2 (M12) |
| **AI — text** | Gemini `gemini-3.6-flash` | Free (rate-limited) | ✅ **key verified working** |
| **AI — embeddings** | Gemini `gemini-embedding-001` @ 1536 dims | Free | ✅ **verified working** |
| **AI — fallback** | Groq `openai/gpt-oss-120b` | Free tier | ✅ **working, verified** |
| **Semantic search** | Gemini embeddings + cosine in C# | Free | ✅ **working** — pgvector not needed, see O7 |
| **Charts** | Chart.js 4 + react-chartjs-2 | Free (MIT) | ✅ rendering in M3 |
| **Version control** | Git + GitHub | Free | ✅ |

### Why Gemini and not another AI provider

`requirements.pdf` §5 lists three options: OpenAI, Azure OpenAI, Gemini — *"select one provider after faculty approval."* OpenAI and Azure OpenAI have no free tier. **Gemini does**, and it provides both text generation and free embeddings, which the semantic-search features need. It satisfies D3 and D5 while staying inside the approved list.

---

## 4. MVC Architecture & Folder Structure

The mandatory MVC split, mapped exactly to NFR1 (*"controllers, models/data and views/client responsibilities should be clearly separated"*):

- **Model** → `backend/Models/` + `backend/Data/` (EF Core entities, DTOs, DbContext)
- **Controller** → `backend/Controllers/` (REST endpoints, request handling)
- **View** → `frontend/src/pages/` + `components/` (React UI)

```
Ai_InnovationHub/
├── PROJECT_REFERENCE.md          ← this file (single source of truth)
├── .gitignore
├── backend/                      ── ASP.NET Core Web API
│   ├── Controllers/              ── C: one controller per module
│   ├── Models/
│   │   ├── Entities/             ── M: EF Core database entities
│   │   └── DTOs/                 ── M: request/response shapes
│   ├── Data/                     ── M: AppDbContext + migrations
│   ├── Services/                 ── business logic + Gemini AI service
│   ├── Hubs/                     ── SignalR real-time hubs
│   └── Program.cs                ── entry point, DI, middleware
└── frontend/                     ── React + Vite
    └── src/
        ├── pages/                ── V: one page per module
        ├── components/           ── V: shared UI
        ├── context/              ── auth/app state
        ├── services/             ── axios API clients
        └── App.jsx               ── routes
```

---

## 4b. Role System (M1)

`requirements.pdf` never enumerates roles — it only requires *"Role selection/access control"* (M1)
and *"role-based access"* (NFR2). The set below is derived from the actors the modules actually
describe. Single source of truth: `frontend/src/constants/roles.js`, mirrored by the
`SelfServiceRoles` allow-list in `backend/Controllers/AuthController.cs`.

### Self-service (choosable at sign-up)

| Role | Icon | Derived from |
|---|---|---|
| Innovator *(default)* | 💡 | M5 Idea Management — the platform's primary actor |
| Researcher | 🔬 | Technical validation across M5/M8 |
| Entrepreneur | 🚀 | M8 F12 Business Model Generator |
| Mentor | 🎓 | M10 Mentor & Investor (20 mentions) |
| Investor | 💰 | M10 Mentor & Investor (16 mentions) |
| Organization | 🏛️ | M9 — *"organizations/admins create challenges"* |

### Admin-granted only (never selectable at sign-up)

| Role | Icon | Derived from |
|---|---|---|
| Judge | ⚖️ | M9 judging/score view |
| Moderator | 🛡️ | M7 + M14 community moderation |
| Admin | ⚙️ | M14 Administration (12 mentions) |

⚠️ **Security note:** the registration endpoint validates the role against the self-service
allow-list server-side. Posting `"role":"Admin"` directly to the API silently stores `Innovator`
— verified by test. Privileged roles must be granted through M14.

---

## 5. The 14 Modules

| # | Module | Purpose | Features | Status |
|---|---|---|---|---|
| M1 | Authentication | Account access, identity, roles | *supporting — not a feature* | ✅ **Done** (Sprint 1) |
| M2 | Landing | Public intro and discovery | *supporting* | ✅ **Done** (Sprint 1) |
| M3 | Dashboard | Personalized overview | F18, F19 | ✅ **Done** (Sprint 1) |
| M4 | Innovation Feed | Social content discovery | F4 | ✅ **Done** (Sprint 2) |
| M5 | Idea Management | Create/manage ideas | F1, F2, F3, F11 | ✅ **Done** (Sprint 2) |
| M6 | Project Collaboration | Teams and execution | F7, F8, F9, F10 | ✅ **Done** (Sprint 2) |
| M7 | Community | Discussion and interaction | F5 | ✅ **Done** (Sprint 3) |
| M8 | AI Intelligence | Central AI services | F2, F3, F6, F11, F12, F18 | ✅ **Done** (Sprint 3) |
| M9 | Innovation Challenges | Competitions, submissions | F14 | ✅ **Done** (Sprint 3) |
| M10 | Mentor & Investor | Guidance and funding | F13, F15 | ✅ **Done** (Sprint 3) |
| M11 | Analytics | Performance metrics | F19 | ✅ **Done** (Sprint 3) |
| M12 | Notifications | Activity and alerts | F17 | ✅ **Done** (Sprint 3) |
| M13 | Profile | Identity and reputation | F16 | ✅ **Done** (Sprint 3) |
| M14 | Administration | Management and moderation | F20 | ✅ **Done** (Sprint 3) |

> **Note:** M1 and M2 carry no feature IDs — the guide excludes login/registration from the feature count. The 20 features spread across the other 12 modules.

---

## 6. The 20 Functional Features

| ID | Feature | Module(s) | What the system must do | Status |
|---|---|---|---|---|
| F1 | Idea Submission System | M5 | Create, save, edit, publish structured ideas | ✅ **Done** — draft/publish |
| F2 | AI Idea Analysis | M5 + M8 | Return AI insights, summaries, improvements | ✅ **Done** — Gemini, cached |
| F3 | AI Similar Idea Detection | M5 + M8 | Find semantically related ideas | ✅ **Done** — cosine, 0.70 cutoff |
| F4 | Innovation Feed | M4 | Latest/trending content with interaction | ✅ **Done** — sort/filter/search |
| F5 | Community Discussion & Comments | M7 | Posts, comments, replies, reactions | ✅ **Done** — posts/comments/upvotes |
| F6 | AI Smart Search | M8 | Semantic search over ideas/projects/users | ✅ **Done** — semantic, 0.55 cutoff |
| F7 | Team Formation | M6 | Invite members, form project teams | ✅ **Done** — invite/accept/roles |
| F8 | Project Workspace | M6 | Central project area | ✅ **Done** — workspace + milestones |
| F9 | Task Management | M6 | Create, assign, track tasks | ✅ **Done** — 3-column board |
| F10 | File & Resource Sharing | M6 | Upload, organize, access resources | ✅ **Done** — upload/download |
| F11 | AI SWOT Analysis | M5 + M8 | Generate SWOT for an idea/project | ✅ **Done** — Gemini, cached |
| F12 | AI Business Model Generator | M8 | Structured business model from a concept | ✅ **Done** — business model canvas |
| F13 | AI Mentor Recommendation | M10 + M8 | Recommend mentors by expertise | ✅ **Done** — AI + reputation fallback |
| F14 | Innovation Challenges | M9 | Create/join, submit, leaderboards | ✅ **Done** — submit/judge/leaderboard |
| F15 | Investor Connect | M10 | Discover investors, funding requests | ✅ **Done** — pitch/accept/decline |
| F16 | Reputation & Badge System | M13 | Award points and badges | ✅ **Done** — 12 badges + levels |
| F17 | Notification System | M12 | Notify on social/project/AI activity | ✅ **Done** — bell + 9 alert types |
| F18 | AI Personalized Recommendation | M3 + M8 | Recommend ideas, mentors, challenges | ✅ **Done** — live Gemini |
| F19 | Analytics Dashboard | M3 + M11 | Engagement and innovation statistics | ✅ **Done** — Chart.js |
| F20 | Admin & AI Content Moderation | M14 | Manage content, review AI flags | ✅ **Done** — AI screening + queue |

**AI-dependent features (8):** F2, F3, F6, F11, F12, F13, F18, F20 — all served by the Gemini free tier via `backend/Services/`.

---

## 7. Sprint Plan

| Sprint | Scope | Status |
|---|---|---|
| 1 | Project setup, MVC skeleton, GitHub workflow, auth foundation (M1), landing (M2), shared UI, database foundation | ⬜ |
| 2 | Dashboard, feed, profile, notifications (M3, M4, M12, M13), idea management (M5), community + collaboration (M6, M7) | ⬜ |
| 3 | AI services (M8), challenges (M9), mentor/investor (M10), analytics (M11) | ⬜ |
| 4 | Integration, bug fixing, validation, responsiveness, moderation (M14), polish, documentation, demo prep | ⬜ |

---

## 8. Environment Setup Status

| Item | Status | Command if missing |
|---|---|---|
| PostgreSQL 16.14 | ✅ running on `:5432` | — |
| Node 26.3.1 / npm 12 | ✅ | — |
| Git 2.50.1 | ✅ | — |
| Homebrew 6.0.19 | ✅ | — |
| `.gitignore` | ✅ attribution configured | — |

| **.NET SDK 10.0.400** | ✅ installed | — |
| **pgvector** | ⚠️ installed but **only builds for PG 17/18** — server is PG 16. See O7. | — |
| **Gemini API key** | ✅ verified — stored in `.env` (gitignored) | — |
| `.env` secrets file | ✅ created, perms `600`, git-ignored | — |

---

## 9. Open Items Requiring Action

| # | Item | Owner | Blocking |
|---|---|---|---|
| O1 | **Faculty approval for Gemini API** — the guide penalizes libraries implementing major features; 8 features depend on AI | Project owner | F2, F3, F6, F11, F12, F13, F18, F20 |
| O2 | Add faculty as GitHub collaborator (required in Sprint 1) | Project owner | Sprint 1 grading |
| O3 | ~~Obtain free Gemini API key~~ ✅ **done and verified** | — | — |
| O6 | **Rotate the Gemini key before final submission** — it was pasted into a chat transcript | Project owner | Security hygiene |
| O4 | Install .NET 8 SDK and pgvector | Setup | Everything |
| O5 | SRS document (worth 2 marks) | Project owner | Final grading |
| O7 | ~~pgvector needs PostgreSQL 17/18~~ **RESOLVED without it.** Embeddings are stored as JSON on `Ideas.EmbeddingJson` and cosine similarity runs in `SimilarityHelper.cs`. Exact, not approximate — pgvector's index is a speed optimisation. Original note: **pgvector needs PostgreSQL 17/18** — Homebrew ships no PG 16 build. Either `brew install postgresql@17` and migrate (DB is small), or build pgvector from source. Not urgent: only F3/F6 in Sprint 3 need it. | Setup | F3, F6 |

---

## 10. Progress Log

Newest entries at the top. Update after every module.

| Date | Change |
|---|---|
| 2026-08-26 | **Refactoring pass + UI label cleanup.** Feature and module codes removed from every user-visible string (kept in code comments for tracing). Ten shared files created — `BaseApiController`, `Format`, `AiJson` on the backend; `PageShell`, `Tabs`, `EmptyState`, `Banner`, `Avatar`, `Skeleton`, `usePageData` on the frontend — absorbing 64 duplicated auth guards, 20 banners, 9 empty states, 7 page shells, 7 avatars, 5 AI JSON parsers, 5 helper methods, 4 skeletons and 3 tab bars. **Net effect 12,418 → 12,315 lines (−103):** ~420 lines of duplication removed, offset by 303 lines of shared code. The gain is single points of change, not size. The refactor itself blanked two pages via missed imports, which `vite build` did not catch — found by loading all 13 pages in a browser. **152/152 tests still pass.** |
| 2026-08-26 | **Groq fallback live; full suite green at 152 assertions, 0 failures, 0 warnings.** Every AI feature (F2, F11, F12, F13, F18, F20) now runs on real models. Two issues resolved: the supplied Groq model `llama-3.3-70b-versatile` **does not exist on this account** (404) — switched to `openai/gpt-oss-120b` after enumerating the 14 available models; and `DashboardService` **hardcoded `source: "gemini"`**, so the UI badge misreported which provider answered — added `IAiProvider.LastProviderUsed`. Fallback proven by deliberately invalidating the Gemini key: F18 returned `source: groq`, logs showed the handoff, and F2/F11/F12/F20 all produced quality output through Groq. F20 correctly flagged a spam post while leaving a genuine technical post alone. |
| 2026-08-26 | **M7–M14 built and verified — all 14 modules and all 20 features are now complete.** 12 new entities (25 tables), 9 services, 8 controllers, 9 React pages. Migration `AddM7_M14_AllRemainingModules` applied. Test suite grown to **140 assertions, 0 failures**. Two defects found and fixed while testing: **F6 reused F3's 0.70 similarity threshold**, which is wrong because a short query scores systematically lower against a document than a document does (measured: relevant 0.62–0.84, irrelevant 0.44–0.49) — F6 now uses 0.55; and **5 SQL-seeded ideas had no embeddings**, so F3/F6 silently skipped them — added `POST /api/search/backfill`, which generated all 5. Confirmed Gemini **embeddings** have a separate quota from text generation and still work while text is rate-limited. |
| 2026-08-26 | **M4 + M5 + M6 built and verified.** 9 features shipped (F1–F4, F7–F11). 7 new entities, 3 controllers, 3 services, 6 React pages. Migration `AddM4_M5_M6_FeedIdeasProjects` applied — 11 tables. Test suite extended to **90 assertions, 0 failures**. Two defects found and fixed during testing: the F3 similarity threshold was too low (an unrelated idea scored 0.56 and passed a 0.55 cutoff — raised to 0.70), and SWOT panels used interpolated Tailwind class names that the JIT compiler cannot see, so the colours never rendered. **Gemini free-tier quota was exhausted (429) during testing**, which proved the graceful-degradation path works and prompted wiring up the Groq fallback promised in D9. |
| 2026-08-26 | **Added `test-modules.sh`** — 51 automated API assertions for M1 + M3 (NFR18 Testability). All passing, including a live Gemini call. Writing it surfaced one real defect: the `FullName` length validator returned the framework default message, which leaked the C# property name to end users (NFR6 violation); now returns "Full name must be between 2 and 100 characters." |
| 2026-08-26 | **Landing redesigned to match the supplied reference.** Light sticky navbar with section links (Features/Workflow/Roles/FAQ/Contact), full-bleed photographic hero with a two-layer scrim, left-aligned headline. Brand palette switched **indigo → teal** (`#0d9488`) with amber as the secondary. New `Logo.jsx` sparkle mark; product name now renders "AI Innovation Hub". Hero uses `/public/hero.jpg` when present and falls back to `hero-fallback.svg` automatically. |
| 2026-08-26 | **UI redesign + role system expanded.** Roles went from 3 to 9 (6 self-service, 3 admin-granted) with a shared constants file and a server-side allow-list. Visual overhaul: design tokens in `tailwind.config.js`, Plus Jakarta Sans + JetBrains Mono, dark gradient hero with animated orbs, glassmorphism, two-panel auth screens with live role preview and password-strength meter, redesigned dashboard tiles/charts. Also fixed validation errors returning ASP.NET problem+json instead of the app's `{message}` shape, so form errors now surface their real reason. Verified desktop + mobile, zero console errors. |
| 2026-08-26 | **M1 + M2 + M3 built and verified end to end.** Backend: ASP.NET Core 10, EF Core, PostgreSQL, JWT, PBKDF2 hashing. Frontend: React 18 + Vite + Tailwind + Chart.js. Migration `InitialCreate_M1_M3` applied (Users, Ideas, ActivityLogs). All endpoints tested; F18 confirmed returning live Gemini output; F19 charts rendering. Sample data seeded. |
| 2026-08-26 | **Gemini API verified live.** Key authenticates (HTTP 200). `gemini-2.5-flash` is retired for new users → using **`gemini-3.6-flash`**. Embeddings via `gemini-embedding-001`, truncated to 1536 dims for pgvector. Secrets written to gitignored `.env`. Groq set as fallback (D9). |
| 2026-08-26 | Read both source PDFs. Locked decisions D1–D8. Confirmed stack. Created this reference file. No modules implemented yet. |
| 2026-08-26 | Step 7 complete — `.gitignore` attribution config. |
| 2026-08-26 | Step 8 complete — `.mcp.json` Supabase MCP server added (auth pending). |

---

## 11. Code Commenting Standard

Every file carries a header block, and every logical section is annotated — required for the 10-mark viva.

```
// ============================================================
// MODULE : M5 — Idea Management
// FEATURE: F1 — Idea Submission System
// LAYER  : Controller  (MVC: C)
// PURPOSE: Handles create/save/edit/publish for innovation ideas.
// ============================================================
```

---

## 12. How to Run the Project

Two servers must run at the same time, in **two separate terminals**.

### Prerequisites (one-time, already done)
```bash
brew services start postgresql@16     # database must be running
```

### Terminal 1 — Backend API (port 5099)
```bash
cd backend
dotnet run --launch-profile http
```
Wait for `Now listening on: http://localhost:5099`.

### Terminal 2 — Frontend (port 5173)
```bash
cd frontend
npm run dev
```
Then open **http://localhost:5173**.

### Test account (seeded)
| Field | Value |
|---|---|
| Email | `test@example.com` |
| Password | `secret123` |

### Verify each layer
| Check | URL / command | Expected |
|---|---|---|
| API alive | http://localhost:5099/api/health | JSON with `"status":"ok"` |
| Landing (M2) | http://localhost:5173/ | Hero, features, AI section, FAQ, footer |
| Register (M1) | http://localhost:5173/register | Creates account, redirects to dashboard |
| Login (M1) | http://localhost:5173/login | Signs in, redirects to dashboard |
| Route guard (M1) | http://localhost:5173/dashboard while logged out | Redirects to `/login` |
| Dashboard (M3) | http://localhost:5173/dashboard | Stats, 2 charts, AI recs, trending, activity |
| Database | `psql -d ai_innovationhub -c '\dt'` | Users, Ideas, ActivityLogs tables |

### Common issues
| Symptom | Cause | Fix |
|---|---|---|
| `Database migration failed` | Postgres not running | `brew services start postgresql@16` |
| Dashboard stats show `—` | Backend not running | Start Terminal 1 |
| Recommendations say `source: fallback` | Gemini key missing or quota hit | Check `backend/appsettings.Development.json`; fallback text is intentional |
| **`Failed to bind to address ... address already in use`** | A previous backend/frontend is still running in another terminal or in the background | Run the "free the ports" command below, then start again |

### Freeing the ports

The most common startup failure is a server left running from an earlier session.
This releases both ports without touching PostgreSQL:

```bash
pkill -f AiInnovationHub.Api; pkill -f "node.*vite"
```

Check what is holding a port before killing anything:

```bash
lsof -ti:5099 -ti:5173
```

Stopping a server properly is **Ctrl+C in its own terminal** — closing the tab or
window can leave the process alive, which is what causes the bind error.

### After changing backend code
`dotnet run` does **not** hot-reload. Stop it (Ctrl+C) and start again, or use `dotnet watch run`.
The frontend **does** hot-reload — Vite applies edits instantly.


---

## 13. Verification Checklist

### Automated tests — `./test-modules.sh`

51 API-level assertions covering M1 and M3. Backend must be running first.

```bash
./test-modules.sh
```

Covers: validation rules and their messages, duplicate-email rejection, all 6
self-service roles, all 4 privileged-role escalation attempts, login success and
both failure paths (identical messages, so no user enumeration), password hashing
and per-user salts, JWT protection on every guarded route, tampered-token
rejection, the full F19 summary payload shape, and a live F18 Gemini call.

The script creates temporary accounts and deletes them on exit. It exits `0` on
success, so it can go into CI later. **It cannot test the UI** — use the manual
checklist below for that.

Tick these to confirm each piece is genuinely implemented. Rows marked **AUTO**
were verified programmatically; the rest need your eyes.

### A. Servers and infrastructure
| # | Check | How | Pass when |
|---|---|---|---|
| A1 | PostgreSQL running | `pg_isready` | `accepting connections` |
| A2 | Backend starts | Terminal 1 | `Now listening on: http://localhost:5099` |
| A3 | Frontend starts | Terminal 2 | `Local: http://localhost:5173/` |
| A4 | API health | open `localhost:5099/api/health` | JSON `"status":"ok"` |
| A5 | Tables exist | `psql -d ai_innovationhub -c '\dt'` | Users, Ideas, ActivityLogs |

### B. M2 — Landing (design match)
| # | Check | Pass when |
|---|---|---|
| B1 | Light navbar, logo left | Teal/amber sparkle + "AI Innovation Hub" |
| B2 | Nav links present | Features · Workflow · Roles · FAQ · Contact |
| B3 | Teal Dashboard button, top right | Solid teal, not indigo |
| B4 | Hero is full-bleed image + dark scrim | Text readable over the image |
| B5 | Eyebrow text | "COLLABORATIVE SOCIAL PLATFORM FOR INNOVATION" |
| B6 | Headline left-aligned, very large, white | "AI Innovation Hub" |
| B7 | Two buttons | Teal "Start Innovating →" + outlined "Login" |
| B8 | Nav links scroll to sections | Clicking Roles jumps to the roles grid |
| B9 | FAQ accordion | Clicking a question expands one answer |
| B10 | Responsive | At 375px wide nothing overflows sideways |

### C. M1 — Authentication
| # | Check | Pass when |
|---|---|---|
| C1 | Register page two-panel | Dark left panel, form right |
| C2 | **9 roles total** | 6 selectable + 3 admin-granted behind the "+" toggle |
| C3 | Role selection updates left panel | Click Investor → preview changes |
| C4 | Password strength meter | Bars fill as password gets stronger |
| C5 | Password mismatch blocked | Red error, no account created |
| C6 | Short password rejected | Error names the 6-character rule |
| C7 | Invalid email rejected | "Please provide a valid email address." |
| C8 | Duplicate email rejected | "An account with this email already exists." |
| C9 | Registration succeeds | Redirects straight to the dashboard |
| C10 | Login works | `test@example.com` / `secret123` → dashboard |
| C11 | Wrong password rejected | "Invalid email or password." |
| C12 | Show/hide password toggle | Password becomes readable |
| C13 | Route guard | `/dashboard` while logged out → `/login` |
| C14 | Session survives refresh | Reload dashboard, still signed in |
| C15 | Logout | Returns to landing; `/dashboard` blocked again |
| C16 | **AUTO** Privilege escalation blocked | POST `"role":"Admin"` stores `Innovator` |

### D. M3 — Dashboard (F18 + F19)
| # | Check | Pass when |
|---|---|---|
| D1 | Greeting + role badge | "Welcome back, <name>" with role chip |
| D2 | Four stat tiles | Ideas 5 · Projects 0 · Reputation 145 · Notifications 0 |
| D3 | **F19** line chart | "Engagement over time" renders 7 day labels |
| D4 | **F19** doughnut chart | "Contribution mix" renders coloured segments |
| D5 | **F18** AI recommendations | 3 items appear |
| D6 | **F18** source badge | Green pill reading `GEMINI` (amber `FALLBACK` if quota hit) |
| D7 | **F18** genuinely personalised | Text references your skills / idea titles |
| D8 | Trending ideas | 5 ideas, ranked, with upvote counts |
| D9 | Recent activity | Rows with "1 day ago" style timestamps |
| D10 | Quick actions | 3 cards marked "Later sprint" |

### E. Not yet built (expected to be absent)
M4–M14 and features F1–F17, F20. Quick-action cards intentionally link nowhere yet.
