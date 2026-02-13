# Resume API (resume-api.campbellthompson.com)

Proxy API for the TalentStrategyAI website. Sits between the frontend and n8n/MySQL — handles chat, resume uploads, jobs, recommendations, and candidate name lookups.

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/chat` | Manager chat → forwards to n8n webhook |
| `POST` | `/api/employee-chat` | Employee chat → forwards to n8n webhook |
| `POST` | `/api/resume/upload` | Resume upload (multipart/form-data) → saves file + forwards to n8n |
| `GET` | `/api/jobs` | List all jobs from MySQL |
| `GET` | `/api/jobs/{id}` | Get a single job |
| `POST` | `/api/jobs` | Create a job in MySQL |
| `PUT` | `/api/jobs/{id}` | Update a job |
| `DELETE` | `/api/jobs/{id}` | Delete a job |
| `GET` | `/api/jobs/{jobId}/recommendations` | Get candidate recommendations for a job (joins `job_recommendations` + `resume_pii`) |
| `GET` | `/api/names` | Returns `{ "resume_id": "candidate_name" }` map from `resume_pii` table |
| `GET` | `/health` | Health check |

## How it connects

```
Browser → TalentStrategyAI.API → resume-api → n8n (webhooks)
                                            → MySQL (jobs, recommendations, resume_pii)
                                            → Gotenberg (PDF conversion)
                                            → /data/incoming-resumes (file storage)
```

## Configuration

All config is in `appsettings.json` or overridden via environment variables at runtime.

| Config Key | Env Var | Description |
|------------|---------|-------------|
| `ConnectionStrings:ResumeDb` | `ConnectionStrings__ResumeDb` | MySQL connection string |
| `Webhooks:Chat` | `Webhooks__Chat` | n8n webhook URL for manager chat |
| `Webhooks:EmployeeChat` | `Webhooks__EmployeeChat` | n8n webhook URL for employee chat |
| `Webhooks:Resumes` | `Webhooks__Resumes` | n8n webhook URL for resume processing |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0` | Allowed CORS origins |
| `Gotenberg:BaseUrl` | `Gotenberg__BaseUrl` | Gotenberg service URL for PDF→text |
| `Resume:SavePath` | `Resume__SavePath` | Directory to save uploaded resumes |
| `MySQL:JobsTable` | `MySQL__JobsTable` | Jobs table name (default: `jobs`) |
| `MySQL:RecommendationsTable` | `MySQL__RecommendationsTable` | Recommendations table name (default: `job_recommendations`) |
| `MySQL:ResumePiiTable` | `MySQL__ResumePiiTable` | Resume PII table name (default: `resume_pii`) |

## Build & Run (Local)

```bash
cd API_For_Server
dotnet run
```

## Docker (Build → Transfer → Deploy)

**1. Build the image:**

```bash
cd API_For_Server
docker build --no-cache -t resume-api:latest .
```

**2. Save and transfer to server:**

```bash
cd ~/AIS/Main/ais-comp
docker save -o resume-api.tar resume-api:latest
scp resume-api.tar root@Helios:/root/
```

**3. Deploy on server:**

```bash
ssh root@Helios

docker load -i /root/resume-api.tar
docker stop resume-api && docker rm resume-api

docker run -d --name resume-api \
  --network resume_net \
  -p 8080:8080 \
  -v /mnt/cache/appdata/n8n-files/AIS-data/incoming-resumes:/data/incoming-resumes \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e "ConnectionStrings__ResumeDb=Server=mysql;Port=3306;Database=resume_ai;User=root;Password=YOUR_PASSWORD;AllowUserVariables=True" \
  -e "Webhooks__Chat=http://n8n:5678/webhook/chat" \
  -e "Webhooks__EmployeeChat=http://n8n:5678/webhook/employee-chat" \
  -e "Webhooks__Resumes=http://n8n:5678/webhook/resume-input" \
  -e "Cors__AllowedOrigins__0=https://campbellthompson.com" \
  -e "Gotenberg__BaseUrl=http://gotenberg:3000" \
  resume-api:latest
```

Point your reverse proxy at `localhost:8080` for `resume-api.campbellthompson.com`.

## MySQL Schema

Resume-api reads/writes to MySQL database `resume_ai`. All tables must exist before use.

### `jobs`

```sql
CREATE TABLE IF NOT EXISTS jobs (
  id INT AUTO_INCREMENT PRIMARY KEY,
  title VARCHAR(500) NOT NULL,
  department VARCHAR(200) NOT NULL DEFAULT '',
  location VARCHAR(200) NOT NULL DEFAULT '',
  description TEXT NOT NULL DEFAULT ''
);
```

### `resume_pii`

Stores candidate names mapped to resume IDs (populated by the resume upload/processing pipeline).

| Column | Type | Description |
|--------|------|-------------|
| `resume_id` | VARCHAR | Unique resume identifier (UUID) |
| `candidate_name` | VARCHAR | Candidate's display name |

### `job_recommendations`

Stores AI-generated candidate-to-job match scores.

| Column | Type | Description |
|--------|------|-------------|
| `job_id` | VARCHAR | References a job |
| `resume_id` | VARCHAR | References a resume in `resume_pii` |
| `score` | INT | Match score (0–100) |

See `schema-jobs-mysql.sql` for the full schema.

## n8n Webhooks

### Manager Chat (`POST /api/chat`)

- **Receives:** `{ "preset": "...", "customText": "...", "jobId": "...", "employeeId": "...", "userEmail": "...", "userName": "...", "userId": "..." }`
- **Must return:** JSON with at least one of: `response`, `message`, `text`, or `content` (string).
- For recommendations, return: `{ "top_candidates": [{ "candidate_name": "...", "resume_id": "...", "score": 85, "strengths": [...], "gaps": [...], "reason": "..." }] }`
- Optionally include `"id_to_name": { "resume-uuid": "Real Name" }` to map resume IDs to names in chat text.

### Employee Chat (`POST /api/employee-chat`)

- Same request/response format as manager chat, but with employee-specific presets (match to roles, suggest upskilling).

### Resume Upload (`POST /api/resume/upload`)

- Receives `multipart/form-data` with `resume` (file) and `candidateName` (string).
- Saves the file to `/data/incoming-resumes` (mapped via Docker volume).
- Forwards metadata to the n8n resumes webhook.

## Without n8n

- **Chat:** Returns a fallback message from TalentStrategyAI.
- **Recommendations:** Falls back to `GET /api/jobs/{id}/recommendations` (MySQL data or test data).
- **Resume upload:** File is saved but no processing occurs.
- **Names:** Still works (reads directly from MySQL `resume_pii`).
