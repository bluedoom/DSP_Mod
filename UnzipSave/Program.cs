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
        static string str = "LZ4 is lossless compression algorithm";
        static string str2 = "providing compression speed > 500 MB/s per core, scalable with multi-cores CPU. ";
        static string str3 = "It features an extremely fast decoder, with speed in multiple GB/s per core";
        static char[] chars = " typically reaching RAM speed limits on multi-core systems.".ToArray();

        static void TestMe()
        {
            var rand = new Random(98765);
            var ints = new int[5000].Select(_ => rand.Next()).ToArray();
            var floats = new float[5000].Select(_ => rand.Next()/10000f).ToArray();
            var doubles = new double[5000].Select(_ => rand.NextDouble()).ToArray();
            var longs = new long[5000].Select(_ => rand.Next() * (long)rand.Next() ).ToArray();
            var bytes = new byte[20000]; rand.NextBytes(bytes);
            using (MemoryStream memoryStream = new MemoryStream(1024 * 1024 * 8))
            {
                using (LZ4CompressionStream cp = new LZ4CompressionStream(memoryStream, LZ4CompressionStream.CreateBuffer(1024), true))
                using (var w = cp.CreateBufferedWriter())
                {
                    for (int i = 0; i < 5000; i++)
                    {
                        w.Write(ints[i]);
                        w.Write(doubles[i]);
                        w.Write(longs[i]);
                        w.Write(bytes[i]);
                        w.Write(floats[i]);

                    }
                    for (int i = 0; i < 5000; i++)
                    {
                        w.Write((sbyte)bytes[i]);
                        w.Write((uint)ints[i]);
                        w.Write(doubles[i]);
                        w.Write(floats[i]);
                        w.Write((ulong)longs[i]);
                    }
                    w.Write(bytes);
                    for (int i = 0; i < 100; i++)
                    {
                        w.Write(str);
                        w.Write(str2);
                        w.Write(str3);
                        w.Write(chars);
                    }
                }
                memoryStream.Position = 0;
                using (var decs = new LZ4DecompressionStream(memoryStream))
                using (BinaryReader br = new BinaryReader(decs))
                {
                    for (int i = 0; i < 5000; i++)
                    {
                        Debug.Assert(
                            (br.ReadInt32() == ints[i] &&
                            br.ReadDouble() == doubles[i] &&
                            br.ReadInt64() == longs[i] &&
                            br.ReadByte() == bytes[i] &&
                            br.ReadSingle() == floats[i])
                            );
                    }
                    for (int i = 0; i < 5000; i++)
                    {
                        Debug.Assert(
                            ( br.ReadSByte() == (sbyte)bytes[i] && 
                            br.ReadUInt32() == (uint)ints[i]  &&
                            br.ReadDouble() == doubles[i] && 
                            br.ReadSingle() == (floats[i]) &&
                            br.ReadUInt64() == (ulong)longs[i])
                            );
                    }
                    Debug.Assert(br.ReadBytes(20000).SequenceEqual(bytes));
                    for (int i = 0; i < 100; i++)
                    {
                        Debug.Assert(
                            (br.ReadString() == str &&
                            br.ReadString() == str2 &&
                            br.ReadString() == str3 &&
                            br.ReadChars(chars.Length).SequenceEqual(chars)));
                    }
                }
                Console.WriteLine("Success");

            }
        }

        static void Main(string[] args)
        {
            try
            {
                TestMe();
                //string dir = Path.GetDirectoryName(args[0]);
                //string filename = "[Recovery]-" + Path.GetFileName(args[0]);
                //string outPath = Path.Combine(dir, filename);
                //using (FileStream inFS = new FileStream(args[0], FileMode.Open))
                //{
                //    for (int i = 0; i < 4; i++)
                //    {
                //        if (inFS.ReadByte() != 0xCC) throw new Exception("Not A Compressed File!");
                //    }
                //    using (LZ4DecompressionStream dcmp = new LZ4DecompressionStream(inFS))
                //    using (var outFS = new FileStream(outPath, FileMode.Create))
                //    {
                //        CopyTo(dcmp, outFS);
                //        outFS.Seek(6, SeekOrigin.Begin);
                //        using (BinaryWriter br = new BinaryWriter(outFS))
                //            br.Write(outFS.Length);
                //    }
                //}
                //Console.WriteLine($"Sucess:{outPath}");
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
