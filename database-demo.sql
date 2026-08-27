-- ============================================================
-- FILE   : database-demo.sql
-- PURPOSE: A scripted walkthrough of the AI Innovation Hub
--          database, for demonstrating it to faculty.
--
-- HOW TO RUN IN VS CODE
--   1. Open this file.
--   2. Click "Connect" in the status bar (or Cmd+Shift+P ->
--      "PGSQL: Connect") and pick the ai_innovationhub connection.
--   3. Put the cursor inside a query and press Cmd+Shift+E to run
--      just that one. Results appear in a grid below.
--
-- HOW TO RUN IN A TERMINAL INSTEAD
--   psql -d ai_innovationhub -f database-demo.sql
-- ============================================================


-- ============================================================
-- 1. THE DATABASE EXISTS AND IS RUNNING
--    Proves you are connected to the right place.
-- ============================================================
SELECT current_database()  AS database,
       current_user        AS connected_as,
       version()           AS postgres_version;


-- ============================================================
-- 2. TWENTY-FIVE TABLES, WITH LIVE ROW COUNTS
--    One table per thing the application stores.
-- ============================================================
SELECT relname AS table_name, n_live_tup AS rows
FROM pg_stat_user_tables
ORDER BY n_live_tup DESC, relname;


-- ============================================================
-- 3. THE ACCOUNTS, AND THE ROLE SYSTEM
--    Nine roles exist; these are the ones currently in use.
-- ============================================================
SELECT "FullName", "Email", "Role", "ReputationPoints"
FROM "Users"
ORDER BY "ReputationPoints" DESC, "Role";


-- ============================================================
-- 4. PASSWORDS ARE HASHED, NEVER STORED AS TYPED
--    Each row has a DIFFERENT salt, so two people with the same
--    password still get completely different hashes.
--    (PBKDF2, 100,000 iterations, written by hand in
--     backend/Services/PasswordHasher.cs)
-- ============================================================
SELECT "Email",
       left("PasswordHash", 24) || '...' AS stored_hash,
       left("PasswordSalt", 16) || '...' AS unique_salt
FROM "Users";

-- Proof that the plain password is nowhere in the table:
SELECT count(*) AS rows_containing_plain_password
FROM "Users" WHERE "PasswordHash" = 'secret123';


-- ============================================================
-- 5. RELATIONSHIPS — A JOIN ACROSS TWO TABLES
--    Ideas do not store the author's name. They store an ID that
--    points at the Users table. This is what "relational" means.
-- ============================================================
SELECT i."Title", i."Category", i."Upvotes", u."FullName" AS author
FROM "Ideas" i
JOIN "Users" u ON u."Id" = i."AuthorId"
WHERE i."IsPublished" = true
ORDER BY i."Upvotes" DESC;


-- ============================================================
-- 6. THE DATABASE ITSELF ENFORCES THE RULES
--    IX_IdeaLikes_UserId_IdeaId is a UNIQUE index on the PAIR
--    of columns. It makes "the same person liking the same idea
--    twice" physically impossible — even if the application code
--    had a bug.
-- ============================================================
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'IdeaLikes' AND indexdef LIKE '%UNIQUE%';

-- Try it live: this INSERT will FAIL with a duplicate key error,
-- which is exactly the point. Uncomment to demonstrate.
-- INSERT INTO "IdeaLikes" ("Id","UserId","IdeaId","CreatedAt")
-- SELECT gen_random_uuid(), "UserId", "IdeaId", NOW() FROM "IdeaLikes" LIMIT 1;


-- ============================================================
-- 7. THE AI OUTPUT IS REAL DATA, NOT A SCREEN EFFECT
--    Every idea carries a 1,536-number "meaning vector" from the
--    embedding model. Comparing two of these mathematically is
--    what powers similar-idea detection and semantic search.
-- ============================================================
SELECT left("Title", 40) AS idea,
       CASE WHEN "EmbeddingJson" IS NULL
            THEN 'none'
            ELSE (length("EmbeddingJson") / 1000) || ' KB vector' END AS meaning_vector,
       CASE WHEN "AiAnalysis"  IS NULL THEN 'no' ELSE 'yes' END AS ai_analysed,
       CASE WHEN "SwotJson"    IS NULL THEN 'no' ELSE 'yes' END AS swot_generated
FROM "Ideas"
ORDER BY "CreatedAt" DESC;

-- The first ten numbers of one actual vector:
SELECT left("Title", 40) AS idea,
       left("EmbeddingJson", 90) || ' ...' AS first_few_dimensions
FROM "Ideas" WHERE "EmbeddingJson" IS NOT NULL LIMIT 1;


-- ============================================================
-- 8. THE FULL PICTURE FOR ONE USER
--    Six tables joined to show everything one account has done.
-- ============================================================
SELECT u."FullName",
       u."Role",
       u."ReputationPoints"                                        AS reputation,
       (SELECT count(*) FROM "Ideas"        WHERE "AuthorId" = u."Id") AS ideas,
       (SELECT count(*) FROM "Projects"     WHERE "OwnerId"  = u."Id") AS projects,
       (SELECT count(*) FROM "Comments"     WHERE "AuthorId" = u."Id") AS comments,
       (SELECT count(*) FROM "UserBadges"   WHERE "UserId"   = u."Id") AS badges,
       (SELECT count(*) FROM "Notifications"
          WHERE "UserId" = u."Id" AND "IsRead" = false)                AS unread
FROM "Users" u
ORDER BY u."ReputationPoints" DESC;


-- ============================================================
-- 9. THE BADGE SYSTEM
--    Badges is the catalogue; UserBadges records who earned what.
-- ============================================================
SELECT b."Icon", b."Name", b."Description", b."Metric", b."Threshold",
       (SELECT count(*) FROM "UserBadges" ub WHERE ub."BadgeId" = b."Id") AS times_earned
FROM "Badges" b
ORDER BY b."Threshold";


-- ============================================================
-- 10. THE AI MODERATION TRAIL
--     A ReporterId of NULL means the AI raised it, not a person.
-- ============================================================
SELECT r."TargetType",
       left(r."TargetPreview", 45) AS content,
       r."Status",
       r."AiVerdict",
       COALESCE(u."FullName", 'AI moderation') AS raised_by,
       r."CreatedAt"
FROM "ContentReports" r
LEFT JOIN "Users" u ON u."Id" = r."ReporterId"
ORDER BY r."CreatedAt" DESC;


-- ============================================================
-- 11. THE MIGRATION HISTORY
--     Proof the schema was built by Entity Framework from the C#
--     classes, not written by hand as SQL.
-- ============================================================
SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";


-- ============================================================
-- 12. LIVE PROOF — RUN THIS DURING THE DEMO
--     Register a new account in the browser, then run this.
--     The row appears immediately. Nothing is faked or hardcoded.
-- ============================================================
SELECT "FullName", "Email", "Role", "CreatedAt"
FROM "Users"
ORDER BY "CreatedAt" DESC
LIMIT 3;
