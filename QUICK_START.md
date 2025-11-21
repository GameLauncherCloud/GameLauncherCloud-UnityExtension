# Game Launcher Cloud - Manager for Unity
## Quick Start Guide

### 📋 Prerequisites

Before you start, make sure you have:
- ✅ Unity 2020.3 or newer installed
- ✅ A Game Launcher Cloud account ([Sign up here](https://app.gamelauncher.cloud))
- ✅ At least one app created in your Game Launcher Cloud dashboard

### 🔑 Step 1: Get Your API Key

1. Log in to [Game Launcher Cloud Dashboard](https://app.gamelauncher.cloud)
2. Navigate to **Settings** → **API Keys**
3. Click **Create New API Key**
4. Give it a name (e.g., "Unity Extension")
5. **Copy the API key** (you won't be able to see it again!)

### 📥 Step 2: Install the Extension

#### Option A: Unity Package Manager (Recommended)
1. Download the `.unitypackage` from [Releases](https://github.com/GameLauncherCloud/GameLauncherCloud-UnityExtension/releases)
2. In Unity, go to **Assets** → **Import Package** → **Custom Package**
3. Select the downloaded `.unitypackage`
4. Click **Import**

#### Option B: Manual Installation
1. Clone or download this repository
2. Copy the `Assets/Plugins/Game Launcher Cloud` folder into your Unity project's `Assets/Plugins/` directory
3. Unity will automatically detect and compile the scripts

### 🚀 Step 3: Open the Manager

1. In Unity, go to the top menu
2. Click **Game Launcher Cloud** → **Manager for Unity**
3. A new window will open

### 🔐 Step 4: Login

1. In the Manager window, go to the **Login** tab
2. Paste your API Key in the text field
3. Click **Login with API Key**
4. You should see "Login successful!" message

### 🎮 Step 5: Build and Upload Your First Patch

1. Go to the **Build & Upload** tab
2. Click **Load My Apps** to fetch your available apps
3. Select the app you want to upload to from the dropdown
4. (Optional) Write some build notes describing what changed
5. Click **Build & Upload to Game Launcher Cloud**
6. Wait for the process to complete (this may take a few minutes depending on build size)
7. Once done, you'll see a success message!

### 💡 Step 6: Learn Best Practices

1. Go to the **Tips** tab
2. Read through the tips to learn how to:
   - Optimize your build size
   - Write better build notes
   - Test properly before uploading
   - Use version control effectively
   - And more!

### 🎉 You're Done!

Your game build has been uploaded to Game Launcher Cloud and is being processed. You can now:
- View your build in the [Game Launcher Cloud Dashboard](https://app.gamelauncher.cloud)
- Share it with your team
- Distribute it to players
- Track downloads and analytics

---

## 🆘 Troubleshooting

### "Login failed" Error
- Make sure you copied the entire API key
- Check that your API key hasn't expired
- Verify you have an active Game Launcher Cloud subscription

### "No apps found" Message
- Make sure you've created at least one app in your Game Launcher Cloud dashboard
- Check that you're logged in with the correct account
- Try clicking **Refresh Apps**

### Build Fails
- Ensure you have scenes added in **File** → **Build Settings**
- Check that your build target is supported (Windows, Linux, macOS)
- Verify you have enough disk space
- Check Unity console for error messages

### Upload Fails
- Check your internet connection
- Verify the build size is within your plan limits
- Make sure you have enough storage quota
- Try uploading again (temporary network issues)

### Extension Not Showing in Menu
- Check that the files are in the correct location: `Assets/Plugins/Game Launcher Cloud/`
- Try reimporting the package
- Check Unity console for compilation errors
- Restart Unity Editor

---

## 📞 Need More Help?

- 📧 Email: support@gamelauncher.cloud
- 💬 Discord: [Join our community](https://discord.com/invite/FpWvUQ2CJP)
- 📚 Full Documentation: [docs.gamelauncher.cloud](https://help.gamelauncher.cloud)
- 🐛 Report bugs: [GitHub Issues](https://github.com/GameLauncherCloud/GameLauncherCloud-UnityExtension/issues)

---

**Happy Building! 🎮✨**
