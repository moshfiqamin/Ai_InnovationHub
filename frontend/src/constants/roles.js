// ============================================================
// MODULE : M1 — Authentication (role selection / access control)
// LAYER  : View support — shared constants
// PURPOSE: Single source of truth for platform roles.
// GROUNDING: requirements.pdf never lists roles explicitly — it only
//   requires "Role selection/access control" (M1) and "role-based
//   access" (NFR2). These roles are derived from the actors the
//   modules actually describe:
//     Mentor / Investor      -> M10 Mentor & Investor
//     Organization / Judge   -> M9 Innovation Challenges
//     Moderator / Admin      -> M14 Administration, M7 Community
//     Innovator              -> M5 Idea Management (default actor)
// IMPORTANT: SELECTABLE_ROLES must stay in sync with the allow-list
//   in backend/Controllers/AuthController.cs.
// ============================================================

// ---- ROLES A VISITOR MAY CHOOSE AT SIGN-UP ----
export const SELECTABLE_ROLES = [
  {
    value: 'Innovator',
    label: 'Innovator',
    icon: '💡',
    tagline: 'Turn problems into ideas',
    desc: 'Submit innovation ideas, build projects and gather collaborators.',
  },
  {
    value: 'Researcher',
    label: 'Researcher',
    icon: '🔬',
    tagline: 'Depth and evidence',
    desc: 'Contribute technical rigour, validate feasibility and cite prior work.',
  },
  {
    value: 'Entrepreneur',
    label: 'Entrepreneur',
    icon: '🚀',
    tagline: 'Take ideas to market',
    desc: 'Shape business models, pursue funding and lead commercialisation.',
  },
  {
    value: 'Mentor',
    label: 'Mentor',
    icon: '🎓',
    tagline: 'Guide the next builders',
    desc: 'Advise innovators, review ideas and accept mentorship requests.',
  },
  {
    value: 'Investor',
    label: 'Investor',
    icon: '💰',
    tagline: 'Back what matters',
    desc: 'Discover promising projects and register funding interest.',
  },
  {
    value: 'Organization',
    label: 'Organization',
    icon: '🏛️',
    tagline: 'Host the challenge',
    desc: 'Publish innovation challenges and manage submissions at scale.',
  },
]

// ---- ROLES ONLY AN ADMINISTRATOR MAY GRANT ----
// Shown on the sign-up page as context, never selectable. A visitor
// must not be able to make themselves an Admin by editing the request.
export const ASSIGNED_ROLES = [
  { value: 'Judge',     label: 'Judge',     icon: '⚖️',  desc: 'Scores challenge submissions (M9). Granted by the hosting organization.' },
  { value: 'Moderator', label: 'Moderator', icon: '🛡️',  desc: 'Reviews reported content (M7/M14). Granted by an administrator.' },
  { value: 'Admin',     label: 'Admin',     icon: '⚙️',  desc: 'Full platform administration (M14). Granted by an administrator.' },
]

export const DEFAULT_ROLE = 'Innovator'
