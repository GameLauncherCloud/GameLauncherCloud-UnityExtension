# Developer Guide - Game Launcher Cloud Unity Extension

## 🔧 Developer Tab

The Unity extension includes a hidden **Developer Tab** that allows easy switching between environments. This tab is **only for internal development** and should **NOT** be visible to end users.

### Activating Developer Mode

To enable the Developer tab:

1. Open the Unity project
2. Navigate to: `Assets/Plugins/Game Launcher Cloud/`
3. Manually edit `glc_config.json` (or create it if it doesn't exist)
4. Set `"showDeveloperTab": true`

Example:
```json
{
  "showDeveloperTab": true,
  "environment": 0
}
```

5. Reopen the GLC Manager window
6. You'll now see a **Developer** tab

### Environment Selection

The Developer tab provides quick switching between:

#### 🏠 Development
- **API URL:** `https://localhost:7226`
- **Frontend URL:** `http://localhost:4200`
- **Database:** Local PostgreSQL
- **Cloudflare R2:** `game-launcher-cloud-development`
- **Stripe:** Test mode

#### 🧪 Staging
- **API URL:** `https://stagingapi.gamelauncher.cloud`
- **Frontend URL:** `https://staging.app.gamelauncher.cloud`
- **Database:** Railway Staging (nozomi)
- **Cloudflare R2:** `game-launcher-cloud-staging`
- **Stripe:** Test mode

#### 🚀 Production
- **API URL:** `https://api.gamelauncher.cloud`
- **Frontend URL:** `https://app.gamelauncher.cloud`
- **Database:** Railway Production (mainline)
- **Cloudflare R2:** `game-launcher-cloud-production`
- **Stripe:** Live mode

### Features in Developer Tab

1. **Environment Dropdown** - Select environment from enum
2. **Quick Action Buttons** - One-click switch to Dev/Staging/Prod
3. **Current Configuration Display** - Shows active API and Frontend URLs
4. **All Environment URLs** - Reference list of all endpoints
5. **Toggle Developer Tab** - Show/hide this tab
6. **Clear Auth Data** - Quick logout and credential clearing

### Configuration Structure

```csharp
public enum GLCEnvironment
{
    Production,  // 0 (default)
    Staging,     // 1
    Development  // 2
}

public class GLCConfig
{
    // ... other properties ...
    
    public GLCEnvironment environment = GLCEnvironment.Production;
    public bool showDeveloperTab = false;
    
    public string GetApiUrl() { /* returns URL based on environment */ }
    public string GetFrontendUrl() { /* returns URL based on environment */ }
}
```

### Before Publishing

**⚠️ CRITICAL:** Before creating a release build for end users:

1. Open the Developer tab
2. Set **Environment** to `Production`
3. Set **Show Developer Tab** to `false` (unchecked)
4. Save and test
5. Verify the Developer tab is no longer visible
6. Build the Unity package

This ensures users don't see internal development options.

### Testing Workflow

#### Local Development Testing
```
1. Set environment to "Development"
2. Run local backend (dotnet run)
3. Run local frontend (ng serve)
4. Test builds upload to local R2 bucket
```

#### Staging Testing
```
1. Set environment to "Staging"
2. Use staging API keys
3. Test with staging database
4. Verify uploads go to staging R2
```

#### Production Testing
```
1. Set environment to "Production"
2. Use production API keys (carefully!)
3. Test end-to-end flow
4. Verify real builds are created
```

### API Key Management

Different environments require different API keys:

- **Development:** Use test API keys from local database
- **Staging:** Use staging API keys from staging.app.gamelauncher.cloud
- **Production:** Use production API keys from app.gamelaunchercloud.com

The extension automatically connects to the correct backend based on the selected environment.

### URL Configuration

All URLs are pulled from `appsettings.json` in the backend:

| Environment | Backend AppSettings File |
|-------------|-------------------------|
| Development | `appsettings.Development.json` |
| Staging     | `appsettings.Staging.json` |
| Production  | `appsettings.json` |

The Unity extension mirrors these URLs in the `GLCConfig.GetApiUrl()` and `GLCConfig.GetFrontendUrl()` methods.

### Troubleshooting

#### Developer Tab Not Showing
- Check `glc_config.json` has `"showDeveloperTab": true`
- Restart Unity Editor
- Reopen the GLC Manager window

#### Wrong Environment Active
- Open Developer tab
- Verify environment in dropdown
- Click appropriate Quick Action button
- Check "Current Configuration" section

#### API Connection Issues
- Verify backend is running (for Development)
- Check URLs match backend appsettings
- Confirm API key is valid for selected environment
- Check firewall/network settings

### Security Notes

🔒 **Never commit:**
- `glc_config.json` with real API keys
- Configuration with `showDeveloperTab: true`
- Production credentials

✅ **Safe to commit:**
- `glc_config.example.json`
- Code changes to environment logic
- Documentation updates

### Automated Testing

For CI/CD pipelines, you can set the environment programmatically:

```csharp
// In Unity Editor script
var config = GLCConfigManager.LoadConfig();
config.environment = GLCEnvironment.Development;
config.showDeveloperTab = false;
GLCConfigManager.SaveConfig(config);
```

---

## 📞 Internal Support

For questions about the developer environment:
- 💬 Internal Slack: #unity-extension-dev
- 📧 Email: dev@gamelaunchercloud.com
- 📚 Backend Docs: Check appsettings files

---

**Remember:** This developer functionality is for internal use only and should never be exposed to end users in the published Unity package!
