# HMACServerApp

Lightweight .NET 8 Web API demonstrating request-level HMAC authentication for protecting API endpoints. The sample includes an Employees API backed by EF Core, seeded client secrets, and a simple HMAC verification middleware.

## Key features
- HMACSHA256 request signing middleware with nonce + timestamp replay protection
- EF Core (SQL Server) DbContext with seeded Employees and ClientSecrets
- Employees CRUD endpoints (api/Employees)
- Toggle HMAC on/off via configuration
- Example client token format and sample client snippet

## Project layout (important files)
- Program.cs — app startup, middleware registration, DbContext registration
- Models/
  - HMACAuthenticationMiddleware.cs — HMAC auth middleware and token validation
  - HMACDbContext.cs — EF Core DbContext with seed data
  - ClientSecret.cs — client credential model
  - ClientSecretService.cs — helper service to look up client secrets
  - Employee.cs — Employee model
- Controllers/
  - EmployeesController.cs — CRUD endpoints for Employee
  - WeatherForecastController.cs — sample controller
- appsettings.json — connection string and HMACSetting:EnableHMAC
- HMACServerApp.http — simple HTTP test file (adjust host address as needed)

## Requirements
- .NET 8 SDK
- SQL Server accessible from your environment (or change connection string to any supported provider)
- Visual Studio 2022 or use the dotnet CLI

## Configuration
- Connection string: set in appsettings.json under `ConnectionStrings:EFCoreDBConnection`.
- Enable/disable HMAC: set `HMACSetting:EnableHMAC` (true/false) in appsettings.json or override with environment variables.

Example snippet from appsettings.json: