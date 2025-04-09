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
        static readonly HttpClient m_client = new HttpClient();
        static readonly MediaTypeHeaderValue m_contentTypeHeader = new MediaTypeHeaderValue("application/octet-stream");

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
                ctx.Headers.Add("K-CustomEventHash", batch.CustomEventsVersion.ToString());

                if (testUser != null)
                    ctx.Headers.Add("K-Tester", testUser);

                ctx.Headers.Add("K-Token", appSecret);

                HttpRequestMessage req = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = endpoint,
                    Content = ctx
                };

                HttpResponseMessage reply = m_client.SendAsync(req, ct).Result;
                batch.Data.LeaveOpen = false;
                return (reply.IsSuccessStatusCode);
            }
            catch
            {
                batch.Data.LeaveOpen = false;
                return false;
            }
        }

        public static bool GetCustomEventIds(Uri endpoint, string appSecret, uint ceVersion, out ushort[] dstIds, out bool needToRegister, CancellationToken ct)
        {
            dstIds = null;
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

                HttpResponseMessage reply = m_client.SendAsync(req, ct).Result;
                switch (reply.StatusCode)
                {
                    case HttpStatusCode.OK:
                    {
                        byte[] data = reply.Content.ReadAsByteArrayAsync().Result;
                        if (data.Length % 2 == 0)
                        {
                            dstIds = new ushort[data.Length / 2];
                            Buffer.BlockCopy(data, 0, dstIds, 0, data.Length);
                            return true;
                        }

                        return false;
                    }
                    case HttpStatusCode.NoContent:
                        needToRegister = true;
                        return false;
                    default:
                        return false;
                }
            }
            catch
            {
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

                req.Content.Headers.ContentEncoding.Add("gzip");
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                HttpResponseMessage reply = m_client.SendAsync(req, ct).Result;
                return reply.StatusCode == HttpStatusCode.OK || reply.StatusCode == HttpStatusCode.Created;
            }
            catch
            {
                return false;
            }
        }
    }
}