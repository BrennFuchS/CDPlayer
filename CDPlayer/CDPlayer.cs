using MSCLoader;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using System.Linq;
using System.IO;

namespace CDplayer
{
    public class CDplayer : Mod
    {
        public override string ID => "CDplayerBase"; //Your mod ID (unique)
        public override string Name => "CDplayerBase"; //You mod name
        public override string Author => "BrennFuchS"; //Your Username
        public override string Version => "1.6"; //Version

        public override bool UseAssetsFolder => false;
        public override bool SecondPass => true;

        public static List<CD> CDs = new List<CD>();
        public FsmGameObject CDsongdatabase;
        public static Transform ItemPivot;

        static Settings otherAudioAutomaticImports = new Settings("oAAI", "other Audio Automatic Importing", false);
        static Settings importAudioFiles = new Settings("iAF", "Import Audio Files!", DoImport);

        public override void ModSettingsLoaded()
        {
            if ((bool)otherAudioAutomaticImports.GetValue()) DoImport();
        }

        static void DoImport()
        {
            if (ModLoader.GetCurrentScene() == CurrentScene.MainMenu) ImportAll();
            else ModConsole.Error("CDplayerBase: Importing Audio only works in the MainMenu!");
        }

        static void ImportAll()
        {
            GameObject.Find("Interface").transform.Find("Songs/button").gameObject.SetActive(false);

            var radio = GameObject.Find("Radio");
            var mainPath = Path.GetFullPath(".") + "\\";
            var CD = radio.transform.Find("CD").GetComponent<PlayMakerFSM>();

            radio.transform.Find("Folk").GetComponents<PlayMakerFSM>()[1].FsmVariables.GetFsmString("Path").Value = "RADIO IMPORTED";

            var text = GameObject.Find("Interface").transform.Find("Songs/Text").GetComponentsInChildren<TextMesh>();
            text[0].text = "CDplayerBase AUTO IMPORT STATUS:";
            text[1].text = "CDplayerBase AUTO IMPORT STATUS:";

            ImportAt(radio.transform.Find("Folk").GetComponents<PlayMakerArrayListProxy>()[0], mainPath + "Radio\\");
            CD.FsmVariables.GetFsmBool("CD1").Value = ImportAt(radio.transform.Find("CD").GetComponents<PlayMakerArrayListProxy>()[0], mainPath + "CD1\\");
            CD.FsmVariables.GetFsmBool("CD2").Value = ImportAt(radio.transform.Find("CD").GetComponents<PlayMakerArrayListProxy>()[1], mainPath + "CD2\\");
            CD.FsmVariables.GetFsmBool("CD3").Value = ImportAt(radio.transform.Find("CD").GetComponents<PlayMakerArrayListProxy>()[2], mainPath + "CD3\\");

            CD.FsmVariables.GetFsmBool("Import").Value = false;
        }
        static bool ImportAt(PlayMakerArrayListProxy proxy, string mainPath)
        {
            var paths = Directory.GetFiles(mainPath).Where(x => !x.Contains(".png"));

            if (paths.Count() <= 0) return false;
            else
            {
                var audios = new List<AudioClip>();
                if (proxy.preFillAudioClipList.Count > 0) proxy.preFillAudioClipList.Clear();
                for (var i = 0; i < paths.Count(); i++) audios.Add(AudioImport.LoadAudioFromFile(paths.ToArray()[i], true, true));
                for (var i = 0; i < audios.Count(); i++) proxy.preFillAudioClipList.Add(audios[i]);
                proxy._arrayList = new ArrayList(audios.Count());
                proxy._arrayList.AddRange(proxy.preFillAudioClipList);
                return true;
            }
        }

        public override void OnLoad()
        {
            CREATORSYSTEM.assetBundle = AssetBundle.CreateFromMemoryImmediate(Properties.Resources.cdplayer);
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

        public override void ModSettings()
        {
            Settings.AddCheckBox(this, otherAudioAutomaticImports);
            Settings.AddButton(this, importAudioFiles);
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
