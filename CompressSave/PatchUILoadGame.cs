﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DSP_Plugin
{
    class PatchUILoadGame
    {
        public static UIButton decompressButton;
        
        [HarmonyPatch(typeof(UILoadGameWindow), "OnSelectedChange"), HarmonyPostfix]
        static void OnSelectedChange(UILoadGameWindow __instance, UIButton ___loadButton, Text ___prop3Text)
        {
            _OnOpen(__instance, ___loadButton, ___prop3Text);
            decompressButton.button.interactable = ___prop3Text.text.Contains("(LZ4)");
        }

        [HarmonyPatch(typeof(UILoadGameWindow), "_OnOpen"), HarmonyPostfix]
        static void _OnOpen(UILoadGameWindow __instance, UIButton ___loadButton, Text ___prop3Text)
        {
            if (!decompressButton)
            {
                decompressButton = ___loadButton;

                decompressButton = (__instance.transform.Find("button-decompress")?.gameObject ?? GameObject.Instantiate(___loadButton.gameObject, ___loadButton.transform.parent)).GetComponent<UIButton>();

                decompressButton.gameObject.name = "button-decompress";
                decompressButton.transform.Translate(new Vector3(-1.5f, 0, 0));
                decompressButton.button.image.color = new Color32(0, 0xf4, 0x92, 0x77);
                var localizer = decompressButton.transform.Find("button-text")?.GetComponent<Localizer>();
                var text = decompressButton.transform.Find("button-text")?.GetComponent<Text>();

                if (localizer)
                {
                    localizer.stringKey = "解压/Decompress";
                    localizer.translation = "解压/Decompress";
                }

                if (text)
                {
                    text.text = "解压/Decompress";
                }

                decompressButton.onClick += _ =>{ 
                    if(___prop3Text.text.Contains("(LZ4)"))
                    {
                        if(PatchSave.DecompressSave(__instance.selected.saveName))
                        {
                            __instance.RefreshList();
                        }
                    }
                };
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