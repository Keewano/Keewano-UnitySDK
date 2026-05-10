
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace Keewano.Internal
{
#pragma warning disable S1118

    internal class KNetwork
    {
        private const string SDK_VERSION = "Unity/1.0.86";

        static readonly HttpClient m_client;
        static readonly MediaTypeHeaderValue m_contentTypeHeader;

        static KNetwork()
        {
            m_client = new HttpClient();
#if KEEWANO_TEST_ENDPOINT
            //Used for internal testing
            m_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("KEEWANO_AUTH_BEARER"));
#endif
            m_contentTypeHeader = new MediaTypeHeaderValue("application/octet-stream");
        }

#if UNITY_EDITOR
        static void logActionableError(Uri endpoint, HttpStatusCode? statusCode)
        {
            if (endpoint.Scheme != "https")
                UnityEngine.Debug.LogError($"[Keewano] Invalid endpoint '{endpoint}'. Please make sure the KEEWANO_TEST_ENDPOINT environment variable is set correctly.");
            else if (statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.Unauthorized)
                UnityEngine.Debug.LogError("[Keewano] Access denied. Please verify that the correct API key is configured in Edit > Project Settings > Keewano.");
        }
#endif

        public static bool SendBatch(Uri endpoint, string appSecret, KBatch batch, string testUser, CancellationToken ct)
        {
            try
            {
                batch.Data.Position = 0;
                batch.Data.LeaveOpen = true;

                StreamContent ctx = new StreamContent(batch.Data);
                ctx.Headers.ContentType = m_contentTypeHeader;
                ctx.Headers.Add("K-InstallId", batch.InstallId.ToString());
                ctx.Headers.Add("K-Uid", batch.UserId.ToString());
                ctx.Headers.Add("K-DS", batch.DataSessionId.ToString());
                ctx.Headers.Add("K-Batch", batch.BatchNum.ToString());
                ctx.Headers.Add("K-BatchStartTime", batch.BatchStartTime.ToString());
                ctx.Headers.Add("K-BatchEndTime", batch.BatchEndTime.ToString());
                ctx.Headers.Add("K-BatchVersion", batch.BatchVersion.ToString());
                ctx.Headers.Add("K-CustomEventHash", batch.CustomEventsVersion.ToString());

                if (testUser != null)
                    ctx.Headers.Add("K-Tester", testUser);

                ctx.Headers.Add("K-Token", appSecret);
                ctx.Headers.Add("K-SDK", SDK_VERSION);

                HttpRequestMessage req = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = endpoint,
                    Content = ctx
                };

                HttpResponseMessage reply = m_client.SendAsync(req, ct).Result;
                batch.Data.LeaveOpen = false;
#if UNITY_EDITOR
                if (!reply.IsSuccessStatusCode)
                    logActionableError(endpoint, reply.StatusCode);
#endif
                return (reply.IsSuccessStatusCode);
            }
            catch
            {
                batch.Data.LeaveOpen = false;
#if UNITY_EDITOR
                logActionableError(endpoint, null);
#endif
                return false;
            }
        }

        public static bool GetCustomEventIds(Uri endpoint, string appSecret, uint ceVersion, out bool needToRegister, CancellationToken ct)
        {
            needToRegister = false;

            try
            {
                HttpRequestMessage req = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = endpoint
                };

                req.Headers.Add("K-Token", appSecret);
                req.Headers.Add("K-CustomEventHash", ceVersion.ToString());
                req.Headers.Add("K-SDK", SDK_VERSION);

                HttpResponseMessage reply = m_client.SendAsync(req, ct).Result;
                switch (reply.StatusCode)
                {
                    case HttpStatusCode.OK:
                        return true;
                    case HttpStatusCode.NoContent:
                        needToRegister = true;
                        return false;
                    default:
#if UNITY_EDITOR
                        logActionableError(endpoint, reply.StatusCode);
#endif
                        return false;
                }
            }
            catch
            {
#if UNITY_EDITOR
                logActionableError(endpoint, null);
#endif
                return false;
            }
        }

        public static bool RegisterCustomEvents(Uri endpoint, string appSecret, CustomEventSet ceSet, CancellationToken ct)
        {
            try
            {
                HttpRequestMessage req = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = endpoint,
                    Content = new ByteArrayContent(ceSet.GzipData)
                };

                req.Headers.Add("K-Token", appSecret);
                req.Headers.Add("K-CustomEventHash", ceSet.Version.ToString());
                req.Headers.Add("K-CustomEventCount", ceSet.EventCount.ToString());
                req.Headers.Add("K-SDK", SDK_VERSION);

                req.Content.Headers.ContentEncoding.Add("gzip");
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                HttpResponseMessage reply = m_client.SendAsync(req, ct).Result;
#if UNITY_EDITOR
                if (reply.StatusCode != HttpStatusCode.OK && reply.StatusCode != HttpStatusCode.Created)
                    logActionableError(endpoint, reply.StatusCode);
#endif
                return reply.StatusCode == HttpStatusCode.OK || reply.StatusCode == HttpStatusCode.Created;
            }
            catch
            {
#if UNITY_EDITOR
                logActionableError(endpoint, null);
#endif
                return false;
            }
        }
    }
}

