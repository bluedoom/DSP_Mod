using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Fix118
{
    [BepInPlugin(PluginId, "Fix118", Version)]
    public class Fix118 : BaseUnityPlugin
    {
        const string PluginId = "com.bluedoom.plugin.Dyson.Fix118";
        const string Version = "1.0.0";

        public static ManualLogSource logger;
        Harmony harmony;
        public void Awake()
        {
            harmony = new Harmony(PluginId);
            logger = base.Logger;
            logger.LogWarning($"This mod will Remove some DIY Ammor Data");

            harmony.PatchAll(typeof(Patch));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    public static class Patch
    {
        [HarmonyPatch(typeof(BoneArmor), "Import"), HarmonyTranspiler]
        [HarmonyPatch(typeof(BoneArmor), "ImportOther")]
        static IEnumerable<CodeInstruction> BoneArmor_Import_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {

            try
            {
                var matcher = new CodeMatcher(instructions, iLGenerator)
                    .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BoneArmor), "SetSize")));
                if(matcher.IsValid)
                    //.MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BinaryReader), "ReadInt32")))
                    matcher.Advance(3)
                    .Set(OpCodes.Pop,null);
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex.ToString());
            }
            return instructions;
        }

        public static int SkipIntData(BinaryReader br)
        {
            br.ReadInt32();
            return 0;
        }


        [HarmonyPatch(typeof(MechaAppearance), "Import"), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> MechaAppearance_Import_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {

            try
            {

                var matcher = new CodeMatcher(instructions, iLGenerator)
                    .MatchForward(true, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BinaryReader), "ReadInt32")))
                    .Advance(1)
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BinaryReader), "ReadInt32")))
                    .Set(OpCodes.Call, AccessTools.Method(typeof(Patch), "SkipIntData"));

                //UnityEngine.Debug.LogWarning(string.Join("\n", matcher.InstructionEnumeration()));

                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex.ToString());
            }
            return instructions;
        }
    }
}
