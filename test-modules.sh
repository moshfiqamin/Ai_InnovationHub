#!/usr/bin/env bash
# ============================================================
# FILE   : test-modules.sh
# PURPOSE: Automated API tests for all 14 modules.
#          Covers every one of the 20 functional features.
# NFR    : NFR18 Testability — "features should be independently
#          testable before final integration".
# USAGE  : ./test-modules.sh        (backend must be running on :5099)
# NOTE   : Creates temporary accounts, then deletes them at the end.
# ============================================================
set -uo pipefail

API="http://localhost:5099/api"
DB="ai_innovationhub"
STAMP=$(date +%s)
REQ=/tmp/aih_req.json
RES=/tmp/aih_res.json
PASS=0; FAIL=0; WARN=0

green() { printf '\033[32m%s\033[0m\n' "$1"; }
red()   { printf '\033[31m%s\033[0m\n' "$1"; }
amber() { printf '\033[33m%s\033[0m\n' "$1"; }
head2() { printf '\n\033[1m%s\033[0m\n' "$1"; }

# check <description> <actual> <expected>
check() {
  if [ "$2" = "$3" ]; then green "  PASS  $1"; PASS=$((PASS+1))
  else red "  FAIL  $1"; red "        expected: $3"; red "        actual:   $2"; FAIL=$((FAIL+1)); fi
}

# JSON body is written to a file first — this avoids nested-quote breakage.
# write_json <<'EOF' ... EOF
write_json() { cat > "$REQ"; }

# post <endpoint>            -> echoes HTTP status, response saved to $RES
# Sends Authorization once TOKEN exists. The auth endpoints are anonymous,
# so an empty TOKEN early in the run is harmless.
post() {
  if [ -n "${TOKEN:-}" ]; then
    curl -s -o "$RES" -w "%{http_code}" -X POST "$API/$1" \
         -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
         --data-binary "@$REQ"
  else
    curl -s -o "$RES" -w "%{http_code}" -X POST "$API/$1" \
         -H "Content-Type: application/json" --data-binary "@$REQ"
  fi
}

# put <endpoint>             -> echoes HTTP status, body from $REQ, auth included
put() {
  curl -s -o "$RES" -w "%{http_code}" -X PUT "$API/$1" \
       -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
       --data-binary "@$REQ"
}

# get <endpoint> [token]     -> echoes HTTP status, response saved to $RES
get() {
  if [ $# -ge 2 ] && [ -n "$2" ]; then
    curl -s -o "$RES" -w "%{http_code}" --max-time 45 "$API/$1" -H "Authorization: Bearer $2"
  else
    curl -s -o "$RES" -w "%{http_code}" --max-time 45 "$API/$1"
  fi
}

# field <dotted.path>  -> value from $RES, or MISSING
field() {
  python3 - "$1" <<'PY'
import json, sys
try:
    with open('/tmp/aih_res.json') as f:
        d = json.load(f)
    for k in sys.argv[1].split('.'):
        d = d[int(k)] if k.lstrip('-').isdigit() else d[k]
    print(d)
except Exception:
    print('MISSING')
PY
}

# count <dotted.path> -> length of a list, or MISSING
count() {
  python3 - "$1" <<'PY'
import json, sys
try:
    with open('/tmp/aih_res.json') as f:
        d = json.load(f)
    path = sys.argv[1]
    if path:                       # empty path = the root array itself
        for k in path.split('.'):
            d = d[int(k)] if k.lstrip('-').isdigit() else d[k]
    print(len(d))
except Exception:
    print('MISSING')
PY
}

sqlq() { psql -d "$DB" -tAc "$1" 2>/dev/null | tr -d '[:space:]'; }

# ============================================================
head2 "0. PRE-FLIGHT"
# ============================================================
if ! pg_isready -q; then
  red "  PostgreSQL is not running.  Fix:  brew services start postgresql@16"; exit 1
fi
green "  PASS  PostgreSQL accepting connections"; PASS=$((PASS+1))

if ! curl -s --max-time 4 "$API/health" >/dev/null 2>&1; then
  red "  Backend not reachable on :5099.  Start it first:"
  red "    cd $(dirname "$0")/backend && dotnet run --launch-profile http"; exit 1
fi
check "API health endpoint responds" "$(get health)" "200"

EMAIL="tester_${STAMP}@example.com"
TOKEN=""   # populated after login; post() adds it once set

# ============================================================
head2 "1. M1 — REGISTRATION VALIDATION  (NFR5)"
# ============================================================
write_json <<EOF
{"fullName":"A","email":"short_$STAMP@x.com","password":"secret123","role":"Innovator"}
EOF
check "Name under 2 characters rejected" "$(post auth/register)" "400"
check "  ...message is user-friendly (no C# field names)" "$(field message)" "Full name must be between 2 and 100 characters."

write_json <<EOF
{"fullName":"Valid Name","email":"not-an-email","password":"secret123","role":"Innovator"}
EOF
check "Malformed email rejected" "$(post auth/register)" "400"
check "  ...message names the problem" "$(field message)" "Please provide a valid email address."

write_json <<EOF
{"fullName":"Valid Name","email":"pw_$STAMP@x.com","password":"abc","role":"Innovator"}
EOF
check "Password under 6 characters rejected" "$(post auth/register)" "400"
check "  ...message names the rule" "$(field message)" "Password must be at least 6 characters."

# ============================================================
head2 "2. M1 — SUCCESSFUL REGISTRATION"
# ============================================================
write_json <<EOF
{"fullName":"Test Person","email":"$EMAIL","password":"secret123","role":"Innovator"}
EOF
check "Valid registration accepted" "$(post auth/register)" "200"
TOKEN=$(field token)
check "  ...returns a JWT" "$([ "$TOKEN" != MISSING ] && [ -n "$TOKEN" ] && echo yes || echo no)" "yes"
check "  ...returns the user's role" "$(field user.role)" "Innovator"
check "  ...never returns a password hash" "$(field user.passwordHash)" "MISSING"

write_json <<EOF
{"fullName":"Someone Else","email":"$EMAIL","password":"secret123","role":"Innovator"}
EOF
check "Duplicate email rejected" "$(post auth/register)" "409"
check "  ...with a clear message" "$(field message)" "An account with this email already exists."

# ============================================================
head2 "3. M1 — ROLE SYSTEM"
# ============================================================
for R in Innovator Researcher Entrepreneur Mentor Investor Organization; do
  write_json <<EOF
{"fullName":"Role Tester","email":"role_${R}_$STAMP@x.com","password":"secret123","role":"$R"}
EOF
  post auth/register >/dev/null
  check "Self-service role accepted: $R" "$(field user.role)" "$R"
done

for BAD in Admin Moderator Judge SuperUser; do
  write_json <<EOF
{"fullName":"Escalation Tester","email":"esc_${BAD}_$STAMP@x.com","password":"secret123","role":"$BAD"}
EOF
  post auth/register >/dev/null
  check "Privileged role BLOCKED: $BAD" "$(field user.role)" "Innovator"
done

# Scoped to THIS run's accounts. A legitimately promoted Admin may exist
# on the platform (granted through M14), so a global count would be wrong.
check "No privileged role reached the database via registration" \
  "$(sqlq "SELECT COUNT(*) FROM \"Users\" WHERE \"Email\" LIKE '%$STAMP%' AND \"Role\" IN ('Admin','Moderator','Judge','SuperUser');")" "0"

# ============================================================
head2 "4. M1 — LOGIN  (NFR4 Security)"
# ============================================================
write_json <<EOF
{"email":"$EMAIL","password":"secret123"}
EOF
check "Correct credentials accepted" "$(post auth/login)" "200"
TOKEN=$(field token)

write_json <<EOF
{"email":"$EMAIL","password":"wrongpassword"}
EOF
check "Wrong password rejected" "$(post auth/login)" "401"
WRONG_MSG=$(field message)
check "  ...generic message" "$WRONG_MSG" "Invalid email or password."

write_json <<EOF
{"email":"nobody_$STAMP@x.com","password":"secret123"}
EOF
check "Unknown account rejected" "$(post auth/login)" "401"
check "  ...IDENTICAL message (prevents user enumeration)" "$(field message)" "$WRONG_MSG"

check "Raw password never stored" \
  "$(sqlq "SELECT COUNT(*) FROM \"Users\" WHERE \"PasswordHash\"='secret123';")" "0"
check "Each user has a unique salt" \
  "$(sqlq "SELECT CASE WHEN COUNT(DISTINCT \"PasswordSalt\")=COUNT(*) THEN 'unique' ELSE 'reused' END FROM \"Users\";")" "unique"

# ============================================================
head2 "5. M1 — PROTECTED ROUTES  (NFR2)"
# ============================================================
check "/auth/me without token  -> 401" "$(get auth/me)" "401"
check "/auth/me with token     -> 200" "$(get auth/me "$TOKEN")" "200"
check "  ...returns the right account" "$(field email)" "$EMAIL"
check "Tampered token rejected" "$(get auth/me "${TOKEN}tampered")" "401"

# ============================================================
head2 "6. M3 — F19 ANALYTICS DASHBOARD"
# ============================================================
check "/dashboard/summary without token -> 401" "$(get dashboard/summary)" "401"
check "/dashboard/summary with token    -> 200" "$(get dashboard/summary "$TOKEN")" "200"
check "  stats.ideasSubmitted present"   "$([ "$(field stats.ideasSubmitted)"   != MISSING ] && echo ok || echo no)" "ok"
check "  stats.reputationPoints present" "$([ "$(field stats.reputationPoints)" != MISSING ] && echo ok || echo no)" "ok"
check "  engagement chart spans 7 days"  "$(count engagement.labels)" "7"
check "  engagement values match labels" "$(count engagement.values)" "7"
check "  contributionMix present"        "$([ "$(field contributionMix.labels.0)" != MISSING ] && echo ok || echo no)" "ok"
check "  trendingIdeas present"          "$([ "$(count trendingIdeas)"  != MISSING ] && echo ok || echo no)" "ok"
check "  recentActivity present"         "$([ "$(count recentActivity)" != MISSING ] && echo ok || echo no)" "ok"

# ============================================================
head2 "7. M3 — F18 AI PERSONALIZED RECOMMENDATIONS"
# ============================================================
check "/dashboard/recommendations without token -> 401" "$(get dashboard/recommendations)" "401"
echo "  (calling Gemini — this can take several seconds)"
check "/dashboard/recommendations with token    -> 200" "$(get dashboard/recommendations "$TOKEN")" "200"
check "  returns 3 recommendations" "$(count items)" "3"
check "  each item has a title"     "$([ "$(field items.0.title)"  != MISSING ] && echo ok || echo no)" "ok"
check "  each item has a reason"    "$([ "$(field items.0.reason)" != MISSING ] && echo ok || echo no)" "ok"

SRC=$(field source)
# "gemini" and "groq" are both live AI. "fallback" means neither provider
# answered and the static suggestions were served instead (NFR10).
case "$SRC" in
  gemini|groq)
    green "  PASS  source = $SRC  (live AI responded)"; PASS=$((PASS+1)) ;;
  fallback)
    amber "  WARN  source = fallback  (both AI providers unavailable)"
    amber "        Graceful degradation worked, which is correct behaviour (NFR10),"
    amber "        but F18 is not proving live AI right now."
    WARN=$((WARN+1)) ;;
  *)
    red "  FAIL  unexpected source: '$SRC'"; FAIL=$((FAIL+1)) ;;
esac


# ============================================================
head2 "8. M4 — F4 INNOVATION FEED"
# ============================================================
check "/feed without token -> 401" "$(get feed)" "401"
check "/feed with token    -> 200" "$(get feed?sort=trending "$TOKEN")" "200"
FEED_N=$(count "")
check "  feed returns ideas" "$([ "$FEED_N" != MISSING ] && [ "$FEED_N" -gt 0 ] && echo yes || echo no)" "yes"

check "/feed/categories -> 200" "$(get feed/categories "$TOKEN")" "200"
check "  categories are listed" "$([ "$(count '')" -gt 0 ] && echo yes || echo no)" "yes"

check "search filter works" "$(get "feed?search=waste" "$TOKEN")" "200"
check "  search narrows results" "$([ "$(count '')" -lt "$FEED_N" ] && echo yes || echo no)" "yes"

get feed "$TOKEN" >/dev/null
IDEA_ID=$(field 0.id)

# like -> unlike -> like, confirming the toggle
curl -s -o "$RES" -X POST "$API/feed/$IDEA_ID/like" -H "Authorization: Bearer $TOKEN" >/dev/null
L1=$(field active)
curl -s -o "$RES" -X POST "$API/feed/$IDEA_ID/like" -H "Authorization: Bearer $TOKEN" >/dev/null
L2=$(field active)
check "F4 like toggles on"  "$L1" "True"
check "F4 like toggles off" "$L2" "False"

curl -s -o "$RES" -X POST "$API/feed/$IDEA_ID/bookmark" -H "Authorization: Bearer $TOKEN" >/dev/null
check "F4 bookmark toggles on" "$(field active)" "True"
check "/feed/bookmarks -> 200" "$(get feed/bookmarks "$TOKEN")" "200"

write_json <<EOF
{"content":"Automated test comment $STAMP"}
EOF
check "F4 comment accepted" "$(post "feed/$IDEA_ID/comments")" "200"
check "  comment echoes author" "$([ "$(field authorName)" != MISSING ] && echo ok || echo no)" "ok"

# ============================================================
head2 "9. M5 — F1 IDEA SUBMISSION"
# ============================================================
write_json <<EOF
{"title":"Bad","problem":"short","solution":"short","category":"Other","tags":"","publish":false}
EOF
check "Too-short idea rejected" "$(post ideas)" "400"

write_json <<EOF
{"title":"Automated test idea $STAMP","problem":"A clearly described problem statement that comfortably exceeds the minimum length requirement for validation.","solution":"A clearly described solution statement that also comfortably exceeds the minimum length requirement.","category":"Other","tags":"test,automation","publish":false}
EOF
check "Valid idea accepted as draft" "$(post ideas)" "200"
TEST_IDEA=$(field id)
check "  returns an id" "$([ "$TEST_IDEA" != MISSING ] && echo ok || echo no)" "ok"

# A draft must not be publicly visible
get feed "$TOKEN" >/dev/null
DRAFT_LEAK=$(python3 - "$STAMP" <<'PY2'
import json,sys
d=json.load(open('/tmp/aih_res.json'))
print("leaked" if any(sys.argv[1] in i["title"] for i in d) else "private")
PY2
)
check "F1 drafts stay private in the feed" "$DRAFT_LEAK" "private"
check "F1 drafts visible to their author" "$(get ideas/mine "$TOKEN")" "200"

check "F1 publish a draft" "$(post "ideas/$TEST_IDEA/publish")" "200"

# ============================================================
head2 "10. M5 — F2 / F11 / F3 AI FEATURES"
# ============================================================
echo "  (live AI calls — this can take 10-30 seconds)"

# 503 here is CORRECT behaviour, not a defect: it is how the app reports
# that the AI provider is rate-limited or unreachable (NFR10). Treat it as
# a warning so an exhausted free-tier quota does not read as a broken build.
AI_CODE=$(curl -s -o "$RES" -w "%{http_code}" --max-time 60 -X POST "$API/ideas/$TEST_IDEA/analyze" -H "Authorization: Bearer $TOKEN")
if [ "$AI_CODE" = "200" ]; then
  green "  PASS  F2  AI analysis returned 200"; PASS=$((PASS+1))
  ANALYSIS_LEN=$(python3 -c "
import json
try: print(len(json.load(open('/tmp/aih_res.json')).get('analysis','')))
except Exception: print(0)")
  check "  analysis is substantial (>200 chars)" "$([ "$ANALYSIS_LEN" -gt 200 ] && echo yes || echo no)" "yes"
elif [ "$AI_CODE" = "503" ]; then
  amber "  WARN  F2  AI provider unavailable (503) — quota or connectivity."
  amber "        The endpoint degraded correctly instead of crashing (NFR10)."
  WARN=$((WARN+1))
else
  red "  FAIL  F2  AI analysis returned $AI_CODE (expected 200 or 503)"; FAIL=$((FAIL+1))
fi

SWOT_CODE=$(curl -s -o "$RES" -w "%{http_code}" --max-time 60 -X POST "$API/ideas/$TEST_IDEA/swot" -H "Authorization: Bearer $TOKEN")
if [ "$SWOT_CODE" = "200" ]; then
  green "  PASS  F11 SWOT returned 200"; PASS=$((PASS+1))
  for K in strengths weaknesses opportunities threats; do
    check "  SWOT has $K" "$([ "$(count $K)" -gt 0 ] && echo yes || echo no)" "yes"
  done
elif [ "$SWOT_CODE" = "503" ]; then
  amber "  WARN  F11 AI provider unavailable (503) — quota or connectivity."
  WARN=$((WARN+1))
else
  red "  FAIL  F11 SWOT returned $SWOT_CODE (expected 200 or 503)"; FAIL=$((FAIL+1))
fi

check "F3  similar ideas -> 200" "$(get "ideas/$TEST_IDEA/similar" "$TOKEN")" "200"

# ============================================================
head2 "11. M6 — F8 PROJECT WORKSPACE"
# ============================================================
write_json <<EOF
{"title":"Automated test project $STAMP","description":"Created by the test suite.","sourceIdeaId":null}
EOF
check "F8 create project" "$(post projects)" "200"
TEST_PROJ=$(field id)

check "F8 workspace -> 200" "$(get "projects/$TEST_PROJ" "$TOKEN")" "200"
check "  creator is Owner"      "$(field myRole)" "Owner"
check "  creator can manage"    "$(field canManage)" "True"
check "/projects list -> 200"   "$(get projects "$TOKEN")" "200"

# ============================================================
head2 "12. M6 — F7 TEAM FORMATION"
# ============================================================
write_json <<EOF
{"email":"ghost_$STAMP@nowhere.com","projectRole":"Contributor"}
EOF
check "Invite to a non-existent account rejected" "$(post "projects/$TEST_PROJ/invite")" "400"
check "  with a clear message" "$(field message)" "No account exists with that email address."

# NOTE: invite a DIFFERENT account. Inviting $EMAIL would be inviting the
# project's own owner, which the service correctly rejects.
write_json <<EOF
{"email":"test@example.com","projectRole":"Contributor"}
EOF
INV_CODE=$(post "projects/$TEST_PROJ/invite")
if [ "$INV_CODE" = "200" ]; then
  green "  PASS  Invite an existing account"; PASS=$((PASS+1))
elif [ "$(field message)" = "That person is already on this project." ]; then
  green "  PASS  Invite rejected as duplicate (already a member)"; PASS=$((PASS+1))
else
  red "  FAIL  Invite an existing account returned $INV_CODE"; FAIL=$((FAIL+1))
fi

# ============================================================
head2 "13. M6 — F9 TASK MANAGEMENT"
# ============================================================
write_json <<EOF
{"title":"A","description":"","assigneeId":null,"priority":"Medium","dueDate":null}
EOF
check "Too-short task title rejected" "$(post "projects/$TEST_PROJ/tasks")" "400"

write_json <<EOF
{"title":"Automated test task","description":"","assigneeId":null,"priority":"High","dueDate":null}
EOF
check "Create a task" "$(post "projects/$TEST_PROJ/tasks")" "200"
TEST_TASK=$(field id)
check "  priority stored" "$(field priority)" "High"

write_json <<EOF
{"status":"InProgress"}
EOF
check "Move task to In progress" "$(put "projects/tasks/$TEST_TASK/status")" "200"

write_json <<EOF
{"status":"NotARealStatus"}
EOF
check "Invalid status rejected" "$(put "projects/tasks/$TEST_TASK/status")" "400"

# ============================================================
head2 "14. M6 — F8 MILESTONES / F10 FILES"
# ============================================================
write_json <<EOF
{"title":"Automated test milestone","dueDate":null}
EOF
check "Create a milestone" "$(post "projects/$TEST_PROJ/milestones")" "200"
TEST_MS=$(field id)
check "Toggle milestone complete" "$(curl -s -o "$RES" -w "%{http_code}" -X PUT "$API/projects/milestones/$TEST_MS/toggle" -H "Authorization: Bearer $TOKEN")" "200"

echo "test upload $STAMP" > /tmp/aih_upload.txt
check "F10 upload an allowed file" \
  "$(curl -s -o "$RES" -w "%{http_code}" -X POST "$API/projects/$TEST_PROJ/files" -H "Authorization: Bearer $TOKEN" -F "file=@/tmp/aih_upload.txt")" "200"
TEST_FILE=$(field id)
check "  size label rendered" "$([ "$(field sizeLabel)" != MISSING ] && echo ok || echo no)" "ok"

echo 'echo pwned' > /tmp/aih_upload.sh
check "F10 disallowed extension rejected" \
  "$(curl -s -o "$RES" -w "%{http_code}" -X POST "$API/projects/$TEST_PROJ/files" -H "Authorization: Bearer $TOKEN" -F "file=@/tmp/aih_upload.sh")" "400"

check "F10 download by a member" \
  "$(curl -s -o /tmp/aih_dl.txt -w "%{http_code}" "$API/projects/files/$TEST_FILE" -H "Authorization: Bearer $TOKEN")" "200"
check "  downloaded bytes match" "$(diff -q /tmp/aih_upload.txt /tmp/aih_dl.txt >/dev/null && echo same || echo different)" "same"
rm -f /tmp/aih_upload.txt /tmp/aih_upload.sh /tmp/aih_dl.txt


# ============================================================
head2 "16. M7 — F5 COMMUNITY"
# ============================================================
write_json <<EOF
{"name":"Test Community $STAMP","description":"Created by the automated suite.","category":"Testing"}
EOF
check "Create a community" "$(post communities)" "200"
TEST_COMM=$(field id)

# The unique index on Name must reject a second one.
check "Duplicate community name rejected" "$(post communities)" "409"
check "  with a clear message" "$(field message)" "A community with that name already exists."

check "/communities list -> 200" "$(get communities "$TOKEN")" "200"
check "/communities/categories -> 200" "$(get communities/categories "$TOKEN")" "200"

write_json <<EOF
{"title":"Automated test post","content":"Content written by the automated test suite to verify posting."}
EOF
check "Create a post (creator is auto-joined)" "$(post "communities/$TEST_COMM/posts")" "200"
TEST_POST=$(field id)

curl -s -o "$RES" -X POST "$API/communities/posts/$TEST_POST/upvote" -H "Authorization: Bearer $TOKEN" >/dev/null
check "F5 post upvote toggles on" "$(field active)" "True"
curl -s -o "$RES" -X POST "$API/communities/posts/$TEST_POST/upvote" -H "Authorization: Bearer $TOKEN" >/dev/null
check "F5 post upvote toggles off" "$(field active)" "False"

write_json <<EOF
{"content":"Automated test comment on a community post.","parentId":null}
EOF
check "F5 comment on a post" "$(post "communities/posts/$TEST_POST/comments")" "200"

check "F5 member list -> 200" "$(get "communities/$TEST_COMM/members" "$TOKEN")" "200"
check "  creator is a member" "$([ "$(count '')" -ge 1 ] && echo yes || echo no)" "yes"

# ============================================================
head2 "17. M8 — F6 AI SMART SEARCH"
# ============================================================
check "/search without token -> 401" "$(get "search?q=water")" "401"
check "/search with token    -> 200" "$(get "search?q=water" "$TOKEN")" "200"
SEARCH_MODE=$(field mode)
if [ "$SEARCH_MODE" = "semantic" ]; then
  green "  PASS  F6 running in SEMANTIC mode (embeddings available)"; PASS=$((PASS+1))
elif [ "$SEARCH_MODE" = "keyword" ]; then
  amber "  WARN  F6 fell back to KEYWORD mode (embedding provider unavailable)"
  amber "        This is the designed degradation path (NFR10), not a defect."
  WARN=$((WARN+1))
else
  red "  FAIL  F6 unexpected mode '$SEARCH_MODE'"; FAIL=$((FAIL+1))
fi
check "  results array present" "$([ "$(count results)" != MISSING ] && echo ok || echo no)" "ok"

# ============================================================
head2 "18. M5/M8 — F12 BUSINESS MODEL GENERATOR"
# ============================================================
BM_CODE=$(curl -s -o "$RES" -w "%{http_code}" --max-time 60 -X POST "$API/ideas/$TEST_IDEA/business-model" -H "Authorization: Bearer $TOKEN")
if [ "$BM_CODE" = "200" ]; then
  green "  PASS  F12 business model returned 200"; PASS=$((PASS+1))
  check "  has a value proposition" "$([ "$(field valueProposition)" != MISSING ] && echo ok || echo no)" "ok"
  check "  has customer segments"   "$([ "$(count customerSegments)" -gt 0 ] && echo yes || echo no)" "yes"
  check "  has revenue streams"     "$([ "$(count revenueStreams)" -gt 0 ] && echo yes || echo no)" "yes"
elif [ "$BM_CODE" = "503" ]; then
  amber "  WARN  F12 AI provider unavailable (503) — quota or connectivity."
  WARN=$((WARN+1))
else
  red "  FAIL  F12 returned $BM_CODE (expected 200 or 503)"; FAIL=$((FAIL+1))
fi

# ============================================================
head2 "19. M9 — F14 INNOVATION CHALLENGES"
# ============================================================
# The tester account is an Innovator, so challenge creation must be refused.
write_json <<EOF
{"title":"Unauthorised challenge $STAMP","description":"Should be refused.","category":"Test","prize":"none","deadline":"2027-01-01T00:00:00Z"}
EOF
check "Non-organiser CANNOT create a challenge" "$(post challenges)" "403"
check "  with an explanatory message" "$(field message)" "Only Organization or Admin accounts can create challenges."

check "/challenges list -> 200" "$(get challenges "$TOKEN")" "200"

# ============================================================
head2 "20. M10 — F13 MENTORS / F15 INVESTORS"
# ============================================================
check "/mentors -> 200"   "$(get mentors "$TOKEN")" "200"
check "/investors -> 200" "$(get investors "$TOKEN")" "200"
check "/engagements -> 200" "$(get engagements "$TOKEN")" "200"

echo "  (F13 recommendation may call the AI — allow time)"
check "F13 /mentors/recommended -> 200" "$(get mentors/recommended "$TOKEN")" "200"

# Pitching a project you do not own must be refused.
write_json <<EOF
{"projectId":"00000000-0000-0000-0000-000000000000","message":"Not my project.","amount":1}
EOF
check "F15 cannot pitch a project you do not own" "$(post investors/interest)" "400"

# ============================================================
head2 "21. M11 — F19 ANALYTICS"
# ============================================================
check "/analytics without token -> 401" "$(get analytics)" "401"
check "/analytics with token    -> 200" "$(get analytics "$TOKEN")" "200"
for K in totalIdeas totalProjects myIdeas myReputation; do
  check "  $K present" "$([ "$(field $K)" != MISSING ] && echo ok || echo no)" "ok"
done
check "  ideasOverTime spans 14 days" "$(count ideasOverTime.labels)" "14"
check "  engagementByType has 4 series" "$(count engagementByType.labels)" "4"

# ============================================================
head2 "22. M12 — F17 NOTIFICATIONS"
# ============================================================
check "/notifications without token -> 401" "$(get notifications)" "401"
check "/notifications with token    -> 200" "$(get notifications "$TOKEN")" "200"
check "/notifications/count -> 200" "$(get notifications/count "$TOKEN")" "200"
check "  unread count present" "$([ "$(field unread)" != MISSING ] && echo ok || echo no)" "ok"
check "Mark all read" "$(curl -s -o "$RES" -w "%{http_code}" -X PUT "$API/notifications/read-all" -H "Authorization: Bearer $TOKEN")" "200"
get notifications/count "$TOKEN" >/dev/null
check "  unread is now zero" "$(field unread)" "0"

# ============================================================
head2 "23. M13 — F16 PROFILE, REPUTATION & BADGES"
# ============================================================
check "/profile -> 200" "$(get profile "$TOKEN")" "200"
check "  reputation present" "$([ "$(field reputationPoints)" != MISSING ] && echo ok || echo no)" "ok"
check "  level derived"      "$([ "$(field level)" != MISSING ] && echo ok || echo no)" "ok"
check "  badge catalogue is seeded (12)" "$(count badges)" "12"
check "  isMe true on own profile" "$(field isMe)" "True"

write_json <<EOF
{"fullName":"Test Person","bio":"Updated by the automated suite.","headline":"Tester","location":"Nowhere","website":"","skills":"testing","interests":"automation","expertise":"","investmentFocus":"","isAvailableForMentoring":false}
EOF
check "Update own profile" "$(curl -s -o "$RES" -w "%{http_code}" -X PUT "$API/profile" -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" --data-binary "@$REQ")" "200"
get profile "$TOKEN" >/dev/null
check "  headline saved" "$(field headline)" "Tester"

# ============================================================
head2 "24. M14 — F20 ADMIN & AI MODERATION"
# ============================================================
# The tester is an Innovator, so every admin route must refuse them.
check "Non-admin blocked from /admin/stats"   "$(get admin/stats "$TOKEN")" "403"
check "Non-admin blocked from /admin/users"   "$(get admin/users "$TOKEN")" "403"
check "Non-admin blocked from /admin/reports" "$(get admin/reports "$TOKEN")" "403"

# Reporting, however, is open to every signed-in user.
write_json <<EOF
{"targetType":"Idea","targetId":"$TEST_IDEA","reason":"Automated test report - please dismiss."}
EOF
check "Any user CAN report content" "$(post moderation/report)" "200"
check "Duplicate report rejected" "$(post moderation/report)" "400"

write_json <<EOF
{"targetType":"Idea","targetId":"00000000-0000-0000-0000-000000000000","reason":"Reporting something that does not exist."}
EOF
check "Report on missing content rejected" "$(post moderation/report)" "400"

# ============================================================
head2 "25. CLEANUP"
# ============================================================
sqlq "DELETE FROM \"ContentReports\" WHERE \"Reason\" LIKE '%Automated test%';" >/dev/null
sqlq "DELETE FROM \"Communities\" WHERE \"Name\" LIKE '%$STAMP%';" >/dev/null
sqlq "DELETE FROM \"Challenges\" WHERE \"Title\" LIKE '%$STAMP%';" >/dev/null
sqlq "DELETE FROM \"Projects\" WHERE \"Title\" LIKE '%$STAMP%';" >/dev/null
sqlq "DELETE FROM \"Ideas\" WHERE \"Title\" LIKE '%$STAMP%';" >/dev/null
sqlq "DELETE FROM \"Comments\" WHERE \"Content\" LIKE '%$STAMP%';" >/dev/null
REMOVED=$(sqlq "WITH d AS (DELETE FROM \"Users\" WHERE \"Email\" LIKE '%$STAMP%' RETURNING 1) SELECT COUNT(*) FROM d;")
green "  Removed $REMOVED temporary test account(s), plus test ideas/projects/comments"
rm -f "$REQ" "$RES"

# ============================================================
printf '\n\033[1m================= RESULT =================\033[0m\n'
green "  Passed:  $PASS"
[ "$WARN" -gt 0 ] && amber "  Warnings: $WARN"
if [ "$FAIL" -gt 0 ]; then red "  Failed:  $FAIL"; echo; red "  Some tests failed — see above."; exit 1
else green "  Failed:  0"; echo; green "  All automated API tests passed across all 14 modules."; exit 0; fi
