using UnityEngine;
using UnityEditor;
using System.Collections;

namespace GameLauncherCloud.Editor
{
    /// <summary>
    /// Epic setup wizard for Game Launcher Cloud Unity Extension
    /// Guides users through initial configuration
    /// </summary>
    public class GLCWizard : EditorWindow
    {
        // ========== WIZARD STATE ========== //
        
        private int currentStep = 0;
        private const int TOTAL_STEPS = 4;
        
        private GLCConfig config;
        private GLCApiClient apiClient;
        
        // ========== WIZARD DATA ========== //
        
        private string apiKeyInput = "";
        private bool isAuthenticating = false;
        private string authMessage = "";
        private MessageType authMessageType = MessageType.Info;
        
        private GLCApiClient.AppInfo[] availableApps = null;
        private int selectedAppIndex = 0;
        private bool isLoadingApps = false;
        
        // ========== STYLES ========== //
        
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle stepStyle;
        private GUIStyle cardStyle;
        private GUIStyle buttonStyle;
        private GUIStyle primaryButtonStyle;
        private bool stylesInitialized = false;
        
        private Texture2D icon;
        
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
            private Particle[] particles = new Particle[50];
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
                        (float)random.NextDouble() * 600,
                        (float)random.NextDouble() * 400
                    ),
                    velocity = new Vector2(
                        ((float)random.NextDouble() - 0.5f) * 0.5f,
                        ((float)random.NextDouble() - 0.5f) * 0.5f
                    ),
                    life = 1f,
                    maxLife = 1f,
                    color = new Color(
                        0.3f + (float)random.NextDouble() * 0.4f,
                        0.5f + (float)random.NextDouble() * 0.3f,
                        0.8f + (float)random.NextDouble() * 0.2f,
                        0.3f
                    )
                };
            }
            
            public void Update(float deltaTime)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i].position += particles[i].velocity;
                    particles[i].life -= deltaTime * 0.2f;
                    
                    if (particles[i].life <= 0)
                    {
                        particles[i] = CreateParticle();
                    }
                    
                    // Wrap around screen
                    if (particles[i].position.x < 0) particles[i].position.x = 600;
                    if (particles[i].position.x > 600) particles[i].position.x = 0;
                    if (particles[i].position.y < 0) particles[i].position.y = 400;
                    if (particles[i].position.y > 400) particles[i].position.y = 0;
                }
            }
            
            public void Draw()
            {
                foreach (var particle in particles)
                {
                    Color c = particle.color;
                    c.a = particle.life * 0.3f;
                    Handles.color = c;
                    Handles.DrawSolidDisc(particle.position, Vector3.forward, 2f);
                }
            }
        }
        
        // ========== WINDOW INITIALIZATION ========== //
        
        [MenuItem("Tools/Game Launcher Cloud - Setup Wizard", false, 100)]
        public static void ShowWizard()
        {
            GLCWizard window = GetWindow<GLCWizard>("GLC Setup Wizard");
            window.minSize = new Vector2(600, 800);
            window.maxSize = new Vector2(600, 800);
            window.Show();
        }
        
        private void OnEnable()
        {
            config = GLCConfigManager.LoadConfig();
            apiClient = new GLCApiClient(config.GetApiUrl(), config.authToken);
            icon = Resources.Load<Texture2D>("GameLauncherCloud_Icon");
            
            // Load saved API key for current environment
            string currentApiKey = config.GetApiKey();
            if (!string.IsNullOrEmpty(currentApiKey))
            {
                apiKeyInput = currentApiKey;
            }
            
            // Check if already configured
            if (GLCConfigManager.IsAuthenticated())
            {
                currentStep = 2; // Skip to app selection
            }
            
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
        
        // ========== GUI ========== //
        
        private void OnGUI()
        {
            InitializeStyles();
            
            // Draw particle background
            particles.Draw();
            
            // Semi-transparent overlay
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0.1f, 0.1f, 0.15f, 0.85f));
            
            // Main wizard container
            GUILayout.BeginArea(new Rect(20, 20, position.width - 40, position.height - 40));
            
            DrawHeader();
            
            GUILayout.Space(20);
            
            DrawStepIndicator();
            
            GUILayout.Space(20);
            
            // Content area
            GUILayout.BeginVertical(cardStyle);
            
            switch (currentStep)
            {
                case 0:
                    DrawWelcomeStep();
                    break;
                case 1:
                    DrawAuthenticationStep();
                    break;
                case 2:
                    DrawAppSelectionStep();
                    break;
                case 3:
                    DrawCompletionStep();
                    break;
            }
            
            GUILayout.EndVertical();
            
            GUILayout.Space(20);
            
            DrawNavigationButtons();
            
            GUILayout.EndArea();
        }
        
        private void InitializeStyles()
        {
            if (stylesInitialized) return;
            
            titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 24;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
            
            subtitleStyle = new GUIStyle(EditorStyles.label);
            subtitleStyle.fontSize = 12;
            subtitleStyle.alignment = TextAnchor.MiddleCenter;
            subtitleStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
            
            stepStyle = new GUIStyle(EditorStyles.boldLabel);
            stepStyle.fontSize = 18;
            stepStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
            
            cardStyle = new GUIStyle();
            cardStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.2f, 0.95f));
            cardStyle.padding = new RectOffset(20, 20, 20, 20);
            cardStyle.border = new RectOffset(2, 2, 2, 2);
            
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.padding = new RectOffset(15, 15, 8, 8);
            
            primaryButtonStyle = new GUIStyle(GUI.skin.button);
            primaryButtonStyle.fontSize = 14;
            primaryButtonStyle.fontStyle = FontStyle.Bold;
            primaryButtonStyle.padding = new RectOffset(20, 20, 10, 10);
            primaryButtonStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.6f, 0.9f, 1f));
            primaryButtonStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.7f, 1f, 1f));
            primaryButtonStyle.normal.textColor = Color.white;
            primaryButtonStyle.hover.textColor = Color.white;
            
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
        
        // ========== WIZARD SECTIONS ========== //
        
        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(60), GUILayout.Height(60));
            }
            else
            {
                GUILayout.Label("☁️", new GUIStyle(titleStyle) { fontSize = 40 }, GUILayout.Width(60));
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.Label("Game Launcher Cloud", titleStyle);
            GUILayout.Label("Unity Extension Setup Wizard", subtitleStyle);
        }
        
        private void DrawStepIndicator()
        {
            string[] stepNames = { "Welcome", "Authentication", "App Setup", "Complete" };
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            for (int i = 0; i < TOTAL_STEPS; i++)
            {
                GUILayout.BeginVertical(GUILayout.Width(100));
                
                // Step circle background
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                GUIStyle circleStyle = new GUIStyle(GUI.skin.box);
                circleStyle.alignment = TextAnchor.MiddleCenter;
                circleStyle.fontSize = 14;
                circleStyle.fontStyle = FontStyle.Bold;
                circleStyle.fixedWidth = 35;
                circleStyle.fixedHeight = 35;
                
                // Green for completed, blue for current/active, gray for pending
                Color circleColor;
                if (i < currentStep)
                    circleColor = new Color(0.2f, 0.8f, 0.3f); // Green for completed
                else if (i == currentStep)
                    circleColor = new Color(0.3f, 0.7f, 1f); // Blue for current
                else
                    circleColor = new Color(0.3f, 0.3f, 0.4f); // Gray for pending
                    
                circleStyle.normal.background = MakeTex(2, 2, circleColor);
                circleStyle.normal.textColor = Color.white;
                
                GUILayout.Label((i + 1).ToString(), circleStyle);
                
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                
                // Step label
                GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.wordWrap = true;
                labelStyle.normal.textColor = i <= currentStep ? new Color(0.9f, 0.95f, 1f) : new Color(0.5f, 0.5f, 0.6f);
                
                GUILayout.Label(stepNames[i], labelStyle);
                
                GUILayout.EndVertical();
                
                // Connection line between steps
                if (i < TOTAL_STEPS - 1)
                {
                    GUILayout.Space(5);
                    
                    GUILayout.BeginVertical();
                    GUILayout.Space(15);
                    
                    // Green if both steps completed, blue if current transition, gray otherwise
                    Color lineColor;
                    if (i < currentStep - 1)
                        lineColor = new Color(0.2f, 0.8f, 0.3f); // Green for completed
                    else if (i < currentStep)
                        lineColor = new Color(0.3f, 0.7f, 1f); // Blue for current
                    else
                        lineColor = new Color(0.3f, 0.3f, 0.4f); // Gray for pending
                    GUIStyle lineStyle = new GUIStyle();
                    lineStyle.normal.background = MakeTex(2, 2, lineColor);
                    lineStyle.fixedHeight = 3;
                    
                    GUILayout.Box("", lineStyle, GUILayout.Width(30), GUILayout.Height(3));
                    
                    GUILayout.EndVertical();
                    
                    GUILayout.Space(5);
                }
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
        }
        
        // ========== STEP CONTENT ========== //
        
        private void DrawWelcomeStep()
        {
            GUILayout.Space(20);
            
            GUILayout.Label("🎮 Welcome to Game Launcher Cloud!", stepStyle);
            
            GUILayout.Space(20);
            
            GUIStyle infoStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            infoStyle.fontSize = 13;
            infoStyle.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
            
            GUILayout.Label("This wizard will help you set up your Unity project to work with Game Launcher Cloud.", infoStyle);
            
            GUILayout.Space(15);
            
            GUILayout.Label("You'll be able to:", EditorStyles.boldLabel);
            
            GUILayout.Space(10);
            
            DrawFeatureItem("🔐", "Authenticate with your API Key");
            DrawFeatureItem("📱", "Select your application");
            DrawFeatureItem("🚀", "Build and upload directly from Unity");
            DrawFeatureItem("📊", "Monitor build status in real-time");
            
            GUILayout.Space(20);
            
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("ℹ️", GUILayout.Width(20));
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Don't have an account yet?", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Sign up at app.gamelauncher.cloud", EditorStyles.linkLabel))
            {
                Application.OpenURL("https://app.gamelauncher.cloud/");
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFeatureItem(string icon, string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(icon, new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 }, GUILayout.Width(30));
            GUILayout.Label(text, new GUIStyle(EditorStyles.label) { fontSize = 12 });
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
        }
        
        private void DrawAuthenticationStep()
        {
            GUILayout.Space(20);
            
            GUILayout.Label("🔐 Authentication", stepStyle);
            
            GUILayout.Space(20);
            
            GUIStyle infoStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            infoStyle.fontSize = 12;
            infoStyle.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
            
            GUILayout.Label("Enter your API Key to connect to Game Launcher Cloud:", infoStyle);
            
            GUILayout.Space(15);
            
            GUILayout.Label("API Key:", EditorStyles.boldLabel);
            apiKeyInput = EditorGUILayout.TextField(apiKeyInput, GUILayout.Height(25));
            
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("📚", GUILayout.Width(20));
            if (GUILayout.Button("How to get my API Key?", EditorStyles.linkLabel))
            {
                Application.OpenURL($"{config.GetFrontendUrl()}/user/api-keys");
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            
            GUI.enabled = !isAuthenticating && !string.IsNullOrEmpty(apiKeyInput);
            
            if (GUILayout.Button(isAuthenticating ? "⏳ Authenticating..." : "🔑 Authenticate", primaryButtonStyle, GUILayout.Height(40)))
            {
                AuthenticateWithApiKey();
            }
            
            GUI.enabled = true;
            
            GUILayout.Space(15);
            
            if (!string.IsNullOrEmpty(authMessage))
            {
                EditorGUILayout.HelpBox(authMessage, authMessageType);
            }
        }
        
        private void DrawAppSelectionStep()
        {
            GUILayout.Space(20);
            
            GUILayout.Label("📱 Select Your Application", stepStyle);
            
            GUILayout.Space(20);
            
            if (availableApps == null || availableApps.Length == 0)
            {
                GUIStyle infoStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                infoStyle.fontSize = 12;
                infoStyle.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
                
                GUILayout.Label("Load your applications from Game Launcher Cloud:", infoStyle);
                
                GUILayout.Space(20);
                
                GUI.enabled = !isLoadingApps;
                
                if (GUILayout.Button(isLoadingApps ? "⏳ Loading..." : "📱 Load My Apps", primaryButtonStyle, GUILayout.Height(40)))
                {
                    LoadApps();
                }
                
                GUI.enabled = true;
                
                GUILayout.Space(20);
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label("ℹ️", GUILayout.Width(20));
                EditorGUILayout.BeginVertical();
                GUILayout.Label("Don't have any apps?", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Create a new app", EditorStyles.linkLabel))
                {
                    Application.OpenURL($"{config.GetFrontendUrl()}/apps/new-app");
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUIStyle infoStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                infoStyle.fontSize = 12;
                infoStyle.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
                
                GUILayout.Label("Choose which application you want to upload builds to:", infoStyle);
                
                GUILayout.Space(15);
                
                GUILayout.Label("Application:", EditorStyles.boldLabel);
                
                string[] appNames = new string[availableApps.Length];
                for (int i = 0; i < availableApps.Length; i++)
                {
                    appNames[i] = $"{availableApps[i].Name} ({availableApps[i].BuildCount} builds)";
                }
                
                selectedAppIndex = EditorGUILayout.Popup(selectedAppIndex, appNames, GUILayout.Height(25));
                
                GUILayout.Space(20);
                
                // Show selected app info
                var selectedApp = availableApps[selectedAppIndex];
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label($"📋 {selectedApp.Name}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
                GUILayout.Space(5);
                GUILayout.Label($"Builds: {selectedApp.BuildCount}", EditorStyles.miniLabel);
                GUILayout.Label($"Owner: {(selectedApp.IsOwnedByUser ? "You" : "Team")}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawCompletionStep()
        {
            GUILayout.Space(20);
            
            GUILayout.Label("🎉 Setup Complete!", stepStyle);
            
            GUILayout.Space(20);
            
            GUIStyle infoStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            infoStyle.fontSize = 13;
            infoStyle.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
            infoStyle.alignment = TextAnchor.MiddleCenter;
            
            GUILayout.Label("Your Unity Extension is now configured and ready to use!", infoStyle);
            
            GUILayout.Space(30);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("✅ Configuration Summary", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label($"📧 Account: {config.userEmail}");
            GUILayout.Label($"💼 Plan: {config.userPlan}");
            
            if (availableApps != null && selectedAppIndex < availableApps.Length)
            {
                GUILayout.Label($"📱 Selected App: {availableApps[selectedAppIndex].Name}");
            }
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(30);
            
            GUILayout.Label("What's next?", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            DrawFeatureItem("1️⃣", "Open the GLC Manager from Tools menu");
            DrawFeatureItem("2️⃣", "Build your game and upload to the cloud");
            DrawFeatureItem("3️⃣", "Monitor your build status in real-time");
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("🚀 Open GLC Manager", primaryButtonStyle, GUILayout.Height(40)))
            {
                GLCManagerWindow.ShowWindow();
                Close();
            }
        }
        
        // ========== NAVIGATION ========== //
        
        private void DrawNavigationButtons()
        {
            GUILayout.BeginHorizontal();
            
            // Back button
            GUI.enabled = currentStep > 0 && currentStep < 3;
            if (GUILayout.Button("← Back", buttonStyle, GUILayout.Height(35), GUILayout.Width(100)))
            {
                currentStep--;
                authMessage = "";
            }
            GUI.enabled = true;
            
            GUILayout.FlexibleSpace();
            
            // Next/Finish button
            bool canProceed = CanProceedToNextStep();
            
            GUI.enabled = canProceed;
            
            string buttonText = currentStep == TOTAL_STEPS - 1 ? "Finish" : "Next →";
            
            if (GUILayout.Button(buttonText, primaryButtonStyle, GUILayout.Height(35), GUILayout.Width(100)))
            {
                if (currentStep == TOTAL_STEPS - 1)
                {
                    FinishWizard();
                }
                else
                {
                    currentStep++;
                }
            }
            
            GUI.enabled = true;
            
            GUILayout.EndHorizontal();
        }
        
        private bool CanProceedToNextStep()
        {
            switch (currentStep)
            {
                case 0: return true; // Welcome
                case 1: return GLCConfigManager.IsAuthenticated(); // Authentication
                case 2: return availableApps != null && availableApps.Length > 0; // App selection
                case 3: return true; // Completion
                default: return false;
            }
        }
        
        // ========== API CALLS ========== //
        
        private void AuthenticateWithApiKey()
        {
            isAuthenticating = true;
            authMessage = "";
            authMessageType = MessageType.Info;
            
            apiClient.LoginWithApiKeyAsync(apiKeyInput, OnLoginComplete);
        }
        
        private void OnLoginComplete(bool success, string message, GLCApiClient.LoginResponse loginResponse)
        {
            EditorApplication.delayCall += () =>
            {
                isAuthenticating = false;
                
                if (success && loginResponse != null)
                {
                    // Save API Key for current environment
                    config.SetApiKey(apiKeyInput);
                    config.authToken = loginResponse.Token;
                    config.userId = loginResponse.Id;
                    config.userEmail = loginResponse.Email;
                    config.userPlan = loginResponse.Subscription?.Plan?.Name ?? "Free";
                    GLCConfigManager.SaveConfig(config);
                    
                    apiClient = new GLCApiClient(config.GetApiUrl(), loginResponse.Token);
                    
                    authMessage = "✅ Authentication successful!";
                    authMessageType = MessageType.Info;
                    
                    // Auto-advance to next step after 1 second
                    EditorApplication.delayCall += () =>
                    {
                        System.Threading.Thread.Sleep(1000);
                        EditorApplication.delayCall += () =>
                        {
                            currentStep++;
                            Repaint();
                        };
                    };
                }
                else
                {
                    authMessage = message;
                    authMessageType = MessageType.Error;
                }
                
                Repaint();
            };
        }
        
        private void LoadApps()
        {
            isLoadingApps = true;
            apiClient.GetAppListAsync(OnAppsLoaded);
        }
        
        private void OnAppsLoaded(bool success, string message, GLCApiClient.AppInfo[] apps)
        {
            EditorApplication.delayCall += () =>
            {
                isLoadingApps = false;
                
                if (success && apps != null && apps.Length > 0)
                {
                    availableApps = apps;
                    selectedAppIndex = 0;
                }
                else
                {
                    EditorUtility.DisplayDialog("Load Apps", message, "OK");
                }
                
                Repaint();
            };
        }
        
        private void FinishWizard()
        {
            if (availableApps != null && selectedAppIndex < availableApps.Length)
            {
                config.selectedAppId = availableApps[selectedAppIndex].Id;
                config.selectedAppName = availableApps[selectedAppIndex].Name;
                GLCConfigManager.SaveConfig(config);
            }
            
            EditorUtility.DisplayDialog(
                "Setup Complete!",
                "Game Launcher Cloud is now configured!\n\nYou can access the GLC Manager from:\nTools > Game Launcher Cloud - Manager",
                "OK"
            );
            
            GLCManagerWindow.ShowWindow();
            Close();
        }
    }
}
