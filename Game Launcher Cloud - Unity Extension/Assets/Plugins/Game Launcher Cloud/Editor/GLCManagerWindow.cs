using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GameLauncherCloud.Editor
{
    /// <summary>
    /// Main editor window for Game Launcher Cloud Unity Extension
    /// Provides UI for authentication, build & upload, and tips
    /// </summary>
    public class GLCManagerWindow : EditorWindow
    {
        // ========== WINDOW PROPERTIES ========== //

        private GLCConfig config;
        private GLCApiClient apiClient;
        private int selectedTab = 0;
        private string[] tabNamesNotAuth = { "Login" };
        private string[] tabNamesAuth = { "Build & Upload" };
        private string[] devTabNamesAuth = { "Build & Upload", "Developer" };

        // ========== STYLES ========== //

        private GUIStyle headerStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle cardStyle;
        private GUIStyle buttonStyle;
        private GUIStyle primaryButtonStyle;
        private GUIStyle linkButtonStyle;
        private GUIStyle tabStyle;
        private bool stylesInitialized = false;

        // ========== ICON ========== //

        private Texture2D icon;

        // ========== LOGIN TAB ========== //

        private string apiKeyInput = "";
        private bool isLoggingIn = false;
        private string loginMessage = "";
        private MessageType loginMessageType = MessageType.Info;

        // ========== BUILD & UPLOAD TAB ========== //

        private GLCApiClient.AppInfo[] availableApps = null;
        private int selectedAppIndex = 0;
        private string buildNotesInput = "";
        private bool isLoadingApps = false;
        private bool isBuilding = false;
        private bool isUploading = false;
        private bool isMonitoringBuild = false;
        private float uploadProgress = 0f;
        private string buildMessage = "";
        private MessageType buildMessageType = MessageType.Info;

        // ========== MENU ITEM ========== //

        [MenuItem("Tools/Game Launcher Cloud - Manager", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<GLCManagerWindow>("GLC Manager");
            window.minSize = new Vector2(550, 650);
            window.Show();
        }

        // ========== UNITY LIFECYCLE ========== //

        private void OnEnable()
        {
            config = GLCConfigManager.LoadConfig();
            Debug.Log($"[GLC] OnEnable - Config loaded. Environment: {config.environment}, API URL: {config.GetApiUrl()}");
            Debug.Log($"[GLC] OnEnable - Auth token length: {config.authToken?.Length ?? 0} chars");
            Debug.Log($"[GLC] OnEnable - User email: {config.userEmail}");
            
            apiClient = new GLCApiClient(config.GetApiUrl(), config.authToken);
            Debug.Log($"[GLC] OnEnable - API Client created");

            if (!string.IsNullOrEmpty(config.apiKey))
            {
                apiKeyInput = config.apiKey;
                Debug.Log($"[GLC] OnEnable - API Key loaded from config");
            }

            // Load icon
            icon = Resources.Load<Texture2D>("GameLauncherCloud_Icon");
        }

        private void OnGUI()
        {
            InitializeStyles();

            // Draw custom header
            DrawModernHeader();

            EditorGUILayout.Space(5);

            // Modern tab bar
            DrawModernTabs();

            EditorGUILayout.Space(10);

            // Content area with scroll view
            Vector2 scrollPosition = Vector2.zero;
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (!GLCConfigManager.IsAuthenticated())
            {
                // Not authenticated - show login
                DrawLoginTab();
            }
            else
            {
                // Authenticated - show based on selected tab
                switch (selectedTab)
                {
                    case 0:
                        DrawBuildUploadTab();
                        break;

                    case 1:
                        if (config.showDeveloperTab)
                        {
                            DrawDeveloperTab();
                        }
                        break;
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);

            // Footer
            DrawFooter();
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            // Header Style
            headerStyle = new GUIStyle();
            headerStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.3f, 0.5f, 1f));
            headerStyle.padding = new RectOffset(15, 15, 15, 15);

            // Title Style
            titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 18;
            titleStyle.normal.textColor = Color.white;
            titleStyle.fontStyle = FontStyle.Bold;

            // Subtitle Style
            subtitleStyle = new GUIStyle(EditorStyles.label);
            subtitleStyle.fontSize = 11;
            subtitleStyle.normal.textColor = new Color(0.8f, 0.9f, 1f, 1f);
            subtitleStyle.fontStyle = FontStyle.Italic;

            // Section Header Style
            sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            sectionHeaderStyle.fontSize = 14;
            sectionHeaderStyle.margin = new RectOffset(0, 0, 10, 5);

            // Card Style
            cardStyle = new GUIStyle(EditorStyles.helpBox);
            cardStyle.padding = new RectOffset(15, 15, 15, 15);
            cardStyle.margin = new RectOffset(10, 10, 5, 5);

            // Button Style
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.padding = new RectOffset(15, 15, 8, 8);

            // Primary Button Style
            primaryButtonStyle = new GUIStyle(GUI.skin.button);
            primaryButtonStyle.fontSize = 13;
            primaryButtonStyle.fontStyle = FontStyle.Bold;
            primaryButtonStyle.padding = new RectOffset(20, 20, 12, 12);
            primaryButtonStyle.normal.textColor = Color.white;

            // Link Button Style
            linkButtonStyle = new GUIStyle(EditorStyles.linkLabel);
            linkButtonStyle.fontSize = 11;

            stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // ========== HEADER ========== //

        private void DrawModernHeader()
        {
            EditorGUILayout.BeginVertical(headerStyle);

            EditorGUILayout.BeginHorizontal();

            // Logo/Icon area
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(52), GUILayout.Height(52));
            }
            else
            {
                GUILayout.Label("☁️", new GUIStyle(titleStyle) { fontSize = 28 }, GUILayout.Width(40));
            }

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Game Launcher Cloud", titleStyle);
            GUILayout.Label("Unity Manager Extension", subtitleStyle);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Status indicator and Logout button
            if (GLCConfigManager.IsAuthenticated())
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.BeginVertical();
                GUILayout.Label("✓ Connected", new GUIStyle(subtitleStyle) { normal = new GUIStyleState { textColor = new Color(0.4f, 1f, 0.4f) } });
                GUILayout.Label(config.userEmail, new GUIStyle(subtitleStyle) { fontSize = 10 });
                if (!string.IsNullOrEmpty(config.userPlan))
                {
                    GUILayout.Label($"Plan: {config.userPlan}", new GUIStyle(subtitleStyle) { fontSize = 9, normal = new GUIStyleState { textColor = new Color(0.8f, 0.9f, 1f, 0.8f) } });
                }
                EditorGUILayout.EndVertical();
                
                GUILayout.Space(10);
                
                // Logout button
                GUIStyle logoutButtonStyle = new GUIStyle(GUI.skin.button);
                logoutButtonStyle.fontSize = 10;
                logoutButtonStyle.padding = new RectOffset(10, 10, 4, 4);
                logoutButtonStyle.normal.textColor = new Color(1f, 0.8f, 0.8f);
                
                if (GUILayout.Button("Logout", logoutButtonStyle, GUILayout.Height(22), GUILayout.Width(60)))
                {
                    Logout();
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            else
            {
                GUILayout.Label("Not connected", new GUIStyle(subtitleStyle) { normal = new GUIStyleState { textColor = new Color(1f, 0.7f, 0.4f) } });
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawModernTabs()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);

            // Select tab names based on authentication status
            string[] currentTabNames;
            if (!GLCConfigManager.IsAuthenticated())
            {
                currentTabNames = tabNamesNotAuth;
            }
            else if (config.showDeveloperTab)
            {
                currentTabNames = devTabNamesAuth;
            }
            else
            {
                currentTabNames = tabNamesAuth;
            }
            
            // Custom tab buttons
            for (int i = 0; i < currentTabNames.Length; i++)
            {
                GUIStyle tabButtonStyle = new GUIStyle(GUI.skin.button);
                tabButtonStyle.fontSize = 12;
                tabButtonStyle.padding = new RectOffset(20, 20, 10, 10);
                
                if (i == selectedTab)
                {
                    tabButtonStyle.fontStyle = FontStyle.Bold;
                    tabButtonStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.4f, 0.6f, 1f));
                    tabButtonStyle.normal.textColor = Color.white;
                }

                if (GUILayout.Button(currentTabNames[i], tabButtonStyle, GUILayout.Height(35)))
                {
                    selectedTab = i;
                }
            }

            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label($"Environment: {config.environment}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Documentation", EditorStyles.toolbarButton))
            {
                Application.OpenURL("https://help.gamelauncher.cloud");
            }
            
            if (GUILayout.Button("Support", EditorStyles.toolbarButton))
            {
                Application.OpenURL("https://gamelauncher.cloud/support");
            }
            
            EditorGUILayout.EndHorizontal();
        }

        // ========== LOGIN TAB ========== //

        private void DrawLoginTab()
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(cardStyle);

            GUILayout.Label("🔐 Login", sectionHeaderStyle);
            EditorGUILayout.Space(10);

            if (GLCConfigManager.IsAuthenticated())
            {
                // Authenticated view
                DrawCard(() =>
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("✓", new GUIStyle(EditorStyles.boldLabel) { fontSize = 24, normal = new GUIStyleState { textColor = new Color(0.2f, 0.8f, 0.2f) } }, GUILayout.Width(40));
                    
                    EditorGUILayout.BeginVertical();
                    GUILayout.Label("Successfully Connected", EditorStyles.boldLabel);
                    GUILayout.Label($"Logged in as: {config.userEmail}", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.EndHorizontal();
                }, new Color(0.9f, 1f, 0.9f, 0.3f));

                EditorGUILayout.Space(15);

                if (GUILayout.Button("Logout", buttonStyle, GUILayout.Height(35)))
                {
                    Logout();
                }
            }
            else
            {
                // Login view
                DrawInfoBox("Connect your Game Launcher Cloud account using an API Key to start uploading builds.", MessageType.Info);

                EditorGUILayout.Space(15);

                GUILayout.Label("API Key", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                apiKeyInput = EditorGUILayout.PasswordField(apiKeyInput, GUILayout.Height(25));

                EditorGUILayout.Space(8);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Don't have an API Key?", EditorStyles.miniLabel);
                if (GUILayout.Button("Get one here →", linkButtonStyle))
                {
                    Application.OpenURL($"{config.GetFrontendUrl()}/user/api-keys");
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(15);

                GUI.enabled = !isLoggingIn && !string.IsNullOrEmpty(apiKeyInput);
                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 1f);
                
                if (GUILayout.Button(isLoggingIn ? "Logging in..." : "Connect Account", primaryButtonStyle, GUILayout.Height(45)))
                {
                    StartLogin();
                }
                
                GUI.backgroundColor = originalColor;
                GUI.enabled = true;

                if (!string.IsNullOrEmpty(loginMessage))
                {
                    EditorGUILayout.Space(15);
                    DrawInfoBox(loginMessage, loginMessageType);
                }
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // About section
            EditorGUILayout.BeginVertical(cardStyle);
            GUILayout.Label("ℹ️ About Game Launcher Cloud", sectionHeaderStyle);
            EditorGUILayout.Space(10);

            GUILayout.Label(
                "Game Launcher Cloud is a powerful platform for game developers to distribute and manage game patches efficiently. " +
                "Build, upload, and deploy your game updates directly from Unity Editor!",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🌐 Visit Website", buttonStyle, GUILayout.Height(30)))
            {
                Application.OpenURL("https://gamelauncher.cloud");
            }
            if (GUILayout.Button("📚 Documentation", buttonStyle, GUILayout.Height(30)))
            {
                Application.OpenURL("https://help.gamelauncher.cloud");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
        }

        private void DrawCard(System.Action content, Color? backgroundColor = null)
        {
            Color originalColor = GUI.backgroundColor;
            if (backgroundColor.HasValue)
                GUI.backgroundColor = backgroundColor.Value;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            content();
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = originalColor;
        }

        private void DrawInfoBox(string message, MessageType type)
        {
            Color boxColor = type == MessageType.Error ? new Color(1f, 0.8f, 0.8f, 0.3f) :
                            type == MessageType.Warning ? new Color(1f, 1f, 0.8f, 0.3f) :
                            type == MessageType.Info ? new Color(0.8f, 0.9f, 1f, 0.3f) :
                            new Color(0.8f, 1f, 0.8f, 0.3f);

            DrawCard(() =>
            {
                string icon = type == MessageType.Error ? "❌" :
                             type == MessageType.Warning ? "⚠️" :
                             type == MessageType.Info ? "ℹ️" : "✓";

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(icon, new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 }, GUILayout.Width(30));
                GUILayout.Label(message, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();
            }, boxColor);
        }

        private void StartLogin()
        {
            isLoggingIn = true;
            loginMessage = "";

            apiClient.LoginWithApiKeyAsync(apiKeyInput, OnLoginComplete);
        }

        private void OnLoginComplete(bool success, string message, GLCApiClient.LoginResponse response)
        {
            isLoggingIn = false;

            if (success && response != null)
            {
                config.apiKey = apiKeyInput;
                config.authToken = response.Token;
                config.userId = response.Id;
                config.userEmail = response.Email;
                config.userPlan = response.Subscription?.Plan?.Name ?? "Free";
                GLCConfigManager.SaveConfig(config);

                apiClient.SetAuthToken(response.Token);

                loginMessage = "Login successful!";
                loginMessageType = MessageType.Info;

                // Reset tab to first (Build & Upload will be shown automatically)
                selectedTab = 0;

                // Load apps
                LoadApps();
            }
            else
            {
                // Show detailed error message from backend
                loginMessage = string.IsNullOrEmpty(message) ? "Login failed - Unknown error" : message;
                loginMessageType = MessageType.Error;
                
                Debug.LogError($"[GLC] Login failed: {loginMessage}");
            }

            Repaint();
        }

        private void Logout()
        {
            if (EditorUtility.DisplayDialog("Logout", "Are you sure you want to logout?", "Yes", "No"))
            {
                GLCConfigManager.ClearAuth();
                config = GLCConfigManager.LoadConfig();
                apiClient.SetAuthToken("");
                availableApps = null;
                selectedAppIndex = 0;
                selectedTab = 0;
                // Keep apiKeyInput - user might want to re-login
                // apiKeyInput will be preserved from config.apiKey
                loginMessage = "";
                Repaint();
            }
        }

        // ========== BUILD & UPLOAD TAB ========== //

        private void DrawBuildUploadTab()
        {
            GUILayout.Space(10);

            if (!GLCConfigManager.IsAuthenticated())
            {
                EditorGUILayout.BeginVertical(cardStyle);
                DrawInfoBox("Please login first to use Build & Upload features", MessageType.Warning);
                EditorGUILayout.Space(10);
                
                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 1f);
                if (GUILayout.Button("Go to Login", primaryButtonStyle, GUILayout.Height(40)))
                {
                    selectedTab = 0;
                }
                GUI.backgroundColor = originalColor;
                
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginVertical(cardStyle);

            GUILayout.Label("🚀 Build & Deploy", sectionHeaderStyle);
            EditorGUILayout.Space(10);

            // Load Apps Button
            if (availableApps == null || availableApps.Length == 0)
            {
                DrawInfoBox("Load your apps from Game Launcher Cloud to start uploading builds.", MessageType.Info);
                EditorGUILayout.Space(10);
                
                GUI.enabled = !isLoadingApps;
                if (GUILayout.Button(isLoadingApps ? "⏳ Loading Apps..." : "📱 Load My Apps", buttonStyle, GUILayout.Height(40)))
                {
                    LoadApps();
                }
                GUI.enabled = true;
            }
            else
            {
                // App Selection Card
                DrawCard(() =>
                {
                    GUILayout.Label("Select Application", EditorStyles.boldLabel);
                    EditorGUILayout.Space(5);

                    string[] appNames = availableApps.Select(a =>
                        $"{a.Name} ({a.BuildCount} builds)" + (a.IsOwnedByUser ? "" : " [Team]")
                    ).ToArray();

                    GUIStyle popupStyle = new GUIStyle(EditorStyles.popup);
                    popupStyle.fontSize = 12;
                    popupStyle.fixedHeight = 25;

                    int newIndex = EditorGUILayout.Popup(selectedAppIndex, appNames, popupStyle);
                    if (newIndex != selectedAppIndex)
                    {
                        selectedAppIndex = newIndex;
                        config.selectedAppId = availableApps[selectedAppIndex].Id;
                        config.selectedAppName = availableApps[selectedAppIndex].Name;
                        GLCConfigManager.SaveConfig(config);
                    }

                    EditorGUILayout.Space(5);

                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("🔄 Refresh Apps", EditorStyles.miniButton, GUILayout.Height(22)))
                    {
                        buildMessage = "";
                        buildMessageType = MessageType.None;
                        isUploading = false;
                        uploadProgress = 0f;
                        LoadApps();
                    }
                    
                    if (GUILayout.Button("⚙️ Manage Apps", EditorStyles.miniButton, GUILayout.Height(22)))
                    {
                        Application.OpenURL("https://app.gamelauncher.cloud/dashboard");
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }, new Color(0.95f, 0.95f, 1f, 0.3f));

                EditorGUILayout.Space(10);

                // Build Notes Card
                DrawCard(() =>
                {
                    GUILayout.Label("Build Notes", EditorStyles.boldLabel);
                    EditorGUILayout.Space(5);
                    GUILayout.Label("Add version info, changelog, or important notes", EditorStyles.miniLabel);
                    EditorGUILayout.Space(5);
                    
                    GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
                    textAreaStyle.wordWrap = true;
                    buildNotesInput = EditorGUILayout.TextArea(buildNotesInput, textAreaStyle, GUILayout.Height(70));
                }, new Color(1f, 0.98f, 0.9f, 0.3f));

                EditorGUILayout.Space(10);

                // Build & Upload Button
                GUI.enabled = !isBuilding && !isUploading;
                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f, 1f);
                
                string buttonText = isBuilding ? "⚙️ Building..." : 
                                   isUploading ? "☁️ Uploading..." : 
                                   "🚀 Build & Upload to Cloud";
                
                if (GUILayout.Button(buttonText, primaryButtonStyle, GUILayout.Height(50)))
                {
                    StartBuildAndUpload();
                }
                
                GUI.backgroundColor = originalColor;
                GUI.enabled = true;

                // Progress Card
                if (isBuilding || isUploading)
                {
                    EditorGUILayout.Space(10);

                    DrawCard(() =>
                    {
                        if (isBuilding)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("⚙️", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 }, GUILayout.Width(30));
                            EditorGUILayout.BeginVertical();
                            GUILayout.Label("Building your game...", EditorStyles.boldLabel);
                            GUILayout.Label("This may take a few minutes", EditorStyles.miniLabel);
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();
                        }
                        else if (isUploading)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("☁️", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 }, GUILayout.Width(30));
                            EditorGUILayout.BeginVertical();
                            GUILayout.Label($"Uploading... {(uploadProgress * 100):F0}%", EditorStyles.boldLabel);
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();
                            
                            EditorGUILayout.Space(5);
                            Rect rect = EditorGUILayout.GetControlRect(false, 24);
                            EditorGUI.ProgressBar(rect, uploadProgress, $"{(uploadProgress * 100):F0}%");
                        }
                    }, new Color(0.9f, 0.95f, 1f, 0.5f));
                }

                // Build Message
                if (!string.IsNullOrEmpty(buildMessage))
                {
                    EditorGUILayout.Space(10);
                    DrawInfoBox(buildMessage, buildMessageType);
                }

                // Build Status Section
                if (isMonitoringBuild)
                {
                    EditorGUILayout.Space(15);
                    
                    GUILayout.Label("Build Status", new GUIStyle(EditorStyles.boldLabel) 
                    { 
                        fontSize = 14,
                        normal = { textColor = new Color(0.3f, 0.3f, 0.3f) }
                    });
                    
                    EditorGUILayout.Space(5);
                    
                    DrawCard(() =>
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("📊", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 }, GUILayout.Width(30));
                        EditorGUILayout.BeginVertical();
                        GUILayout.Label("Processing build on server...", EditorStyles.boldLabel);
                        GUILayout.Label("This may take several minutes", EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                    }, new Color(0.95f, 0.9f, 1f, 0.5f));
                }
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
        }

        private void LoadApps()
        {
            isLoadingApps = true;
            apiClient.GetAppListAsync(OnAppsLoaded);
        }

        private void OnAppsLoaded(bool success, string message, GLCApiClient.AppInfo[] apps)
        {
            isLoadingApps = false;

            if (success && apps != null && apps.Length > 0)
            {
                availableApps = apps;

                // Try to restore previously selected app
                if (config.selectedAppId > 0)
                {
                    for (int i = 0; i < apps.Length; i++)
                    {
                        if (apps[i].Id == config.selectedAppId)
                        {
                            selectedAppIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Load Apps", message, "OK");
            }

            Repaint();
        }

        private void StartBuildAndUpload()
        {
            if (availableApps == null || selectedAppIndex >= availableApps.Length)
            {
                EditorUtility.DisplayDialog("Error", "Please select a valid app", "OK");
                return;
            }

            // Build notes are optional - no need to prompt if empty

            isBuilding = true;
            buildMessage = "Starting build process...";
            buildMessageType = MessageType.Info;

            // Build the game
            BuildGame();
        }

        private void BuildGame()
        {
            string buildPath = Path.Combine(Application.dataPath, "..", "Builds", "GLC_Upload");
            string buildName = Application.productName;

            // Determine build target and extension
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            string extension = "";

            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    extension = ".exe";
                    break;

                case BuildTarget.StandaloneLinux64:
                    extension = "";
                    break;

                case BuildTarget.StandaloneOSX:
                    extension = ".app";
                    break;

                default:
                    buildMessage = $"Build target {buildTarget} is not supported for upload";
                    buildMessageType = MessageType.Error;
                    isBuilding = false;
                    Repaint();
                    return;
            }

            string fullBuildPath = Path.Combine(buildPath, buildName + extension);

            // Create build directory if it doesn't exist
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            // Get scenes
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                buildMessage = "No scenes found in Build Settings. Please add scenes first.";
                buildMessageType = MessageType.Error;
                isBuilding = false;
                Repaint();
                return;
            }

            // Build options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullBuildPath,
                target = buildTarget,
                options = BuildOptions.None
            };

            // Build
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                buildMessage = $"Build successful! Size: {report.summary.totalSize / (1024 * 1024):F2} MB";
                buildMessageType = MessageType.Info;
                isBuilding = false;

                // Compress and upload
                CompressAndUpload(buildPath);
            }
            else
            {
                buildMessage = $"Build failed: {report.summary.result}";
                buildMessageType = MessageType.Error;
                isBuilding = false;
            }

            Repaint();
        }

        private void CompressAndUpload(string buildPath)
        {
            try
            {
                Debug.Log($"[GLC] === Starting CompressAndUpload ===");
                Debug.Log($"[GLC] Build path: {buildPath}");
                
                string zipPath = Path.Combine(Application.dataPath, "..", "Builds", $"{Application.productName}_upload.zip");
                Debug.Log($"[GLC] ZIP path: {zipPath}");

                // Delete existing zip if exists
                if (File.Exists(zipPath))
                {
                    Debug.Log($"[GLC] Deleting existing ZIP file...");
                    File.Delete(zipPath);
                }
                else
                {
                    Debug.Log($"[GLC] No existing ZIP file to delete");
                }

                buildMessage = "Compressing build...";
                buildMessageType = MessageType.Info;
                Repaint();

                // Compress
                Debug.Log($"[GLC] Starting ZIP compression from: {buildPath}");
                Debug.Log($"[GLC] Creating ZIP at: {zipPath}");
                ZipFile.CreateFromDirectory(buildPath, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
                Debug.Log($"[GLC] ZIP compression completed");

                FileInfo zipInfo = new FileInfo(zipPath);
                long zipSize = zipInfo.Length;
                Debug.Log($"[GLC] ZIP file size: {zipSize} bytes ({zipSize / (1024 * 1024):F2} MB)");

                // Calculate uncompressed size from ZIP metadata (like CLI does)
                long? uncompressedSize = null;
                try
                {
                    buildMessage = "Reading ZIP metadata...";
                    Repaint();
                    
                    using (var zip = ZipFile.OpenRead(zipPath))
                    {
                        uncompressedSize = zip.Entries.Sum(e => e.Length);
                    }
                    
                    Debug.Log($"[GLC] Compressed: {zipSize / (1024 * 1024):F2} MB, Uncompressed: {uncompressedSize.Value / (1024 * 1024):F2} MB");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[GLC] Could not read ZIP metadata: {ex.Message}");
                    // Continue without uncompressed size - backend will validate after upload
                }

                buildMessage = $"Build compressed: {zipSize / (1024 * 1024):F2} MB. Starting upload...";
                buildMessageType = MessageType.Info;
                Repaint();

                // Start upload coroutine
                Debug.Log($"[GLC] === Starting Upload Coroutine ===");
                Debug.Log($"[GLC] App ID: {availableApps[selectedAppIndex].Id}");
                Debug.Log($"[GLC] ZIP path: {zipPath}");
                Debug.Log($"[GLC] ZIP size: {zipSize} bytes");
                Debug.Log($"[GLC] Uncompressed size: {(uncompressedSize.HasValue ? uncompressedSize.Value + " bytes" : "null")}");
                
                uploadProgress = 0f;
                isUploading = true;

                var selectedApp = availableApps[selectedAppIndex];

                EditorCoroutineUtility.StartCoroutine(
                    UploadBuild(selectedApp.Id, zipPath, zipSize, uncompressedSize),
                    this
                );
            }
            catch (System.Exception ex)
            {
                buildMessage = $"Compression failed: {ex.Message}";
                buildMessageType = MessageType.Error;
                isBuilding = false;
                Repaint();
            }
        }

        private IEnumerator UploadBuild(long appId, string zipPath, long zipSize, long? uncompressedSize)
        {
            Debug.Log($"[GLC] === UploadBuild Coroutine Started ===");
            Debug.Log($"[GLC] App ID: {appId}");
            Debug.Log($"[GLC] ZIP path: {zipPath}");
            Debug.Log($"[GLC] ZIP size: {zipSize} bytes");
            Debug.Log($"[GLC] Uncompressed size: {(uncompressedSize.HasValue ? uncompressedSize.Value.ToString() : "null")} bytes");
            
            // Step 1: Request presigned URL
            buildMessage = "Step 1/3: Requesting upload URL...";
            buildMessageType = MessageType.Info;
            Repaint();
            
            Debug.Log($"[GLC] Calling apiClient.StartUploadAsync...");
            
            bool startSuccess = false;
            GLCApiClient.StartUploadResponse uploadResponse = null;
            bool callbackReceived = false;

            // Use default build notes if empty
            string notes = string.IsNullOrWhiteSpace(buildNotesInput) ? "Uploaded from Unity Extension" : buildNotesInput;
            
            apiClient.StartUploadAsync(
                appId,
                Path.GetFileName(zipPath),
                zipSize,
                uncompressedSize,
                notes,
                (success, message, response) =>
                {
                    Debug.Log($"[GLC] StartUpload callback received - Success: {success}, Message: {message}");
                    startSuccess = success;
                    uploadResponse = response;
                    callbackReceived = true;

                    if (!success)
                    {
                        buildMessage = $"Failed to start upload: {message}";
                        buildMessageType = MessageType.Error;
                        Debug.LogError($"[GLC] StartUpload failed: {message}");
                    }
                    else
                    {
                        buildMessage = $"✓ Upload URL obtained (Build ID: #{response.AppBuildId})";
                        buildMessageType = MessageType.Info;
                        Debug.Log($"[GLC] Upload URL obtained for Build #{response.AppBuildId}");
                        Debug.Log($"[GLC] Upload URL: {response.UploadUrl}");
                    }
                    
                    Repaint();
                }
            );
            
            // Wait for callback
            Debug.Log($"[GLC] Waiting for StartUpload callback...");
            while (!callbackReceived)
            {
                yield return null;
            }
            
            Debug.Log($"[GLC] StartUpload completed. Success: {startSuccess}");

            if (!startSuccess || uploadResponse == null)
            {
                isUploading = false;
                Repaint();
                yield break;
            }

            // Step 2: Upload file to cloud storage
            buildMessage = "Step 2/3: Uploading to cloud storage (0%)...";
            buildMessageType = MessageType.Info;
            Repaint();
            
            bool uploadSuccess = false;
            bool uploadCallbackReceived = false;
            List<GLCApiClient.PartETag> uploadedParts = null;
            
            // Check if we should use multipart upload
            bool isMultipart = uploadResponse.PartUrls != null && uploadResponse.PartUrls.Count > 0;
            
            if (isMultipart)
            {
                Debug.Log($"[GLC] Starting multipart upload with {uploadResponse.PartUrls.Count} parts");
                Debug.Log($"[GLC] Part size: {uploadResponse.PartSize.Value / (1024f * 1024f):F2} MB");
                
                apiClient.UploadMultipartAsync(
                    zipPath,
                    uploadResponse.PartUrls,
                    (success, message, progress, parts) =>
                    {
                        // Ensure callback executes on main thread
                        EditorApplication.delayCall += () =>
                        {
                            uploadProgress = progress;

                            if (success && progress >= 1.0f && parts != null)
                            {
                                // Upload completed successfully
                                uploadSuccess = true;
                                uploadCallbackReceived = true;
                                uploadedParts = parts;
                                buildMessage = "✓ Upload completed! Notifying backend...";
                                buildMessageType = MessageType.Info;
                                Debug.Log($"[GLC] Multipart upload callback - SUCCESS! {parts.Count} parts uploaded");
                            }
                            else if (!success && progress == 0f && !message.Contains("part"))
                            {
                                // Error occurred
                                uploadSuccess = false;
                                uploadCallbackReceived = true;
                                buildMessage = $"Upload failed: {message}";
                                buildMessageType = MessageType.Error;
                                Debug.LogError($"[GLC] Multipart upload callback - ERROR: {message}");
                            }
                            else if (!success && message.Contains("part"))
                            {
                                // Part error
                                uploadSuccess = false;
                                uploadCallbackReceived = true;
                                buildMessage = $"Upload failed: {message}";
                                buildMessageType = MessageType.Error;
                                Debug.LogError($"[GLC] Multipart upload callback - PART ERROR: {message}");
                            }
                            else
                            {
                                // Progress update
                                int percentage = Mathf.RoundToInt(progress * 100f);
                                float sizeMB = zipSize / (1024f * 1024f);
                                buildMessage = $"Step 2/3: {message} ({percentage}% of {sizeMB:F2} MB)...";
                                buildMessageType = MessageType.Info;
                            }

                            Repaint();
                        };
                    }
                );
            }
            else
            {
                Debug.Log($"[GLC] Starting single-part upload to: {uploadResponse.UploadUrl}");
                
                apiClient.UploadFileAsync(
                    uploadResponse.UploadUrl,
                    zipPath,
                    (success, message, progress) =>
                    {
                        // Ensure callback executes on main thread
                        EditorApplication.delayCall += () =>
                        {
                            uploadProgress = progress;

                            if (success && progress >= 1.0f)
                            {
                                // Upload completed successfully
                                uploadSuccess = true;
                                uploadCallbackReceived = true;
                                buildMessage = "✓ Upload completed! Notifying backend...";
                                buildMessageType = MessageType.Info;
                                Debug.Log($"[GLC] Upload callback - SUCCESS! Progress: {progress}");
                            }
                            else if (!success && progress == 0f && message != "Uploading...")
                            {
                                // Error occurred (but ignore initial "Uploading..." message)
                                uploadSuccess = false;
                                uploadCallbackReceived = true;
                                buildMessage = $"Upload failed: {message}";
                                buildMessageType = MessageType.Error;
                                Debug.LogError($"[GLC] Upload callback - ERROR: {message}");
                            }
                            else
                            {
                                // Progress update (including initial "Uploading..." message)
                                int percentage = Mathf.RoundToInt(progress * 100f);
                                float sizeMB = zipSize / (1024f * 1024f);
                                buildMessage = $"Step 2/3: Uploading to cloud storage ({percentage}% of {sizeMB:F2} MB)...";
                                buildMessageType = MessageType.Info;
                            }

                            Repaint();
                        };
                    }
                );
            }
            
            // Wait for upload callback
            Debug.Log($"[GLC] Waiting for upload callback...");
            while (!uploadCallbackReceived)
            {
                yield return null;
            }
            
            Debug.Log($"[GLC] Upload completed. Success: {uploadSuccess}, IsMultipart: {isMultipart}");

            if (!uploadSuccess)
            {
                buildMessage = "Upload to cloud storage failed!";
                buildMessageType = MessageType.Error;
                isUploading = false;
                Repaint();
                yield break;
            }

            // Step 3: Notify backend that file is ready
            buildMessage = "Step 3/3: Notifying backend for processing...";
            buildMessageType = MessageType.Info;
            Repaint();
            
            Debug.Log($"[GLC] Calling NotifyFileReadyAsync...");
            
            bool notifySuccess = false;
            bool notifyCallbackReceived = false;

            apiClient.NotifyFileReadyAsync(
                uploadResponse.AppBuildId,
                uploadResponse.Key,
                uploadResponse.UploadId,
                uploadedParts, // parts - populated for multipart uploads, null for single-part
                (success, message) =>
                {
                    Debug.Log($"[GLC] NotifyFileReady callback received - Success: {success}, Message: {message}");
                    notifySuccess = success;
                    notifyCallbackReceived = true;

                    if (success)
                    {
                        buildMessage = "✓ Build uploaded successfully! Processing on server...";
                        buildMessageType = MessageType.Info;
                    }
                    else
                    {
                        buildMessage = $"Failed to notify server: {message}";
                        buildMessageType = MessageType.Warning;
                    }
                    
                    Repaint();
                }
            );
            
            // Wait for notification callback
            Debug.Log($"[GLC] Waiting for NotifyFileReady callback...");
            while (!notifyCallbackReceived)
            {
                yield return null;
            }
            
            Debug.Log($"[GLC] NotifyFileReady completed. Success: {notifySuccess}");

            // Cleanup zip file
            try
            {
                File.Delete(zipPath);
            }
            catch { }

            // Upload process is complete
            isUploading = false;
            isBuilding = false;

            if (notifySuccess)
            {
                // Start monitoring build status
                isMonitoringBuild = true;
                buildMessage = "✓ Upload complete! Monitoring build progress...";
                buildMessageType = MessageType.Info;
                Repaint();
                
                yield return EditorCoroutineUtility.StartCoroutine(
                    MonitorBuildStatus(uploadResponse.AppBuildId),
                    this
                );
            }

            Repaint();
        }

        private IEnumerator MonitorBuildStatus(long appBuildId)
        {
            Debug.Log($"[GLC] === Starting Build Status Monitor for Build #{appBuildId} ===");
            
            bool isMonitoring = true;
            int pollCount = 0;
            const int maxPolls = 600; // 50 minutes maximum (5 seconds * 600 = 50 minutes)
            
            while (isMonitoring && pollCount < maxPolls)
            {
                pollCount++;
                
                bool statusReceived = false;
                GLCApiClient.BuildStatusResponse statusResponse = null;
                
                apiClient.GetBuildStatusAsync(
                    appBuildId,
                    (success, message, response) =>
                    {
                        EditorApplication.delayCall += () =>
                        {
                            statusReceived = true;
                            
                            if (success && response != null)
                            {
                                statusResponse = response;
                                
                                // Update UI based on status
                                string statusIcon = GetStatusIcon(response.Status);
                                buildMessage = $"{statusIcon} Build #{appBuildId}: {response.Status}";
                                
                                // Add progress information if available
                                if (response.StageProgress > 0)
                                {
                                    buildMessage += $" ({response.StageProgress}%)";
                                }
                                
                                // Check if build is in final state
                                if (response.Status == "Completed")
                                {
                                    buildMessage = $"✅ Build #{appBuildId} completed successfully!";
                                    buildMessageType = MessageType.Info;
                                    isMonitoring = false;
                                    isMonitoringBuild = false;
                                    
                                    // Show success dialog
                                    EditorApplication.delayCall += () =>
                                    {
                                        if (EditorUtility.DisplayDialog("Build Completed",
                                            $"Build #{appBuildId} processed successfully!\n\nDo you want to view it in Game Launcher Cloud?",
                                            "Yes", "No"))
                                        {
                                            Application.OpenURL($"{config.GetFrontendUrl()}/apps/id/{response.AppId}/builds");
                                        }
                                    };
                                }
                                else if (response.Status == "Failed")
                                {
                                    string errorMsg = string.IsNullOrEmpty(response.ErrorMessage) 
                                        ? "Unknown error" 
                                        : response.ErrorMessage;
                                    buildMessage = $"❌ Build #{appBuildId} failed: {errorMsg}";
                                    buildMessageType = MessageType.Error;
                                    isMonitoring = false;
                                    isMonitoringBuild = false;
                                    
                                    EditorUtility.DisplayDialog("Build Failed",
                                        $"Build #{appBuildId} processing failed:\n\n{errorMsg}",
                                        "OK");
                                }
                                else if (response.Status == "Cancelled" || response.Status == "Deleted")
                                {
                                    buildMessage = $"⚠️ Build #{appBuildId} was {response.Status.ToLower()}";
                                    buildMessageType = MessageType.Warning;
                                    isMonitoring = false;
                                    isMonitoringBuild = false;
                                }
                                else
                                {
                                    // Still processing
                                    buildMessageType = MessageType.Info;
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"[GLC] Failed to get build status: {message}");
                                // Don't stop monitoring on temporary errors
                            }
                            
                            Repaint();
                        };
                    }
                );
                
                // Wait for status callback
                float timeout = 0f;
                while (!statusReceived && timeout < 10f) // 10 second timeout per request
                {
                    timeout += 0.1f;
                    yield return new WaitForSeconds(0.1f);
                }
                
                // Wait 5 seconds before next poll (if still monitoring)
                if (isMonitoring)
                {
                    yield return new WaitForSeconds(5f);
                }
            }
            
            if (pollCount >= maxPolls)
            {
                buildMessage = $"⚠️ Build #{appBuildId} monitoring timed out. Check status manually.";
                buildMessageType = MessageType.Warning;
                isMonitoringBuild = false;
                Repaint();
            }
            
            Debug.Log($"[GLC] === Build Status Monitor Ended ===");
        }
        
        private string GetStatusIcon(string status)
        {
            return status switch
            {
                "Pending" => "⏳",
                "GeneratingPresignedUrl" => "🔗",
                "UploadingBuild" => "⬆️",
                "Enqueued" => "📋",
                "DownloadingBuild" => "⬇️",
                "DownloadingPreviousBuild" => "⬇️",
                "UnzippingBuild" => "📦",
                "UnzippingPreviousBuild" => "📦",
                "CreatingPatch" => "🔧",
                "DeployingPatch" => "🚀",
                "Completed" => "✅",
                "Failed" => "❌",
                "Cancelled" => "⚠️",
                "Deleted" => "🗑️",
                _ => "📊"
            };
        }

        // ========== TIPS TAB ========== //





        // ========== DEVELOPER TAB ========== //

        private void DrawDeveloperTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Label("🔧 Developer Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "⚠️ DEVELOPER ONLY - These settings are for development purposes.\n" +
                "This tab will not be visible to end users.",
                MessageType.Warning
            );

            EditorGUILayout.Space(10);

            // Environment Selection
            EditorGUILayout.LabelField("Environment:", EditorStyles.boldLabel);
            GLCEnvironment newEnv = (GLCEnvironment)EditorGUILayout.EnumPopup("Target Environment", config.environment);

            if (newEnv != config.environment)
            {
                config.environment = newEnv;

                // Update API client with new URL
                apiClient = new GLCApiClient(config.GetApiUrl(), config.authToken);

                GLCConfigManager.SaveConfig(config);

                EditorGUILayout.HelpBox($"Environment changed to: {newEnv}", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // Current Environment Info
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Current Environment Configuration:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Environment:", config.environment.ToString());
            EditorGUILayout.LabelField("API URL:", config.GetApiUrl());
            EditorGUILayout.LabelField("Frontend URL:", config.GetFrontendUrl());

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Quick Actions
            EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🏠 Development", GUILayout.Height(40)))
            {
                config.environment = GLCEnvironment.Development;
                apiClient = new GLCApiClient(config.GetApiUrl(), config.authToken);
                GLCConfigManager.SaveConfig(config);
            }

            if (GUILayout.Button("🧪 Staging", GUILayout.Height(40)))
            {
                config.environment = GLCEnvironment.Staging;
                apiClient = new GLCApiClient(config.GetApiUrl(), config.authToken);
                GLCConfigManager.SaveConfig(config);
            }

            if (GUILayout.Button("🚀 Production", GUILayout.Height(40)))
            {
                config.environment = GLCEnvironment.Production;
                apiClient = new GLCApiClient(config.GetApiUrl(), config.authToken);
                GLCConfigManager.SaveConfig(config);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Environment Details
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Environment URLs:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Development:");
            EditorGUILayout.SelectableLabel("https://localhost:7226", EditorStyles.miniLabel, GUILayout.Height(16));
            EditorGUILayout.SelectableLabel("http://localhost:4200", EditorStyles.miniLabel, GUILayout.Height(16));

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Staging:");
            EditorGUILayout.SelectableLabel("https://stagingapi.gamelauncher.cloud", EditorStyles.miniLabel, GUILayout.Height(16));
            EditorGUILayout.SelectableLabel("https://staging.app.gamelauncher.cloud", EditorStyles.miniLabel, GUILayout.Height(16));

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Production:");
            EditorGUILayout.SelectableLabel("https://api.gamelaunchercloud.com", EditorStyles.miniLabel, GUILayout.Height(16));
            EditorGUILayout.SelectableLabel("https://app.gamelaunchercloud.com", EditorStyles.miniLabel, GUILayout.Height(16));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Toggle Developer Tab
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Developer Tab Visibility:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            bool newShowDev = EditorGUILayout.Toggle("Show Developer Tab", config.showDeveloperTab);
            if (newShowDev != config.showDeveloperTab)
            {
                config.showDeveloperTab = newShowDev;
                GLCConfigManager.SaveConfig(config);
            }

            EditorGUILayout.HelpBox(
                "Disable this before publishing to hide developer options from end users.",
                MessageType.Info
            );

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Clear Auth
            if (GUILayout.Button("Clear All Authentication Data", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear Auth", "Are you sure? This will logout and clear all saved credentials.", "Yes", "No"))
                {
                    GLCConfigManager.ClearAuth();
                    config = GLCConfigManager.LoadConfig();
                    apiClient.SetAuthToken("");
                    EditorUtility.DisplayDialog("Success", "Authentication data cleared!", "OK");
                }
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// Utility class to run coroutines in the Unity Editor
    /// </summary>
    public static class EditorCoroutineUtility
    {
        public static EditorCoroutine StartCoroutine(IEnumerator routine, EditorWindow window)
        {
            return new EditorCoroutine(routine, window);
        }

        public class EditorCoroutine
        {
            private IEnumerator routine;
            private EditorWindow window;

            public EditorCoroutine(IEnumerator routine, EditorWindow window)
            {
                this.routine = routine;
                this.window = window;
                EditorApplication.update += Update;
            }

            private void Update()
            {
                if (routine == null)
                {
                    EditorApplication.update -= Update;
                    return;
                }

                if (!routine.MoveNext())
                {
                    EditorApplication.update -= Update;
                }
            }
        }
    }
}