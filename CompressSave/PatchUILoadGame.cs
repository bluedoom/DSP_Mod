using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection.Emit;
using System.Reflection;

namespace DSP_Plugin
{
    class PatchUILoadGame
    {
        static UIButton decompressButton;

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UISaveGameWindow), "OnSelectedChange")]
        [HarmonyPatch(typeof(UILoadGameWindow), "OnSelectedChange")]
        static IEnumerable<CodeInstruction> OnSelectedChange_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var ReadHeader = typeof(GameSave).GetMethod("ReadHeader", BindingFlags.Static | BindingFlags.Public);
            if (ReadHeader == null) return instructions;
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode == OpCodes.Ldstr && code.OperandIs("#,##0"))
                {
                    var iffalse = generator.DefineLabel();
                    var callLabel = generator.DefineLabel();
                    code.WithLabels(iffalse)
                        .operand = "(N)#,##0";
                    codes[i + 1].WithLabels(callLabel);
                    var IL = new List<CodeInstruction> {
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompressionGameSaveHeader),"IsCompressed")),
                        new CodeInstruction(OpCodes.Brfalse_S,iffalse),
                        new CodeInstruction(OpCodes.Ldstr,"(LZ4)#,##0"),
                        new CodeInstruction(OpCodes.Br_S,callLabel),
                    };
                    codes.InsertRange(i, IL);
                    break;
                }
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(UILoadGameWindow), "OnSelectedChange"), HarmonyPostfix]
        static void OnSelectedChange(UILoadGameWindow __instance, UIButton ___loadButton, Text ___prop3Text)
        {
            bool compressedSave = (___prop3Text != null &&___prop3Text.text.Contains("LZ4")) || (___loadButton.button.interactable == false && SaveUtil.IsCompressedSave(__instance.selected?.saveName));
            if (decompressButton)
                decompressButton.button.interactable = compressedSave;
        }

        [HarmonyPatch(typeof(UILoadGameWindow), "_OnOpen"), HarmonyPostfix]
        static void _OnOpen(UILoadGameWindow __instance, UIButton ___loadButton, List<UIGameSaveEntry> ___entries)
        {
            if (!decompressButton)
            {
                decompressButton = ___loadButton;

                decompressButton = (__instance.transform.Find("button-decompress")?.gameObject ?? GameObject.Instantiate(___loadButton.gameObject, ___loadButton.transform.parent)).GetComponent<UIButton>();

                decompressButton.gameObject.name = "button-decompress";
                decompressButton.transform.Translate(new Vector3(-2.0f, 0, 0));
                decompressButton.button.image.color = new Color32(0, 0xf4, 0x92, 0x77);
                var localizer = decompressButton.transform.Find("button-text")?.GetComponent<Localizer>();
                var text = decompressButton.transform.Find("button-text")?.GetComponent<Text>();

                if (localizer)
                {
                    localizer.stringKey = "解压/Decompress".Translate();
                    localizer.translation = "解压/Decompress".Translate();
                }
                if (text)
                    text.text = "解压/Decompress".Translate();

                decompressButton.onClick += _ =>{ 
                    if(SaveUtil.DecompressSave(__instance.selected.saveName, out var newfileName))
                    {
                        __instance.RefreshList();
                        __instance.selected = ___entries.First(e => e.saveName == newfileName);
                    }
                };
                decompressButton.button.interactable = false;
            }
        }
        
        public static void OnDestroy()
        {
            if (decompressButton)
                GameObject.Destroy(decompressButton.gameObject);
            decompressButton = null;
        }
    }
}
