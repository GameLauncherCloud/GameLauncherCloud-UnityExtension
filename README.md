# Game Launcher Cloud - Manager for Unity

**Promote your game development with Game Launcher Cloud!**

A powerful Unity Editor extension that allows you to build and upload your game patches directly to [Game Launcher Cloud](https://gamelaunchercloud.com) platform from within Unity.

## 🌟 Features

### ✓ **Connect to Your Account**
- Easy authentication using **API Key** or **Login credentials**
- Secure connection to Game Launcher Cloud backend
- Persistent login sessions

### ✓ **Build and Upload Patches**
- Build your Unity game directly from the editor
- Automatic compression and optimization
- Upload builds to Game Launcher Cloud with one click
- Real-time upload progress tracking
- Support for Windows, Linux, and macOS builds

### ✓ **Tips and Best Practices**
- Receive helpful tips to improve patch quality
- Learn optimization techniques
- Best practices for game distribution
- Build size recommendations

## 📦 Installation

1. Download the latest release from [GitHub Releases](https://github.com/GameLauncherCloud/GameLauncherCloud-UnityExtension/releases)
2. Import the `.unitypackage` into your Unity project
3. The extension will be available under **Game Launcher Cloud > Manager for Unity** menu

## 🚀 Quick Start

### Step 1: Get Your API Key

1. Go to [Game Launcher Cloud Dashboard](https://app.gamelaunchercloud.com)
2. Navigate to **Settings > API Keys**
3. Click **Create New API Key**
4. Copy your API key

### Step 2: Connect to Game Launcher Cloud

1. In Unity, open **Game Launcher Cloud > Manager for Unity**
2. Go to the **Login** tab
3. Paste your API Key
4. Click **Login with API Key**

### Step 3: Build and Upload

1. Go to the **Build & Upload** tab
2. Click **Load My Apps** to see your available apps
3. Select the app you want to upload to
4. Write some **Build Notes** describing what changed
5. Click **Build & Upload to Game Launcher Cloud**
6. Wait for the build and upload to complete!

## 📖 Documentation

### Authentication

The extension supports authentication via **API Keys**. This is the recommended method for automated builds and CI/CD pipelines.

To get an API Key:
- Visit [Game Launcher Cloud Dashboard](https://app.gamelaunchercloud.com/dashboard/settings/api-keys)
- Create a new API key with appropriate permissions
- Copy and save the key securely

### Building and Uploading

The extension will:
1. Build your Unity project for the currently selected platform
2. Compress the build into a ZIP file
3. Upload it to Game Launcher Cloud
4. Process the patch automatically on the server

**Supported Platforms:**
- Windows (64-bit)
- Linux (64-bit)
- macOS

### Configuration

The extension saves your settings in:
```
Assets/Plugins/Game Launcher Cloud/glc_config.json
```

**Note:** Add this file to `.gitignore` to avoid committing your API key!

## 💡 Tips for Better Patches

### Optimize Build Size
- Compress textures appropriately
- Remove unused assets
- Use Asset Bundles for large content
- Enable code stripping in Build Settings

### Use Descriptive Build Notes
Always include:
- Version number
- New features added
- Bugs fixed
- Known issues

### Test Before Uploading
- Run the build locally first
- Check for crashes or errors
- Verify all features work
- Test performance

## 🔧 Requirements

- **Unity 2020.3** or newer
- **.NET Framework 4.x** or **.NET Standard 2.1**
- Active **Game Launcher Cloud** account

## 🤝 Support

Need help? We're here for you!

- 📧 Email: support@gamelaunchercloud.com
- 💬 Discord: [Join our community](https://discord.com/invite/FpWvUQ2CJP)
- 📚 Documentation: [docs.gamelaunchercloud.com](https://docs.gamelaunchercloud.com)
- 🌐 Website: [gamelaunchercloud.com](https://gamelaunchercloud.com)

## 📝 License

This extension is provided free of charge for use with Game Launcher Cloud platform.

## 🎮 About Game Launcher Cloud

Game Launcher Cloud is a comprehensive platform for game developers to:
- Distribute game patches efficiently
- Manage multiple games and versions
- Track downloads and analytics
- Provide seamless updates to players

Visit [gamelaunchercloud.com](https://gamelaunchercloud.com) to learn more!

---

Made with ❤️ by the Game Launcher Cloud team

