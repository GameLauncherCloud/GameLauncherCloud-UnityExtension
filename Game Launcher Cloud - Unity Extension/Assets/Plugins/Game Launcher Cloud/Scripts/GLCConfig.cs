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
        // Legacy field for backward compatibility
        public string apiKey = "";
        
        // Environment-specific API Keys
        public string apiKeyProduction = "";
        public string apiKeyStaging = "";
        public string apiKeyDevelopment = "";
        
        public string authToken = "";
        public string userId = "";
        public string userEmail = "";
        public string userPlan = "";
        public long selectedAppId = 0;
        public string selectedAppName = "";
        public bool rememberMe = true;
        public string apiBaseUrl = "https://api.gamelauncher.cloud";
        
        // Build settings
        public string buildNotes = "";
        public string lastBuildPath = "";
        public bool autoOpenAfterBuild = false;
        
        // Developer settings (not visible to end users)
        public GLCEnvironment environment = GLCEnvironment.Production;
        
        /// <summary>
        /// Get API Key for the current environment
        /// </summary>
        public string GetApiKey()
        {
            switch (environment)
            {
                case GLCEnvironment.Development:
                    return string.IsNullOrEmpty(apiKeyDevelopment) ? apiKey : apiKeyDevelopment;
                case GLCEnvironment.Staging:
                    return string.IsNullOrEmpty(apiKeyStaging) ? apiKey : apiKeyStaging;
                case GLCEnvironment.Production:
                default:
                    return string.IsNullOrEmpty(apiKeyProduction) ? apiKey : apiKeyProduction;
            }
        }
        
        /// <summary>
        /// Set API Key for the current environment
        /// </summary>
        public void SetApiKey(string key)
        {
            switch (environment)
            {
                case GLCEnvironment.Development:
                    apiKeyDevelopment = key;
                    break;
                case GLCEnvironment.Staging:
                    apiKeyStaging = key;
                    break;
                case GLCEnvironment.Production:
                default:
                    apiKeyProduction = key;
                    break;
            }
        }
        
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
                    return "https://api.gamelauncher.cloud";
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
                    return "https://app.gamelauncher.cloud";
            }
        }
    }
}
