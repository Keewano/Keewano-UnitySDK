using System;
using System.Collections.Generic;
using System.IO;


namespace Keewano.Internal
{
    /*
    There is an issue with HttpClient, which causes StreamContent to close our batch data MemoryStream after sending.
    However, we want to reuse the same MemoryStream repeatedly for the next batch.
    In newer .NET versions, the issue was resolved by adding the leaveOpen flag to the StreamContent constructor,
    but for now, we have to use this workaround.
    */
#pragma warning disable S1104
    public class NonClosableStream : Stream
    {
        private readonly MemoryStream m_innerStream;
        public bool LeaveOpen;

        public NonClosableStream(MemoryStream innerStream, bool leaveOpen)
        {
            m_innerStream = innerStream;
            LeaveOpen = leaveOpen;
        }

        public override bool CanRead => m_innerStream.CanRead;
        public override bool CanSeek => m_innerStream.CanSeek;
        public override bool CanWrite => m_innerStream.CanWrite;
        public override long Length => m_innerStream.Length;

        public override long Position
        {
            get => m_innerStream.Position;
            set => m_innerStream.Position = value;
        }

        public byte[] GetBuffer() => m_innerStream.GetBuffer();

        public override void Flush() => m_innerStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            m_innerStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) =>
            m_innerStream.Seek(offset, origin);

        public override void SetLength(long value) =>
            m_innerStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) =>
            m_innerStream.Write(buffer, offset, count);

        public override void Close()
        {
            if (!LeaveOpen)
                m_innerStream.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !LeaveOpen)
                m_innerStream.Dispose();

            base.Dispose(disposing);
        }
    }
}
