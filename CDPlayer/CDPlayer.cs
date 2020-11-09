﻿using MSCLoader;
using UnityEngine;
using System.Collections.Generic;
using HutongGames.PlayMaker;

namespace CDplayer
{
    public class CDplayer : Mod
    {
        public override string ID => "CDplayerBase"; //Your mod ID (unique)
        public override string Name => "CDplayerBase"; //You mod name
        public override string Author => "BrennFuchS"; //Your Username
        public override string Version => "1.2"; //Version

        // Set this to true if you will be load custom assets from Assets folder.
        // This will create subfolder in Assets folder for your mod.
        public override bool UseAssetsFolder => true;
        public override bool SecondPass => true;

        public static List<CD> CDs = new List<CD>();
        public FsmGameObject CDsongdatabase;

        public override void OnLoad()
        {
            CREATORSYSTEM.assetBundle = LoadAssets.LoadBundle(this, "cdplayer");

            if (PlayMakerGlobals.Instance.Variables.GetFsmGameObject("SongDatabaseCD").Value != null)
            {
                CDsongdatabase = PlayMakerGlobals.Instance.Variables.GetFsmGameObject("SongDatabaseCD");
                ModConsole.Print("Got Song Database");
            }

            if (GameObject.Find("cd(item1)") != null)
            {
                CD thisCD = GameObject.Find("cd(item1)").AddComponent<CD>();
                List<AudioClip> clips = new List<AudioClip>();
                foreach(AudioClip clip in CDsongdatabase.Value.GetComponents<PlayMakerArrayListProxy>()[0]._arrayList)
                {
                    clips.Add(clip);
                }
                thisCD.Clips = clips.ToArray();
                thisCD.Part = GameObject.Find("cd(item1)");
                thisCD.ID = 1;

                CDs.Add(thisCD);

                ModConsole.Print("Got CD1");
            }

            if (GameObject.Find("cd(item2)") != null)
            {
                CD thisCD = GameObject.Find("cd(item2)").AddComponent<CD>();
                List<AudioClip> clips = new List<AudioClip>();
                foreach (AudioClip clip in CDsongdatabase.Value.GetComponents<PlayMakerArrayListProxy>()[1]._arrayList)
                {
                    clips.Add(clip);
                }
                thisCD.Clips = clips.ToArray();
                thisCD.Part = GameObject.Find("cd(item2)");
                thisCD.ID = 2;

                CDs.Add(thisCD);

                ModConsole.Print("Got CD2");
            }

            if (GameObject.Find("cd(item3)") != null)
            {
                CD thisCD = GameObject.Find("cd(item3)").AddComponent<CD>();
                List<AudioClip> clips = new List<AudioClip>();
                foreach (AudioClip clip in CDsongdatabase.Value.GetComponents<PlayMakerArrayListProxy>()[2]._arrayList)
                {
                    clips.Add(clip);
                }
                thisCD.Clips = clips.ToArray();
                thisCD.Part = GameObject.Find("cd(item3)");
                thisCD.ID = 3;

                CDs.Add(thisCD);

                ModConsole.Print("Got CD3");
            }

            ModConsole.Print($"CD List Length = {CDs.Count}");
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
            functions = CDPLAYER.AddComponent<CDPlayerFunctions>();
            functions.RADIOCD = RADIOCD;
            functions.Channel = Channel;
            CDPLAYER.AddComponent<InteractionRaycast>();
            CDPLAYER.AddComponent<VolumeKnob>();
            CDPLAYER.transform.SetParent(SelectedVehicle.transform, false);
            CDPLAYER.transform.localEulerAngles = Vector3.zero;
            if(SelectedVehicle.transform.Find("Radio") != null) SelectedVehicle.transform.Find("Radio").gameObject.SetActive(false);
            functions.sourcepivot = SelectedVehicle.transform.Find("Speaker");
            Component.Destroy(functions.sourcepivot.GetComponent<PlayMakerFSM>());
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