using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.IO;
using System.Collections.Generic;

namespace Keewano.Internal
{
#pragma warning disable IDE0180 //Tuple swap takes more cpu instructions we don't want to waste them
#pragma warning disable IDE1006 //We use other convention for function names, internalFunc vs privateFunc 
#pragma warning disable IDE0079
#pragma warning disable S3251, IDE0079 //Partial class implementation warnings

    struct CustomEventSet
    {
        public uint Version;
        public byte[] GzipData;
        public ushort EventCount;
    }

    struct PendingBatchInfo
    {
        public uint BatchEndTime;
        public int BatchNum;
        public uint Size;
    }

    partial class KEventDispatcher
    {
        readonly object m_swapLock = new object();
        readonly AutoResetEvent m_readyToSendEvent = new AutoResetEvent(false);
        readonly Thread m_sendThread;
        readonly CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();
        readonly DateTime m_utcEpoch = DateTime.UnixEpoch;

        KBatch m_inBatch;
        KBatch m_sendingBatch;

        readonly string m_appSecret;

        Guid m_userId;

        readonly string m_workFolder;
        Dictionary<string, uint> m_onboardingMilestones;

        string m_testUserName;
        string m_sendTestUserName;

        private Uri m_ingresEndPoint;
        private Uri m_ceRegPoint;

        internal KEventDispatcher(string workingDirectory, string endpoint, string appSecret, Guid installId, Guid userId, Guid dataSessionId)
        {
            m_ingresEndPoint = new Uri(endpoint + "/in");
            m_ceRegPoint = new Uri(endpoint + "/custom");

            m_appSecret = appSecret;
            m_userId = userId;
            m_workFolder = workingDirectory;
            m_testUserName = null;
            m_sendTestUserName = null;

            m_inBatch = new KBatch(installId, userId, dataSessionId);
            m_inBatch.BatchStartTime = (uint)(DateTime.UtcNow - m_utcEpoch).TotalSeconds;
            m_sendingBatch = new KBatch(installId, userId, dataSessionId);

            try
            {
                if (!Directory.Exists(m_workFolder))
                    Directory.CreateDirectory(m_workFolder);
            }
            catch
            {
                //Unable to create working directory :(
            }

            try
            {
                m_testUserName = File.ReadAllText(getTestUserFileName());
            }
            catch (Exception)
            {
                m_testUserName = null;
            }

            CustomEventSet ceSet = default;
            getCustomEventSet(ref ceSet);

            if (ceSet.Version != 0)
            {
                string filename = getCustomEventSetFilename(ceSet.Version);
                KSerializer.SaveToFile(filename, ceSet);
            }

            m_inBatch.CustomEventsVersion = ceSet.Version;
            m_sendingBatch.CustomEventsVersion = ceSet.Version;

            m_sendThread = new Thread(sendThreadFunc);
            m_sendThread.Start();
        }

        internal void Stop()
        {
            m_cancellationTokenSource.Cancel();
            m_readyToSendEvent.Set();
            m_sendThread.Join();
        }

        internal void SendNow()
        {
            m_readyToSendEvent.Set();
        }

        private void sendThreadFunc()
        {
            CustomEventsMap ceMap = new CustomEventsMap();
            int nextBatchNum = 0;

            CancellationToken ct = m_cancellationTokenSource.Token;
            List<PendingBatchInfo> pendingBatches = loadUnsentBatchesList(m_workFolder);

            const uint diskUsageLimit = 50 * 1024 * 1024; //50mb
            reduceStorageSize(pendingBatches, diskUsageLimit);

            while (!ct.IsCancellationRequested)
            {
                bool serverReachable = sendPendingBatches(pendingBatches, ref ceMap, ct);
                bool noWorkToDo = !serverReachable || pendingBatches.Count == 0;

                bool newDataReady = m_readyToSendEvent.WaitOne(noWorkToDo ? 30_000 : 0);

                if (newDataReady)
                {
                    swapBatches();

                    uint secondsSinceEpoch = (uint)(DateTime.UtcNow - m_utcEpoch).TotalSeconds;
                    m_sendingBatch.BatchEndTime = secondsSinceEpoch;

                    uint currentDiskUsage = reduceStorageSize(pendingBatches, diskUsageLimit);

                    if (currentDiskUsage < diskUsageLimit)
                    {
                        KBatch subBatch = new KBatch(m_sendingBatch.InstallId, m_sendingBatch.UserId, m_sendingBatch.DataSessionId)
                        {
                            BatchStartTime = m_sendingBatch.BatchStartTime,
                            BatchEndTime = m_sendingBatch.BatchEndTime,
                            CustomEventsVersion = m_sendingBatch.CustomEventsVersion,
                        };

                        int cutPosIdx = 0;
                        int lastReadPos = 0;
                        byte[] dataBuff = m_sendingBatch.Data.GetBuffer();

                        //Split to subbatches
                        while (cutPosIdx < m_sendingBatch.CutPositions.Count)
                        {
                            int cutPos = m_sendingBatch.CutPositions[cutPosIdx];
                            int dataSize = cutPos - lastReadPos;

                            subBatch.Data.SetLength(0);
                            subBatch.BatchNum = nextBatchNum++;
                            subBatch.Data.Write(dataBuff, lastReadPos, dataSize);

                            uint bytesWritten = KSerializer.SaveToFile(subBatch, getBatchFilename(subBatch));
                            pendingBatches.Add(new PendingBatchInfo { BatchEndTime = subBatch.BatchEndTime, BatchNum = subBatch.BatchNum, Size = bytesWritten });

                            lastReadPos = cutPos;

                            ++cutPosIdx;
                        }

                        //Save remaining batch
                        int remainingBytes = (int)m_sendingBatch.Data.Length - lastReadPos;
                        if (remainingBytes > 0)
                        {
                            subBatch.Data.SetLength(0);
                            subBatch.BatchNum = nextBatchNum++;
                            subBatch.Data.Write(dataBuff, lastReadPos, remainingBytes);

                            uint bytesWritten = KSerializer.SaveToFile(subBatch, getBatchFilename(subBatch));
                            pendingBatches.Add(new PendingBatchInfo { BatchEndTime = subBatch.BatchEndTime, BatchNum = subBatch.BatchNum, Size = bytesWritten });
                        }
                    }
                    else //We have no storage, let's set the 
                    {
                        KBatch reducedBatch = new KBatch(m_sendingBatch.InstallId, m_sendingBatch.UserId, m_sendingBatch.DataSessionId)
                        {
                            BatchStartTime = m_sendingBatch.BatchStartTime,
                            BatchEndTime = m_sendingBatch.BatchEndTime,
                            CustomEventsVersion = m_sendingBatch.CustomEventsVersion,
                            BatchNum = nextBatchNum++
                        };

                        reducedBatch.Writer.Write((ushort)KEvents.BATCH_DROPPED);
                        reducedBatch.Writer.Write((uint)KBatchDropReason.TOO_MANY_UNSENT_EVENTS);

                        uint bytesWritten = KSerializer.SaveToFile(reducedBatch, getBatchFilename(reducedBatch));
                        pendingBatches.Add(new PendingBatchInfo { BatchEndTime = reducedBatch.BatchEndTime, BatchNum = reducedBatch.BatchNum, Size = bytesWritten });
                    }
                }
            }
        }

        string getBatchFilename(KBatch batch)
        {
            return getBatchFilename(batch.BatchEndTime, batch.BatchNum);
        }

        string getBatchFilename(uint batchEndTime, int batchNum)
        {
            return string.Format("{0}/{1}_{2}.kwub", m_workFolder, batchEndTime, batchNum);
        }

        string getCustomEventSetFilename(uint version)
        {
            return string.Format("{0}/{1}.map.gz", m_workFolder, version);
        }

        string getTestUserFileName()
        {
            return string.Format("{0}/test_user.info", m_workFolder);
        }

        private void swapBatches()
        {
            lock (m_swapLock)
            {
                KBatch tmp = m_sendingBatch;
                m_sendingBatch = m_inBatch;
                m_inBatch = tmp;

                m_inBatch.BatchNum = 0; //We will assign next number later

                m_inBatch.UserId = m_userId;
                m_inBatch.Data.SetLength(0);
                m_inBatch.BatchStartTime = (uint)(DateTime.UtcNow - m_utcEpoch).TotalSeconds;
                m_inBatch.CutPositions.Clear();

                if (m_testUserName != null)
                {
                    m_sendTestUserName = m_testUserName;
                    m_testUserName = null;
                }

            }
        }

        static void deleteFile(string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    File.Delete(filename);
                }
                catch {/*Huh?*/}
            }
        }

        uint reduceStorageSize(List<PendingBatchInfo> unsentBatches, uint topLimit)
        {
            const long BATCH_DROP_TRESHOLD = 70;

            uint totalUnsentBytes = 0;
            for (int j = 0; j < unsentBatches.Count; ++j)
                totalUnsentBytes += unsentBatches[j].Size;


            if (totalUnsentBytes > topLimit)
            {
                KBatch reducedBatch = new KBatch(Guid.Empty, Guid.Empty, Guid.Empty);
                int i = 0;
                while (totalUnsentBytes > topLimit && i < unsentBatches.Count)
                {
                    PendingBatchInfo batchInfo = unsentBatches[i];

                    if (batchInfo.Size > BATCH_DROP_TRESHOLD)
                    {
                        string filename = getBatchFilename(batchInfo.BatchEndTime, batchInfo.BatchNum);
                        if (KSerializer.LoadFromFile(filename, reducedBatch, out uint _))
                        {
                            totalUnsentBytes -= batchInfo.Size;
                            reducedBatch.Data.SetLength(0);
                            reducedBatch.Writer.Write((ushort)KEvents.BATCH_DROPPED);
                            reducedBatch.Writer.Write((uint)KBatchDropReason.TOO_MANY_UNSENT_EVENTS);
                            batchInfo.Size = KSerializer.SaveToFile(reducedBatch, filename);
                            unsentBatches[i] = batchInfo;
                            totalUnsentBytes += batchInfo.Size;
                        }
                    }

                    i++;
                }
            }

            return totalUnsentBytes;
        }

        bool sendPendingBatches(List<PendingBatchInfo> unsentBatches, ref CustomEventsMap ceMap, CancellationToken ct)
        {
            int numSentBatches = 0;
            bool serverReachable = true;

            if (unsentBatches.Count > 0)
            {
                KBatch b = new KBatch(m_sendingBatch.InstallId, Guid.Empty, Guid.Empty);

                /*We are sending max 30 batches per cycle to allow swap with m_collectingBatch
                 * Otherwise Under extremely heavy load the app collecting batch will run out of system memory.
                 */

                int batchCountToSend = Math.Min(30, unsentBatches.Count);
                for (int i = 0; i < batchCountToSend; i++)
                {
                    string filename = getBatchFilename(unsentBatches[i].BatchEndTime, unsentBatches[i].BatchNum);
                    if (!KSerializer.LoadFromFile(filename, b, out uint _))
                    {
                        deleteFile(filename);
                        ++numSentBatches;
                    }
                    else
                    {
                        serverReachable &= sendBatch(b, ceMap, ct);

                        if (serverReachable)
                        {
                            deleteFile(filename);
                            ++numSentBatches;
                        }
                        else
                            break;
                    }
                }
            }

            unsentBatches.RemoveRange(0, numSentBatches);
            return serverReachable;
        }

        bool sendBatch(KBatch b, CustomEventsMap ceMap, CancellationToken ct)
        {
            if (b.CustomEventsVersion != ceMap.Version && b.CustomEventsVersion != 0)
            {
                bool hasMapping = KNetwork.GetCustomEventIds(m_ceRegPoint, m_appSecret,
                    b.CustomEventsVersion, out ushort[] ids, out bool needToRegister, ct);

                if (needToRegister)
                {

                    string ceFilename = getCustomEventSetFilename(b.CustomEventsVersion);
                    if (!KSerializer.LoadFromFile(ceFilename, out CustomEventSet ceSet))
                        return false;
                    hasMapping = KNetwork.RegisterCustomEvents(m_ceRegPoint, m_appSecret, ceSet, ct);
                }

                if (hasMapping)
                    ceMap.Version = b.CustomEventsVersion;
                else
                    return false;
            }

            return KNetwork.SendBatch(m_ingresEndPoint, m_appSecret, b, m_sendTestUserName, ct);
        }

        static List<PendingBatchInfo> loadUnsentBatchesList(string folder)
        {
            List<PendingBatchInfo> result = new List<PendingBatchInfo>(100);

            try
            {
                if (Directory.Exists(folder))
                {
                    IEnumerable<string> filenames = Directory.EnumerateFiles(folder, "*.kwub");
                    foreach (string filename in filenames)
                    {
                        string filename_no_ext = Path.GetFileNameWithoutExtension(filename);
                        uint fileSize = (uint)(new FileInfo(filename).Length);
                        string[] parts = filename_no_ext.Split('_');

                        if (parts.Length == 2 &&
                            uint.TryParse(parts[0], out uint timestamp) &&
                            int.TryParse(parts[1], out int num))
                        {
                            result.Add(new PendingBatchInfo { BatchEndTime = timestamp, BatchNum = num, Size = fileSize });
                        }
                    }

                    result.Sort((a, b) =>
                    {
                        int cmp = a.BatchEndTime.CompareTo(b.BatchEndTime);
                        return cmp != 0 ? cmp : a.BatchNum.CompareTo(b.BatchNum);
                    });
                }
            }
            catch
            {
                result.Clear();
            }

            return result;
        }

        internal void SetUserId(Guid userId)
        {
            m_userId = userId;
            lock (m_swapLock)
            {
                m_inBatch.UserId = userId;
                m_inBatch.Writer.Write((ushort)KEvents.USER_ID_ASSIGNED);

            }

        }

        internal void AssignToABTestGroup(string testName, char group)
        {
            lock (m_swapLock)
            {
                m_inBatch.Writer.Write((ushort)KEvents.AB_TEST_ASSIGNMENT);
                m_inBatch.Writer.Write(testName);
                m_inBatch.Writer.Write(group);
                sendIfNeeded();
            }
        }

        internal void ReportInAppPurchase(string productName, uint priceUsdCents)
        {
            lock (m_swapLock)
            {
                uint timestamp = (uint)(DateTime.UtcNow - m_utcEpoch).TotalSeconds;

                m_inBatch.Writer.Write((ushort)KEvents.PURCHASE_TIMESTAMP);
                m_inBatch.Writer.Write(timestamp);
                m_inBatch.Writer.Write((ushort)KEvents.PURCHASE_PRODUCT_ID);
                m_inBatch.Writer.Write(productName);
                m_inBatch.Writer.Write((ushort)KEvents.PURCHASE_PRODUCT_PRICE_USD_CENTS);
                m_inBatch.Writer.Write(priceUsdCents);
                sendIfNeeded();
            }
        }

        internal void ReportInstallCampaign(string campaignName)
        {
            addEvent((ushort)KEvents.INSTALL_CAMPAIGN, campaignName);
        }

        internal void ReportWindowOpen(string windowName)
        {
            addEvent((ushort)KEvents.WINDOW_OPEN, windowName);
        }

        internal void ReportUserCountry(string countryName)
        {
            addEvent((ushort)KEvents.COUNTRY, countryName);
        }

        internal void ReportWindowClose(string windowName)
        {
            addEvent((ushort)KEvents.WINDOW_CLOSE, windowName);
        }

        internal void ReportAppPause()
        {
            addEvent((ushort)KEvents.APP_PAUSE, DateTime.Now);
        }

        internal void ReportAppResume()
        {
            addEvent((ushort)KEvents.APP_RESUME, DateTime.Now);
        }

        internal void ReportInternetConnected()
        {
            addEvent((ushort)KEvents.INTERNET_CONNECTED);
        }

        internal void ReportInternetDisconnected()
        {
            addEvent((ushort)KEvents.INTERNET_DISCONNECTED);
        }

        internal void ReportButtonClick(string btnName)
        {
            addEvent((ushort)KEvents.BUTTON_CLICK, btnName);
        }

        internal void ReportOnboardingMilestone(string milestone)
        {
            string filename = string.Format("{0}/onboarding.counters", m_workFolder);
            if (m_onboardingMilestones == null)
                m_onboardingMilestones = loadOnboardingCounters(filename);

            string key = milestone;
            if (m_onboardingMilestones.TryGetValue(key, out uint occurences))
            {
                ++occurences;
                milestone = string.Format("{0} (#{1})", key, occurences);
            }
            else
                occurences = 1;

            m_onboardingMilestones[key] = occurences;
            saveOnboardingCounters(filename, m_onboardingMilestones);
            addEvent((ushort)KEvents.ONBOARDING_MILESTONE, milestone);
        }

        internal void LogError(string msg)
        {
            addEvent((ushort)KEvents.ERROR_MSG, msg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void addEvent(ushort eventType)
        {
            lock (m_swapLock)
            {
                m_inBatch.Writer.Write(eventType);
                sendIfNeeded();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void addEvent(ushort eventType, string str)
        {
            lock (m_swapLock)
            {
                m_inBatch.Writer.Write(eventType);
                m_inBatch.Writer.Write(str);
                sendIfNeeded();
            }
        }

        internal void addEvent(ushort eventType, uint value)
        {
            lock (m_swapLock)
            {
                m_inBatch.Writer.Write(eventType);
                m_inBatch.Writer.Write(value);
                sendIfNeeded();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void addEvent(ushort eventType, ushort x, ushort y)
        {
            lock (m_swapLock)
            {
                m_inBatch.Writer.Write(eventType);
                m_inBatch.Writer.Write(x);
                m_inBatch.Writer.Write(y);
                sendIfNeeded();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void addEvent(ushort eventType, DateTime timestamp)
        {
            DateTime utcDate = timestamp.ToUniversalTime();
            uint secondsSinceEpoch = (uint)(utcDate - m_utcEpoch).TotalSeconds;
            lock (m_swapLock)
            {
                m_inBatch.Writer.Write(eventType);
                m_inBatch.Writer.Write(secondsSinceEpoch);

                sendIfNeeded();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void addEvent(ushort eventType, int data)
        {
            lock (m_swapLock)
            {
                m_inBatch.Writer.Write(eventType);
                m_inBatch.Writer.Write(data);
                sendIfNeeded();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void addEvent(ushort eventType, bool flag)
        {
            lock (m_swapLock)
            {
                m_inBatch.Writer.Write(eventType);
                byte b = flag ? (byte)2 : (byte)1;
                m_inBatch.Writer.Write(b);
                sendIfNeeded();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void sendIfNeeded()
        {
            int currentBatchSize = (int)m_inBatch.Data.Length;

            if (currentBatchSize >= 1024)
            {
                const uint BATCH_CUTTING_TRESHOLD = 100 * 1024;

                int lastIdx = m_inBatch.CutPositions.Count - 1;
                int lastCutPos = lastIdx == -1 ? 0 : m_inBatch.CutPositions[lastIdx];

                if (currentBatchSize - lastCutPos >= BATCH_CUTTING_TRESHOLD)
                    m_inBatch.CutPositions.Add(currentBatchSize);

                m_readyToSendEvent.Set();
            }
        }

        private static void writeItems(BinaryWriter w, ReadOnlySpan<Item> items)
        {
            if (items == null)
            {
                w.Write(0);
                return;
            }

            w.Write(items.Length);
            for (int i = 0; i < items.Length; ++i)
            {
                w.Write(items[i].UniqItemName);
                w.Write(items[i].Count);
            }
        }

        internal void ReportItemExchange(string exchangePoint, ReadOnlySpan<Item> from, ReadOnlySpan<Item> to)
        {
            lock (m_swapLock)
            {
                m_inBatch.Writer.Write((ushort)KEvents.ITEMS_EXCHANGE);
                m_inBatch.Writer.Write(exchangePoint);
                writeItems(m_inBatch.Writer, from);
                writeItems(m_inBatch.Writer, to);
                sendIfNeeded();
            }
        }

        internal void ReportItemsReset(string location, ReadOnlySpan<Item> items)
        {
            lock (m_swapLock)
            {
                m_inBatch.Writer.Write((ushort)KEvents.ITEMS_RESET);
                m_inBatch.Writer.Write(location);
                writeItems(m_inBatch.Writer, items);
                sendIfNeeded();
            }
        }

        internal void SetTestUserName(string testerName)
        {
            m_testUserName = testerName;
            File.WriteAllText(getTestUserFileName(), m_testUserName);
        }

        internal void ReportLowMemory()
        {
            addEvent((ushort)KEvents.LOW_MEM_WARNING);
        }

        internal void ReportGameLanguage(string language)
        {
            addEvent((ushort)KEvents.GAME_LANG, language);
        }

        static Dictionary<string, uint> loadOnboardingCounters(string filePath)
        {
            Dictionary<string, uint> dictionary = new Dictionary<string, uint>();
            if (!File.Exists(filePath))
                return dictionary;

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader r = new BinaryReader(fs))
                {

                    int count = r.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        string key = r.ReadString();
                        uint value = r.ReadUInt32();
                        dictionary[key] = value;
                    }
                }
            }
            catch (Exception)
            {
                dictionary.Clear();
            }
            return dictionary;
        }

        static void saveOnboardingCounters(string filePath, Dictionary<string, uint> dictionary)
        {
            string tempFilePath = filePath + ".tmp";
            try
            {
                using (FileStream fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (BinaryWriter w = new BinaryWriter(fs))
                {
                    w.Write(dictionary.Count);
                    foreach (KeyValuePair<string, uint> pair in dictionary)
                    {
                        w.Write(pair.Key);
                        w.Write(pair.Value);
                    }
                }

                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.Move(tempFilePath, filePath);
            }
            catch (Exception)
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        partial void getCustomEventSet(ref CustomEventSet dst);
    }
}