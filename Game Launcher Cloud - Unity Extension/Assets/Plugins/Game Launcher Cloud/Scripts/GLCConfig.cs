using UnityEngine;

namespace GameLauncherCloud
{
    /// <summary>
    /// Environment types for Game Launcher Cloud
    /// </summary>
    public enum GLCEnvironment
    {
        Production,
        Staging,
        Development
    }

    /// <summary>
    /// Configuration data for Game Launcher Cloud Unity Extension
    /// Stores authentication credentials and app settings
    /// </summary>
    [System.Serializable]
    public class GLCConfig
    {
        public string apiKey = "";
        public string authToken = "";
        public string userId = "";
        public string userEmail = "";
        public string userPlan = "";
        public long selectedAppId = 0;
        public string selectedAppName = "";
        public bool rememberMe = true;
        public string apiBaseUrl = "https://api.gamelaunchercloud.com";
        
        // Build settings
        public string buildNotes = "";
        public string lastBuildPath = "";
        public bool autoOpenAfterBuild = false;
        
        // Developer settings (not visible to end users)
        public GLCEnvironment environment = GLCEnvironment.Production;
        
        /// <summary>
        /// Get API URL based on selected environment
        /// </summary>
        public string GetApiUrl()
        {
            switch (environment)
            {
                case GLCEnvironment.Development:
                    // Use HTTPS with 127.0.0.1 (backend redirects HTTP to HTTPS)
                    return "https://127.0.0.1:7226";
                case GLCEnvironment.Staging:
                    return "https://stagingapi.gamelauncher.cloud";
                case GLCEnvironment.Production:
                default:
                    return "https://api.gamelaunchercloud.com";
            }
        }
        
        /// <summary>
        /// Get Frontend URL based on selected environment
        /// </summary>
        public string GetFrontendUrl()
        {
            switch (environment)
            {
                case GLCEnvironment.Development:
                    return "http://localhost:4200";
                case GLCEnvironment.Staging:
                    return "https://staging.app.gamelauncher.cloud";
                case GLCEnvironment.Production:
                default:
                    return "https://app.gamelaunchercloud.com";
            }
        }
    }
}
