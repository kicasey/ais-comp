# TalentStrategyAI

A production-ready .NET 8 Web API solution for AI-powered talent strategy management.

## Tech Stack

- **Backend**: .NET 8 Web API
- **Frontend**: Vanilla JS/HTML/CSS (served from wwwroot)
- **Database**: SQL Server (Entity Framework Core)
- **AI Integration**: OpenAI API

## Project Structure

```
TalentStrategyAI/
├── TalentStrategyAI.API/
│   ├── Controllers/          # API Controllers
│   ├── Data/                 # Database context and configurations
│   │   └── AppDbContext.cs
│   ├── Models/               # Data models
│   │   └── EmployeeProfile.cs
│   ├── Services/             # Business logic and external service integrations
│   │   ├── IOpenAIService.cs
│   │   └── OpenAIService.cs
│   ├── wwwroot/              # Frontend static files
│   │   ├── index.html
│   │   ├── css/
│   │   │   └── styles.css
│   │   └── js/
│   │       └── app.js
│   ├── appsettings.json      # Configuration
│   └── Program.cs            # Application entry point
└── TalentStrategyAI.sln      # Solution file
```

## Team Roles

### Frontend Team
- **Location**: `wwwroot/` directory
- **Responsibilities**: 
  - Develop HTML, CSS, and JavaScript for the user interface
  - Create interactive components and user experience
  - Integrate with backend APIs

### Backend Team
- **Location**: `Controllers/` directory
- **Responsibilities**:
  - Design and implement RESTful API endpoints
  - Handle HTTP requests and responses
  - Implement business logic and data validation

### AI Team
- **Location**: `Services/` directory
- **Responsibilities**:
  - Implement OpenAI API integration
  - Develop AI-powered features and services
  - Handle AI-related business logic

## Getting Started

### Prerequisites

- .NET 8 SDK (or .NET 9 SDK)
- Database: SQLite (default, no setup needed) or SQL Server (see setup options below)
- OpenAI API Key (for AI features)

### Database Setup

**Option 1: SQLite (Default - Easiest for Development)**

The project is currently configured to use SQLite, which requires no additional setup. The database file (`TalentStrategyAI.db`) will be created automatically when you run migrations.

**Option 2: SQL Server with Docker (For Production-like Environment)**

1. Install Docker Desktop for Mac if you haven't already
2. Run SQL Server in a Docker container:
   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
      -p 1433:1433 --name sqlserver \
      -d mcr.microsoft.com/mssql/server:2022-latest
   ```
3. Update `appsettings.json` to use SQL Server:
   ```json
   "DefaultConnection": "Server=localhost,1433;Database=TalentStrategyAI;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;MultipleActiveResultSets=true"
   ```
4. Update `Program.cs` to use `UseSqlServer` instead of `UseSqlite`
5. Remove old migrations and create new ones for SQL Server

**Option 3: Azure SQL Database**
- Use an Azure SQL Database connection string:
  ```json
  "DefaultConnection": "Server=tcp:YOUR_SERVER.database.windows.net,1433;Database=TalentStrategyAI;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=true;TrustServerCertificate=false"
  ```

**Option 4: Remote SQL Server**
- Use a connection string pointing to your remote SQL Server instance

### Setup Instructions

1. **Clone the repository** (if applicable)

2. **Database is ready!** 
   - The project is configured to use SQLite by default (no setup required)
   - If you prefer SQL Server, see the Database Setup section above

3. **Configure OpenAI API Key**:
   - Update `appsettings.json` with your OpenAI API key:
   ```json
   "OpenAI": {
     "ApiKey": "your-openai-api-key-here"
   }
   ```

4. **Create the database**:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

5. **Run the application**:
   ```bash
   dotnet run
   ```

6. **Access the application**:
   - Frontend: Navigate to `http://localhost:xxxx/index.html` (replace xxxx with your port)
   - Backend API (Swagger): Navigate to `http://localhost:xxxx/swagger`

## Database Migrations

### Creating a Migration
```bash
dotnet ef migrations add MigrationName
```

### Applying Migrations
```bash
dotnet ef database update
```

### Removing the Last Migration
```bash
dotnet ef migrations remove
```

## Development Workflow

1. **Frontend Development**: 
   - Edit files in `wwwroot/`
   - Changes are automatically served when the application is running

2. **Backend Development**:
   - Add new controllers in `Controllers/`
   - Controllers should inherit from `ControllerBase`
   - Use Swagger UI at `/swagger` to test APIs

3. **Database Changes**:
   - Update models in `Models/`
   - Update `AppDbContext.cs` to include new DbSets
   - Create and apply migrations as needed

4. **AI Service Development**:
   - Implement methods in `OpenAIService.cs`
   - Update `IOpenAIService.cs` interface as needed

## Configuration

### CORS Policy
Currently configured to allow all origins for development ease. Update in `Program.cs` for production:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### Static Files
The application is configured to serve static files from `wwwroot/` and default to `index.html` at the root URL.

## API Documentation

Swagger UI is enabled in development mode. Access it at `/swagger` when running the application.

## Notes

- The `EmployeeProfile` model in `Models/` is a placeholder - update it according to your requirements
- OpenAI service implementation is empty - add your AI integration logic in `Services/OpenAIService.cs`
- Ensure your OpenAI API key is kept secure and not committed to version control

## License

[Add your license information here]

