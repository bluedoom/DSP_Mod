using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace LZ4
{
    public class ByteSpan
    {
        public byte[] Buffer { get; private set; }
        //public int Start;
        public int Length;
        public int Capacity { get => Buffer.Length; }
        public int IdleCapacity => Capacity - Length;

        public int Position { get; set; }
        public ByteSpan(byte[] buffer)
        {
            Buffer = buffer;
        }
        public void Clear()
        {
            Length = 0;
            Position = 0;
        }
        public int Write(byte[] src, int offset, int count)
        {
            int writeLen = Math.Min(Capacity - Length, count);
            Array.Copy(src, offset, Buffer, Length, writeLen);
            Length += writeLen;
            return writeLen;
        }

        public int Read(byte[] dst, int offset, int count)
        {
            count = Math.Min(Length - Position, count);
            Array.Copy(Buffer, Position, dst, offset, count);
            Position += count;
            return count;
        }

        public static implicit operator byte[](ByteSpan bs) => bs.Buffer;
    }

    public class DoubleBuffer
    {
        public const int MB = 1024 * 1024;

        public ByteSpan writeBuffer;
        public ByteSpan readBuffer;
        private ByteSpan midBuffer;

        Semaphore write = new Semaphore(1, 1);
        Semaphore read = new Semaphore(0, 1);

        public DoubleBuffer(byte[] readBuffer, byte[] writeBuffer)
        {
            this.midBuffer = new ByteSpan(readBuffer);
            this.writeBuffer = new ByteSpan(writeBuffer);
        }

        public ByteSpan ReadBegin()
        {
            read.WaitOne();
            return readBuffer;
        }

        public void ReadEnd()
        {
            readBuffer.Clear();
            midBuffer = readBuffer;
            readBuffer = null;
            write.Release();
        }

        public ByteSpan SwapBuffer()
        {
            var buffer = SwapBegin();
            SwapEnd();
            return buffer;
        }

        public void WaitReadEnd()
        {
            write.WaitOne();
            write.Release();
        }

        public ByteSpan SwapBegin()
        {
            write.WaitOne();
            readBuffer = writeBuffer;
            writeBuffer = midBuffer;
            midBuffer = null;
            return writeBuffer;
        }

        public void SwapEnd()
        {
            read.Release();
        }
    }
}