using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;
using System.Reflection.Emit;
using System.Reflection;
using Mono.Cecil;
namespace DSP_Plugin
{
    static class PatchUISaveGame
    {
        [HarmonyPatch(typeof(UISaveGameWindow), "_OnDestroy"), HarmonyPostfix]
        static void _OnDestroy()
        {
            //Console.WriteLine("OnCreate");
            context = new UIContext();
        }

        //[HarmonyPatch(typeof(UISaveGameWindow), "_OnRegEvent"), HarmonyPostfix]
        //static void _OnRegEvent()
        //{
        //    Console.WriteLine("OnRegEvent");
        //}
        //[HarmonyPatch(typeof(UISaveGameWindow), "_OnUnregEvent"), HarmonyPostfix]
        //static void _OnUnregEvent()
        //{
        //    Console.WriteLine("OnUnregEvent");
        //}

        static void Test()
        {
            var header = GameSave.ReadHeader("aa", true);
            if (header != null)
                if (header is CompressionGameSaveHeader)
                    "b".Translate();
                else
                    "a".Translate();
            "d.A".Translate();
            return;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UISaveGameWindow), "OnSelectedChange")]
        [HarmonyPatch(typeof(UILoadGameWindow), "OnSelectedChange")]
        static IEnumerable<CodeInstruction> MyTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var ReadHeader = typeof(GameSave).GetMethod("ReadHeader", BindingFlags.Static|BindingFlags.Public);
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
                        new CodeInstruction(OpCodes.Isinst,typeof(CompressionGameSaveHeader)),
                        new CodeInstruction(OpCodes.Brfalse_S,iffalse),
                        new CodeInstruction(OpCodes.Ldstr,"(LZ4)#,##0"),
                        new CodeInstruction(OpCodes.Br_S,callLabel),
                    };
                    codes.InsertRange(i, IL);
                    //for(int j = i -1; j< i+IL.Count + 2;j++)
                    //{
                    //    Console.WriteLine(codes[j]);
                    //}
                    break;
                }
            }

            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(UISaveGameWindow), "OnSaveClick"), HarmonyReversePatch]
        public static void OSaveGameAs(this UISaveGameWindow ui, int data) { }

        [HarmonyPatch(typeof(UISaveGameWindow), "CheckAndSetSaveButtonEnable"), HarmonyPostfix]
        static void CheckAndSetSaveButtonEnable(UISaveGameWindow __instance, UIButton ___saveButton, Text ___saveButtonText)
        {
            _OnOpen(__instance, ___saveButton, ___saveButtonText);
            if (context.saveButtonText && context.saveButton)
                SetButtonState(context.saveButtonText.text, context.saveButton.button.interactable);
        }

        static void SetButtonState(string text, bool interactable)
        {
            context.buttonCompress.button.interactable = interactable;
            context.buttonCompressText.text = text;
        }

        class UIContext
        {
            public UIButton buttonCompress;
            public UIButton saveButton;
            public Text buttonCompressText;
            public Text saveButtonText;
            public UISaveGameWindow ui;
        }

        [HarmonyPatch(typeof(UISaveGameWindow), "OnSaveClick"), HarmonyPrefix]
        static void OnSaveClick()
        {
            PatchSave.UseCompressSave = true;
        }

        static UIContext context = new UIContext();

        [HarmonyPatch(typeof(UISaveGameWindow), "_OnOpen"), HarmonyPostfix]
        static void _OnOpen(UISaveGameWindow __instance,  UIButton ___saveButton, Text ___saveButtonText)
        {
            if (!context.buttonCompress)
            {
                context.saveButton = ___saveButton;
                context.saveButtonText = ___saveButtonText;

                context.ui = __instance;
                context.buttonCompress = (__instance.transform.Find("button-compress")?.gameObject??GameObject.Instantiate(___saveButton.gameObject, ___saveButton.transform.parent)).GetComponent<UIButton>();
                
                context.buttonCompress.gameObject.name = "button-compress";
                context.buttonCompress.transform.Translate(new Vector3(-1.5f, 0, 0));
                context.buttonCompress.button.image.color = new Color32(0xfc,0x6f,00,0x77);
                context.buttonCompressText = context.buttonCompress.transform.Find("button-text")?.GetComponent<Text>();

                context.buttonCompress.onClick += __instance.OnSaveClick;
                context.saveButton.onClick -= __instance.OnSaveClick;
                context.saveButton.onClick += WrapClick;
            }
        }

        static void WrapClick(int data)
        {
            PatchSave.UseCompressSave = false;
            context.ui.OSaveGameAs(data);
        }


        public static void OnDestroy()
        {
            if (context.buttonCompress)
                GameObject.Destroy(context.buttonCompress.gameObject);
            if (context.ui)
            {
                context.saveButton.onClick -= WrapClick;
                context.saveButton.onClick += context.ui.OnSaveClick;
            }
            _OnDestroy();
        }
    }

}
