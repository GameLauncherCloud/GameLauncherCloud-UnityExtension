<div align="center">
   
# Game Launcher Cloud - Manager for Unity
### **The Next-Generation Custom Game Launchers Creator Platform**
   
<img width="2800" height="720" alt="Game Launcher Cloud for Unity - Full Logo with Background" src="https://github.com/user-attachments/assets/b54c9b97-dac8-4e90-aa6e-81deafc4daed" />


**Build and upload your game from Unreal Engine to Game Launcher Cloud!**

[![Website](https://img.shields.io/badge/Website-gamelauncher.cloud-blue?style=for-the-badge&logo=internet-explorer)](https://gamelauncher.cloud/)
[![Status](https://img.shields.io/badge/Status-Live-success?style=for-the-badge)](https://gamelauncher.cloud/)
[![Platform](https://img.shields.io/badge/Platform-Cross--Platform-orange?style=for-the-badge)](https://gamelauncher.cloud/)
[![Unity Extension](https://img.shields.io/badge/Unity-AssetStore-white?style=for-the-badge&logo=unity)](https://assetstore.unity.com/packages/tools/utilities/game-launcher-cloud-manager-for-unity-245359?srsltid=AfmBOoryQwHxR5Kdg4yukaEysaNJH0VNGp3g_jVXxCTb3AAfi1S2Yxya)

</div>

**Build and upload your game from Unity to Game Launcher Cloud!**

A powerful Unity Editor extension that allows you to build and upload your game patches directly to [Game Launcher Cloud](https://gamelauncher.cloud) platform from within Unity.

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

1. Download the latest release from [Unity Asset Store](https://assetstore.unity.com/packages/tools/utilities/game-launcher-cloud-manager-for-unity-245359)
2. Import into your Unity project
3. The extension will be available under **Tools/Game Launcher Cloud > Manager** menu

## 🚀 Quick Start

### Step 1: Get Your API Key

1. Go to [Game Launcher Cloud Dashboard](https://app.gamelauncher.cloud)
2. Navigate to **User Profile > API Keys**
3. Click **Create New API Key**
4. Copy your API key

### Step 2: Connect to Game Launcher Cloud

1. In Unity, open **Tools/Game Launcher Cloud > Manager**
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
- Visit [Game Launcher Cloud Dashboard](https://app.gamelauncher.cloud/user/api-keys)
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

- 🌐 Website: [gamelauncher.cloud](https://gamelauncher.cloud)
- 💬 Discord: [Join our community](https://discord.com/invite/FpWvUQ2CJP)
- 📚 Documentation: [docs.gamelauncher.cloud](https://help.gamelauncher.cloud)

## 📝 License

This extension is provided free of charge for use with Game Launcher Cloud platform.

## 🎮 About Game Launcher Cloud

Game Launcher Cloud is a comprehensive platform for game developers to:
- Create custom game launchers in minutes
- Distribute game patches efficiently
- Manage multiple games and versions
- Track downloads and analytics
- Provide seamless updates to players

Visit [gamelauncher.cloud](https://gamelauncher.cloud) to learn more!

---

Made with ❤️ by the Game Launcher Cloud team

