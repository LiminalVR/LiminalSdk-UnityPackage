using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Liminal.Firebase
{
    /// <summary>
    /// Inherit firestore controller 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FirestoreService : MonoBehaviour
    {
        [Header("Firebase (REST Anon Auth)")] 
        [SerializeField] private string _webApiKey = "";

        [Header("Firestore")] [SerializeField] 
        private string _projectId = "";

        [SerializeField] 
        private string _collection = "";

        private string _idToken;
        private string _refreshToken;
        private DateTime _idTokenExpiryUtc = DateTime.MinValue;

        public async Task Authenticate(CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_webApiKey))
            {
                throw new Exception($"WebApiKey can't be empty or null");
            }

            if (string.IsNullOrEmpty(_idToken) || string.IsNullOrEmpty(_refreshToken))
            {
                var res = await FirebaseAuthenticationService.SignInAnonymouslyAsync(_webApiKey, ct);
                _idToken = res.idToken;
                _refreshToken = res.refreshToken;
                _idTokenExpiryUtc = DateTime.UtcNow.AddSeconds(res.expiresInSec);
                return;
            }

            const int refreshSkewSeconds = 120;
            if (DateTime.UtcNow.AddSeconds(refreshSkewSeconds) >= _idTokenExpiryUtc)
            {
                var res = await FirebaseAuthenticationService.RefreshIdTokenAsync(_webApiKey, _refreshToken, ct);
                _idToken = res.idToken;
                _refreshToken = res.refreshToken;
                _idTokenExpiryUtc = DateTime.UtcNow.AddSeconds(res.expiresInSec);
            }
        }


        public async Task<string> AddSessionAsync(string jsonBody, string documentId = null,
            CancellationToken ct = default)
        {
            await Authenticate(ct);

            var baseUrl = $"{GetBaseURL()}/{_collection}";
            var url = string.IsNullOrEmpty(documentId)
                ? baseUrl
                : $"{baseUrl}?documentId={UnityWebRequest.EscapeURL(documentId)}";

            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                AttachJson(req, jsonBody);
                AttachBearer(req, _idToken);
                await SendAsync(req, ct);
                EnsureSuccess(req, "AddSession");
                return req.downloadHandler.text;
            }
        }

        public async Task<string> GetCollectionAsync(CancellationToken ct = default)
        {
            await Authenticate(ct);
            Debug.Log("Authenticated");

            var url = $"{GetBaseURL()}:runQuery";
            var queryBody = new
            {
                structuredQuery = new
                {
                    from = new[] { new { collectionId = _collection } }
                }
            };
            string jsonBody = JsonConvert.SerializeObject(queryBody);

            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                AttachJson(req, jsonBody);
                AttachBearer(req, _idToken);
                await SendAsync(req, ct);
                EnsureSuccess(req, "QueryFirestore");
                return req.downloadHandler.text;
            }
        }

        public async Task<string> GetDeviceDataAsync(string deviceID = "deviceID", CancellationToken ct = default)
        {
            await Authenticate(ct);

            var url = $"{GetBaseURL()}:runQuery";
            var queryBody = new
            {
                structuredQuery = new
                {
                    from = new[] { new { collectionId = _collection } },
                    where = new 
                    {
                        fieldFilter = new
                        {
                            field = new { fieldPath = deviceID },
                            op = "EQUAL",
                            value = new { stringValue = SystemInfo.deviceUniqueIdentifier }
                        }
                    }
                }
            };
            string jsonBody = JsonConvert.SerializeObject(queryBody);

            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                AttachJson(req, jsonBody);
                AttachBearer(req, _idToken);
                await SendAsync(req, ct);
                EnsureSuccess(req, "QueryFirestore");
                return req.downloadHandler.text;
            }
        }

        private string GetBaseURL()
        {
            if (string.IsNullOrEmpty(_projectId))
            {
                throw new Exception($"ProjectId can't be empty or null");
            }

            if (string.IsNullOrEmpty(_collection))
            {
                throw new Exception($"Collection can't be empty or null");
            }

            string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents";

            return url;
        }

        private static void AttachJson(UnityWebRequest req, string json)
        {
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
        }

        private static void AttachBearer(UnityWebRequest req, string idToken)
        {
            req.SetRequestHeader("Authorization", "Bearer " + idToken);
        }

        private static async Task SendAsync(UnityWebRequest request, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<object>();
            var op = request.SendWebRequest();
            using (ct.Register(() =>
                   {
                       try
                       {
                           request.Abort();
                       }
                       catch
                       {
                           // Ignored
                       }

                       tcs.TrySetCanceled(ct);
                   }))
            {
                op.completed += _ => tcs.TrySetResult(null);
                await tcs.Task;
            }
        }

        private static void EnsureSuccess(UnityWebRequest req, string label)
        {
#if UNITY_2020_1_OR_NEWER
        bool hasError = req.result == UnityWebRequest.Result.ConnectionError
                        || req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool hasError = req.isNetworkError || req.isHttpError;
#endif
            if (hasError)
            {
                var msg = $"{label} HTTP {(int)req.responseCode} {req.error}\nBody: {req.downloadHandler.text}";
                throw new InvalidOperationException(msg);
            }
        }
    }
}