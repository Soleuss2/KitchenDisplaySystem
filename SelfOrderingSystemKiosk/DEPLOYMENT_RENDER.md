# Render Deployment Guide

Complete step-by-step guide to deploy your .NET Core MVC application to Render.

## Prerequisites

1. ✅ GitHub account with your code repository
2. ✅ Render account (sign up at https://render.com - free)
3. ✅ MongoDB connection string ready

## Step 1: Prepare Your Repository

### 1.1 Verify appsettings.json
Make sure `appsettings.json` has an empty connection string:
```json
{
  "DataCon": {
    "ConnectionString": ""
  }
}
```

### 1.2 Ensure appsettings.Development.json is in .gitignore
This file should NOT be committed (already configured).

### 1.3 Push to GitHub
Make sure all your code is committed and pushed to GitHub.

## Step 2: Create Render Account

1. Go to https://render.com
2. Click "Get Started for Free"
3. Sign up with your GitHub account (recommended for easy integration)

## Step 3: Create New Web Service

1. In Render dashboard, click **"New +"** button
2. Select **"Web Service"**
3. Choose **"Build and deploy from a Git repository"**
4. Connect your GitHub account if not already connected
5. Select your repository: `SelfOrderingSystemKiosk` (or your repo name)
6. Click **"Connect"**

## Step 4: Configure Build Settings

Render will auto-detect .NET, but verify these settings:

### Basic Settings:
- **Name**: `kitchen-system-kiosk` (or your preferred name)
- **Region**: Choose closest to your users (e.g., `Oregon (US West)`)
- **Branch**: `main` or `master` (your default branch)
- **Root Directory**: Leave empty (or `SelfOrderingSystemKiosk` if your repo has multiple projects)

### Build & Deploy Settings:

**Build Command:**
```bash
dotnet restore && dotnet publish -c Release -o ./publish
```

**Start Command:**
```bash
dotnet ./publish/SelfOrderingSystemKiosk.dll
```

**OR** if the above doesn't work, try:
```bash
cd SelfOrderingSystemKiosk && dotnet restore && dotnet publish -c Release -o ./publish && dotnet ./publish/SelfOrderingSystemKiosk.dll
```

### Environment:
- **Environment**: `.NET`
- **Docker**: Leave unchecked (unless you have a Dockerfile)

## Step 5: Add Environment Variables

This is the most important step! Click on **"Environment"** tab or scroll down to "Environment Variables" section.

Click **"Add Environment Variable"** and add:

| Key | Value |
|-----|-------|
| `DataCon__ConnectionString` | `mongodb+srv://Admin:YourPassword@cluster0.f5tgv.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

**Important Notes:**
- Use **double underscore** `__` for nested properties: `DataCon__ConnectionString`
- Replace `YourPassword` with your actual MongoDB password
- Keep the connection string in quotes if it contains special characters

### Additional Environment Variables (Optional):

You can also set these if needed:
- `ASPNETCORE_URLS` - Leave default (Render sets this automatically)
- `ASPNETCORE_ENVIRONMENT` - Set to `Production`

## Step 6: Plan Selection

- **Free Plan**: Good for testing (spins down after 15 min inactivity)
- **Starter Plan ($7/month)**: Always on, better for production

For testing, start with **Free Plan**. You can upgrade later.

## Step 7: Deploy

1. Click **"Create Web Service"** at the bottom
2. Render will start building your application
3. Watch the build logs in real-time
4. Build typically takes 3-5 minutes

## Step 8: Verify Deployment

### Check Build Logs:
Look for:
- ✅ `dotnet restore` - Success
- ✅ `dotnet publish` - Success
- ✅ Application started successfully

### Common Build Issues:

**Issue: "Could not find project file"**
- **Fix**: Set Root Directory to `SelfOrderingSystemKiosk`

**Issue: "Build failed"**
- Check build logs for specific errors
- Verify .NET 8.0 SDK is available (Render supports it)

**Issue: "Application crashed"**
- Check runtime logs
- Verify environment variables are set correctly
- Check MongoDB connection string format

### Test Your Application:
1. Once deployed, Render gives you a URL like: `https://your-app-name.onrender.com`
2. Visit the URL in your browser
3. Test the application functionality

## Step 9: Configure Custom Domain (Optional)

1. Go to your service → **"Settings"** → **"Custom Domains"**
2. Add your domain
3. Follow DNS configuration instructions

## Step 10: Monitor and Maintain

### View Logs:
- Go to your service → **"Logs"** tab
- View real-time application logs
- Check for errors or warnings

### Update Application:
- Push changes to GitHub
- Render automatically detects and redeploys
- Or manually trigger: **"Manual Deploy"** → **"Deploy latest commit"**

### Update Environment Variables:
- Go to **"Environment"** tab
- Edit or add variables
- Changes take effect on next deploy (or restart service)

## Troubleshooting

### Application Not Starting

**Check 1: Environment Variables**
```bash
# In Render logs, verify connection string is set
# Look for: "Connection String configured: True"
```

**Check 2: MongoDB Connection**
- Verify MongoDB Atlas allows connections from Render's IPs
- In MongoDB Atlas → Network Access → Add `0.0.0.0/0` (allow all) for testing
- Or add Render's specific IP ranges

**Check 3: Port Configuration**
Render automatically sets `PORT` environment variable. Your app should use:
```csharp
// In Program.cs (should already be configured)
app.Run();
// Or
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
```

### Build Fails

**Error: "SDK not found"**
- Render supports .NET 8.0
- Verify your `.csproj` targets `net8.0`

**Error: "NuGet restore failed"**
- Check internet connectivity during build
- Verify all NuGet packages are publicly available

### Runtime Errors

**Error: "Connection string is empty"**
- Verify environment variable name: `DataCon__ConnectionString` (double underscore)
- Check variable is set in Render dashboard
- Restart the service after adding variables

**Error: "MongoDB connection failed"**
- Check MongoDB Atlas Network Access settings
- Verify connection string format
- Check MongoDB user credentials

## Render-Specific Configuration

### Auto-Deploy Settings:
- **Auto-Deploy**: Enabled by default
- Deploys on every push to main branch
- Can disable in Settings → "Auto-Deploy"

### Health Checks:
Render automatically checks if your app responds on the root URL.
- Make sure your app has a route at `/` or `/Home`

### Resource Limits (Free Plan):
- **512 MB RAM**
- **0.5 CPU**
- **Spins down after 15 min inactivity** (first request after spin-down takes ~30 seconds)

## Cost Optimization

### Free Plan Limitations:
- ✅ Good for development/testing
- ❌ Spins down after inactivity (slow first request)
- ❌ Limited resources

### For Production:
- Upgrade to **Starter Plan ($7/month)**
- Always-on service
- Better performance
- More resources

## Quick Reference

### Environment Variable Format:
```
DataCon__ConnectionString = mongodb+srv://...
```

### Build Command:
```bash
dotnet restore && dotnet publish -c Release -o ./publish
```

### Start Command:
```bash
dotnet ./publish/SelfOrderingSystemKiosk.dll
```

### Service URL Format:
```
https://your-service-name.onrender.com
```

## Next Steps After Deployment

1. ✅ Test all functionality
2. ✅ Verify MongoDB connection
3. ✅ Test order creation
4. ✅ Check admin panel access
5. ✅ Monitor logs for errors
6. ✅ Set up custom domain (optional)
7. ✅ Configure backup/restore (if needed)

## Support

- **Render Docs**: https://render.com/docs
- **Render Status**: https://status.render.com
- **Render Community**: https://community.render.com

---

**Congratulations!** Your .NET Core MVC application is now live on Render! 🎉

