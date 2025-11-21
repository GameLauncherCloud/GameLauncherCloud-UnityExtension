using UnityEngine;
using UnityEditor;
using System.IO;

namespace GameLauncherCloud.Editor
{
    /// <summary>
    /// Configuration manager for Game Launcher Cloud Unity Extension
    /// Handles saving and loading user settings
    /// </summary>
    public static class GLCConfigManager
    {
        private static readonly string CONFIG_PATH = "Assets/Plugins/Game Launcher Cloud/glc_config.json";
        private static GLCConfig cachedConfig;

        /// <summary>
        /// Load configuration from disk
        /// </summary>
        public static GLCConfig LoadConfig()
        {
            if (cachedConfig != null)
                return cachedConfig;

            if (File.Exists(CONFIG_PATH))
            {
                try
                {
                    string json = File.ReadAllText(CONFIG_PATH);
                    cachedConfig = JsonUtility.FromJson<GLCConfig>(json);
                    return cachedConfig;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GLC] Failed to load config: {ex.Message}");
                }
            }

            // Return default config
            cachedConfig = new GLCConfig();
            return cachedConfig;
        }

        /// <summary>
        /// Save configuration to disk
        /// </summary>
        public static void SaveConfig(GLCConfig config)
        {
            try
            {
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(CONFIG_PATH, json);
                cachedConfig = config;
                AssetDatabase.Refresh();
                Debug.Log("[GLC] Configuration saved successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GLC] Failed to save config: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear authentication data (keeps API Key for re-login)
        /// </summary>
        public static void ClearAuth()
        {
            var config = LoadConfig();
            // Keep API Key - only clear session data
            config.authToken = "";
            config.userId = "";
            config.userEmail = "";
            config.userPlan = "";
            SaveConfig(config);
        }

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        public static bool IsAuthenticated()
        {
            var config = LoadConfig();
            return !string.IsNullOrEmpty(config.authToken);
        }
    }
}
