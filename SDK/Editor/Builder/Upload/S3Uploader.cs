using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Liminal.SDK.Build
{
    /// <summary>
    /// Identifies the S3 endpoint and the credentials used to authorize an upload.
    /// </summary>
    public struct S3Config
    {
        /// <summary>AWS access key id (the "AKIA..." part).</summary>
        public string AccessKeyId;

        /// <summary>AWS secret access key. Used only to derive the signing key; never sent over the wire.</summary>
        public string SecretAccessKey;

        /// <summary>Region the bucket lives in, e.g. <c>ap-southeast-2</c> (Sydney).</summary>
        public string Region;

        /// <summary>Target bucket, e.g. <c>liminal-resources</c>.</summary>
        public string Bucket;
    }

    /// <summary>
    /// Outcome of a single PutObject call.
    /// </summary>
    public struct S3UploadResult
    {
        public bool Success;
        public HttpStatusCode StatusCode;

        /// <summary>The virtual-hosted-style URL the object was written to.</summary>
        public string ObjectUrl;

        /// <summary>S3 error XML (or the exception message) when <see cref="Success"/> is false.</summary>
        public string Error;
    }

    /// <summary>
    /// Minimal, dependency-free S3 uploader. Performs a virtual-hosted-style PutObject by hand-rolling
    /// AWS Signature V4 signing and issuing a synchronous <see cref="HttpWebRequest"/>, so nothing needs
    /// to be imported into the project (no AWS SDK / no extra DLLs).
    /// </summary>
    public static class S3Uploader
    {
        private const string Service = "s3";
        private const string Aws4Request = "aws4_request";
        private const string Algorithm = "AWS4-HMAC-SHA256";

        /// <summary>
        /// Uploads <paramref name="filePath"/> to <c>s3://&lt;bucket&gt;/&lt;objectKey&gt;</c>. The file is
        /// streamed from disk — its SHA-256 is computed up front for the signed content hash and the body
        /// is sent in chunks — so large builds are never buffered fully in memory.
        /// </summary>
        /// <param name="cannedAcl">
        /// Optional S3 canned ACL (e.g. <c>public-read</c>) sent and signed as <c>x-amz-acl</c>. Leave null
        /// to inherit the bucket default. NOTE: fails with <c>AccessControlListNotSupported</c> on buckets
        /// whose Object Ownership is "Bucket owner enforced" (ACLs disabled) — grant public read via a bucket
        /// policy there instead.
        /// </param>
        /// <param name="onProgress">Optional (bytesSent, totalBytes) callback raised as the body uploads.</param>
        public static S3UploadResult PutObjectFromFile(
            S3Config config,
            string objectKey,
            string filePath,
            string contentType = "application/zip",
            string cannedAcl = null,
            Action<long, long> onProgress = null)
        {
            ValidateConfig(config);
            if (string.IsNullOrEmpty(objectKey))
                throw new ArgumentException("Object key is required.", nameof(objectKey));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File to upload was not found.", filePath);

            objectKey = objectKey.TrimStart('/');

            var host = $"{config.Bucket}.s3.{config.Region}.amazonaws.com";
            // Canonical URI: each key segment URI-encoded, slashes preserved.
            var segments = Array.ConvertAll(objectKey.Split('/'), s => AwsUriEncode(s, encodeSlash: false));
            var canonicalUri = "/" + string.Join("/", segments);
            var url = $"https://{host}{canonicalUri}";

            var now = DateTime.UtcNow;
            var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
            var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            var payloadHash = HashFileHex(filePath);
            var includeAcl = !string.IsNullOrEmpty(cannedAcl);

            // --- Canonical request (headers must be sorted by lowercased name) ---
            // x-amz-acl sorts after host and before x-amz-content-sha256.
            var canonicalHeaders =
                $"content-type:{contentType}\n" +
                $"host:{host}\n" +
                (includeAcl ? $"x-amz-acl:{cannedAcl}\n" : string.Empty) +
                $"x-amz-content-sha256:{payloadHash}\n" +
                $"x-amz-date:{amzDate}\n";

            var signedHeaders = includeAcl
                ? "content-type;host;x-amz-acl;x-amz-content-sha256;x-amz-date"
                : "content-type;host;x-amz-content-sha256;x-amz-date";

            var canonicalRequest = string.Join("\n",
                "PUT",
                canonicalUri,
                string.Empty,           // empty canonical query string
                canonicalHeaders,       // trailing \n already included above
                signedHeaders,
                payloadHash);

            // --- String to sign ---
            var credentialScope = $"{dateStamp}/{config.Region}/{Service}/{Aws4Request}";
            var stringToSign = string.Join("\n",
                Algorithm,
                amzDate,
                credentialScope,
                ToHex(Sha256(Encoding.UTF8.GetBytes(canonicalRequest))));

            // --- Signature ---
            var signingKey = GetSignatureKey(config.SecretAccessKey, dateStamp, config.Region, Service);
            var signature = ToHex(HmacSha256(signingKey, stringToSign));

            var authorization =
                $"{Algorithm} Credential={config.AccessKeyId}/{credentialScope}, " +
                $"SignedHeaders={signedHeaders}, Signature={signature}";

            return Send(url, contentType, amzDate, payloadHash, authorization, cannedAcl, filePath, onProgress);
        }

        private static S3UploadResult Send(
            string url,
            string contentType,
            string amzDate,
            string payloadHash,
            string authorization,
            string cannedAcl,
            string filePath,
            Action<long, long> onProgress)
        {
            try
            {
                // AWS endpoints require TLS 1.2; Expect-100-continue adds latency and trips some proxies.
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = false;

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "PUT";
                request.ContentType = contentType;                          // signed as content-type
                request.Headers["x-amz-content-sha256"] = payloadHash;      // signed
                request.Headers["x-amz-date"] = amzDate;                    // signed
                if (!string.IsNullOrEmpty(cannedAcl))
                    request.Headers["x-amz-acl"] = cannedAcl;               // signed
                request.Headers["Authorization"] = authorization;
                request.AllowWriteStreamBuffering = false;

                var total = new FileInfo(filePath).Length;
                request.ContentLength = total;

                using (var requestStream = request.GetRequestStream())
                using (var fileStream = File.OpenRead(filePath))
                {
                    var buffer = new byte[81920];
                    long sent = 0;
                    int read;
                    onProgress?.Invoke(0, total);
                    while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        requestStream.Write(buffer, 0, read);
                        sent += read;
                        onProgress?.Invoke(sent, total);
                    }
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return new S3UploadResult
                    {
                        Success = (int)response.StatusCode < 300,
                        StatusCode = response.StatusCode,
                        ObjectUrl = url,
                    };
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                return new S3UploadResult
                {
                    Success = false,
                    StatusCode = response?.StatusCode ?? 0,
                    ObjectUrl = url,
                    Error = ReadError(ex, response),
                };
            }
        }

        private static string ReadError(WebException ex, HttpWebResponse response)
        {
            if (response == null)
                return ex.Message;

            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
                return $"{(int)response.StatusCode} {response.StatusCode}: {reader.ReadToEnd()}";
        }

        private static void ValidateConfig(S3Config config)
        {
            if (string.IsNullOrEmpty(config.AccessKeyId))
                throw new ArgumentException("Access Key Id is required.");
            if (string.IsNullOrEmpty(config.SecretAccessKey))
                throw new ArgumentException("Secret Access Key is required.");
            if (string.IsNullOrEmpty(config.Region))
                throw new ArgumentException("Region is required.");
            if (string.IsNullOrEmpty(config.Bucket))
                throw new ArgumentException("Bucket is required.");
        }

        #region SigV4 primitives

        private static byte[] Sha256(byte[] data)
        {
            using (var sha = SHA256.Create())
                return sha.ComputeHash(data);
        }

        private static string HashFileHex(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return ToHex(sha.ComputeHash(stream));
        }

        private static byte[] HmacSha256(byte[] key, string data)
        {
            using (var hmac = new HMACSHA256(key))
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static byte[] GetSignatureKey(string secret, string dateStamp, string region, string service)
        {
            var kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secret), dateStamp);
            var kRegion = HmacSha256(kDate, region);
            var kService = HmacSha256(kRegion, service);
            return HmacSha256(kService, Aws4Request);
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        /// <summary>RFC 3986 percent-encoding as required by SigV4 (unreserved set kept verbatim).</summary>
        private static string AwsUriEncode(string value, bool encodeSlash)
        {
            var sb = new StringBuilder();
            foreach (var by in Encoding.UTF8.GetBytes(value))
            {
                var c = (char)by;
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
                    c == '_' || c == '-' || c == '~' || c == '.')
                    sb.Append(c);
                else if (c == '/')
                    sb.Append(encodeSlash ? "%2F" : "/");
                else
                    sb.Append('%').Append(((int)by).ToString("X2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        #endregion
    }
}
