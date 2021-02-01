using MSCLoader;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using System.Linq;
using System.IO;
using System;

namespace CDplayer
{
    public class CDplayer : Mod
    {
        public override string ID => "CDplayerBase"; //Your mod ID (unique)
        public override string Name => "CDplayerBase"; //You mod name
        public override string Author => "BrennFuchS"; //Your Username
        public override string Version => "1.4"; //Version

        public override bool UseAssetsFolder => true;
        public override bool SecondPass => true;

        public static List<CD> CDs = new List<CD>();
        public FsmGameObject CDsongdatabase;
        public static Transform ItemPivot;

        public override void OnLoad()
        {
            CREATORSYSTEM.assetBundle = LoadAssets.LoadBundle(this, "cdplayer");
            ItemPivot = GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/1Hand_Assemble/ItemPivot");

            if (PlayMakerGlobals.Instance.Variables.GetFsmGameObject("SongDatabaseCD").Value != null)
            {
                CDsongdatabase = PlayMakerGlobals.Instance.Variables.GetFsmGameObject("SongDatabaseCD");
            }

            var CDsInScene = Resources.FindObjectsOfTypeAll<GameObject>().Where(x => x.name.Contains("cd(item"));
            for (var i = 0; i < CDsInScene.Count(); i++)
            {
                var thisCD = CDsInScene.ToArray()[i].AddComponent<CD>();
                var clips = new List<AudioClip>();

                switch (thisCD.name)
                {
                    case "cd(item1)":
                        clips.AddRange(CDsongdatabase.Value.GetComponents<PlayMakerArrayListProxy>()[0]._arrayList.ToArray().Select(x => (AudioClip)x));
                        thisCD.Clips = clips.ToArray();
                        thisCD.Part = thisCD.gameObject;
                        thisCD.ID = 1;
                        ModConsole.Print($"CD 1: {clips.Count} Tracks found");
                        break;
                    case "cd(item2)":
                        clips.AddRange(CDsongdatabase.Value.GetComponents<PlayMakerArrayListProxy>()[1]._arrayList.ToArray().Select(x => (AudioClip)x));
                        thisCD.Clips = clips.ToArray();
                        thisCD.Part = thisCD.gameObject;
                        thisCD.ID = 2;
                        ModConsole.Print($"CD 2: {clips.Count} Tracks found");
                        break;
                    case "cd(item3)":
                        clips.AddRange(CDsongdatabase.Value.GetComponents<PlayMakerArrayListProxy>()[2]._arrayList.ToArray().Select(x => (AudioClip)x));
                        thisCD.Clips = clips.ToArray();
                        thisCD.Part = thisCD.gameObject;
                        thisCD.ID = 3;
                        ModConsole.Print($"CD 3: {clips.Count} Tracks found");
                        break;
                }

                CDs.Add(thisCD);
            }

            ModConsole.Print($"CDplayerBase: CD List Length = {CDs.Count}");
        }

        public override void SecondPassOnLoad()
        {
            CREATORSYSTEM.assetBundle.Unload(false);
        }
    }

    public class CREATORSYSTEM : MonoBehaviour
    {
        public static GameObject CDPLAYER;
        public static CDHandler handler;
        public static CDPlayerFunctions functions;
        public static AssetBundle assetBundle;

        public static void AddCDplayer(GameObject SelectedVehicle, int Partname, bool RADIOCD, bool Channel)
        {
            CDPLAYER = GameObject.Instantiate<GameObject>(assetBundle.LoadAsset<GameObject>("CD_PLAYER.prefab"));
            CDPLAYER.transform.position = Vector3.zero;
            CDPLAYER.transform.eulerAngles = new Vector3(270f, 0f, 0f);
            handler = CDPLAYER.AddComponent<CDHandler>();
            handler.Partname = Partname;
            var col = CDPLAYER.AddComponent<SphereCollider>();
            col.radius = 0.1f;
            col.isTrigger = true;
            col.center = new Vector3(0f, -0.05f, 0f);
            functions = CDPLAYER.AddComponent<CDPlayerFunctions>();
            functions.RADIOCD = RADIOCD;
            functions.Channel = Channel;
            CDPLAYER.AddComponent<InteractionRaycast>();
            CDPLAYER.AddComponent<VolumeKnob>();
            CDPLAYER.transform.SetParent(SelectedVehicle.transform, false);
            CDPLAYER.transform.localEulerAngles = Vector3.zero;
            if (SelectedVehicle.transform.Find("Radio") != null) SelectedVehicle.transform.Find("Radio").gameObject.SetActive(false);
            if (SelectedVehicle.transform.Find("Speaker") != null) functions.sourcepivot = SelectedVehicle.transform.Find("Speaker");
            handler.GetCDfromLastSession();
            Component.DestroyImmediate(functions.sourcepivot.GetComponent<PlayMakerFSM>());
        }
        public static void AddVisualizer(List<Transform> TargetBones, float BoneStandardposition, List<Renderer> TargetRenderers, List<Light> TargetLights)
        {
            functions.audioVisualizer = CDPLAYER.AddComponent<BrennsAudioVisualizer>();
            functions.audioVisualizer.TargetBones = TargetBones;
            functions.audioVisualizer.BoneStandardposition = BoneStandardposition;
            functions.audioVisualizer.TargetRenderers = TargetRenderers;
            functions.audioVisualizer.TargetLights = TargetLights;
        }
    }
}
