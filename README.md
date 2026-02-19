# PennyWise

PennyWise is a lightweight ASP.NET Core Razor Pages web application scaffold. It provides a clean starting point for building a server-rendered .NET web app with shared layout, static assets, and environment-aware middleware.

## Tech Stack

- ASP.NET Core Razor Pages
- .NET 10 (`net10.0`)
- Static assets served from `wwwroot`

## Project Structure

```text
PennyWise/
├── Pages/                 # Razor pages and page models
│   ├── Shared/            # Shared layout and partial views
│   └── *.cshtml(.cs)      # Routeable pages and backing models
├── wwwroot/               # CSS, JavaScript, and static files
├── Program.cs             # Application startup and middleware
├── appsettings.json       # Base configuration
└── PennyWise.csproj       # Project definition
```

## Getting Started

### Prerequisites

- .NET SDK 10.0 (or compatible preview SDK that supports `net10.0`)

### Run locally

```bash
dotnet restore
dotnet run
```

Then open the URL shown in the terminal (typically `https://localhost:5001` or `http://localhost:5000`).

## Build and Test

```bash
dotnet build
```

If you add tests later, run them with:

```bash
dotnet test
```

## Configuration

Application settings are loaded from:

- `appsettings.json`
- `appsettings.Development.json`

You can override settings using environment variables or user secrets for local development.

## Notes

- In development, detailed errors are enabled.
- In non-development environments, the app uses exception handling and HSTS middleware.
- Razor Pages and static assets are mapped in `Program.cs`.
