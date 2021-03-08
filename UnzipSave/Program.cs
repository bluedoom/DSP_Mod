using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LZ4;
using MonoMod.Utils;

namespace UnzipSave
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string dir = Path.GetDirectoryName(args[0]);
                string filename = "[Recovery]-" + Path.GetFileName(args[0]);
                string outPath = Path.Combine(dir, filename);
                using (FileStream inFS = new FileStream(args[0], FileMode.Open))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (inFS.ReadByte() != 0xCC) throw new Exception("Not A Compressed File!");
                    }
                    using (LZ4DecompressionStream dcmp = new LZ4DecompressionStream(inFS))
                    using (var outFS = new FileStream(outPath, FileMode.Create))
                    {
                        CopyTo(dcmp, outFS);
                        outFS.Seek(6, SeekOrigin.Begin);
                        using (BinaryWriter br = new BinaryWriter(outFS))
                            br.Write(outFS.Length);
                    }
                }
                Console.WriteLine($"Sucess:{outPath}");
            }
            catch(Exception e)
            {
                Console.WriteLine("Failed:",e.ToString());
            }

            Console.ReadLine();
        }

        public static void CopyTo(Stream src, Stream dst, byte[] buffer = null)
        {
            buffer = buffer ?? new byte[8 * 1024 * 1024];
            int count;
            while ((count = src.Read(buffer, 0, buffer.Length)) > 0)
            {
                dst.Write(buffer, 0, count);
            }
        }
    }
}
