using System;
using System.IO;

#pragma warning disable S1118

namespace Keewano.Internal
{
    class KSerializer
    {
        const uint BATCH_FORMAT_VERSION = 1;
        const int BATCH_FOURCC = 0x57554242; //KWUB

        static void writeGuid(FileStream fs, Guid guid)
        {
            Span<byte> guidSpan = stackalloc byte[16];
            guid.TryWriteBytes(guidSpan);
            fs.Write(guidSpan);
        }

        static Guid readGuid(FileStream fs)
        {
            Span<byte> guidSpan = stackalloc byte[16];
            int bytesRead = fs.Read(guidSpan);
            return bytesRead == 16 ? new Guid(guidSpan)
                : throw new EndOfStreamException("Could not read a full GUID from the stream.");
        }

        public static void copyBytes(Stream src, Stream dst, long count)
        {
            Span<byte> buffer = stackalloc byte[8192];

            while (count > 0)
            {
                int size = (int)Math.Min(buffer.Length, count);

                int bytesRead = src.Read(buffer.Slice(0, size));
                if (bytesRead > 0)
                {
                    dst.Write(buffer.Slice(0, bytesRead));
                    count -= bytesRead;
                }
                else
                    throw new EndOfStreamException();
            }
        }

        public static uint SaveToFile(KBatch batch, string filename)
        {
            try
            {
                long bytesWritten = 0;
                if (File.Exists(filename))
                    File.Delete(filename);

                using (FileStream fs = File.Create(filename))
                using (BinaryWriter w = new BinaryWriter(fs))
                {
                    //FourCC
                    w.Write(BATCH_FOURCC);
                    w.Write(BATCH_FORMAT_VERSION);

                    writeGuid(fs, batch.UserId);
                    writeGuid(fs, batch.DataSessionId);
                    w.Write(batch.BatchNum);
                    w.Write(batch.BatchStartTime);
                    w.Write(batch.BatchEndTime);

                    w.Write((int)batch.Data.Length);
                    batch.Data.Position = 0;
                    batch.Data.CopyTo(fs);

                    w.Write(batch.CustomEventsVersion);

                    bytesWritten = fs.Position;
                }

                return (uint)bytesWritten;
            }
            catch
            {
                //There is nothing we can do if the write mechanism fails, just drop the batch
                return 0;
            }
        }

        public static bool LoadFromFile(string filename, KBatch dst, out uint bytesRead)
        {
            bytesRead = 0;

            try
            {
                using (FileStream fs = File.OpenRead(filename))
                using (BinaryReader r = new BinaryReader(fs))
                {
                    int fourcc = r.ReadInt32();
                    if (fourcc != BATCH_FOURCC)
                        return false;

                    uint format_version = r.ReadUInt32();
                    if (format_version != BATCH_FORMAT_VERSION)
                        return false;

                    dst.UserId = readGuid(fs);
                    dst.DataSessionId = readGuid(fs);
                    dst.BatchNum = r.ReadInt32();
                    dst.BatchStartTime = r.ReadUInt32();
                    dst.BatchEndTime = r.ReadUInt32();

                    int dataSize = r.ReadInt32();
                    dst.Data.SetLength(0);
                    copyBytes(fs, dst.Data, dataSize);

                    dst.CustomEventsVersion = r.ReadUInt32();

                    bytesRead = (uint)fs.Position;

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        static public void SaveToFile(string filename, CustomEventSet set)
        {
            try
            {
                using (FileStream f = new FileStream(filename, FileMode.Create))
                using (BinaryWriter w = new BinaryWriter(f))
                {
                    const byte FILE_FORMAT_VERSION = 1;
                    w.Write(FILE_FORMAT_VERSION);
                    w.Write(set.Version);
                    w.Write(set.EventCount);
                    w.Write(set.GzipData.Length);
                    w.Write(set.GzipData);
                }
            }
            catch { /* Nothing to do here, it will be written again later after restart again */ }
        }

        static public bool LoadFromFile(string filename, out CustomEventSet set)
        {
            set = default;

            if (File.Exists(filename))
            {
                try
                {
                    using (FileStream f = new FileStream(filename, FileMode.Open))
                    using (BinaryReader r = new BinaryReader(f))
                    {
                        byte fileFormatVersion = r.ReadByte();
                        if (fileFormatVersion == 1)
                        {
                            set.Version = r.ReadUInt32();
                            set.EventCount = r.ReadUInt16();
                            int len = r.ReadInt32();
                            set.GzipData = r.ReadBytes(len);
                            return true;
                        }
                    }
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}