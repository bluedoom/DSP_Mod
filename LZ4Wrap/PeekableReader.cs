using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security;

namespace LZ4
{
    class PeekableReader : BinaryReader
    {
        LZ4DecompressionStream lzstream;
        public PeekableReader(LZ4DecompressionStream input) : base (input)
        {
            lzstream = input;
        }

        public override int PeekChar()
        {            
            return lzstream.PeekByte();
        }
    }
}
