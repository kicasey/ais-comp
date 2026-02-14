# Test Data

Sample data for development and testing. Automatically seeded on app startup via `DataSeeder`.

## Files

| File | Description |
|------|-------------|
| `sample-logins.json` | Test user credentials (email, password, role, displayName). Seeded into `Users` table on first run. |
| `sample-employees.json` | Employee profiles (name, position, department, location). |
| `sample-managers.json` | Manager profiles with direct report lists. |
| `sample-jobs.json` | Job postings (title, department, location, description). Seeded into `Jobs` table if empty. |
| `sample-resumes.json` | Resume metadata and summaries per employee. |
| `sample-recommendations.json` | Precomputed job-to-employee match scores with confidence percentages. |
| `sample-resume-content.txt` | Plain-text resume snippets for AI/matching tests. |

## Test Logins

All employees use password **`Test123!`**:

| Email | Name |
|-------|------|
| jchen@ey.com | James Chen |
| smartinez@ey.com | Sarah Martinez |
| mjohnson@ey.com | Marcus Johnson |
| enguyen@ey.com | Emily Nguyen |
| dpark@ey.com | David Park |
| athompson@ey.com | Alexandra Thompson |
| mrodriguez@ey.com | Michael Rodriguez |
| jwilliams@ey.com | Jessica Williams |
| rkim@ey.com | Robert Kim |
| rfoster@ey.com | Rachel Foster |

**Do not use these passwords in production.**

## How Seeding Works

On startup, `DataSeeder` (in `Data/DataSeeder.cs`):

1. Applies pending EF Core migrations
2. If the `Users` table is empty, reads `sample-logins.json`, hashes passwords with BCrypt, and creates a `User` for each entry. For employees, also creates a linked `EmployeeProfile`.
3. If the `Jobs` table is empty, reads `sample-jobs.json` and inserts the job postings.
4. In Development mode, syncs passwords to match the JSON file (useful if you change test passwords).

## Adding New Test Data

- **New user:** Add an entry to `sample-logins.json` with `userId`, `email`, `password`, `role` (`"employee"` or `"manager"`), and `displayName`. Restart the app (or delete `talent.db` to re-seed).
- **New job:** Add to `sample-jobs.json`. Jobs only seed if the table is empty, so clear the table or delete `talent.db` first.
- **Resume uploads:** Upload real PDF/DOCX files through the UI. The candidate name is pulled from the logged-in user's display name.
