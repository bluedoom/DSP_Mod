using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace LZ4
{
    public class LZ4CompressionStream : Stream, IDisposable
    {
        public const int MB = 1024 * 1024;
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => totalWrite;

        // only use for game statistics
        public override long Position { get => BufferWriter.WriteSum; set => new NotImplementedException(); }

        readonly Stream outStream;

        long totalWrite = 0;
        bool useMultiThread;
        DoubleBuffer doubleBuffer;

        private byte[] outBuffer;

        IntPtr cctx;
        long lastError = 0;
        bool stopWorker = true;
        Thread compressThread;

        public bool HasError()
        {
            return lastError != 0;
        }

        public void HandleError(long errorCode)
        {
            if (errorCode < 0)
            {
                LZ4API.FreeCompressContext(cctx);
                cctx = IntPtr.Zero;
                lastError = errorCode;
                throw new Exception(errorCode.ToString());
            }
        }

        public struct CompressBuffer
        {
            public byte[] readBuffer;
            public byte[] writeBuffer;
            public byte[] outBuffer;
        }

        public static CompressBuffer CreateBuffer(int ExBufferSize = 4 * MB)
        {
            try
            {
                return new CompressBuffer
                {
                    outBuffer = new byte[LZ4API.CalCompressOutBufferSize(ExBufferSize) + 1],
                    readBuffer = new byte[ExBufferSize],
                    writeBuffer = new byte[ExBufferSize],
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return new CompressBuffer();
        }
       
        public BufferWriter BufferWriter => bfferWriter;
        BufferWriter bfferWriter;

        public LZ4CompressionStream(Stream outStream, CompressBuffer compressBuffer,bool useMultiThread)
        {
            this.outStream = outStream;
            InitBuffer(compressBuffer.readBuffer, compressBuffer.writeBuffer, compressBuffer.outBuffer);
            long writeSize = LZ4API.CompressBegin(out cctx, outBuffer, outBuffer.Length);
            HandleError(writeSize);
            outStream.Write(outBuffer, 0, (int)writeSize);
            this.useMultiThread = useMultiThread;
            if(useMultiThread)
            {
                stopWorker = false;
                compressThread = new Thread(() => CompressAsync());
                compressThread.Start();
            }

        }

        void InitBuffer(byte[] readBuffer, byte[] writeBuffer, byte[] outBuffer)
        {
            doubleBuffer = new DoubleBuffer(readBuffer ?? new byte[4 * MB], writeBuffer ?? new byte[4 * MB], Compress);
            this.outBuffer = outBuffer ?? new byte[LZ4API.CalCompressOutBufferSize(writeBuffer.Length)];
            bfferWriter = new BufferWriter(doubleBuffer);
        }

        public override void Flush()
        {
            doubleBuffer.SwapBuffer();
            if(useMultiThread)
            {
                doubleBuffer.WaitReadEnd();
            }
            lock (outBuffer)
            {
                outStream.Flush();
            }
        }

        void Compress()
        {
            if (!useMultiThread)
            {
                Compress_Internal();
            }
        }

        void Compress_Internal()
        {
            var consumeBuffer = doubleBuffer.ReadBegin();
            if (consumeBuffer.Length > 0)
            {
                lock (outBuffer)
                {
                    long writeSize = 0;
                    try
                    {
                        writeSize = LZ4API.CompressUpdateEx(cctx, outBuffer, 0, consumeBuffer.Buffer, 0, consumeBuffer.Length);
                        HandleError(writeSize);
                    }
                    finally
                    {
                        doubleBuffer.ReadEnd();
                    }
                    outStream.Write(outBuffer, 0, (int)writeSize);
                    totalWrite += writeSize;
                }
            }
            else
            {
                doubleBuffer.ReadEnd();
            }
        }

        void CompressAsync()
        {
            while(!stopWorker)
            {
                Compress_Internal();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
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
            BufferWriter.Write(buffer, offset, count);
            //var writeBuffer = doubleBuffer.writeBuffer;
            //int writeSize = writeBuffer.Write(buffer, offset, count);
            //while (count - writeSize > 0)
            //{
            //    SwapBuffer(ref writeBuffer);
            //    offset += writeSize;
            //    count -= writeSize;
            //    writeSize = writeBuffer.Write(buffer, offset, count);
            //}
            //inputSum += count;
        }

        protected void FreeContext()
        {
            LZ4API.FreeCompressContext(cctx);
            cctx = IntPtr.Zero;
        }

        bool closed = false;
        public override void Close()
        {
            if(!closed)
            {
                BufferWriter.Close();
                closed = true;
                //Console.WriteLine($"FLUSH");

                Flush();

                // try stop the worker
                stopWorker = true;
                doubleBuffer.SwapBuffer();

                long size = LZ4API.CompressEnd(cctx, outBuffer, outBuffer.Length);
                //Debug.Log($"End");
                outStream.Write(outBuffer, 0, (int)size);
                base.Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            FreeContext();
            base.Dispose(disposing);
        }

    }
}