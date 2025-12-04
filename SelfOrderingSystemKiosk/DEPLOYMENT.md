# Deployment Guide - Connection String Configuration

## ⚠️ Important: Netlify Limitation

**Netlify is NOT recommended for .NET Core MVC applications** because:
- Netlify is designed for static sites and serverless functions (Node.js, Go, Python)
- .NET Core requires a full server runtime that Netlify doesn't support
- Your application needs continuous server-side processing (MongoDB connections, sessions, etc.)

### Better Alternatives for .NET Core:
- **Azure App Service** (Recommended - best .NET support)
- **AWS Elastic Beanstalk** or **AWS App Runner**
- **Railway** (Easy deployment, good .NET support)
- **Render** (Simple deployment, supports .NET)
- **Fly.io** (Good for .NET applications)
- **DigitalOcean App Platform**
- **Heroku** (though being phased out)

### If You Must Use Netlify:
You would need to:
1. Separate frontend (static HTML/JS) and deploy to Netlify
2. Deploy backend API to a .NET-compatible platform
3. Use Netlify Functions for serverless endpoints (but limited .NET support)

This requires significant architecture changes and is not recommended.

## Overview
This application uses environment variables for secure connection string management in production. The connection string should **never** be hardcoded in `appsettings.json` or committed to source control.

## Configuration Priority
ASP.NET Core reads configuration in this order (later values override earlier ones):
1. `appsettings.json`
2. `appsettings.{Environment}.json` (e.g., `appsettings.Development.json`)
3. **Environment Variables** (highest priority)
4. Command-line arguments

## Setting Connection String via Environment Variables

### Option 1: Environment Variable (Recommended for Production)

#### Windows (PowerShell)
```powershell
$env:DataCon__ConnectionString = "mongodb+srv://Admin:Password@cluster0.f5tgv.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0"
```

#### Windows (Command Prompt)
```cmd
set DataCon__ConnectionString=mongodb+srv://Admin:Password@cluster0.f5tgv.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0
```

#### Linux/Mac (Bash)
```bash
export DataCon__ConnectionString="mongodb+srv://Admin:Password@cluster0.f5tgv.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0"
```

### Option 2: Platform-Specific Deployment

#### Azure App Service
1. Go to Azure Portal → Your App Service → Configuration
2. Click "Application settings" → "New application setting"
3. Add:
   - **Name**: `DataCon__ConnectionString`
   - **Value**: Your MongoDB connection string
4. Click "Save"

#### AWS Elastic Beanstalk
1. Go to AWS Console → Elastic Beanstalk → Your Environment → Configuration
2. Click "Software" → "Environment properties"
3. Add:
   - **Key**: `DataCon__ConnectionString`
   - **Value**: Your MongoDB connection string
4. Click "Apply"

#### AWS EC2 / Linux Server
Create a file `/etc/environment` or use systemd service file:
```bash
# /etc/systemd/system/your-app.service
[Service]
Environment="DataCon__ConnectionString=mongodb+srv://Admin:Password@cluster0.f5tgv.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0"
```

#### Docker
```dockerfile
# Dockerfile
ENV DataCon__ConnectionString="mongodb+srv://Admin:Password@cluster0.f5tgv.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0"
```

Or use docker-compose:
```yaml
# docker-compose.yml
services:
  app:
    environment:
      - DataCon__ConnectionString=mongodb+srv://Admin:Password@cluster0.f5tgv.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0
```

#### IIS (Windows Server)
1. Open IIS Manager
2. Select your Application → Configuration Editor
3. Navigate to `system.webServer/aspNetCore`
4. Add environment variable:
   ```xml
   <environmentVariables>
     <environmentVariable name="DataCon__ConnectionString" value="mongodb+srv://..." />
   </environmentVariables>
   ```

Or use `web.config`:
```xml
<configuration>
  <system.webServer>
    <aspNetCore>
      <environmentVariables>
        <environmentVariable name="DataCon__ConnectionString" value="mongodb+srv://Admin:Password@cluster0.f5tgv.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

## Local Development

### Option 1: appsettings.Development.json (Recommended)
1. Create `appsettings.Development.json` in the project root
2. Add your connection string:
   ```json
   {
     "DataCon": {
       "ConnectionString": "your-connection-string-here"
     }
   }
   ```
3. **Important**: Add `appsettings.Development.json` to `.gitignore`

### Option 2: User Secrets (Most Secure for Local)
```bash
dotnet user-secrets init
dotnet user-secrets set "DataCon:ConnectionString" "mongodb+srv://Admin:Password@cluster0.f5tgv.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0"
```

## Security Best Practices

1. ✅ **DO**: Use environment variables in production
2. ✅ **DO**: Use User Secrets for local development
3. ✅ **DO**: Add `appsettings.Development.json` to `.gitignore`
4. ❌ **DON'T**: Commit connection strings to source control
5. ❌ **DON'T**: Hardcode credentials in code
6. ❌ **DON'T**: Share connection strings in chat/email

## Verifying Configuration

To verify your connection string is being read correctly, you can temporarily add logging:

```csharp
// In Program.cs (remove after verification)
var connectionString = builder.Configuration["DataCon:ConnectionString"];
Console.WriteLine($"Connection String configured: {!string.IsNullOrEmpty(connectionString)}");
```

## Troubleshooting

### Connection string not working?
1. Check environment variable name uses double underscore `__` for nested properties: `DataCon__ConnectionString`
2. Restart the application after setting environment variables
3. Verify the connection string format is correct
4. Check application logs for connection errors

### For IIS:
- Make sure the Application Pool has permission to read environment variables
- Restart the Application Pool after setting environment variables

