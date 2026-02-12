# Test Data for TalentStrategyAI

This folder contains **sample data** for development and testing: employees, managers, logins, resumes, jobs, and recommendations.

## Files

| File | Contents |
|------|----------|
| **sample-logins.json** | Test login credentials (email, password, role). Use only in dev/test. |
| **sample-employees.json** | Employee profiles (name, email, position, department, resume file). |
| **sample-managers.json** | Manager profiles and which employees they oversee. |
| **sample-resumes.json** | Resume metadata and short summaries per employee. |
| **sample-jobs.json** | Open job postings (title, department, location, description). |
| **sample-recommendations.json** | Precomputed match recommendations: for each job, a list of employees with confidence %. |
| **sample-resume-content.txt** | Plain-text resume snippets per person (for AI or matching tests). |

## Test logins (quick reference)

**Employees** (password for all: `Test123!`)

- alex.chen@ey.com  
- jordan.smith@ey.com  
- sam.williams@ey.com  
- casey.brown@ey.com  
- morgan.lee@ey.com  

**Managers** (password for all: `Manager1!`)

- taylor.johnson@ey.com  
- riley.davis@ey.com  

**Do not use these passwords in production.**

## How to use this data

1. **Manual testing**  
   Use the logins above when you add real login. Use the employee/manager IDs and names when testing the manager flow (jobs, recommend employees, employee popout).

2. **API / database seeding**  
   - If your **resume-api** or database has an import or seed step, you can map these JSON files to that format and run the import.  
   - If this app has its **own database** (e.g. SQLite/SQL Server with EF Core), a developer can add a **seed** that reads these files (or equivalent C# data) and inserts employees, jobs, and recommendations so the app has data to test against.

3. **Resume uploads**  
   You can upload real PDF/DOCX files and name them to match `resumeFileName` in **sample-employees.json** / **sample-resumes.json**, or use any file and type the **candidate name** from these files when testing the resume upload flow.

4. **Matching and AI**  
   **sample-recommendations.json** and **sample-resume-content.txt** can be used to drive or validate job–employee matching and AI explanations (e.g. in resume-api or in tests).

## Seeding the database (this app)

**Login (Users) seeding:** On startup, the API runs a **DataSeeder** that:

1. Applies any pending EF migrations (e.g. `Users` table and `EmployeeProfile.UserId`).
2. If the `Users` table is empty, reads **sample-logins.json**, hashes passwords with BCrypt, and creates a `User` for each entry. For each user with role `employee`, it also creates an **EmployeeProfile** linked to that user.

So you can **log in** with any of the test logins above (e.g. `alex.chen@ey.com` / `Test123!` or `taylor.johnson@ey.com` / `Manager1!`) as soon as the app and database are running. Ensure SQL Server (or your configured database) is running and run `dotnet run`; on first run, migrations and seed will run.

To seed **jobs** and **recommendations** from JSON (optional): keep calling the resume-api for that data, or add a similar seed step that reads `sample-jobs.json` / `sample-recommendations.json` and inserts into local tables if you add them.
