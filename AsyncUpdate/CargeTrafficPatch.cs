using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncUpdate
{
    public class CargeTrafficPatch
    {
        class CargoPathRuner : MTRunner<CargoPath>
        {
            public CargoTraffic __instance;

            public override void Process(int i)
            {
                var item = items[__instance.pathCursor - i];
                //lock(item.outputPath)
                //{
                //    lock(item)
                //    {
                        if (item != null && item.id == i)
                        { item.Update(); }
                //    }
                //}
            }
        }
        class SplitterRunner : MTRunner<SplitterComponent>
        {
            public CargoTraffic __instance;
            public override void Process( int i)
            {
                var item = items[i];

                if (item.id == i)
                {
                    __instance.UpdateSplitter(i, item.input0, item.input1, item.input2, item.output0, item.output1, item.output2, item.outFilter);
                }
            }
        }
        class CargoPathRuner2 : MTRunner<CargoPath>
        {
            public override void Process( int i)
            {
                var item = items[i];

                //__instance.pathPool[l] != null && __instance.pathPool[l].id > 0
                //lock (item.outputPath)
                //{
                //    lock (item)
                //    {
                        if (item != null && item.id > 0)
                        {

                            item.PresentCargos();
                        }
                //    }
                //}
            }
        }

        static CargoPathRuner cargoPathRuner = new CargoPathRuner();
        static SplitterRunner splitterRunner = new SplitterRunner();
        static CargoPathRuner2 cargoPathRuner2 = new CargoPathRuner2();
        [HarmonyPatch(typeof(CargoTraffic), "GameTick"), HarmonyPrefix]
        static bool GameTick(CargoTraffic __instance, long time, bool presentCargos)
        {
            //bool flag = time / 10L % 2L == 0L;
            //flag = true;
            if (true)
            {
                for (int i = 1; i < __instance.pathCursor; i++)
                {
                    var item = __instance.pathPool[i];
                    if (__instance.pathPool[i] != null && __instance.pathPool[i].id == i)
                    {
                        //__instance.pathPool[i].Update();

                    }
                }
            }
            else
            {
                //Nerver Go here
                for (int j = __instance.pathCursor - 1; j > 0; j--)
                {
                    if (__instance.pathPool[j] != null && __instance.pathPool[j].id == j)
                    {
                        __instance.pathPool[j].Update();
                    }
                }
            }
            for (int k = 1; k < __instance.splitterCursor; k++)
            {
                var item = __instance.splitterPool[k];
                if (item.id == k)
                {
                    __instance.UpdateSplitter(k, item.input0, item.input1, item.input2, item.output0, item.output1, item.output2, item.outFilter);
                }
            }

            if (presentCargos)
            {
                cargoPathRuner2.DoParallel(__instance.pathPool, 0, __instance.pathCursor);
            }
            return false;
        }
    }
}
