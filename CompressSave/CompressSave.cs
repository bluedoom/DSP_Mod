using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace DSP_Plugin
{
    [BepInPlugin("com.bluedoom.plugin.Dyson.CompressSave", "CompressSave", "1.1.7")]
    public class CompressSave : BaseUnityPlugin
    {
        Harmony harmony;
        public void Awake()
        {
            harmony = new Harmony("com.bluedoom.plugin.Dyson.CompressSave");
            SaveUtil.logger = Logger;
            if (LZ4API.Avaliable)
            {
                if (GameConfig.gameVersion != SaveUtil.VerifiedVersion)
                {
                    SaveUtil.logger.LogWarning($"Save versions are not matched. Expect:{SaveUtil.VerifiedVersion},Current:{GameConfig.gameVersion}");
                }
                harmony.PatchAll(typeof(PatchSave));
                if (PatchSave.EnableCompress)
                    harmony.PatchAll(typeof(PatchUISaveGame));
                harmony.PatchAll(typeof(PatchUILoadGame));
            }
            else
                SaveUtil.logger.LogWarning("LZ4.dll is not avaliable.");
        }

        public void OnDestroy()
        {
            PatchUISaveGame.OnDestroy();
            PatchUILoadGame.OnDestroy();
            harmony.UnpatchSelf();
        }
    }

    class PatchSave
    {
        const long MB = 1024 * 1024;
        static LZ4CompressionStream.CompressBuffer compressBuffer = LZ4CompressionStream.CreateBuffer((int)MB); //Bigger buffer for GS2 compatible
        public static bool UseCompressSave = false;
        public static bool IsCompressedSave;
        static Stream lzstream = null;
        public static bool EnableCompress;
        public static bool EnableDecompress;

        private static void WriteHeader(FileStream fileStream)
        {
            for (int i = 0; i < 4; i++)
                fileStream.WriteByte(0xCC);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSave), "AutoSave")]
        [HarmonyPatch(typeof(GameSave), "SaveAsLastExit")]
        static void BeforeAutoSave()
        {
            UseCompressSave = EnableCompress;
        }

        [HarmonyPatch(typeof(GameSave), "SaveCurrentGame"), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> SaveCurrentGame_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            /* BinaryWriter binaryWriter = new BinaryWriter(fileStream); => Create lzstream and replace binaryWriter.
             * set PerformanceMonitor.BeginStream to lzstream.
             * fileStream.Seek(6L, SeekOrigin.Begin); binaryWriter.Write(position); => Disable seek&write function.
             * binaryWriter.Dispose(); => Dispose lzstream before fileStream close.
            */
            try
            {
                var matcher = new CodeMatcher(instructions, iLGenerator)
                    .MatchForward(false, new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(BinaryWriter), new Type[] { typeof(FileStream) })))
                    .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "CreateBinaryWriter"))
                    .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PerformanceMonitor), "BeginStream")))
                    .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "MonitorStream"))
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IO.Stream), "Seek")))
                    .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthWrite0"))
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BinaryWriter), "Write", new Type[] { typeof(long) })))
                    .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthWrite1"))
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IDisposable), "Dispose")))
                    .Advance(1)
                    .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "DisposeLzstream")));
                EnableCompress = true;
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                SaveUtil.logger.LogError("SaveCurrentGame_Transpiler failed. Mod version not compatible with game version.");
                SaveUtil.logger.LogError(ex);
            }
            return instructions;
        }

        public static void MonitorStream(Stream fileStream)
        {
            PerformanceMonitor.BeginStream(UseCompressSave ? lzstream : fileStream);
        }

        public static BinaryWriter CreateBinaryWriter(FileStream fileStream)
        {
            if (UseCompressSave)
            {
                SaveUtil.logger.LogDebug("Begin compress save");
                WriteHeader(fileStream);
                lzstream = new LZ4CompressionStream(fileStream, compressBuffer, true); //need to dispose after use
                return ((LZ4CompressionStream)lzstream).BufferWriter;
            }
            SaveUtil.logger.LogDebug("Begin normal save");
            return new BinaryWriter(fileStream);
        }

        public static long FileLengthWrite0(FileStream fileStream, long offset, SeekOrigin origin)
        {
            if (!UseCompressSave)
                return fileStream.Seek(offset, origin);
            return 0L;
        }

        public static void FileLengthWrite1(BinaryWriter binaryWriter, long value)
        {
            if (!UseCompressSave)
                binaryWriter.Write(value);
        }

        public static void DisposeLzstream()
        {
            if (UseCompressSave)
            {
                bool writeflag = lzstream.CanWrite;
                lzstream?.Dispose(); //Dispose need to be done before fstream closed.
                lzstream = null;
                if (writeflag) //Reset UseCompressSave after writing to file
                    UseCompressSave = false;
                return;
            }
        }


        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameSave), "LoadCurrentGame")]
        [HarmonyPatch(typeof(GameSave), "LoadGameDesc")]
        [HarmonyPatch(typeof(GameSave), "ReadHeaderAndDesc")]
        static IEnumerable<CodeInstruction> LoadCurrentGame_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            /* using (BinaryReader binaryReader = new BinaryReader(fileStream)) => Create lzstream and replace binaryReader.
             * set PerformanceMonitor.BeginStream to lzstream.
             * if (fileStream.Length != binaryReader.ReadInt64()) => Replace binaryReader.ReadInt64() to pass file length check.
             * fileStream.Seek((long)num2, SeekOrigin.Current); => Use lzstream.Read to seek forward
             * binaryReader.Dispose(); => Dispose lzstream before fileStream close.
             */
            try
            {
                var matcher = new CodeMatcher(instructions, iLGenerator)
                    .MatchForward(false, new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(BinaryReader), new Type[] { typeof(FileStream) })))
                    .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "CreateBinaryReader"))
                    .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PerformanceMonitor), "BeginStream")));

                if (matcher.IsValid)
                    matcher.Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "MonitorStream"));

                matcher.Start().MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BinaryReader), "ReadInt64")))
                .Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "FileLengthRead"))
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IDisposable), "Dispose")))
                .Advance(1)
                .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "DisposeLzstream")))
                .MatchBack(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(System.IO.Stream), "Seek")));
                if (matcher.IsValid)
                    matcher.Set(OpCodes.Call, AccessTools.Method(typeof(PatchSave), "ReadSeek"));
                matcher.Start()
                    .MatchForward(false, new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(GameSaveHeader))));
                if (matcher.IsValid)
                    matcher.Set(OpCodes.Newobj, AccessTools.Constructor(typeof(CompressionGameSaveHeader))); //ReadHeader

                EnableDecompress = true;
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                SaveUtil.logger.LogError("LoadCurrentGame_Transpiler failed. Mod version not compatible with game version.");
                SaveUtil.logger.LogError(ex);
            }
            return instructions;
        }

        [HarmonyPatch(typeof(GameSave), "ReadHeaderAndDesc"), HarmonyPostfix]
        static void ReadHeader_Postfix(string __0, bool __1,ref GameSaveHeader header,object __3)
        {
            if (header != null)
                ((CompressionGameSaveHeader)header).IsCompressed = IsCompressedSave;
        }

        public static BinaryReader CreateBinaryReader(FileStream fileStream)
        {
            if ((IsCompressedSave = SaveUtil.IsCompressedSave(fileStream)))
            {
                UseCompressSave = true;
                lzstream = new LZ4DecompressionStream(fileStream);
                return new PeekableReader((LZ4DecompressionStream)lzstream);
            }
            else
            {
                UseCompressSave = false;
                fileStream.Seek(0, SeekOrigin.Begin);
                return new BinaryReader(fileStream);
            }
        }

        public static long FileLengthRead(BinaryReader binaryReader)
        {
            if (UseCompressSave)
            {
                binaryReader.ReadInt64();
                return lzstream.Length;
            }
            else
                return binaryReader.ReadInt64();
        }

        public static long ReadSeek(FileStream fileStream, long offset, SeekOrigin origin)
        {
            if (UseCompressSave)
            {
                while (offset > 0)
                    offset -= lzstream.Read(compressBuffer.outBuffer, 0, (int)offset);
                return lzstream.Position;
            }
            else
                return fileStream.Seek(offset, origin);
        }
    }

}
