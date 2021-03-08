using System; 
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LZ4
{
    class LZ4DecompressionStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => inStream.Length;

        public override long Position { get=>inStream.Position; set{ long a = value; } }

        public Stream inStream;

        IntPtr dctx = IntPtr.Zero;

        ByteSpan srcBuffer;
        ByteSpan dcmpBuffer;
        private bool decompressFinish = false;
        long startPos = 0;
        public LZ4DecompressionStream(Stream inStream,int extraBufferSize = 512*1024)
        {
            this.inStream = inStream;
            startPos = inStream.Position;
            srcBuffer = new ByteSpan(new byte[extraBufferSize]);
            int len = Fill();
            long expect = LZ4API.DecompressBegin(ref dctx, srcBuffer.Buffer, ref len, out var blockSize);
            srcBuffer.Position += len;
            if (expect < 0) throw new Exception(expect.ToString());
            dcmpBuffer = new ByteSpan(new byte[blockSize]);
        }

        public void ResetStream()
        {
            inStream.Seek(startPos, SeekOrigin.Begin);
            decompressFinish = false;
            srcBuffer.Clear();
            dcmpBuffer.Clear();
            LZ4API.ResetDecompresssCTX(dctx);
        }

        public int Fill()
        {
            int suplus = srcBuffer.Length - srcBuffer.Position;
            if (srcBuffer.Length> 0 && srcBuffer.Position >= suplus)
            {
                Array.Copy(srcBuffer, srcBuffer.Position, srcBuffer, 0, suplus);
                srcBuffer.Length -= srcBuffer.Position;
                srcBuffer.Position = 0;
            }
            if (srcBuffer.IdleCapacity > 0)
            {
                var readlen = inStream.Read(srcBuffer, srcBuffer.Length, srcBuffer.IdleCapacity);
                srcBuffer.Length += readlen;
            }
            return srcBuffer.Length - srcBuffer.Position;
        }

        public override void Flush()
        {
            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int readlen = 0;
            while (count > (readlen += dcmpBuffer.Read(buffer, offset + readlen, count - readlen)) && !decompressFinish)
            {
                var buffSize = Fill();
                if (buffSize <= 0) return readlen;

                var rt = LZ4API.DecompressUpdateEx(dctx, dcmpBuffer, 0, dcmpBuffer.Capacity, srcBuffer, srcBuffer.Position,buffSize, null);
                if (rt.expect < 0) throw new Exception(rt.expect.ToString());
                if (rt.expect == 0) decompressFinish = true;

                srcBuffer.Position += (int)rt.readLen;
                dcmpBuffer.Position = 0;
                dcmpBuffer.Length = (int)rt.writeLen;
            }
            return readlen;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
