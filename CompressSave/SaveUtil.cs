using BepInEx.Logging;
using LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DSP_Plugin
{
    class SaveUtil
    {
        public static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("SaveCompress");
        

        public static readonly Version VerifiedVersion = new Version
        {
            Major = 0,
            Minor = 6,
            Release = 17,
            Build = 5831,
        };

        public static string UnzipToFile(LZ4DecompressionStream lzStream, string fullPath)
        {
            lzStream.ResetStream();
            string dir = Path.GetDirectoryName(fullPath);
            string filename = "[Recovery]-" + Path.GetFileNameWithoutExtension(fullPath);
            fullPath = Path.Combine(dir, filename + GameSave.saveExt);
            int i = 0;
            while(File.Exists(fullPath))
            {
                fullPath = Path.Combine(dir, $"{filename}[{i++}]{GameSave.saveExt}"); 
            }
            var buffer = new byte[1024 * 1024];
            using (var fs = new FileStream(fullPath, FileMode.Create))
            using (var br = new BinaryWriter(fs))
            {
                for (int read = lzStream.Read(buffer, 0, buffer.Length); read > 0; read = lzStream.Read(buffer, 0, buffer.Length))
                {
                    fs.Write(buffer, 0, read);
                }
                fs.Seek(6L, SeekOrigin.Begin);
                br.Write(fs.Length);

            }
            return filename;
        }

        public static bool DecompressSave(string saveName)
        {
            string path = GameConfig.gameSaveFolder + saveName + GameSave.saveExt;
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (!IsCompressedSave(fileStream)) return false;
                    using (var lzstream = new LZ4DecompressionStream(fileStream))
                    {
                        UnzipToFile(lzstream, path);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return false;
            }
        }
        public static bool IsCompressedSave(FileStream fs)
        {
            for (int i = 0; i < 4; i++)
            {
                if (0xCC != fs.ReadByte())
                    return false;
            }
            return true;
        }

        internal static bool IsCompressedSave(string saveName)
        {
            try
            {
                using (FileStream fileStream = new FileStream(GetFullSavePath(saveName), FileMode.Open))
                    return IsCompressedSave(fileStream);
            }
            catch (Exception e)
            {
                logger.LogWarning(e);
                return false;
            }
        }

        public static string GetFullSavePath(string saveName) => GameConfig.gameSaveFolder + saveName + GameSave.saveExt;

        public static bool VerifyVersion(int majorVersion, int minorVersion, int releaseVersion)
        {
            return new Version(majorVersion, minorVersion, releaseVersion) == VerifiedVersion;
        }
    }
}
