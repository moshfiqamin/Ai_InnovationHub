# AI Innovation Hub

> Where Great Ideas Find Great Minds

A collaborative social platform for innovation and problem solving. Members publish
innovation ideas, get AI-assisted analysis on them, form teams to build them, discuss
them in communities, enter them into competitions, and connect with mentors and investors.

**CSE470 Software Engineering — BRAC University**

---

## Team

| Name |
|------|-----------|
| S M Moshfiq Ul Amin 

---

## Architecture

The project follows the **MVC pattern**, split across two applications:

| Layer | Location | Technology |
|-------|----------|-----------|
| **Model** | `backend/Models/`, `backend/Data/` | Entity Framework Core entities and DbContext |
| **View** | `frontend/src/pages/`, `frontend/src/components/` | React 18 components |
| **Controller** | `backend/Controllers/` | ASP.NET Core Web API controllers |

Business logic is deliberately kept out of the controllers and lives in `backend/Services/`,
so controllers only handle HTTP concerns. The React frontend has no database access — it can
only call the API, and the API decides what it is permitted to have.

### Stack

| Concern | Technology |
|---------|-----------|
| Backend | ASP.NET Core 10 Web API (C#) |
| Frontend | React 18 + Vite 6 + Tailwind CSS 3 |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core 10 + Npgsql |
| Authentication | JWT bearer tokens + PBKDF2 password hashing |
| Charts | Chart.js 4 |
| AI — text | Google Gemini (`gemini-3.6-flash`) |
| AI — embeddings | Google Gemini (`gemini-embedding-001`, 1536 dimensions) |
| AI — fallback | Groq (`openai/gpt-oss-120b`) |

---

## Getting started

### Prerequisites

| Requirement | Install |
|-------------|---------|
| .NET SDK 10 | `brew install dotnet` |
| Node.js 20+ | `brew install node` |
| PostgreSQL 16 | `brew install postgresql@16` |
| EF Core CLI | `dotnet tool install --global dotnet-ef` |

### 1. Start PostgreSQL and create the database

```bash
brew services start postgresql@16
createdb ai_innovationhub
```

### 2. Configure secrets

Create `backend/appsettings.Development.json`. **This file is gitignored and must never
be committed.**

```json
{
  "Jwt": {
    "Secret": "replace-with-a-long-random-string-at-least-32-characters"
  },
  "Gemini": {
    "ApiKey": "your-gemini-api-key"
  },
  "Groq": {
    "ApiKey": "your-groq-api-key",
    "Model": "openai/gpt-oss-120b"
  }
}
```

Free API keys:
- **Gemini** — https://aistudio.google.com/apikey
- **Groq** — https://console.groq.com/keys

> Model availability varies per Groq account. Check yours with:
> `curl -H "Authorization: Bearer $KEY" https://api.groq.com/openai/v1/models`

If `backend/appsettings.json` points at a different PostgreSQL username, update the
`ConnectionStrings:DefaultConnection` value to match your local setup.

### 3. Run the backend

```bash
cd backend
dotnet restore
dotnet run --launch-profile http
```

The database schema is created automatically on first run — EF Core applies all pending
migrations at startup. The API listens on **http://localhost:5099**.

### 4. Run the frontend

In a second terminal:

```bash
cd frontend
npm install
npm run dev
```

Open **http://localhost:5173**.

---

## Verifying the installation

```bash
./test-modules.sh
```

Runs **152 API assertions** covering every module: validation rules, authorisation
boundaries, role escalation attempts, password hashing, JWT protection, and the AI
endpoints. It creates temporary accounts and removes them on exit.

Expected output: `Passed: 152 · Failed: 0`

---

## Modules and features

Fourteen modules deliver twenty functional features.

| Module | Features |
|--------|----------|
| Authentication | Registration, login, nine-role access control *(not counted as a feature per the course guide)* |
| Landing | Public entry page |
| Dashboard | AI personalised recommendations · Analytics summary |
| Innovation Feed | Feed with sorting, filtering, search, upvotes, bookmarks, comments |
| Idea Management | Idea submission · AI idea analysis · AI similar-idea detection · AI SWOT analysis |
| Project Collaboration | Team formation · Project workspace · Task management · File sharing |
| Community | Community discussion and threaded comments |
| AI Intelligence | AI smart search · AI business model generator |
| Innovation Challenges | Create, enter, judge, leaderboard |
| Mentor & Investor | AI mentor recommendation · Investor connect |
| Analytics | Detailed platform and personal analytics |
| Notifications | Notification system across all activity types |
| Profile | Reputation and badge system |
| Administration | Admin management and AI content moderation |

---

## Roles

Six roles are self-selectable at registration. Three are granted only by an administrator —
the registration endpoint validates against a server-side allow-list, so a client cannot
assign itself a privileged role.

| Role | Granted by | Key capability |
|------|-----------|----------------|
| Innovator | Self | Submit ideas, run AI tools, start projects |
| Researcher | Self | As Innovator, with a research identity |
| Entrepreneur | Self | As Innovator, with a commercial identity |
| Mentor | Self | Appears in the mentor directory, accepts mentorship requests |
| Investor | Self | Appears in the investor directory, responds to funding interest |
| Organization | Self | Creates innovation challenges |
| Judge | Admin | Scores submissions in any challenge |
| Moderator | Admin | Reviews the content moderation queue |
| Admin | Admin | Full platform administration |

### Creating the first administrator

The API refuses to grant `Admin`, `Moderator` or `Judge` at registration, so the first
administrator must be promoted directly:

```bash
psql -d ai_innovationhub -c "UPDATE \"Users\" SET \"Role\"='Admin' WHERE \"Email\"='you@example.com';"
```

Subsequent role grants happen through the administration panel.

---

## AI provider resilience

Every AI call depends on a single interface, `IAiProvider`. `ResilientAiProvider` tries
Gemini first and transparently falls back to Groq if Gemini is unavailable or rate limited.
If both fail, endpoints return `503` with a readable message and features that can degrade
fall back to static behaviour — nothing crashes.

### Semantic search without pgvector

The requirements suggest pgvector for semantic search, but Homebrew ships no pgvector build
for PostgreSQL 16. Embeddings are therefore stored as JSON on the `Ideas` table and cosine
similarity is computed in `SimilarityHelper.cs`. This is mathematically exact — pgvector's
contribution is an index that makes large-scale search faster, not more correct.

Two different similarity thresholds are used deliberately:

| Feature | Threshold | Why |
|---------|-----------|-----|
| Similar idea detection | 0.70 | Compares one full idea against another; these score high |
| Smart search | 0.55 | Compares a short query against a full idea; these score lower |

---

## Project structure

```
Ai_InnovationHub/
├── backend/                      ASP.NET Core Web API
│   ├── Controllers/              14 controllers — HTTP handling only
│   ├── Services/                 23 services — all business logic
│   ├── Models/Entities/          17 entities — one per database table
│   ├── Models/DTOs/              request and response shapes
│   ├── Data/AppDbContext.cs      25 tables, relationships, indexes
│   ├── Migrations/               EF Core schema history
│   └── Program.cs                startup, DI, authentication, routing
├── frontend/                     React + Vite
│   └── src/
│       ├── pages/                17 module screens
│       ├── components/           10 shared UI components
│       ├── hooks/                shared data-loading logic
│       ├── services/             API clients
│       ├── context/              authentication state
│       └── constants/            role definitions
├── test-modules.sh               152 automated API assertions
└── database-demo.sql             12 annotated queries for inspecting the database
```

Every source file opens with a header naming its module, MVC layer and the feature it
implements, so any feature can be located with a single project-wide search.

---

## Inspecting the database

```bash
psql -d ai_innovationhub -f database-demo.sql
```

Or browse it visually:

```bash
brew install pgweb
pgweb --url "postgresql://$(whoami)@localhost:5432/ai_innovationhub?sslmode=disable"
```

> Table names are case-sensitive because Entity Framework created them with capitals.
> Quote them in SQL: `SELECT * FROM "Users";`

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `Failed to bind to address ... already in use` | A previous server is still running | `pkill -f AiInnovationHub.Api; pkill -f "node.*vite"` |
| `Database migration failed` | PostgreSQL is not running | `brew services start postgresql@16` |
| AI features return `503` | Free-tier quota exhausted on both providers | Wait, or add a working key to `appsettings.Development.json` |
| Recommendations show `FALLBACK` | Neither AI provider responded | Expected degradation — check your keys |
| A role change appears to do nothing | Roles are carried inside the JWT | Log out and back in |

---

## License

Coursework submitted for CSE470 at BRAC University.
