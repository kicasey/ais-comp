# Resume API (resume-api.campbellthompson.com)

Proxy API for the TalentStrategyAI website. Authenticates with Client ID/Secret and forwards:

- **Chat** → `POST /api/chat` → n8n webhook `http://192.168.1.161:5678/webhook/chat`
- **Resumes** → `POST /api/resume/upload` → n8n webhook `http://192.168.1.161:5678/webhook/resumes`
- **Jobs & recommendations** → `GET /api/jobs`, `GET /api/jobs/{id}`, `GET /api/jobs/{jobId}/recommendations` → MySQL on same Docker network

## Auth (website → API)

Every request must include either:

1. **Headers:** `X-Client-Id`, `X-Client-Secret`
2. **Basic:** `Authorization: Basic base64(clientId:clientSecret)`

Set in config or env:

- `ClientAuth:ClientId`
- `ClientAuth:ClientSecret`

(Use env vars in production, e.g. `ClientAuth__ClientId`, `ClientAuth__ClientSecret`.)

## Contract (matches main site)

- **Chat:** `POST /api/chat`, body JSON `{ "preset": "...", "customText": "..." }`. Response passed through from n8n (site expects `{ "response": "..." }`).
- **Resume:** `POST /api/resume/upload`, `multipart/form-data`: `resume` (file), `candidateName` (string). Response passed through from n8n.

## Hosting (resume-api.campbellthompson.com)

Run behind your reverse proxy (e.g. nginx/Caddy) with HTTPS for `resume-api.campbellthompson.com`. Point the site’s `fetch` base URL to `https://resume-api.campbellthompson.com` and send the same Client ID/Secret in headers (or Basic) on each request.

## Build & run (local)

```bash
cd API_For_Server
dotnet run
```

## Docker (build here, run on server)

**Build** (from repo root or from `API_For_Server`):

```bash
cd API_For_Server
docker build -t resume-api .
```

**Save image** to copy to server (no registry):

```bash
docker save resume-api:latest | gzip > resume-api.tar.gz
# copy resume-api.tar.gz to server, then on server:
# gunzip -c resume-api.tar.gz | docker load
```

**Run on server** (set env vars for auth and webhook URLs; container listens on 8080):

```bash
docker run -d --name resume-api -p 8080:8080 \
  -e ClientAuth__ClientId="your-client-id" \
  -e ClientAuth__ClientSecret="your-client-secret" \
  -e Webhooks__Chat="http://192.168.1.161:5678/webhook/chat" \
  -e Webhooks__Resumes="http://192.168.1.161:5678/webhook/resumes" \
  resume-api:latest
```

Point your reverse proxy at `localhost:8080` for `resume-api.campbellthompson.com`.

## MySQL (jobs & recommendations)

Resume-api reads **jobs** and **job_recommendations** from MySQL (same Docker network; container name `MySQL`). Set the connection string via env:

- `ConnectionStrings__ResumeDb` = `Server=MySQL;Port=3306;Database=resume_ai;User=...;Password=...`

Expected schema (see `schema-jobs-mysql.sql`):

- **jobs**: `id`, `title`, `department`, `location`, `description`
- **job_recommendations**: `job_id`, `resume_id`, `score` (join to `resume_pii.resume_id` / `candidate_name` for names)

If the connection string is missing or MySQL is down, jobs and recommendations endpoints return empty arrays.
