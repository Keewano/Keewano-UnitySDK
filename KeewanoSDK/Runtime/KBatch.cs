using System;
using System.Collections.Generic;
using System.IO;

namespace Keewano.Internal
{
    struct CutPoint
    {
        public int Pos;
        public uint LastEventTime;
    }

    class KBatch
    {
        public Guid DataSessionId;
        public Guid InstallId;
        public Guid UserId;

        public int BatchNum;
        public NonClosableStream Data;

        public uint CustomEventsVersion;
        public uint BatchStartTime;
        public uint BatchEndTime;
        public uint BatchVersion;

        public BinaryWriter Writer;

        public List<CutPoint> CutPositions;

        public KBatch(Guid installId, Guid userId, Guid dataSessionId)
        {
            InstallId = installId;
            UserId = userId;
            DataSessionId = dataSessionId;
            BatchVersion = KSerializer.CURRENT_BATCH_VERSION;

            Data = new NonClosableStream(new MemoryStream(), false);
            Writer = new BinaryWriter(Data, System.Text.Encoding.UTF8);
            CutPositions = new();
        }
    }
}
