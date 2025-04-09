using System;
using System.Collections.Generic;
using System.IO;

namespace Keewano.Internal
{
    struct CutPosition
    {
        public uint Position;
        public uint EventCount;
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

        public BinaryWriter Writer;

        public List<int> CutPositions;

        public KBatch(Guid installId, Guid userId, Guid dataSessionId)
        {
            InstallId = installId;
            UserId = userId;
            DataSessionId = dataSessionId;

            Data = new NonClosableStream(new MemoryStream(), false);
            Writer = new BinaryWriter(Data, System.Text.Encoding.UTF8);
            CutPositions = new();
        }
    }
}
