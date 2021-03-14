using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace AsyncUpdate
{
	[BepInPlugin("com.bluedoom.plugin.Dyson.AsyncUpdate", "AsyncUpdate", "1.0")]
	public class AsyncUpdate : BaseUnityPlugin
	{
		Harmony patchInstance;
		private void Start()
		{
			patchInstance = Harmony.CreateAndPatchAll(typeof(CargeTrafficPatch), null);
			Logger.LogWarning("GameSave Start");
		}

		void OnDestroy()
		{
			patchInstance?.UnpatchSelf();
			patchInstance = null;
		}

		public void Update()
        {
			if (Input.GetKeyDown(KeyCode.Home))
            {
				if (patchInstance == null)
                {
					Start();
                }
				else
                {
					OnDestroy();
                }
            }
        }
	}

	class PatchTick
    {

        //[HarmonyPatch(typeof(GameMain), "FixedUpdate"), HarmonyPostfix]
        //static void FixedUpdate(GameMain __instance,bool ___running)
        //{
        //	Console.WriteLine(___running);
        //	//if (!__instance._running)
        //	//{
        //	//	pause = true;
        //	//}
        //	//if (!__instance._paused)
        //	//{
        //	//	int num = 1;
        //	//	if (__instance._fullscreenPaused && !__instance._fullscreenPausedUnlockOneFrame)
        //	//	{
        //	//		num = 0;
        //	//	}
        //	//}
        //}
        //static HighStopwatch ht = new HighStopwatch();
        //[HarmonyPatch(typeof(PlanetFactory), "GameTick"), HarmonyPrefix]
        //static bool PFGameTick(PlanetFactory __instance,long time)
        //      {

        //	bool flag = GameMain.localPlanet == __instance.planet;
        //	if (__instance.factorySystem != null)
        //	{
        //		ht.Begin();
        //		__instance.factorySystem.GameTickBeforePower(time, flag);
        //		Console.WriteLine("factorySystem.GameTickBeforePower:"+ht.duration);
        //	}
        //	if (__instance.transport != null)
        //	{
        //		ht.Begin();
        //		__instance.transport.GameTickBeforePower(time, flag);
        //		Console.WriteLine("transport.GameTickBeforePower:" + ht.duration);
        //	}
        //	if (__instance.powerSystem != null)
        //	{
        //		ht.Begin();
        //		__instance.powerSystem.GameTick(time, flag);
        //		Console.WriteLine("powerSystem.GameTick:" + ht.duration);
        //	}
        //	if (__instance.factorySystem != null)
        //	{
        //		ht.Begin();
        //		__instance.factorySystem.GameTick(time, flag);
        //		Console.WriteLine("factorySystem.GameTick:" + ht.duration);

        //	}
        //	if (__instance.transport != null)
        //	{
        //		ht.Begin();
        //		__instance.transport.GameTick(time, flag);
        //		Console.WriteLine("transport.GameTick:" + ht.duration);

        //	}
        //	if (__instance.factorySystem != null)
        //	{
        //		ht.Begin();
        //		__instance.factorySystem.GameTickInserters(time, flag);
        //		Console.WriteLine("factorySystem.GameTick:" + ht.duration);

        //	}
        //	if (__instance.factoryStorage != null)
        //	{
        //		ht.Begin();
        //		__instance.factoryStorage.GameTick(time, flag);
        //		Console.WriteLine("factoryStorage.GameTick:" + ht.duration);

        //	}
        //	if (__instance.cargoTraffic != null)
        //	{
        //		ht.Begin();
        //		__instance.cargoTraffic.GameTick(time, flag);
        //		Console.WriteLine("cargoTraffic.GameTick:" + ht.duration);

        //	}
        //	if (__instance.monsterSystem != null)

        //	{
        //		__instance.monsterSystem.GameTick(time, flag);
        //	}
        //	return false;
        //      }

        class PlanetFactoryRunner : MTRunner<PlanetFactory>
        {
			public long time;
            public override void Process(int i)
            {
                var item = items[i];

				if (item != null)
				{
					item.GameTick(time);
				}
			}
        }
		static PlanetFactoryRunner planetFactoryRunner = new PlanetFactoryRunner();


        [HarmonyPatch(typeof(GameData), "GameTick"), HarmonyPrefix]
		static bool GameTick(GameData __instance, long time,ref bool ___demoTicked)
        {
			double gameTime = GameMain.gameTime;
			__instance.statistics.PrepareTick();
			__instance.history.PrepareTick();
			if (__instance.localPlanet != null && __instance.localPlanet.factoryLoaded)
			{
				__instance.localPlanet.physics.GameTick();
			}
			if (__instance.guideMission != null)
			{
				__instance.guideMission.GameTick();
			}
			if (__instance.mainPlayer != null && !___demoTicked)
			{
				__instance.mainPlayer.GameTick(time);
			}
			__instance.DetermineRelative();
			for (int i = 0; i < __instance.dysonSpheres.Length; i++)
			{
				if (__instance.dysonSpheres[i] != null)
				{
					__instance.dysonSpheres[i].BeforeGameTick(time);
				}
			}
			for (int j = 0; j < __instance.factoryCount; j++)
			{
				Assert.NotNull(__instance.factories[j]);
				if (__instance.factories[j] != null)
				{
					__instance.factories[j].BeforeGameTick(time);
				}
			}
			planetFactoryRunner.time = time;
			planetFactoryRunner.DoParallel(__instance.factories, 0, __instance.factoryCount);
			//for (int k = 0; k < __instance.factoryCount; k++)
			//{
			//	if (__instance.factories[k] != null)
			//	{
			//		__instance.factories[k].GameTick(time);
			//	}
			//}
			__instance.galacticTransport.GameTick(time);
			for (int l = 0; l < __instance.dysonSpheres.Length; l++)
			{
				if (__instance.dysonSpheres[l] != null)
				{
					__instance.dysonSpheres[l].GameTick(time);
				}
			}
			if (__instance.localPlanet != null && __instance.localPlanet.factoryLoaded)
			{
				__instance.localPlanet.audio.GameTick();
			}
			if (!DSPGame.IsMenuDemo)
			{
				__instance.statistics.GameTick(time);
			}
			if (!DSPGame.IsMenuDemo)
			{
				__instance.warningSystem.GameTick(time);
			}
			__instance.history.AfterTick();
			__instance.statistics.AfterTick();
			__instance.preferences.Collect();
			if (DSPGame.IsMenuDemo)
			{
				___demoTicked = true;
			}
			return false;
		}
	}

}
