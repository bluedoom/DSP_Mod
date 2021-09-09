using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security;


namespace LZ4
{
    public class BufferWriter : BinaryWriter
    {
        ByteSpan currentBuffer;
		private int position;
		private int suplusCapacity;
		byte[] buffer;

		private Encoding _encoding;

		private Encoder encoder;

		public delegate ByteSpan BufferFullCallback(ByteSpan filledBuffer);

        BufferFullCallback onBufferFull;

		public BufferWriter (BufferFullCallback callback, ByteSpan byteSpan)
			:this(callback, byteSpan,new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true))
		{ 
			
		}

		BufferWriter(BufferFullCallback callback,ByteSpan byteSpan ,UTF8Encoding encoding) :base(Stream.Null, encoding)
		{
			currentBuffer = byteSpan;
			onBufferFull = callback;
			RefreshStatus();
			_encoding = encoding;
			encoder = _encoding.GetEncoder();
		}

		void SwapBuffer()
		{
			currentBuffer.Position = 0;
			currentBuffer.Length = position;

			currentBuffer = onBufferFull(currentBuffer);
			RefreshStatus();
		}

		void RefreshStatus()
		{
			position = currentBuffer.Position;
			suplusCapacity = currentBuffer.Capacity - position;
			buffer = currentBuffer.Buffer;
		}


        void CheckCapacityAndSwap(int requiredCapacity)
        {
            if( suplusCapacity < requiredCapacity)
            {
				SwapBuffer();
            }

			suplusCapacity -= requiredCapacity;
        }

        public override void Write(byte value)
        {
			CheckCapacityAndSwap(1);
			buffer[position++] = value;
        }

		public override void Write(bool value) => Write((byte)(value ? 1 : 0));

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
				SwapBuffer();
            }
			base.Dispose(disposing);
        }

		public override void Close()
		{
			Dispose(disposing: true);
		}

		public override void Flush()
		{
			SwapBuffer();
		}

		public override long Seek(int offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void Write(sbyte value) => Write((byte)value);

		public override void Write(byte[] _buffer) => Write(_buffer, 0, _buffer.Length);


		public override void Write(byte[] _buffer, int index, int count)
		{
			if (_buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			int writed = 0;
			while (suplusCapacity <= count)
			{
				Array.Copy(_buffer,index + writed, buffer, position, suplusCapacity);
				writed += suplusCapacity;
				count -= suplusCapacity;
				position += suplusCapacity;
				SwapBuffer();
			}
			Array.Copy(_buffer,index + writed, buffer, position, count);
			position += count;
			suplusCapacity -= count;
		}

		public unsafe override void Write(char ch)
		{
			if (char.IsSurrogate(ch))
			{
				throw new ArgumentException("Arg_SurrogatesNotAllowedAsSingleChar");
			}

			if (suplusCapacity < 4)
				SwapBuffer();

			int writed = 0;
			fixed (byte* bytes = buffer)
			{
				writed = encoder.GetBytes(&ch, 1, bytes + position, suplusCapacity, flush: true);
			}
			position += writed;
			suplusCapacity -= writed;
		}

		//slow
		public override void Write(char[] chars)
		{
			if (chars == null)
			{
				throw new ArgumentNullException("chars");
			}
			byte[] bytes = _encoding.GetBytes(chars, 0, chars.Length);
			Write(bytes);
		}
		
		public unsafe override void Write(double value)
		{
			CheckCapacityAndSwap(8);
			
			ulong num = (ulong)(*(long*)(&value));
			buffer[position++] = (byte)num;
			buffer[position++] = (byte)(num >> 8);
			buffer[position++] = (byte)(num >> 16);
			buffer[position++] = (byte)(num >> 24);
			buffer[position++] = (byte)(num >> 32);
			buffer[position++] = (byte)(num >> 40);
			buffer[position++] = (byte)(num >> 48);
			buffer[position++] = (byte)(num >> 56);
		}

		//slow
		public override void Write(decimal d)
		{
			CheckCapacityAndSwap(16) ;
			int[] bits = decimal.GetBits(d);

			Write(bits[0]);
			Write(bits[1]);
			Write(bits[2]);
			Write(bits[3]);
		}

		
		public override void Write(short value)
		{
			CheckCapacityAndSwap(2);
			buffer[position++] = (byte)value;
			buffer[position++] = (byte)(value >> 8);
		}

		public override void Write(ushort value)
		{
			CheckCapacityAndSwap(2);
			buffer[position++] = (byte)value;
			buffer[position++] = (byte)(value >> 8);
		}

		
		public override void Write(int value)
		{
			CheckCapacityAndSwap(4);
			buffer[position++] = (byte)value;
			buffer[position++] = (byte)(value >> 8);
			buffer[position++] = (byte)(value >> 16);
			buffer[position++] = (byte)(value >> 24);
		}

		public override void Write(uint value)
		{
			CheckCapacityAndSwap(4);
			buffer[position++] = (byte)value;
			buffer[position++] = (byte)(value >> 8);
			buffer[position++] = (byte)(value >> 16);
			buffer[position++] = (byte)(value >> 24);
		}

		
		public override void Write(long value)
		{
			CheckCapacityAndSwap(8);
			buffer[position++] = (byte)value;
			buffer[position++] = (byte)(value >> 8);
			buffer[position++] = (byte)(value >> 16);
			buffer[position++] = (byte)(value >> 24);
			buffer[position++] = (byte)(value >> 32);
			buffer[position++] = (byte)(value >> 40);
			buffer[position++] = (byte)(value >> 48);
			buffer[position++] = (byte)(value >> 56);
		}

		public override void Write(ulong value)
		{
			CheckCapacityAndSwap(8);
			buffer[position++] = (byte)value;
			buffer[position++] = (byte)(value >> 8);
			buffer[position++] = (byte)(value >> 16);
			buffer[position++] = (byte)(value >> 24);
			buffer[position++] = (byte)(value >> 32);
			buffer[position++] = (byte)(value >> 40);
			buffer[position++] = (byte)(value >> 48);
			buffer[position++] = (byte)(value >> 56);
		}

		public unsafe override void Write(float value)
		{
			CheckCapacityAndSwap(4);
			uint num = *(uint*)(&value);
			buffer[position++] = (byte)num;
			buffer[position++] = (byte)(num >> 8);
			buffer[position++] = (byte)(num >> 16);
			buffer[position++] = (byte)(num >> 24);
		}

		
		//slow
		public unsafe override void Write(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			int byteCount = _encoding.GetByteCount(value);
			Write7BitEncodedInt(byteCount);
			if (byteCount <= suplusCapacity)
			{
				int Wcount = _encoding.GetBytes(value, 0, value.Length, buffer, position);
				suplusCapacity -= Wcount;
				position += Wcount;
				//Console.WriteLine($"Using quick write!");
				return;
			}

			int charIndex = 0;
			bool completed;
			do
			{
				fixed (char* chars = value)
				{
					fixed (byte* bytes = buffer)
					{
						encoder.Convert(chars + charIndex, value.Length - charIndex,
							bytes + position, suplusCapacity, false,
							out int charsConsumed, out int bytesWritten, out completed);
						charIndex += charsConsumed;						
						position += bytesWritten;
						suplusCapacity -= bytesWritten;
						//Console.WriteLine($"charsConsumed{charsConsumed} charIndex{charIndex} bytesWritten{bytesWritten} position{position} suplusCapacity{suplusCapacity}");
					}
				}
				if (suplusCapacity <= 0) 
					SwapBuffer();	
			} while (!completed);
			encoder.Reset(); //flush
		}

        protected new void Write7BitEncodedInt(int value)
        {
            uint num;
            for (num = (uint)value; num >= 128; num >>= 7)
            {
                Write((byte)(num | 0x80));
            }
            Write((byte)num);
        }
    }
}
