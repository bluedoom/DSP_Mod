
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LZ4;
using System;

using System.IO;
using UnityEngine;

namespace DSP_Plugin
{
	[BepInPlugin("com.bluedoom.plugin.Dyson.CompressSave", "CompressSave", "1.0")]
	public class CompressSave : BaseUnityPlugin
	{
		Harmony patchInstance;
		private void Start()
		{
 			patchInstance = Harmony.CreateAndPatchAll(typeof(PatchSave), null);
			Logger.LogWarning("GameSave Start");
		}

		void OnDestroy()
        {
			patchInstance?.UnpatchSelf();
        }
	}

	class PatchSave
	{
        static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("SaveCompress");
        const bool SkipHostFunc = false;
        const bool RunHostFunc = true;
		const long MB = 1024 * 1024;
		static LZ4CompressionStream.CompressBuffer compressBuffer = LZ4CompressionStream.CreateBuffer((int)MB);

        public static string UnzipToFile(LZ4DecompressionStream lzStream,string fullPath)
        {
            lzStream.ResetStream();
            string dir = Path.GetDirectoryName(fullPath);
            string filename = "[Recovery]-" + Path.GetFileNameWithoutExtension(fullPath);
            fullPath = Path.Combine(dir, filename+ GameSave.saveExt);
            var buffer = new byte[64 * 1024];
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

		static readonly Version VerifiedVersion = new Version {
			Major = 0,
			Minor = 6,
			Release = 17,
			Build = 5831,
		};

		static bool VerifyVersion(int majorVersion,int minorVersion,int releaseVersion)
        {
			return new Version(majorVersion, minorVersion, releaseVersion) == VerifiedVersion;
		}


        private static void WriteHeader(FileStream fileStream)
        {
            for(int i=0;i<4;i++)
                fileStream.WriteByte(0xCC);
        }

        static bool IsCompressedSave(FileStream fs)
        {
            for (int i = 0; i < 4; i++)
            {
                if (0xCC != fs.ReadByte())
                    return false;
            }
            return true;
        }
		[HarmonyPatch(typeof(GameSave), "SaveCurrentGame"), HarmonyPrefix]
		static bool Save_Wrap(ref bool __result, string saveName)
        {
            if (VerifiedVersion != GameConfig.gameVersion)
            {
                //logger.LogWarning($"VersionVerify Failed. Expect:{VerifiedVersion},Current:{GameConfig.gameVersion}");
                Debug.LogWarning($"VersionVerify Failed. Expect:{VerifiedVersion},Current:{GameConfig.gameVersion}");
                return RunHostFunc;
            }
			__result = Save(saveName);
			return !__result;
        }
		static bool Save(string saveName)
		{
			HighStopwatch highStopwatch = new HighStopwatch();
			highStopwatch.Begin();
			if (DSPGame.Game == null)
			{
				Debug.LogError("No game to save");
				return false;

			}
			GameCamera.CaptureSaveScreenShot();
			saveName = saveName.ValidFileName();
			string path = GameConfig.gameSaveFolder + saveName + GameSave.saveExt;
			try
			{
				using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
				{
                    WriteHeader(fileStream);

                    using (LZ4CompressionStream lzstream = new LZ4CompressionStream(fileStream, compressBuffer, true))
                    using (BinaryWriter binaryWriter = lzstream.CreateBufferWriter())
					{
						binaryWriter.Write('V');
						binaryWriter.Write('F');
						binaryWriter.Write('S');
						binaryWriter.Write('A');
						binaryWriter.Write('V');
						binaryWriter.Write('E');
						binaryWriter.Write(0L);
						binaryWriter.Write(5);
						binaryWriter.Write(GameConfig.gameVersion.Major);
						binaryWriter.Write(GameConfig.gameVersion.Minor);
						binaryWriter.Write(GameConfig.gameVersion.Release);
						binaryWriter.Write(GameMain.gameTick);
						long ticks = DateTime.Now.Ticks;
						binaryWriter.Write(ticks);
						GameData data = GameMain.data;
						if (data.screenShot != null)
						{
							int num = data.screenShot.Length;
							binaryWriter.Write(num);
							binaryWriter.Write(data.screenShot, 0, num);
						}
						else
						{
							binaryWriter.Write(0);
						}
						ulong num2 = 0UL;
						DysonSphere[] dysonSpheres = data.dysonSpheres;
						int num3 = dysonSpheres.Length;
						for (int i = 0; i < num3; i++)
						{
							if (dysonSpheres[i] != null)
							{
								num2 += (ulong)dysonSpheres[i].energyGenCurrentTick;
							}
						}
						data.account.Export(binaryWriter);
						binaryWriter.Write(num2);
						data.Export(binaryWriter);
						//long position = fileStream.Position;
						//fileStream.Seek(6L, SeekOrigin.Begin);
						//binaryWriter.Write(position);
					}
				}
				double duration = highStopwatch.duration;
				Debug.Log("Game save file wrote, time cost: " + duration + "s");
				STEAMX.UploadScoreToLeaderboard(GameMain.data);
				return true;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				return false;
			}
			
		}

        [HarmonyPatch(typeof(GameSave), "ReadHeader"), HarmonyPrefix]
        static bool ReadHeader_Wrap(ref GameSaveHeader __result, string saveName, bool readImage)
        {
            __result = ReadHeader(saveName, readImage);
            return __result == null;
        }

        static bool VerifyHeader(BinaryReader binaryReader)
        {
            return binaryReader.ReadChar() == 'V'
             && binaryReader.ReadChar() == 'F'
             && binaryReader.ReadChar() == 'S'
             && binaryReader.ReadChar() == 'A'
             && binaryReader.ReadChar() == 'V'
             && binaryReader.ReadChar() == 'E';
        }

        static GameSaveHeader ReadHeader(string saveName, bool readImage)
        {
            if (saveName == null)
            {
                return null;
            }
            saveName = saveName.ValidFileName();
            string path = GameConfig.gameSaveFolder + saveName + GameSave.saveExt;
            if (!File.Exists(path))
            {
                return null;
            }
            GameSaveHeader result;
            try
            {
                GameSaveHeader gameSaveHeader = new GameSaveHeader();
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (!IsCompressedSave(fileStream)) return null;
                    using (var lzstream = new LZ4DecompressionStream(fileStream))
                    using (BinaryReader binaryReader = new BinaryReader(lzstream))
                    {
                        if (!VerifyHeader(binaryReader))
                        {
                            Debug.LogError("Invalid game save file");
                            return null;
                        }

                        binaryReader.ReadInt64();
                        gameSaveHeader.fileSize = fileStream.Length;
        
                        gameSaveHeader.headerVersion = binaryReader.ReadInt32();
                        if (gameSaveHeader.headerVersion < 1)
                        {
                            return null;
                        }
                        gameSaveHeader.gameClientVersion.Major = binaryReader.ReadInt32();
                        gameSaveHeader.gameClientVersion.Minor = binaryReader.ReadInt32();
                        gameSaveHeader.gameClientVersion.Release = binaryReader.ReadInt32();
                        gameSaveHeader.gameTick = binaryReader.ReadInt64();
                        long value = binaryReader.ReadInt64();
                        gameSaveHeader.saveTime = default(DateTime).AddTicks(value);
                        if (readImage)
                        {
                            int count = binaryReader.ReadInt32();
                            gameSaveHeader.themeImage = binaryReader.ReadBytes(count);
                        }
                        if (gameSaveHeader.headerVersion >= 5)
                        {
                            gameSaveHeader.accountData.Import(binaryReader);
                            gameSaveHeader.clusterGeneration = binaryReader.ReadUInt64();
                        }
                        else
                        {
                            gameSaveHeader.accountData = AccountData.NULL;
                            gameSaveHeader.clusterGeneration = 0UL;
                        }
                    }
                }
                result = gameSaveHeader;
            }
            catch (Exception)
            {
                result = null;
            }
            return result;
        }

        [HarmonyPatch(typeof(GameSave), "LoadCurrentGame"), HarmonyPrefix]
        static bool Load_Wrap(ref bool __result, ref string saveName)
        {
            __result = LoadCurrentGame(ref saveName);
            return !__result;
        }

        static bool LoadCurrentGame(ref string saveName)
        {
            if (DSPGame.Game == null)
            {
                Debug.LogError("No game to load");
                return false;
            }
            saveName = saveName.ValidFileName();
            string path = GameConfig.gameSaveFolder + saveName + GameSave.saveExt;
            if (!File.Exists(path))
            {
                Debug.LogError("Game save not exist");
                return false;
            }
            bool result;
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (!IsCompressedSave(fileStream)) return false;
                    using (var lzstream = new LZ4DecompressionStream(fileStream))
                    using (BinaryReader binaryReader = new BinaryReader(lzstream))
                    {
                        if (!VerifyHeader(binaryReader))
                        {
                            Debug.LogError("Invalid game save file");
                            return false;
                        }
                        binaryReader.ReadInt64();
                        //long length = fileStream.Length;
                        //if (length != binaryReader.ReadInt64())
                        //{
                        //	Debug.LogError("Incomplete game save");
                        //	return false;
                        //}
                        int num = binaryReader.ReadInt32();
                        if (num < 4)
                        {
                            Debug.LogError("Game save is too old");
                            return false;
                        }
                        if (!VerifyVersion(binaryReader.ReadInt32(),
                            binaryReader.ReadInt32(),
                            binaryReader.ReadInt32()))
                        {
                            saveName = UnzipToFile(lzstream, path);
                            return false;
                        }


                        binaryReader.ReadInt64();
                        binaryReader.ReadInt64();
                        int num2 = binaryReader.ReadInt32();
                        while (num2 > 0)
                        {
                            num2 -= lzstream.Read(compressBuffer.outBuffer, 0, num2);

                            //fileStream.Seek((long)num2, SeekOrigin.Current);
                        }
                        if (num >= 5)
                        {
                            AccountData.NULL.Import(binaryReader);
                            binaryReader.ReadUInt64();
                        }
                        GameMain.data.Import(binaryReader);
                    }

                    result = true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                result = false;
            }
            return result;
        }
    }

}
