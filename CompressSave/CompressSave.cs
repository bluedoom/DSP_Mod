
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DSP_Plugin
{
	[BepInPlugin("com.bluedoom.plugin.Dyson.CompressSave", "CompressSave", "1.1.0")]
	public class CompressSave : BaseUnityPlugin
	{
        List<Harmony> patchList;
        Harmony patchUILoadGameInstance;

        private void Start()
        {
            //SaveUtil.logger = Logger;
            if(LZ4API.Avaliable)
            {
                BepInEx.Logging.Logger.Sources.Add(SaveUtil.logger);

                if (GameConfig.gameVersion == SaveUtil.VerifiedVersion)
                {
                    patchList = new List<Harmony>
                    {
                        Harmony.CreateAndPatchAll(typeof(PatchSave), null),
                        Harmony.CreateAndPatchAll(typeof(PatchUISaveGame), null),
                    };
                }
                else
                {
                    SaveUtil.logger.LogWarning($"Version Verify Failed. Expect:{SaveUtil.VerifiedVersion},Current:{GameConfig.gameVersion}");
                }
                patchUILoadGameInstance = Harmony.CreateAndPatchAll(typeof(PatchUILoadGame), null);
            }
        }

		void OnDestroy()
        {
            BepInEx.Logging.Logger.Sources.Remove(SaveUtil.logger);
            PatchUISaveGame.OnDestroy();
            PatchUILoadGame.OnDestroy();
            patchUILoadGameInstance?.UnpatchSelf();
            patchList?.ForEach(h => h?.UnpatchSelf());
            patchList?.Clear();
        }
	}

	class PatchSave
	{
        const bool SkipHostFunc = false;
        const bool RunHostFunc = true;
		const long MB = 1024 * 1024;
		static LZ4CompressionStream.CompressBuffer compressBuffer = LZ4CompressionStream.CreateBuffer((int)MB);
        public static bool UseCompressSave = false;

        private static void WriteHeader(FileStream fileStream)
        {
            for(int i=0;i<4;i++)
                fileStream.WriteByte(0xCC);
        }


        [HarmonyPatch(typeof(GameSave), "AutoSave"),HarmonyPrefix]
        static void BeforeAutoSave()
        {
            UseCompressSave = true;
        }

		[HarmonyPatch(typeof(GameSave), "SaveCurrentGame"), HarmonyPrefix]
		static bool Save_Wrap(ref bool __result, string saveName)
        {
            if (SaveUtil.VerifiedVersion != GameConfig.gameVersion)
            {
                //logger.LogWarning($"VersionVerify Failed. Expect:{VerifiedVersion},Current:{GameConfig.gameVersion}");
                return RunHostFunc;
            }
            if (!UseCompressSave) return RunHostFunc;

			__result = Save(saveName);
			return !__result;
        }
		public static bool Save(string saveName)
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
                    using (BinaryWriter binaryWriter = lzstream.CreateBufferedWriter())
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
                CompressionGameSaveHeader gameSaveHeader = new CompressionGameSaveHeader();
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (SaveUtil.IsCompressedSave(fileStream))
                    {
                        gameSaveHeader.IsCompressed = true;
                    }
                    else
                    {
                        return null; 
                    }
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
        static bool Load_Wrap(ref bool __result, string saveName)
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
                    if (!SaveUtil.IsCompressedSave(fileStream)) return false;
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
                        binaryReader.ReadInt32();
                        binaryReader.ReadInt32();
                        binaryReader.ReadInt32();


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
