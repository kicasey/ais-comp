# TalentStrategyAI

AI-powered talent strategy platform built for EY. Matches employees to open roles, provides AI-driven recommendations, and supports resume uploads with automated processing.

## Architecture

```
Browser (SPA)
  └── TalentStrategyAI.API (.NET 9, serves frontend + API)
        ├── SQLite/SQL Server (users, jobs, employee profiles)
        └── resume-api (API_For_Server, Docker)
              ├── MySQL resume_ai (jobs, recommendations, resume_pii)
              ├── n8n (AI chat webhooks, resume processing)
              └── Gotenberg (PDF → text conversion)
```

## Project Structure

```
ais-comp/
├── TalentStrategyAI.API/        # Main application
│   ├── Controllers/             # API endpoints
│   │   ├── AuthController.cs    # Login, register, /me
│   │   ├── JobsController.cs    # Job CRUD + recommendations
│   │   ├── ChatController.cs    # Manager AI chat proxy
│   │   ├── EmployeeChatController.cs  # Employee AI chat proxy
│   │   ├── ResumeController.cs  # Resume upload
│   │   └── NamesController.cs   # Candidate name lookup proxy
│   ├── Data/                    # EF Core context + seeder
│   ├── Models/                  # User, EmployeeProfile, Job
│   ├── Services/                # OpenAI integration (placeholder)
│   ├── TestData/                # Sample logins, jobs, employees
│   └── wwwroot/                 # Frontend SPA
│       ├── index.html           # Single-page app (landing, login, employee/manager views)
│       ├── css/styles.css       # EY-themed styling with dark/light mode
│       ├── js/app.js            # Client-side logic
│       └── images/              # EY logo
├── API_For_Server/              # Resume API (deployed as Docker container)
│   ├── Controllers/             # Chat, jobs, resume, names endpoints
│   ├── Services/                # Webhook proxy service
│   ├── Dockerfile               # Multi-stage .NET build
│   └── schema-jobs-mysql.sql    # MySQL table definitions
└── TalentStrategyAI.sln         # Solution file
```

## Features

- **Dual interface:** Employee view (upload resume, see matches, chat with AI) and Manager view (manage jobs, get candidate recommendations, AI chat)
- **Resume upload:** PDF/DOC/DOCX, processed via n8n pipeline
- **AI chat:** Manager and employee chatbots powered by n8n webhooks
- **Job management:** Full CRUD for managers, synced to MySQL via resume-api
- **Candidate matching:** AI-scored recommendations with strengths, gaps, and upskilling plans
- **Authentication:** JWT-based with BCrypt password hashing
- **Theming:** Dark/light mode toggle with EY branding
- **Per-user state:** Chat history and upload tracking persisted per user

## Tech Stack

- **Backend:** .NET 9 Web API
- **Frontend:** Vanilla JS/HTML/CSS (single-page app served from wwwroot)
- **Database:** SQLite (dev) / SQL Server or MySQL (production)
- **AI:** n8n webhooks (chat, resume processing, recommendations)
- **External:** resume-api (Docker), Gotenberg (PDF conversion)

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQLite (default, no setup needed) or SQL Server

### Run Locally

```bash
cd TalentStrategyAI.API
dotnet run
```

The app starts at `http://localhost:5278`. On first run, it auto-migrates the database and seeds test users from `TestData/sample-logins.json`.

### Test Logins

All employees use password `Test123!`:

| Email | Name |
|-------|------|
| jchen@crimson.ua.edu | James Chen |
| smartinez@crimson.ua.edu | Sarah Martinez |
| mjohnson@crimson.ua.edu | Marcus Johnson |
| enguyen@crimson.ua.edu | Emily Nguyen |
| dpark@crimson.ua.edu | David Park |
| athompson@crimson.ua.edu | Alexandra Thompson |
| mrodriguez@crimson.ua.edu | Michael Rodriguez |
| jwilliams@crimson.ua.edu | Jessica Williams |
| rkim@crimson.ua.edu | Robert Kim |
| rfoster@crimson.ua.edu | Rachel Foster |

### Configuration

Main config is in `appsettings.json` / `appsettings.Development.json`:

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | Database connection string |
| `ResumeApi:BaseUrl` | resume-api URL (e.g. `https://resume-api.campbellthompson.com`) |
| `JWT:SecretKey` | JWT signing key |
| `OpenAI:ApiKey` | OpenAI API key (placeholder) |

### Database Migrations

```bash
cd TalentStrategyAI.API
dotnet ef migrations add MigrationName
dotnet ef database update
```

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/auth/login` | Authenticate user, returns JWT |
| `POST` | `/api/auth/register` | Register new user |
| `GET` | `/api/auth/me` | Get current user (requires JWT) |
| `GET` | `/api/jobs` | List jobs |
| `GET` | `/api/jobs/{id}` | Get job details |
| `POST` | `/api/jobs` | Create job |
| `PUT` | `/api/jobs/{id}` | Update job |
| `DELETE` | `/api/jobs/{id}` | Delete job |
| `GET` | `/api/jobs/{jobId}/recommendations` | Get candidate recommendations |
| `POST` | `/api/chat` | Manager AI chat |
| `POST` | `/api/employee-chat` | Employee AI chat |
| `POST` | `/api/resume/upload` | Upload resume (PDF/DOC/DOCX) |
| `GET` | `/api/names` | Candidate name map (resume_id → name) |

## Deployment

### TalentStrategyAI.API

Deploy as a standard .NET web app (Azure App Service, Linux VM, etc.). Ensure `ResumeApi:BaseUrl` points to the deployed resume-api instance.

### API_For_Server (resume-api)

Deployed as a Docker container. See [API_For_Server/README.md](API_For_Server/README.md) for build/deploy instructions.

## Development Workflow

1. **Frontend:** Edit files in `wwwroot/` — changes are served immediately
2. **Backend:** Add/modify controllers in `Controllers/`
3. **Database:** Update models → create migration → apply
4. **resume-api:** Edit `API_For_Server/` → rebuild Docker image → deploy
