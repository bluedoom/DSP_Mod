using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;

namespace DSP_Plugin
{
    class PatchUISaveGame
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
        
        [HarmonyPatch(typeof(UISaveGameWindow), "CheckAndSetSaveButtonEnable"), HarmonyPostfix]
        static void CheckAndSetSaveButtonEnable(UISaveGameWindow __instance, UIButton ___saveButton, Text ___saveButtonText)
        {

            //if (string.IsNullOrEmpty(context.nameInput.text))
            //{
            //    SetButtonState("名称为空".Translate(), false);
            //}
            //else if (context.nameInput.text.Equals(GameSave.AutoSaveTmp) ||
            //        (!context.nameInput.text.IsValidFileName(false)) ||
            //    context.nameInput.text.IndexOf("autosave") >= 0 || context.nameInput.text.IndexOf("lastexit") >= 0)
            //{
            //    SetButtonState("无效名称".Translate(), false);
            //}
            //else
            //{
            //    context.buttonCompress.button.interactable = true;
            //}
            _OnOpen(__instance, ___saveButton, ___saveButtonText);
            if (context.saveButtonText && context.saveButton)
                SetButtonState(context.saveButtonText.text, context.saveButton.button.interactable);
        }

        static void SetButtonState(string text,bool interactable)
        {
            context.buttonCompress.button.interactable = interactable;
            context.buttonCompressText.text = text;
        }

        class UIContext
        {
            public UIButton buttonCompress;
            public UIButton saveButton;
            //public InputField nameInput;
            public Text buttonCompressText;
            public Text saveButtonText;
            public UISaveGameWindow ui;
        }

        static UIContext context = new UIContext();

        [   
            HarmonyPatch(typeof(UISaveGameWindow), "_OnOpen"),
            HarmonyPostfix]
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
                context.buttonCompress.button.image.color = new Color32(0xbc,0x6f,00,255);
                context.buttonCompressText = context.buttonCompress.transform.Find("button-text")?.GetComponent<Text>();
                //context.nameInput = __instance.transform.Find("input-filename/InputField")?.GetComponent<InputField>();

                context.buttonCompress.onClick += _=> {
                    PatchSave.UseCompressSave = true;
                    __instance.OnSaveClick(0);
                };
                context.saveButton.onClick -= __instance.OnSaveClick;
                context.saveButton.onClick += WrapClick;
            }
        }

        static void WrapClick(int data)
        {
            PatchSave.UseCompressSave = false;
            context.ui?.OnSaveClick(data);
        }

        //public static void SaveGameAs(UISaveGameWindow ui)
        //{
        //    if (context.buttonCompress.button.interactable)
        //    {
        //        var nameInput = context.nameInput;
        //        var buttonText = context.buttonCompressText;
        //        if (nameInput == null)
        //        {
        //            UIMessageBox.Show("保存游戏失败".Translate(), "需要升级插件/Need to Upgrade Plugin", "确定".Translate(), 3);
        //            return;
        //        }
        //        //ui.inputEventLock = true;
        //        nameInput.text = nameInput.text.ValidFileName();
        //        nameInput.text = nameInput.text.Trim();
        //        //ui.inputEventLock = false;
        //        if (string.IsNullOrEmpty(nameInput.text))
        //        {
        //            return;
        //        }
        //        if (nameInput.text.Equals(GameSave.AutoSaveTmp))
        //        {
        //            return;
        //        }
        //        if (!nameInput.text.IsValidFileName(false))
        //        {
        //            return;
        //        }
        //        if (nameInput.text.IndexOf("autosave") >= 0 || nameInput.text.IndexOf("lastexit") >= 0)
        //        {
        //            context.buttonCompress.button.interactable = false;
        //            buttonText.text = "无效名称".Translate();
        //            return;
        //        }
        //        void SaveGame()
        //        {
        //            if (nameInput.text.IndexOf("autosave") < 0 && nameInput.text.IndexOf("lastexit") < 0)
        //            {
        //                GameMain.gameName = nameInput.text;
        //            }
        //            if (PatchSave.Save(nameInput.text))
        //            {
        //                UIAutoSave.lastSaveTick = GameMain.gameTick;
        //                UIMessageBox.Show("保存游戏".Translate(), "游戏保存成功".Translate(), "确定".Translate(), 0, new UIMessageBox.Response(ui._Close));
        //            }
        //            else
        //            {
        //                UIMessageBox.Show("保存游戏失败".Translate(), "路径和权限".Translate(), "确定".Translate(), 3);
        //            }
        //        }
        //        if (ui.selected != null && !GameMain.gameName.Equals(nameInput.text))
        //        {
        //            UIMessageBox.Show("覆盖存档".Translate(), string.Format("是否覆盖存档".Translate(), nameInput.text), "取消".Translate(), "覆盖".Translate(), 2, null, new UIMessageBox.Response(SaveGame));
        //        }
        //        else
        //        {
        //            SaveGame();
        //        }
        //    }
        //}

        public static void OnDestroy()
        {
            if (context.buttonCompress)
                GameObject.Destroy(context.buttonCompress);
            if (context.ui)
            {
                context.saveButton.onClick -= WrapClick;
                context.saveButton.onClick += context.ui.OnSaveClick;
            }
            _OnDestroy();
        }
    }

}
