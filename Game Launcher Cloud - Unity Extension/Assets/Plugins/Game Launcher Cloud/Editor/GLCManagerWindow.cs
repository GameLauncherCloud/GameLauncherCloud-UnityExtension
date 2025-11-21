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
    /// Provides UI for authentication and build & upload
    /// </summary>
    public class GLCManagerWindow : EditorWindow
    {
        // ========== DEVELOPER SETTINGS ========== //

        // Set to true to show Developer tab, false to hide it from end users
        private const bool SHOW_DEVELOPER_TAB = false;

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

        // Build detection
        private bool hasBuildReady = false;

        private System.DateTime lastBuildDate;
        private string lastBuildPath = "";
        private long lastBuildSize = 0;
        private long uncompressedBuildSize = 0;
        private int totalFileCount = 0;
        private bool isCompressed = false;

        // ========== PARTICLES EFFECT ========== //

        private ParticleSystem particles = new ParticleSystem();

        private class Particle
        {
            public Vector2 position;
            public Vector2 velocity;
            public float life;
            public float maxLife;
            public Color color;
        }

        private class ParticleSystem
        {
            private Particle[] particles = new Particle[30];
            private System.Random random = new System.Random();

            public ParticleSystem()
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i] = CreateParticle();
                }
            }

            private Particle CreateParticle()
            {
                return new Particle
                {
                    position = new Vector2(
                        (float)random.NextDouble() * 550,
                        (float)random.NextDouble() * 650
                    ),
                    velocity = new Vector2(
                        ((float)random.NextDouble() - 0.5f) * 0.3f,
                        ((float)random.NextDouble() - 0.5f) * 0.3f
                    ),
                    life = 1f,
                    maxLife = 1f,
                    color = new Color(
                        0.2f + (float)random.NextDouble() * 0.3f,
                        0.4f + (float)random.NextDouble() * 0.3f,
                        0.7f + (float)random.NextDouble() * 0.3f,
                        0.2f
                    )
                };
            }

            public void Update(float deltaTime)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i].position += particles[i].velocity;
                    particles[i].life -= deltaTime * 0.15f;

                    if (particles[i].life <= 0)
                    {
                        particles[i] = CreateParticle();
                    }

                    // Wrap around screen
                    if (particles[i].position.x < 0)
                        particles[i].position.x = 550;
                    if (particles[i].position.x > 550)
                        particles[i].position.x = 0;
                    if (particles[i].position.y < 0)
                        particles[i].position.y = 650;
                    if (particles[i].position.y > 650)
                        particles[i].position.y = 0;
                }
            }

            public void Draw()
            {
                foreach (var particle in particles)
                {
                    Color c = particle.color;
                    c.a = particle.life * 0.2f;
                    Handles.color = c;
                    Handles.DrawSolidDisc(particle.position, Vector3.forward, 1.5f);
                }
            }
        }

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
            
            // Force Production environment when Developer tab is disabled
            if (!SHOW_DEVELOPER_TAB && config.environment != GLCEnvironment.Production)
            {
                config.environment = GLCEnvironment.Production;
                GLCConfigManager.SaveConfig(config);
                Debug.Log($"[GLC] OnEnable - Forced environment to Production (Developer tab disabled)");
            }
            
            Debug.Log($"[GLC] OnEnable - Config loaded. Environment: {config.environment}, API URL: {config.GetApiUrl()}");
            Debug.Log($"[GLC] OnEnable - Auth token length: {config.authToken?.Length ?? 0} chars");
            Debug.Log($"[GLC] OnEnable - User email: {config.userEmail}");

            // Reset to first tab if Developer tab is disabled and currently selected
            if (!SHOW_DEVELOPER_TAB && selectedTab == 1)
            {
                selectedTab = 0;
                Debug.Log($"[GLC] OnEnable - Reset selectedTab to 0 because Developer tab is disabled");
            }

            apiClient = new GLCApiClient(config.GetApiUrl(), config.authToken);
            Debug.Log($"[GLC] OnEnable - API Client created");

            // Load API Key for current environment
            string currentApiKey = config.GetApiKey();
            if (!string.IsNullOrEmpty(currentApiKey))
            {
                apiKeyInput = currentApiKey;
                Debug.Log($"[GLC] OnEnable - API Key loaded from config for environment: {config.environment}");
            }

            // Load icon
            icon = Resources.Load<Texture2D>("GameLauncherCloud_Icon");

            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            particles.Update(0.016f);
            Repaint();
        }

        private void OnGUI()
        {
            InitializeStyles();

            // Draw particle background
            particles.Draw();

            // Semi-transparent overlay for content readability
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0.1f, 0.1f, 0.15f, 0.75f));

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
                        if (SHOW_DEVELOPER_TAB)
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
            if (stylesInitialized)
                return;

            // Header Style with epic gradient-like effect
            headerStyle = new GUIStyle();
            headerStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.25f, 0.45f, 0.95f));
            headerStyle.padding = new RectOffset(15, 15, 15, 15);
            headerStyle.border = new RectOffset(0, 0, 0, 2);

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

            // Section Header Style with epic appearance
            sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            sectionHeaderStyle.fontSize = 15;
            sectionHeaderStyle.margin = new RectOffset(0, 0, 10, 5);
            sectionHeaderStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
            sectionHeaderStyle.fontStyle = FontStyle.Bold;

            // Card Style with epic appearance
            cardStyle = new GUIStyle(EditorStyles.helpBox);
            cardStyle.padding = new RectOffset(15, 15, 15, 15);
            cardStyle.margin = new RectOffset(10, 10, 5, 5);
            cardStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.2f, 0.9f));
            cardStyle.border = new RectOffset(2, 2, 2, 2);

            // Button Style with epic appearance
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.padding = new RectOffset(15, 15, 8, 8);
            buttonStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.3f, 0.4f, 1f));
            buttonStyle.hover.background = MakeTex(2, 2, new Color(0.35f, 0.4f, 0.5f, 1f));
            buttonStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
            buttonStyle.hover.textColor = Color.white;

            // Primary Button Style with epic appearance
            primaryButtonStyle = new GUIStyle(GUI.skin.button);
            primaryButtonStyle.fontSize = 14;
            primaryButtonStyle.fontStyle = FontStyle.Bold;
            primaryButtonStyle.padding = new RectOffset(20, 20, 12, 12);
            primaryButtonStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.6f, 0.9f, 1f));
            primaryButtonStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.7f, 1f, 1f));
            primaryButtonStyle.active.background = MakeTex(2, 2, new Color(0.15f, 0.5f, 0.8f, 1f));
            primaryButtonStyle.normal.textColor = Color.white;
            primaryButtonStyle.hover.textColor = Color.white;
            primaryButtonStyle.active.textColor = new Color(0.9f, 0.95f, 1f);

            // Link Button Style with epic appearance
            linkButtonStyle = new GUIStyle(EditorStyles.linkLabel);
            linkButtonStyle.fontSize = 11;
            linkButtonStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);
            linkButtonStyle.hover.textColor = new Color(0.6f, 0.9f, 1f);

            // Tab Style for modern tabs
            tabStyle = new GUIStyle(GUI.skin.button);
            tabStyle.fontSize = 13;
            tabStyle.padding = new RectOffset(20, 20, 10, 10);
            tabStyle.margin = new RectOffset(2, 2, 0, 0);
            tabStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.25f, 0.35f, 0.8f));
            tabStyle.hover.background = MakeTex(2, 2, new Color(0.25f, 0.3f, 0.4f, 0.9f));
            tabStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
            tabStyle.hover.textColor = new Color(0.9f, 0.95f, 1f);

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

            // Quick access links
            GUIStyle linkStyle = new GUIStyle(GUI.skin.button);
            linkStyle.fontSize = 11;
            linkStyle.padding = new RectOffset(8, 8, 4, 4);
            linkStyle.normal.textColor = new Color(0.7f, 0.9f, 1f);

            GUIStyle headerLinkStyle = new GUIStyle(GUI.skin.button);
            headerLinkStyle.fontSize = 11;
            headerLinkStyle.padding = new RectOffset(8, 8, 4, 4);
            headerLinkStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.6f, 0.8f));
            headerLinkStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.5f, 0.7f, 1f));
            headerLinkStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
            headerLinkStyle.hover.textColor = Color.white;

            if (GUILayout.Button("📚 Docs", headerLinkStyle, GUILayout.Height(24), GUILayout.Width(65)))
            {
                Application.OpenURL("https://help.gamelauncher.cloud");
            }

            if (GUILayout.Button("💬 Discord", headerLinkStyle, GUILayout.Height(24), GUILayout.Width(75)))
            {
                Application.OpenURL("https://discord.com/invite/FpWvUQ2CJP");
            }

            GUILayout.Space(10);

            // Status indicator and Logout button
            if (GLCConfigManager.IsAuthenticated())
            {
                EditorGUILayout.BeginVertical();

                GUILayout.Label("✓ Connected", new GUIStyle(subtitleStyle) { normal = new GUIStyleState { textColor = new Color(0.4f, 1f, 0.4f) } });
                GUILayout.Label(config.userEmail, new GUIStyle(subtitleStyle) { fontSize = 11 });
                if (!string.IsNullOrEmpty(config.userPlan))
                {
                    GUILayout.Label($"Plan: {config.userPlan}", new GUIStyle(subtitleStyle) { fontSize = 10, normal = new GUIStyleState { textColor = new Color(0.8f, 0.9f, 1f, 0.8f) } });
                }

                EditorGUILayout.Space(3);

                // Logout button with epic styling
                GUIStyle logoutButtonStyle = new GUIStyle(GUI.skin.button);
                logoutButtonStyle.fontSize = 10;
                logoutButtonStyle.padding = new RectOffset(8, 8, 3, 3);
                logoutButtonStyle.normal.background = MakeTex(2, 2, new Color(0.6f, 0.2f, 0.2f, 0.8f));
                logoutButtonStyle.hover.background = MakeTex(2, 2, new Color(0.8f, 0.3f, 0.3f, 1f));
                logoutButtonStyle.normal.textColor = new Color(1f, 0.9f, 0.9f);
                logoutButtonStyle.hover.textColor = Color.white;

                if (GUILayout.Button("Logout", logoutButtonStyle, GUILayout.Height(20), GUILayout.Width(55)))
                {
                    Logout();
                }

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
            else if (SHOW_DEVELOPER_TAB)
            {
                currentTabNames = devTabNamesAuth;
            }
            else
            {
                currentTabNames = tabNamesAuth;
            }

            // Custom tab buttons with epic styling
            for (int i = 0; i < currentTabNames.Length; i++)
            {
                GUIStyle tabButtonStyle = new GUIStyle(tabStyle);

                if (i == selectedTab)
                {
                    // Active tab with epic glow effect
                    tabButtonStyle.fontStyle = FontStyle.Bold;
                    tabButtonStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.55f, 0.85f, 1f));
                    tabButtonStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.6f, 0.9f, 1f));
                    tabButtonStyle.normal.textColor = Color.white;
                    tabButtonStyle.hover.textColor = Color.white;
                }

                if (GUILayout.Button(currentTabNames[i], tabButtonStyle, GUILayout.Height(38)))
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

            GUIStyle envStyle = new GUIStyle(EditorStyles.label);
            envStyle.fontSize = 10;
            envStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
            GUILayout.Label($"Environment: {config.environment}", envStyle);
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
                    GUIStyle loggedInStyle = new GUIStyle(EditorStyles.label);
                    loggedInStyle.fontSize = 11;
                    loggedInStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
                    GUILayout.Label($"Logged in as: {config.userEmail}", loggedInStyle);
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
                GUIStyle apiKeyHintStyle = new GUIStyle(EditorStyles.label);
                apiKeyHintStyle.fontSize = 11;
                apiKeyHintStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
                GUILayout.Label("Don't have an API Key?", apiKeyHintStyle);
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
            GUIStyle secondaryButtonStyle = new GUIStyle(buttonStyle);
            secondaryButtonStyle.fontSize = 12;
            secondaryButtonStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.4f, 0.6f, 1f));
            secondaryButtonStyle.hover.background = MakeTex(2, 2, new Color(0.35f, 0.5f, 0.7f, 1f));

            if (GUILayout.Button("🌐 Visit Website", secondaryButtonStyle, GUILayout.Height(34)))
            {
                Application.OpenURL("https://gamelauncher.cloud");
            }
            if (GUILayout.Button("📚 Documentation", secondaryButtonStyle, GUILayout.Height(34)))
            {
                Application.OpenURL("https://help.gamelauncher.cloud");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
        }

        private void DrawCard(System.Action content, Color? backgroundColor = null)
        {
            // Epic card with enhanced styling
            GUIStyle epicCardStyle = new GUIStyle(cardStyle);

            if (backgroundColor.HasValue)
            {
                epicCardStyle.normal.background = MakeTex(2, 2, new Color(
                    backgroundColor.Value.r * 0.3f + 0.1f,
                    backgroundColor.Value.g * 0.3f + 0.1f,
                    backgroundColor.Value.b * 0.3f + 0.15f,
                    0.9f
                ));
            }

            EditorGUILayout.BeginVertical(epicCardStyle);
            content();
            EditorGUILayout.EndVertical();
        }

        private void DrawInfoBox(string message, MessageType type)
        {
            // Epic info box colors with glow effect
            Color boxColor = type == MessageType.Error ? new Color(0.8f, 0.2f, 0.2f, 0.5f) :
                            type == MessageType.Warning ? new Color(0.9f, 0.7f, 0.2f, 0.5f) :
                            type == MessageType.Info ? new Color(0.2f, 0.6f, 0.9f, 0.5f) :
                            new Color(0.2f, 0.8f, 0.3f, 0.5f);

            DrawCard(() =>
            {
                string icon = type == MessageType.Error ? "❌" :
                             type == MessageType.Warning ? "⚠️" :
                             type == MessageType.Info ? "ℹ️" : "✅";

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(icon, new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 }, GUILayout.Width(35));

                GUIStyle messageStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                messageStyle.fontSize = 12;
                messageStyle.normal.textColor = new Color(0.95f, 0.97f, 1f);

                GUILayout.Label(message, messageStyle);
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
                // Save API Key for current environment
                config.SetApiKey(apiKeyInput);
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

            // CLI Warning for large builds
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("⚠️", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 }, GUILayout.Width(25));
            EditorGUILayout.BeginVertical();
            GUILayout.Label("For large builds (>5GB), we recommend using the CLI", EditorStyles.wordWrappedLabel);
            EditorGUILayout.BeginHorizontal();
            GUIStyle cliInfoStyle = new GUIStyle(EditorStyles.label);
            cliInfoStyle.fontSize = 11;
            cliInfoStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
            GUILayout.Label("The CLI is optimized for heavy builds with multipart upload.", cliInfoStyle);
            if (GUILayout.Button("Download CLI", EditorStyles.linkLabel, GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL("https://help.gamelauncher.cloud/applications/cli-releases");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

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
                    GUIStyle cardLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                    cardLabelStyle.fontSize = 13;
                    cardLabelStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);

                    GUILayout.Label("Select Application", cardLabelStyle);
                    EditorGUILayout.Space(5);

                    string[] appNames = availableApps.Select(a =>
                        $"{a.Name} ({a.BuildCount} builds)" + (a.IsOwnedByUser ? "" : " [Team]")
                    ).ToArray();

                    GUIStyle popupStyle = new GUIStyle(EditorStyles.popup);
                    popupStyle.fontSize = 13;
                    popupStyle.fixedHeight = 28;
                    popupStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.25f, 1f));
                    popupStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
                    popupStyle.border = new RectOffset(4, 20, 4, 4);

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

                    GUIStyle miniButtonStyle = new GUIStyle(GUI.skin.button);
                    miniButtonStyle.fontSize = 11;
                    miniButtonStyle.padding = new RectOffset(12, 12, 6, 6);
                    miniButtonStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.35f, 0.5f, 1f));
                    miniButtonStyle.hover.background = MakeTex(2, 2, new Color(0.35f, 0.45f, 0.6f, 1f));
                    miniButtonStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
                    miniButtonStyle.hover.textColor = Color.white;

                    if (GUILayout.Button("🔄 Refresh Apps", miniButtonStyle, GUILayout.Height(26)))
                    {
                        buildMessage = "";
                        buildMessageType = MessageType.None;
                        isUploading = false;
                        uploadProgress = 0f;
                        LoadApps();
                    }

                    if (GUILayout.Button("⚙️ Manage App", miniButtonStyle, GUILayout.Height(26)))
                    {
                        if (availableApps != null && selectedAppIndex < availableApps.Length)
                        {
                            long appId = availableApps[selectedAppIndex].Id;
                            string url = $"{config.GetFrontendUrl()}/apps/id/{appId}/overview";
                            Application.OpenURL(url);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                });

                EditorGUILayout.Space(5);

                // Open Dashboard Button with epic styling
                GUIStyle dashboardButtonStyle = new GUIStyle(buttonStyle);
                dashboardButtonStyle.fontSize = 13;
                dashboardButtonStyle.fontStyle = FontStyle.Bold;
                dashboardButtonStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.5f, 0.7f, 1f));
                dashboardButtonStyle.hover.background = MakeTex(2, 2, new Color(0.4f, 0.6f, 0.8f, 1f));

                if (GUILayout.Button("📊 Open Dashboard", dashboardButtonStyle, GUILayout.Height(36)))
                {
                    string url = $"{config.GetFrontendUrl()}/dashboard";
                    Application.OpenURL(url);
                }

                EditorGUILayout.Space(10);

                // Build Notes Card with epic styling
                DrawCard(() =>
                {
                    GUIStyle notesLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                    notesLabelStyle.fontSize = 13;
                    notesLabelStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);

                    GUIStyle miniLabelStyle = new GUIStyle(EditorStyles.label);
                    miniLabelStyle.fontSize = 11;
                    miniLabelStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);

                    GUILayout.Label("Build Notes", notesLabelStyle);
                    EditorGUILayout.Space(5);
                    GUILayout.Label("Add version info, changelog, or important notes", miniLabelStyle);
                    EditorGUILayout.Space(5);

                    GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
                    textAreaStyle.wordWrap = true;
                    textAreaStyle.fontSize = 11;
                    textAreaStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.15f, 1f));
                    textAreaStyle.normal.textColor = new Color(0.95f, 0.97f, 1f);
                    textAreaStyle.padding = new RectOffset(8, 8, 8, 8);

                    buildNotesInput = EditorGUILayout.TextArea(buildNotesInput, textAreaStyle, GUILayout.Height(80));
                }, new Color(0.15f, 0.2f, 0.3f, 0.85f));

                EditorGUILayout.Space(10);

                // Check for existing build
                CheckForExistingBuild();

                // Build Status Card (if build exists)
                if (hasBuildReady)
                {
                    DrawCard(() =>
                    {
                        GUIStyle statusLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                        statusLabelStyle.fontSize = 14;
                        statusLabelStyle.normal.textColor = new Color(0.2f, 0.9f, 0.3f);

                        GUIStyle infoStyle = new GUIStyle(EditorStyles.label);
                        infoStyle.fontSize = 11;
                        infoStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("✅", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 }, GUILayout.Width(30));
                        EditorGUILayout.BeginVertical();
                        GUILayout.Label("Build Ready", statusLabelStyle);
                        GUILayout.Label($"Last build: {lastBuildDate:yyyy-MM-dd HH:mm:ss}", infoStyle);

                        // Show detailed information
                        if (isCompressed)
                        {
                            if (uncompressedBuildSize > 0)
                            {
                                float compressionRatio = (1 - (lastBuildSize / (float)uncompressedBuildSize)) * 100;
                                GUILayout.Label($"Files: {totalFileCount} | Uncompressed: {uncompressedBuildSize / (1024.0 * 1024.0):F2} MB | Compressed: {lastBuildSize / (1024.0 * 1024.0):F2} MB ({compressionRatio:F1}% saved)", infoStyle);
                            }
                            else
                            {
                                GUILayout.Label($"Compressed size: {lastBuildSize / (1024.0 * 1024.0):F2} MB", infoStyle);
                            }
                        }
                        else
                        {
                            GUILayout.Label($"Files: {totalFileCount} | Size: {lastBuildSize / (1024.0 * 1024.0):F2} MB (Not compressed)", infoStyle);
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();

                        // Show in Explorer button (only if compressed)
                        if (isCompressed && File.Exists(lastBuildPath))
                        {
                            EditorGUILayout.Space(8);

                            GUIStyle showInExplorerStyle = new GUIStyle(GUI.skin.button);
                            showInExplorerStyle.fontSize = 11;
                            showInExplorerStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.3f, 0.4f, 0.9f));
                            showInExplorerStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.4f, 0.5f, 1f));
                            showInExplorerStyle.active.background = MakeTex(2, 2, new Color(0.2f, 0.25f, 0.35f, 0.9f));
                            showInExplorerStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
                            showInExplorerStyle.hover.textColor = new Color(1f, 1f, 1f);

                            if (GUILayout.Button("📁 Show in Explorer", showInExplorerStyle, GUILayout.Height(28)))
                            {
                                EditorUtility.RevealInFinder(lastBuildPath);
                            }
                        }
                    }, new Color(0.15f, 0.3f, 0.2f, 0.85f));

                    EditorGUILayout.Space(10);
                }

                // Build & Upload Buttons
                GUI.enabled = !isBuilding && !isUploading;

                GUIStyle buildButtonStyle = new GUIStyle(GUI.skin.button);
                buildButtonStyle.fontSize = 16;
                buildButtonStyle.fontStyle = FontStyle.Bold;
                buildButtonStyle.padding = new RectOffset(24, 24, 14, 14);
                buildButtonStyle.alignment = TextAnchor.MiddleCenter;
                buildButtonStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.75f, 0.25f, 1f));
                buildButtonStyle.hover.background = MakeTex(2, 2, new Color(0.2f, 0.85f, 0.3f, 1f));
                buildButtonStyle.active.background = MakeTex(2, 2, new Color(0.1f, 0.65f, 0.2f, 1f));
                buildButtonStyle.normal.textColor = new Color(1f, 1f, 1f);
                buildButtonStyle.hover.textColor = new Color(1f, 1f, 1f);
                buildButtonStyle.active.textColor = new Color(0.95f, 0.95f, 0.95f);
                buildButtonStyle.border = new RectOffset(6, 6, 6, 6);

                if (hasBuildReady)
                {
                    // Show separate Build and Upload buttons
                    EditorGUILayout.BeginHorizontal();

                    string buildText = isBuilding ? "⚙️ Building..." : "🔨 Build";
                    if (GUILayout.Button(buildText, buildButtonStyle, GUILayout.Height(50)))
                    {
                        StartBuildOnly();
                    }

                    GUILayout.Space(10);

                    GUIStyle uploadButtonStyle = new GUIStyle(buildButtonStyle);
                    uploadButtonStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.5f, 0.9f, 1f));
                    uploadButtonStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.6f, 1f, 1f));
                    uploadButtonStyle.active.background = MakeTex(2, 2, new Color(0.15f, 0.45f, 0.8f, 1f));

                    string uploadText = isUploading ? "☁️ Uploading..." : "☁️ Upload to Cloud";
                    if (GUILayout.Button(uploadText, uploadButtonStyle, GUILayout.Height(50)))
                    {
                        StartUploadOnly();
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // Show combined Build & Upload button
                    string buttonText = isBuilding ? "⚙️ Building..." :
                                       isUploading ? "☁️ Uploading..." :
                                       "🚀 Build & Upload to Cloud";

                    if (GUILayout.Button(buttonText, buildButtonStyle, GUILayout.Height(50)))
                    {
                        StartBuildAndUpload();
                    }
                }

                GUI.enabled = true;

                EditorGUILayout.Space(8);

                // Unity Build Profiles Button
                GUIStyle buildProfilesButtonStyle = new GUIStyle(buttonStyle);
                buildProfilesButtonStyle.fontSize = 12;
                buildProfilesButtonStyle.fontStyle = FontStyle.Normal;
                buildProfilesButtonStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.3f, 0.4f, 0.9f));
                buildProfilesButtonStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.4f, 0.5f, 1f));
                buildProfilesButtonStyle.active.background = MakeTex(2, 2, new Color(0.2f, 0.25f, 0.35f, 0.9f));
                buildProfilesButtonStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
                buildProfilesButtonStyle.hover.textColor = new Color(1f, 1f, 1f);

                if (GUILayout.Button("🔧 Unity Build Profiles", buildProfilesButtonStyle, GUILayout.Height(32)))
                {
                    EditorApplication.ExecuteMenuItem("File/Build Profiles");
                }

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
                            GUIStyle progressHintStyle = new GUIStyle(EditorStyles.label);
                            progressHintStyle.fontSize = 11;
                            progressHintStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
                            GUILayout.Label("This may take a few minutes", progressHintStyle);
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
                        GUIStyle uploadHintStyle = new GUIStyle(EditorStyles.label);
                        uploadHintStyle.fontSize = 11;
                        uploadHintStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
                        GUILayout.Label("This may take several minutes", uploadHintStyle);
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

        private void CheckForExistingBuild()
        {
            string buildPath = Path.Combine(Application.dataPath, "..", "Builds", "GLC_Upload");
            string zipPath = Path.Combine(Application.dataPath, "..", "Builds", $"{Application.productName}_upload.zip");

            // Check if compressed build exists
            if (File.Exists(zipPath))
            {
                FileInfo zipInfo = new FileInfo(zipPath);
                hasBuildReady = true;
                lastBuildDate = zipInfo.LastWriteTime;
                lastBuildPath = zipPath;
                lastBuildSize = zipInfo.Length;
                isCompressed = true;

                // Get uncompressed size and file count from build directory if it exists
                if (Directory.Exists(buildPath))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(buildPath);
                    FileInfo[] files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                    totalFileCount = files.Length;
                    uncompressedBuildSize = files.Sum(fi => fi.Length);
                }
                else
                {
                    totalFileCount = 0;
                    uncompressedBuildSize = 0;
                }
            }
            // Check if uncompressed build exists
            else if (Directory.Exists(buildPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(buildPath);
                hasBuildReady = true;
                lastBuildDate = dirInfo.LastWriteTime;
                lastBuildPath = buildPath;
                isCompressed = false;

                // Calculate directory size and file count
                FileInfo[] files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                totalFileCount = files.Length;
                lastBuildSize = files.Sum(fi => fi.Length);
                uncompressedBuildSize = lastBuildSize;
            }
            else
            {
                hasBuildReady = false;
            }
        }

        private void StartBuildOnly()
        {
            if (availableApps == null || selectedAppIndex >= availableApps.Length)
            {
                EditorUtility.DisplayDialog("Error", "Please select a valid app", "OK");
                return;
            }

            isBuilding = true;
            buildMessage = "Starting build process...";
            buildMessageType = MessageType.Info;

            // Build the game (compress only, don't upload)
            BuildGame(compressOnly: true);
        }

        private void StartUploadOnly()
        {
            if (availableApps == null || selectedAppIndex >= availableApps.Length)
            {
                EditorUtility.DisplayDialog("Error", "Please select a valid app", "OK");
                return;
            }

            if (!hasBuildReady)
            {
                EditorUtility.DisplayDialog("Error", "No build found. Please build first.", "OK");
                return;
            }

            string buildPath = Path.Combine(Application.dataPath, "..", "Builds", "GLC_Upload");

            if (isCompressed)
            {
                // Upload existing compressed build
                long appId = availableApps[selectedAppIndex].Id;

                // Calculate uncompressed size if build directory exists
                long? uncompressedSize = null;
                if (Directory.Exists(buildPath))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(buildPath);
                    uncompressedSize = dirInfo.GetFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
                }

                isUploading = true;
                buildMessage = "Starting upload...";
                buildMessageType = MessageType.Info;
                EditorApplication.delayCall += () =>
                {
                    EditorCoroutineUtility.StartCoroutine(
                        UploadBuild(appId, lastBuildPath, lastBuildSize, uncompressedSize),
                        this
                    );
                };
            }
            else
            {
                // Compress and upload
                CompressAndUpload(buildPath);
            }
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

        private void BuildGame(bool compressOnly = false)
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

                // Update build detection
                CheckForExistingBuild();

                if (compressOnly)
                {
                    // Only compress, don't upload
                    CompressOnly(buildPath);
                }
                else
                {
                    // Compress and upload
                    CompressAndUpload(buildPath);
                }
            }
            else
            {
                buildMessage = $"Build failed: {report.summary.result}";
                buildMessageType = MessageType.Error;
                isBuilding = false;
            }

            Repaint();
        }

        private void CompressOnly(string buildPath)
        {
            try
            {
                Debug.Log($"[GLC] === Starting CompressOnly ===");
                Debug.Log($"[GLC] Build path: {buildPath}");

                string zipPath = Path.Combine(Application.dataPath, "..", "Builds", $"{Application.productName}_upload.zip");
                Debug.Log($"[GLC] ZIP path: {zipPath}");

                // Delete existing zip if exists
                if (File.Exists(zipPath))
                {
                    Debug.Log($"[GLC] Deleting existing ZIP file...");
                    File.Delete(zipPath);
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

                buildMessage = $"Build compressed successfully! Size: {zipSize / (1024 * 1024):F2} MB";
                buildMessageType = MessageType.Info;

                // Update build detection
                CheckForExistingBuild();

                Debug.Log($"[GLC] Compression completed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GLC] Compression failed: {ex.Message}");
                Debug.LogError($"[GLC] Stack trace: {ex.StackTrace}");
                buildMessage = $"Compression failed: {ex.Message}";
                buildMessageType = MessageType.Error;
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
                
                // Load API Key for the new environment
                string newEnvApiKey = config.GetApiKey();
                if (!string.IsNullOrEmpty(newEnvApiKey))
                {
                    apiKeyInput = newEnvApiKey;
                    Debug.Log($"[GLC] Loaded API Key for environment: {newEnv}");
                }
                else
                {
                    apiKeyInput = "";
                    Debug.Log($"[GLC] No API Key found for environment: {newEnv}");
                }

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

            // Saved API Keys Info
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Saved API Keys per Environment:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            string FormatApiKey(string key)
            {
                if (string.IsNullOrEmpty(key)) return "Not set";
                if (key.Length <= 8) return "••••••••";
                return key.Substring(0, 4) + "••••" + key.Substring(key.Length - 4);
            }

            EditorGUILayout.LabelField("Production:", FormatApiKey(config.apiKeyProduction));
            EditorGUILayout.LabelField("Staging:", FormatApiKey(config.apiKeyStaging));
            EditorGUILayout.LabelField("Development:", FormatApiKey(config.apiKeyDevelopment));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Quick Actions
            EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🏠 Development", GUILayout.Height(40)))
            {
                config.environment = GLCEnvironment.Development;
                apiClient = new GLCApiClient(config.GetApiUrl(), config.authToken);
                apiKeyInput = config.GetApiKey();
                GLCConfigManager.SaveConfig(config);
            }

            if (GUILayout.Button("🧪 Staging", GUILayout.Height(40)))
            {
                config.environment = GLCEnvironment.Staging;
                apiClient = new GLCApiClient(config.GetApiUrl(), config.authToken);
                apiKeyInput = config.GetApiKey();
                GLCConfigManager.SaveConfig(config);
            }

            if (GUILayout.Button("🚀 Production", GUILayout.Height(40)))
            {
                config.environment = GLCEnvironment.Production;
                apiClient = new GLCApiClient(config.GetApiUrl(), config.authToken);
                apiKeyInput = config.GetApiKey();
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
            EditorGUILayout.SelectableLabel("https://api.gamelauncher.cloud", EditorStyles.miniLabel, GUILayout.Height(16));
            EditorGUILayout.SelectableLabel("https://app.gamelauncher.cloud", EditorStyles.miniLabel, GUILayout.Height(16));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Toggle Developer Tab
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Developer Tab Visibility:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                $"Developer tab is currently: {(SHOW_DEVELOPER_TAB ? "ENABLED" : "DISABLED")}\n\n" +
                "To change this, modify the SHOW_DEVELOPER_TAB constant in GLCManagerWindow.cs\n" +
                "Set to false before publishing to hide developer options from end users.",
                SHOW_DEVELOPER_TAB ? MessageType.Warning : MessageType.Info
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