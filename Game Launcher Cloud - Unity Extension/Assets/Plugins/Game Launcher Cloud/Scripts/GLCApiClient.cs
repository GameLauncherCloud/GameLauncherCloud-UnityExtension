using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameLauncherCloud
{
    /// <summary>
    /// API Client for Game Launcher Cloud backend communication
    /// Handles authentication, app listing, and build upload operations
    /// Uses HttpClient for better SSL handling and compatibility
    /// </summary>
    public class GLCApiClient
    {
        private string baseUrl;
        private string authToken;
        private static HttpClient httpClient;

        public GLCApiClient(string baseUrl, string authToken = "")
        {
            this.baseUrl = baseUrl;
            this.authToken = authToken;
            
            // Initialize HttpClient with SSL certificate handler for localhost
            if (httpClient == null)
            {
                var handler = new HttpClientHandler();
                if (baseUrl.Contains("localhost") || baseUrl.Contains("127.0.0.1"))
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                }
                httpClient = new HttpClient(handler);
                httpClient.Timeout = TimeSpan.FromMinutes(10);
            }
        }

        public void SetAuthToken(string token)
        {
            this.authToken = token;
        }

        /// <summary>
        /// Authenticate with API Key (Interactive Login)
        /// </summary>
        public async void LoginWithApiKeyAsync(string apiKey, Action<bool, string, LoginResponse> callback)
        {
            try
            {
                UnityEngine.Debug.Log("[GLC] === LoginWithApiKey ASYNC Started ===");
                
                string url = $"{baseUrl}/api/cli/build/login-interactive";
                var requestData = new LoginInteractiveRequest { ApiKey = apiKey };
                string jsonData = JsonConvert.SerializeObject(requestData);
                
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<LoginResponse>>(responseBody);
                        
                        // Check if we have a valid result with token
                        if (apiResponse.Result != null && !string.IsNullOrEmpty(apiResponse.Result.Token))
                        {
                            authToken = apiResponse.Result.Token;
                            UnityEngine.Debug.Log($"[GLC] Login successful as {apiResponse.Result.Email}");
                            callback?.Invoke(true, "Login successful", apiResponse.Result);
                        }
                        else
                        {
                            // Show all error messages
                            string error = "Login failed";
                            if (apiResponse.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0)
                            {
                                error = string.Join("\n", apiResponse.ErrorMessages);
                            }
                            UnityEngine.Debug.LogWarning($"[GLC] Login failed: {error}");
                            callback?.Invoke(false, error, null);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        UnityEngine.Debug.LogError($"[GLC] Failed to parse login response: {parseEx.Message}");
                        callback?.Invoke(false, $"Parse error: {parseEx.Message}", null);
                    }
                }
                else
                {
                    // Show HTTP error with response body
                    string errorMsg = $"HTTP {(int)response.StatusCode} {response.StatusCode}\n\n";
                    
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<LoginResponse>>(responseBody);
                        if (apiResponse.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0)
                        {
                            errorMsg += string.Join("\n", apiResponse.ErrorMessages);
                        }
                        else
                        {
                            errorMsg += responseBody;
                        }
                    }
                    catch
                    {
                        errorMsg += responseBody;
                    }
                    
                    UnityEngine.Debug.LogError($"[GLC] Login request failed: {errorMsg}");
                    callback?.Invoke(false, errorMsg, null);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GLC] Connection error during login: {ex.Message}");
                callback?.Invoke(false, $"Connection error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Get list of apps accessible to the user
        /// </summary>
        public async void GetAppListAsync(Action<bool, string, AppInfo[]> callback)
        {
            if (string.IsNullOrEmpty(authToken))
            {
                callback?.Invoke(false, "Not authenticated", null);
                return;
            }

            try
            {
                UnityEngine.Debug.Log("[GLC] === GetAppList ASYNC Started ===");
                
                var handler = new HttpClientHandler();
                if (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("localhost"))
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                }

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                    
                    string url = $"{baseUrl}/api/cli/build/list-apps";
                    var response = await client.GetAsync(url);
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var settings = new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() };
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<AppListResponse>>(responseBody, settings);
                        
                        if (apiResponse.IsSuccess && apiResponse.Result != null)
                        {
                            UnityEngine.Debug.Log($"[GLC] Retrieved {apiResponse.Result.Apps.Length} apps successfully");
                            callback?.Invoke(true, "Apps retrieved successfully", apiResponse.Result.Apps);
                        }
                        else
                        {
                            string error = apiResponse.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0
                                ? apiResponse.ErrorMessages[0]
                                : "Failed to get apps";
                            UnityEngine.Debug.LogWarning($"[GLC] Get apps failed: {error}");
                            callback?.Invoke(false, error, null);
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[GLC] Get apps request failed: {response.StatusCode}");
                        callback?.Invoke(false, $"Request failed: {response.StatusCode}", null);
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GLC] Error getting app list: {ex.Message}");
                callback?.Invoke(false, $"Connection error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Check if user can upload a build with specified file size
        /// </summary>
        public async void CanUploadAsync(long fileSizeBytes, long? uncompressedSizeBytes, long appId, Action<bool, string, CanUploadResponse> callback)
        {
            if (string.IsNullOrEmpty(authToken))
            {
                callback?.Invoke(false, "Not authenticated", null);
                return;
            }

            try
            {
                UnityEngine.Debug.Log("[GLC] === CanUpload ASYNC Started ===");
                
                var handler = new HttpClientHandler();
                if (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("localhost"))
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                }

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                    
                    string url = $"{baseUrl}/api/cli/build/can-upload?fileSizeBytes={fileSizeBytes}&appId={appId}";
                    if (uncompressedSizeBytes.HasValue)
                    {
                        url += $"&uncompressedSizeBytes={uncompressedSizeBytes.Value}";
                    }
                    
                    var response = await client.GetAsync(url);
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var settings = new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() };
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<CanUploadResponse>>(responseBody, settings);
                        
                        if (apiResponse.IsSuccess && apiResponse.Result != null)
                        {
                            UnityEngine.Debug.Log("[GLC] Upload check successful");
                            callback?.Invoke(true, "Upload check successful", apiResponse.Result);
                        }
                        else
                        {
                            string error = apiResponse.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0
                                ? apiResponse.ErrorMessages[0]
                                : "Upload check failed";
                            UnityEngine.Debug.LogWarning($"[GLC] Upload check failed: {error}");
                            callback?.Invoke(false, error, null);
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[GLC] Upload check request failed: {response.StatusCode}");
                        callback?.Invoke(false, $"Request failed: {response.StatusCode}", null);
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GLC] Error checking upload: {ex.Message}");
                callback?.Invoke(false, $"Connection error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Start upload process - get presigned URL for file upload (async method - no coroutines)
        /// </summary>
        public async void StartUploadAsync(long appId, string fileName, long fileSize, long? uncompressedFileSize, string buildNotes, Action<bool, string, StartUploadResponse> callback)
        {
            UnityEngine.Debug.LogWarning($"[GLC] ★★★★★ STARTUPLOAD ASYNC METHOD - NO COROUTINES ★★★★★");
            
            if (string.IsNullOrEmpty(authToken))
            {
                UnityEngine.Debug.LogError($"[GLC] Auth token is empty or null!");
                callback?.Invoke(false, "Not authenticated", null);
                return;
            }

            UnityEngine.Debug.Log($"[GLC] Starting upload request for {fileName} ({fileSize} bytes, uncompressed: {uncompressedFileSize})...");
            UnityEngine.Debug.Log($"[GLC] Auth token length: {authToken?.Length ?? 0} chars");
            UnityEngine.Debug.Log($"[GLC] Base URL: {baseUrl}");
            
            try
            {
                UnityEngine.Debug.Log($"[GLC] Creating HTTP client for {baseUrl}");
                var handler = new HttpClientHandler();
                if (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("localhost"))
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                    UnityEngine.Debug.Log("[GLC] SSL validation bypass enabled for localhost");
                }

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                    
                    var requestData = new StartUploadRequest
                    {
                        AppId = appId,
                        FileName = fileName,
                        FileSize = fileSize,
                        UncompressedFileSize = uncompressedFileSize,
                        BuildNotes = buildNotes
                    };
                    
                    var settings = new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() };
                    string jsonData = JsonConvert.SerializeObject(requestData, settings);
                    
                    string url = $"{baseUrl}/api/cli/build/start-upload";
                    UnityEngine.Debug.Log($"[GLC] Sending POST to {url}");
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    UnityEngine.Debug.Log($"[GLC] Response status: {response.StatusCode}");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    UnityEngine.Debug.Log($"[GLC] Response body length: {responseBody?.Length ?? 0} chars");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<StartUploadResponse>>(responseBody, settings);
                        
                        if (apiResponse.IsSuccess && apiResponse.Result != null)
                        {
                            UnityEngine.Debug.Log($"[GLC] Upload started successfully. Build ID: {apiResponse.Result.AppBuildId}");
                            callback?.Invoke(true, "Upload started successfully", apiResponse.Result);
                        }
                        else
                        {
                            string error = apiResponse.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0
                                ? apiResponse.ErrorMessages[0]
                                : "Failed to start upload";
                            UnityEngine.Debug.LogError($"[GLC] Upload start failed: {error}");
                            callback?.Invoke(false, error, null);
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[GLC] Request failed with status: {response.StatusCode}");
                        callback?.Invoke(false, $"Request failed: {response.StatusCode}", null);
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GLC] Error starting upload: {ex.Message}\nStack trace: {ex.StackTrace}");
                callback?.Invoke(false, $"Connection error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Upload file to presigned URL using HttpClient (async - no coroutines)
        /// </summary>
        public async void UploadFileAsync(string presignedUrl, byte[] fileData, Action<bool, string, float> progressCallback)
        {
            UnityEngine.Debug.LogWarning($"[GLC] === UploadFile ASYNC Started ===");
            UnityEngine.Debug.Log($"[GLC] Presigned URL: {presignedUrl?.Substring(0, Math.Min(100, presignedUrl?.Length ?? 0))}...");
            UnityEngine.Debug.Log($"[GLC] File data size: {fileData.Length} bytes ({fileData.Length / (1024f * 1024f):F2} MB)");
            
            // Report initial progress
            progressCallback?.Invoke(false, "Uploading...", 0f);
            
            try
            {
                UnityEngine.Debug.Log($"[GLC] Creating HttpClient for upload...");
                var handler = new HttpClientHandler();
                // No SSL bypass needed for R2/S3 presigned URLs
                
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromHours(1); // 1 hour timeout for large files
                    UnityEngine.Debug.Log($"[GLC] HttpClient created with 1 hour timeout");
                    
                    using (var content = new ByteArrayContent(fileData))
                    {
                        // Use application/octet-stream like the CLI does
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                        content.Headers.ContentLength = fileData.Length;
                        UnityEngine.Debug.Log($"[GLC] ByteArrayContent created with Content-Type: application/octet-stream, sending PUT request...");
                        
                        var response = await client.PutAsync(presignedUrl, content);
                        UnityEngine.Debug.Log($"[GLC] PUT request completed. Status: {response.StatusCode}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            UnityEngine.Debug.Log($"[GLC] Upload successful!");
                            progressCallback?.Invoke(true, "Upload completed", 1.0f);
                        }
                        else
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            string error = $"Upload failed: {response.StatusCode}";
                            UnityEngine.Debug.LogError($"[GLC] {error}");
                            UnityEngine.Debug.LogError($"[GLC] Response body: {responseBody}");
                            progressCallback?.Invoke(false, error, 0f);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GLC] Upload error: {ex.Message}\nStack trace: {ex.StackTrace}");
                progressCallback?.Invoke(false, $"Upload error: {ex.Message}", 0f);
            }
        }

        /// <summary>
        /// Notify backend that file is ready for processing (async - no coroutines)
        /// For multipart uploads, includes uploadId and parts list
        /// </summary>
        public async void NotifyFileReadyAsync(long appBuildId, string key, string uploadId, List<PartETag> parts, Action<bool, string> callback)
        {
            UnityEngine.Debug.LogWarning($"[GLC] === NotifyFileReady ASYNC Started ===");
            UnityEngine.Debug.Log($"[GLC] AppBuildId: {appBuildId}, Key: {key}");
            
            if (string.IsNullOrEmpty(authToken))
            {
                UnityEngine.Debug.LogError($"[GLC] Auth token is empty!");
                callback?.Invoke(false, "Not authenticated");
                return;
            }

            try
            {
                UnityEngine.Debug.Log($"[GLC] Creating HTTP client for file-ready notification...");
                var handler = new HttpClientHandler();
                if (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("localhost"))
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                    UnityEngine.Debug.Log($"[GLC] SSL bypass enabled");
                }

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                    
                    var requestData = new FileReadyRequest 
                    { 
                        AppBuildId = appBuildId, 
                        Key = key,
                        UploadId = uploadId,
                        Parts = parts
                    };
                    var settings = new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() };
                    string jsonData = JsonConvert.SerializeObject(requestData, settings);
                    
                    string url = $"{baseUrl}/api/cli/build/file-ready";
                    UnityEngine.Debug.Log($"[GLC] Sending POST to {url}");
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    UnityEngine.Debug.Log($"[GLC] Response status: {response.StatusCode}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        UnityEngine.Debug.Log($"[GLC] File ready notification sent successfully!");
                        callback?.Invoke(true, "File ready notification sent");
                    }
                    else
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        UnityEngine.Debug.LogError($"[GLC] File ready notification failed: {response.StatusCode}");
                        UnityEngine.Debug.LogError($"[GLC] Response body: {responseBody}");
                        callback?.Invoke(false, $"Request failed: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GLC] Error notifying file ready: {ex.Message}\nStack trace: {ex.StackTrace}");
                callback?.Invoke(false, $"Connection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get build status
        /// </summary>
        public async void GetBuildStatusAsync(long appBuildId, Action<bool, string, BuildStatusResponse> callback)
        {
            if (string.IsNullOrEmpty(authToken))
            {
                callback?.Invoke(false, "Not authenticated", null);
                return;
            }

            try
            {
                UnityEngine.Debug.Log($"[GLC] === GetBuildStatus ASYNC Started for Build #{appBuildId} ===");
                
                var handler = new HttpClientHandler();
                if (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("localhost"))
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                }

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                    
                    string url = $"{baseUrl}/api/cli/build/status/{appBuildId}";
                    var response = await client.GetAsync(url);
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var settings = new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() };
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<BuildStatusResponse>>(responseBody, settings);
                        
                        if (apiResponse.IsSuccess && apiResponse.Result != null)
                        {
                            UnityEngine.Debug.Log($"[GLC] Build status: {apiResponse.Result.Status}");
                            callback?.Invoke(true, "Status retrieved successfully", apiResponse.Result);
                        }
                        else
                        {
                            string error = apiResponse.ErrorMessages != null && apiResponse.ErrorMessages.Length > 0
                                ? apiResponse.ErrorMessages[0]
                                : "Failed to get build status";
                            UnityEngine.Debug.LogWarning($"[GLC] Get build status failed: {error}");
                            callback?.Invoke(false, error, null);
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[GLC] Get build status request failed: {response.StatusCode}");
                        callback?.Invoke(false, $"Request failed: {response.StatusCode}", null);
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GLC] Error getting build status: {ex.Message}");
                callback?.Invoke(false, $"Connection error: {ex.Message}", null);
            }
        }

        #region API Data Models

        [Serializable]
        public class LoginInteractiveRequest
        {
            public string ApiKey { get; set; }
        }

        [Serializable]
        public class LoginResponse
        {
            public string Id { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string Token { get; set; }
            public string[] Roles { get; set; }
            public SubscriptionInfo Subscription { get; set; }
        }

        [Serializable]
        public class SubscriptionInfo
        {
            public PlanInfo Plan { get; set; }
        }

        [Serializable]
        public class PlanInfo
        {
            public string Name { get; set; }
        }

        [Serializable]
        public class ApiResponse<T>
        {
            public T Result { get; set; }
            public bool IsSuccess { get; set; }
            public string[] ErrorMessages { get; set; }
            public int StatusCode { get; set; }
        }

        [Serializable]
        public class AppListResponse
        {
            public AppInfo[] Apps { get; set; } = new AppInfo[0];
            public int TotalApps { get; set; }
            public string PlanName { get; set; } = "";
        }

        [Serializable]
        public class AppInfo
        {
            public long Id { get; set; }
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public int BuildCount { get; set; }
            public bool IsOwnedByUser { get; set; }
        }

        [Serializable]
        public class CanUploadResponse
        {
            public bool CanUpload { get; set; }
            public long FileSizeBytes { get; set; }
            public long UncompressedSizeBytes { get; set; }
            public string PlanName { get; set; } = "";
            public int MaxCompressedSizeGB { get; set; }
            public int MaxUncompressedSizeGB { get; set; }
        }

        [Serializable]
        public class StartUploadRequest
        {
            public long AppId { get; set; }
            public string FileName { get; set; } = "";
            public long FileSize { get; set; }
            public long? UncompressedFileSize { get; set; }
            public string BuildNotes { get; set; } = "";
        }

        [Serializable]
        public class StartUploadResponse
        {
            public long AppBuildId { get; set; }
            public string UploadUrl { get; set; } = "";
            public string Key { get; set; } = "";
            public string FinalUrl { get; set; } = "";
            public List<PresignedPartUrl> PartUrls { get; set; }
            public string UploadId { get; set; }
            public long? PartSize { get; set; }
            public int? TotalParts { get; set; }
        }

        [Serializable]
        public class PresignedPartUrl
        {
            public int PartNumber { get; set; }
            public string UploadUrl { get; set; } = "";
            public long StartByte { get; set; }
            public long EndByte { get; set; }
        }

        [Serializable]
        public class PartETag
        {
            public int PartNumber { get; set; }
            public string ETag { get; set; } = "";
        }

        [Serializable]
        public class FileReadyRequest
        {
            public long AppBuildId { get; set; }
            public string Key { get; set; } = "";
            public string UploadId { get; set; }
            public List<PartETag> Parts { get; set; }
        }

        [Serializable]
        public class BuildStatusResponse
        {
            public long AppBuildId { get; set; }
            public long AppId { get; set; }
            public string Status { get; set; } = "";
            public string FileName { get; set; } = "";
            public string BuildNotes { get; set; } = "";
            public string ErrorMessage { get; set; } = "";
            public long FileSize { get; set; }
            public long CompressedFileSize { get; set; }
            public int StageProgress { get; set; }
        }

        #endregion
    }
}
