using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Liminal.Firebase
{
    public static class FirebaseAuthenticationService
    {
        [Serializable]
        private class SignUpReq
        {
            public bool returnSecureToken = true;
        }

        [Serializable]
        private class SignUpRes
        {
            public string idToken;
            public string refreshToken;
            public string localId;
            public string expiresIn; // seconds as string
        }

        [Serializable]
        private class RefreshRes
        {
            public string id_token; // new ID token
            public string refresh_token; // possibly rotated
            public string expires_in; // seconds as string
            public string user_id;
            public string project_id;
            public string token_type; // "Bearer"
        }

        public static async Task<(string idToken, string refreshToken, int expiresInSec)>
            SignInAnonymouslyAsync(string apiKey, CancellationToken ct = default)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";
            var body = JsonConvert.SerializeObject(new SignUpReq());

            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                await SendAsync(req, ct);

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                    throw new Exception("Anon sign-in failed: " + req.error + "\n" + req.downloadHandler.text);

                SignUpRes res = JsonConvert.DeserializeObject<SignUpRes>(req.downloadHandler.text);
                int.TryParse(res.expiresIn, out var exp);
                return (res.idToken, res.refreshToken, exp);
            }
        }

        public static async Task<(string idToken, string refreshToken, int expiresInSec)>
            RefreshIdTokenAsync(string apiKey, string refreshToken, CancellationToken ct = default)
        {
            string url = $"https://securetoken.googleapis.com/v1/token?key={apiKey}";
            var form = new WWWForm();
            form.AddField("grant_type", "refresh_token");
            form.AddField("refresh_token", refreshToken);

            using (var req = UnityWebRequest.Post(url, form))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                await SendAsync(req, ct);

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                    throw new Exception("Token refresh failed: " + req.error + "\n" + req.downloadHandler.text);

                RefreshRes res = JsonConvert.DeserializeObject<RefreshRes>(req.downloadHandler.text);
                int.TryParse(res.expires_in, out var exp);
                return (res.id_token, res.refresh_token, exp);
            }
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
                       }

                       tcs.TrySetCanceled(ct);
                   }))
            {
                op.completed += _ => tcs.TrySetResult(null);
                await tcs.Task;
            }
        }
    }
}