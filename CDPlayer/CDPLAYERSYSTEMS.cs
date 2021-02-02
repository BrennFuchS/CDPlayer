using MSCLoader;
using UnityEngine;
using HutongGames.PlayMaker;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CDplayer
{
    public class VolumeKnob : MonoBehaviour
    {
        public Transform knob;
        public Collider knobCollider;

        public float Volume = 0f;

        public InteractionRaycast Raycast;
        public CDPlayerFunctions Functions;

        public int KnobState = 0;

        private FsmBool guiUse;
        private FsmString guiInteraction;
        string interaction = "ON/OFF/VOLUME";

        string mouseWheel = "Mouse ScrollWheel";
        float inputDelay = 0f;
        public float volumeDelay = 0f;

        public void Start()
        {
            Raycast = GetComponent<InteractionRaycast>();
            Functions = GetComponent<CDPlayerFunctions>();

            knob = transform.Find("Pivot/knob");
            knobCollider = transform.Find("ButtonsCD/RadioVolume").GetComponent<SphereCollider>();

            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");

            knob.localEulerAngles = new Vector3(0f, -30 * KnobState, 0f);
        }

        public void Update()
        {
            Volume = KnobState / 10f;
            inputDelay -= Time.deltaTime;
            volumeDelay -= Time.deltaTime;

            if (Raycast.GetHit(knobCollider))
            {
                guiUse.Value = true;
                guiInteraction.Value = interaction;

                if (KnobState < 10f && Input.GetAxis(mouseWheel) < 0f && inputDelay <= 0f)
                {
                    KnobState++;
                    knob.localEulerAngles = new Vector3( 0f, -30 * KnobState, 0f);
                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "cd_button");
                    inputDelay = 0.1f;
                    volumeDelay = 0.2f;
                }
                else if (KnobState > 0f && Input.GetAxis(mouseWheel) > 0f && inputDelay <= 0f)
                {
                    KnobState--;
                    knob.localEulerAngles = new Vector3( 0f, -30 * KnobState, 0f);
                    inputDelay = 0.1f;
                    volumeDelay = 0.2f;
                }
            }
        }
    }

    public class CDPlayerFunctions : MonoBehaviour
    {
        //Radio
        public Transform Channels;
        public Transform Radio;
        public PlayMakerFSM RadioChannelFSM;
        public bool FoundRadio = false;

        public Collider RadioCDSwitch;
        public string InteractionRadioCD = "RADIO / CD";
        public bool RADIOCD = true; //Needs Saving

        public Collider TrackChannelSwitch;
        public string InteractionTrackChannel1 = "RADIO CHANNEL";
        public string InteractionTrackChannel2 = "NEXT/PREVIOUS SONG";

        public Collider Eject;
        public string InteractionEject = "EJECT CD";

        public TextMesh LCD;

        public InteractionRaycast Raycast;

        public AudioSource cdAudio;

        public Animation Sled;

        public CDHandler handler;

        public PlayMakerFSM Hand;

        private FsmBool guiUse;
        private FsmString guiInteraction;

        public bool ON = false;
        public bool Channel = false; //Needs Saving
        public bool ChangedChannel = true;

        public bool CD = false;

        public VolumeKnob Volumeknob;
        private bool ResetChannelParent = false;

        public AudioClip[] clips;

        public int PlayingTrack = 0;
        public int LastTrack = 0;
        public float TrackStoppedAt = 0f;

        public float inputDelay = 0f;
        public bool readytoplay = false;
        public bool waitforreaction = false;

        public float displaydelay = 0f;

        public Transform sourcepivot;
        public bool CDInjected = false;

        public BrennsAudioVisualizer audioVisualizer;

        public TimeSpan currentTime;

        public void Start()
        {
            Radio = GameObject.Find("RADIO").transform;

            guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
            guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");

            Hand = GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/1Hand_Assemble/Hand").GetComponent<PlayMakerFSM>();

            RadioCDSwitch = transform.Find("ButtonsCD/RadioCDSwitch").GetComponent<SphereCollider>();
            TrackChannelSwitch = transform.Find("ButtonsCD/TrackChannelSwitch").GetComponent<SphereCollider>();
            Eject = transform.Find("ButtonsCD/Eject").GetComponent<SphereCollider>();
            LCD = transform.Find("LCD").GetComponent<TextMesh>();
            Raycast = GetComponent<InteractionRaycast>();
            cdAudio = transform.Find("Speaker").GetComponent<AudioSource>();
            Sled = transform.Find("Sled/cd_sled_pivot").GetComponent<Animation>();
            Volumeknob = GetComponent<VolumeKnob>();
            handler = GetComponent<CDHandler>();

            GetRadio();

            cdAudio.transform.SetParent(sourcepivot, false);

            if (GetComponent<BrennsAudioVisualizer>() != null) audioVisualizer = GetComponent<BrennsAudioVisualizer>();
        }

        public void Update()
        {
            if (waitforreaction)
            {
                if (handler.AnchorCD != null && Hand.FsmVariables.GetFsmGameObject("PickedObject").Value.name == handler.Part.name)
                {
                    handler.cantakeCD = true;
                    FixedJoint.Destroy(handler.AnchorCD);
                }
                for (var i = 0; i < CDplayer.CDs.Count; i++)
                {
                    CD cd = CDplayer.CDs[i];

                    if (handler.AnchorCD == null && handler.Part.gameObject.name == cd.Part.name)
                    {
                        waitforreaction = false;
                    }
                }
            }
            if (Volumeknob.Volume >= 0.1f)
            {
                ON = true;
                GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(1f, 1f, 1f, 1f));
            }
            else if (Volumeknob.Volume == 0)
            {
                if (TrackStoppedAt == 0f) TrackStoppedAt = cdAudio.time;
                LastTrack = PlayingTrack;
                cdAudio.Stop();
                ON = false;
                ChangedChannel = true;
                GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0f, 0f, 0f, 0f));
                LCD.text = "";
            }
            if (RADIOCD && ON && audioVisualizer != null)
            {
                if (Channel)
                {
                    audioVisualizer.source = Channels.Find("Channel1").GetComponent<AudioSource>();
                }
                else
                {
                    audioVisualizer.source = Channels.Find("Folk").GetComponent<AudioSource>();
                }
            }
            else if (!RADIOCD && ON && audioVisualizer != null)
            {
                if (CD)
                {
                    audioVisualizer.source = cdAudio;
                }
                else
                {
                    audioVisualizer.source = null;
                }
            }
            else if (!ON && audioVisualizer != null) audioVisualizer.source = null;
            RadioTuner();
            if (ON && FoundRadio)
            {
                CDPlayer();
                Buttons();
                Display();
            }
        }   

        void Buttons()
        {
            inputDelay -= Time.deltaTime;
            //RadioCDSwitch
            if (Raycast.GetHit(RadioCDSwitch))
            {
                guiUse.Value = true;
                guiInteraction.Value = InteractionRadioCD;

                if(Input.GetMouseButtonDown(0) && inputDelay <= 0f)
                {
                    if (RADIOCD)
                    {
                        RADIOCD = false;
                        displaydelay = 0.95f;
                    }
                    else
                    {
                        RADIOCD = true;
                        ChangedChannel = true;
                    }

                    MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "cd_button");
                    inputDelay = 0.02f;
                }
            }
            //TrackChannelSwitch
            if (Raycast.GetHit(TrackChannelSwitch))
            {
                guiUse.Value = true;

                if (RADIOCD)
                {
                    guiInteraction.Value = InteractionTrackChannel1;

                    if (Input.GetMouseButtonDown(0) && inputDelay <= 0f)
                    {
                        if (Channel)
                        {
                            Channel = false;
                            ChangedChannel = true;
                        }
                        else
                        {
                            Channel = true;
                            ChangedChannel = true;
                        }

                        MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "cd_button");
                        inputDelay = 0.02f;
                    }
                }
                else if (!RADIOCD && CD)
                {
                    guiInteraction.Value = InteractionTrackChannel2;

                    if (Input.GetMouseButtonDown(0) && inputDelay <= 0f)
                    {
                        if (PlayingTrack == clips.Length) PlayingTrack = 0;
                        else PlayingTrack++;
                        cdAudio.clip = clips[PlayingTrack];
                        cdAudio.Play();

                        MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "cd_button");
                        inputDelay = 0.02f;                      
                    }
                    else if (Input.GetMouseButtonDown(1) && inputDelay <= 0f)
                    {
                        if (cdAudio.time >= 5f)
                        {
                            cdAudio.Play();
                        }
                        else
                        {
                            if (PlayingTrack == 0) PlayingTrack = clips.Length - 1;
                            else PlayingTrack--;
                            cdAudio.clip = clips[PlayingTrack];
                            cdAudio.Play();
                        }

                        MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "cd_button");
                        inputDelay = 0.02f;
                    }
                }
            }
            //Eject
            if (Raycast.GetHit(Eject))
            {
                if (CD)
                {
                    guiUse.Value = true;
                    guiInteraction.Value = InteractionEject;

                    if (Input.GetMouseButtonDown(0) && inputDelay <= 0f)
                    {
                        StartCoroutine(SledOut());

                        MasterAudio.PlaySound3DAndForget("CarFoley", transform, variationName: "cd_button");
                        inputDelay = 0.02f;
                    }
                }
            }
        }

        void Display()
        {
            if (Volumeknob.volumeDelay <= 0f)
            {
                if (RADIOCD)
                {
                    if (Channel) LCD.text = "ALIVIESKAN";
                    else LCD.text = "TOIVERADIO";
                }
                else
                {
                    if (CD)
                    {
                        displaydelay -= Time.deltaTime;

                        if (displaydelay >= 0.4f) LCD.text = "READING";
                        else if (displaydelay >= 0.05f && !cdAudio.isPlaying) LCD.text = "PLAYING";
                        else if (displaydelay <= 0f && !cdAudio.isPlaying) LCD.text = $"{(PlayingTrack + 1).ToString("00")} - {currentTime.Minutes.ToString("00")}:{currentTime.Seconds.ToString("00")}";
                    }
                    else LCD.text = "NO CD";
                }
            }
            else if (Volumeknob.KnobState > 0 && Volumeknob.volumeDelay >= 0.01f)
            {
                LCD.text = $"VOL {(Volumeknob.Volume * 10).ToString("00")}";
                if (!RADIOCD && CD) displaydelay = 0.95f;
            }
            else if (Volumeknob.KnobState == 0) LCD.text = "";
        }

        void RadioTuner()
        {
            if (ON && RADIOCD)
            {
                if (TrackStoppedAt == 0f) TrackStoppedAt = cdAudio.time;
                LastTrack = PlayingTrack;
                cdAudio.Stop();

                ResetChannelParent = false;
                RadioChannelFSM.FsmVariables.GetFsmFloat("Volume").Value = Volumeknob.Volume;
                RadioChannelFSM.FsmVariables.GetFsmFloat("_DistortionLevel").Value = 0.5f;
                RadioChannelFSM.FsmVariables.GetFsmBool("OnStatic").Value = false;
                RadioChannelFSM.FsmVariables.GetFsmBool("OnStaticAntenna").Value = false;
                RadioChannelFSM.FsmVariables.GetFsmBool("OnStaticAntenna1").Value = false;
                RadioChannelFSM.FsmVariables.GetFsmBool("_HighPass").Value = false;
                RadioChannelFSM.FsmVariables.GetFsmBool("_LowPass").Value = true;
                RadioChannelFSM.FsmVariables.GetFsmGameObject("Parent").Value = gameObject;

                if (Channel && ChangedChannel)
                {
                    Channels.SetParent(sourcepivot, false);
                    RadioChannelFSM.enabled = true;
                    RadioChannelFSM.FsmVariables.GetFsmBool("OnMuteChannel1").Value = false;
                    RadioChannelFSM.FsmVariables.GetFsmBool("OnMuteFolk").Value = true;
                    ChangedChannel = false;
                    if (audioVisualizer != null) audioVisualizer.source = Channels.Find("Channel1").GetComponent<AudioSource>();
                }
                else if (!Channel && ChangedChannel)
                {                 
                    Channels.SetParent(sourcepivot, false);
                    RadioChannelFSM.enabled = true;
                    RadioChannelFSM.FsmVariables.GetFsmBool("OnMuteChannel1").Value = true;
                    RadioChannelFSM.FsmVariables.GetFsmBool("OnMuteFolk").Value = false;
                    ChangedChannel = false;
                    if (audioVisualizer != null) audioVisualizer.source = Channels.Find("Folk").GetComponent<AudioSource>();
                }
            }
            else if (!ON && !ResetChannelParent || !RADIOCD && !ResetChannelParent) OGChannelParent();
        }

        void OGChannelParent()
        {
            Channels.SetParent(Radio, false);
            RadioChannelFSM.enabled = false;
            ResetChannelParent = true;
        }

        void CDPlayer()
        {
            if (ON && !RADIOCD && handler.Part != null && CD && displaydelay <= 0.4f)
            {
                cdAudio.volume = Volumeknob.Volume;
                if (audioVisualizer != null) audioVisualizer.source = cdAudio;
                if (cdAudio.isPlaying == false)
                {
                    if (CDplayer.CDs.FirstOrDefault(CD => CD.ID == handler.Partname).Part.transform == handler.Part)
                    {
                        if (cdAudio.isPlaying == false)
                        {
                            readytoplay = false;
                            if (TrackStoppedAt != 0f && !readytoplay || CDInjected && !readytoplay)
                            {
                                if (PlayingTrack != clips.Length)
                                {
                                    if (CDInjected)
                                    {
                                        PlayingTrack = 0;
                                        readytoplay = true;
                                        CDInjected = false;
                                    }
                                    else
                                    {
                                        PlayingTrack = LastTrack;
                                        readytoplay = true;
                                    }
                                }
                            }
                            if (TrackStoppedAt == 0f && !readytoplay && !CDInjected)
                            {
                                if (PlayingTrack != clips.Length)
                                {
                                    PlayingTrack += 1;
                                    readytoplay = true;
                                }
                                else if (PlayingTrack == clips.Length)
                                {
                                    PlayingTrack = 0;
                                    readytoplay = true;
                                }
                            }
                        }
                        if (CDplayer.CDs.FirstOrDefault(CD => CD.ID == handler.Partname).Part.transform == handler.Part)
                        {
                            if (TrackStoppedAt != 0f && readytoplay)
                            {
                                cdAudio.clip = clips[PlayingTrack];
                                cdAudio.Play();
                                cdAudio.time = TrackStoppedAt;
                                TrackStoppedAt = 0f;
                            }
                            else if (TrackStoppedAt == 0f && readytoplay)
                            {
                                cdAudio.clip = clips[PlayingTrack];
                                cdAudio.Play();
                                cdAudio.time = 0f;
                            }
                        }
                    }
                }
                else currentTime = TimeSpan.FromSeconds(cdAudio.time);
            }         
            if (!ON || RADIOCD)
            {
                if(TrackStoppedAt == 0) TrackStoppedAt = cdAudio.time;
                LastTrack = PlayingTrack;
            }
        }

        IEnumerator SledOut()
        {
            CD = false;
            TrackStoppedAt = 0f;
            PlayingTrack = 0;
            cdAudio.Stop();
            handler.Part.SetParent(handler.sledpivot, false);
            handler.Part.localPosition = Vector3.zero;
            handler.Part.gameObject.SetActive(true);
            Sled.Play("cd_sled_out", PlayMode.StopAll);

            yield return new WaitForSeconds(1.2f);

            handler.Part.gameObject.tag = "PART";
            handler.Part.gameObject.layer = LayerMask.NameToLayer("Parts");
            handler.Part.GetComponent<BoxCollider>().enabled = true;

            waitforreaction = true;

            yield return null;
            yield break;
        }

        void GetRadio()
        {
            if (GameObject.Find("RADIO").transform.Find("RadioChannels") != null)
            {
                Channels = GameObject.Find("RADIO").transform.Find("RadioChannels");
                RadioChannelFSM = Channels.GetComponent<PlayMakerFSM>();
                FoundRadio = true;
            }
            else if (GameObject.Find("KEKMET(350-400psi)").transform.Find("RadioPivot/Speaker/RadioChannels") != null)
            {
                Channels = GameObject.Find("KEKMET(350-400psi)").transform.Find("RadioPivot/Speaker/RadioChannels");
                RadioChannelFSM = Channels.GetComponent<PlayMakerFSM>();
                FoundRadio = true;
            }
            else if (GameObject.Find("GIFU(750/450psi)").transform.Find("RadioPivot/Speaker/RadioChannels") != null)
            {
                Channels = GameObject.Find("GIFU(750/450psi)").transform.Find("RadioPivot/Speaker/RadioChannels");
                RadioChannelFSM = Channels.GetComponent<PlayMakerFSM>();
                FoundRadio = true;
            }
            else if (GameObject.Find("ITEMS").transform.Find("radio(itemx)/Speaker/RadioChannels") != null)
            {
                Channels = GameObject.Find("ITEMS").transform.Find("radio(itemx)/Speaker/RadioChannels");
                RadioChannelFSM = Channels.GetComponent<PlayMakerFSM>();
                FoundRadio = true;
            }
            else if (GameObject.Find("JAIL").transform.Find("radio(Clone)/Speaker/RadioChannels") != null)
            {
                Channels = GameObject.Find("JAIL").transform.Find("radio(Clone)/Speaker/RadioChannels");
                RadioChannelFSM = Channels.GetComponent<PlayMakerFSM>();
                FoundRadio = true;
            }
            else if (GameObject.Find("FERNDALE(1630kg)").transform.Find("RadioPivot/Speaker/RadioChannels") != null)
            {
                Channels = GameObject.Find("FERNDALE(1630kg)").transform.Find("RadioPivot/Speaker/RadioChannels");
                RadioChannelFSM = Channels.GetComponent<PlayMakerFSM>();
                FoundRadio = true;
            }
            else if (GameObject.Find("HAYOSIKO(1500kg, 250)").transform.Find("RadioPivot/Speaker/RadioChannels") != null)
            {
                Channels = GameObject.Find("HAYOSIKO(1500kg, 250)").transform.Find("RadioPivot/Speaker/RadioChannels");
                RadioChannelFSM = Channels.GetComponent<PlayMakerFSM>();
                FoundRadio = true;
            }
            else if (GameObject.Find("SATSUMA(557kg, 248)").transform.Find("Electricity/SpeakerBass/RadioChannels") != null)
            {
                Channels = GameObject.Find("SATSUMA(557kg, 248)").transform.Find("Electricity/SpeakerBass/RadioChannels");
                RadioChannelFSM = Channels.GetComponent<PlayMakerFSM>();
                FoundRadio = true;
            }
            else if (GameObject.Find("SATSUMA(557kg, 248)").transform.Find("Electricity/SpeakerDash/RadioChannels") != null)
            {
                Channels = GameObject.Find("SATSUMA(557kg, 248)").transform.Find("Electricity/SpeakerDash/RadioChannels");
                RadioChannelFSM = Channels.GetComponent<PlayMakerFSM>();
                FoundRadio = true;
            }
        }
    }

    public class CD : MonoBehaviour
    {
        public GameObject Part;
        public int ID;
        public AudioClip[] Clips;
    }

    public class CDHandler : MonoBehaviour
    {
        public int CDint;

        public Transform sledpivot;
        public Transform trigger;

        public Transform Part;
        public int Partname = 0; //Needs Saving

        public bool cantakeCD = true;

        public FixedJoint AnchorCD;

        public CDPlayerFunctions functions;

        FsmBool guiAssemble;

        private void Start()
        {
            guiAssemble = FsmVariables.GlobalVariables.GetFsmBool("GUIassemble");
            functions = GetComponent<CDPlayerFunctions>();
            sledpivot = transform.Find("Sled/cd_sled_pivot");
            trigger = transform.Find("trigger_disc");   
        }

        public void GetCDfromLastSession()
        {
            if (Partname != 0)
            {
                CD cd = CDplayer.CDs.FirstOrDefault(x => x.ID == Partname);

                cd.Part.transform.SetParent(sledpivot, false);
                cd.Part.transform.localEulerAngles = Vector3.zero;
                cd.Part.tag = "Untagged";
                cd.Part.layer = LayerMask.NameToLayer("Default");
                Part = cd.Part.transform;
                CDint = cd.ID;
                StartCoroutine(SledIn());
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (cantakeCD && trigger.childCount == 0 && functions.ON)
            {
                CD cd = null;
                if(other.GetComponent<CD>() != null) cd = other.GetComponent<CD>();
                if (cd != null)
                {
                    guiAssemble.Value = true;

                    if (other.transform.parent == CDplayer.ItemPivot && Input.GetMouseButtonDown(0))
                    {
                        cd.Part.transform.SetParent(sledpivot, false);
                        cd.Part.transform.localEulerAngles = Vector3.zero;
                        cd.Part.tag = "Untagged";
                        cd.Part.layer = LayerMask.NameToLayer("Default");
                        functions.clips = cd.Clips;
                        Part = cd.Part.transform;
                        CDint = cd.ID;
                        guiAssemble.Value = false;
                        StartCoroutine(SledIn());
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            CD cd = null;
            if (other.GetComponent<CD>() != null) cd = other.GetComponent<CD>();
            if (cd.ID == CDint)
            {
                Part = null;
                CDint = 0;
                cantakeCD = true;
            }
        }

        IEnumerator SledIn()
        {
            functions.Hand.FsmVariables.GetFsmBool("HandEmpty").Value = true;
            functions.Hand.FsmVariables.GetFsmGameObject("PickedObject").Value = null;
            functions.Hand.FsmVariables.GetFsmGameObject("ItemPivot").Value.transform.DetachChildren();
            functions.Hand.FsmVariables.GetFsmGameObject("ItemPivot").Value.transform.parent.parent.Find("SelectItem").gameObject.SetActive(true);
            functions.Hand.enabled = false;
            Part.SetParent(sledpivot);
            Part.gameObject.SetActive(false);
            Part.localEulerAngles = Vector3.zero;
            Part.localPosition = Vector3.zero;
            if (AnchorCD == null)
            {
                AnchorCD = Part.gameObject.AddComponent<FixedJoint>();
                AnchorCD.connectedBody = sledpivot.GetComponent<Rigidbody>();
            }
            Part.gameObject.SetActive(true);
            Part.GetComponent<BoxCollider>().enabled = false;
            Part.tag = "Untagged";
            Part.gameObject.layer = LayerMask.NameToLayer("Default");
            cantakeCD = false;
            Partname = CDint;
            functions.Hand.enabled = true;
            functions.Sled.Play("cd_sled_in", PlayMode.StopAll);
            functions.CDInjected = true;
            functions.PlayingTrack = 0;

            yield return new WaitForSeconds(1.2f);

            functions.displaydelay = 0.95f;
            Part.gameObject.SetActive(false);
            functions.CD = true;

            yield return null;
            yield break;
        }
    }

    public class BrennsAudioVisualizer : MonoBehaviour
    {
        public AudioSource source;
        public List<Transform> TargetBones;
        public List<Renderer> TargetRenderers;
        public List<Light> TargetLights;
        public Color EmissionColor;
        public float BoneStandardposition;
        private float rawBeatstrength;
        public float Beatstrength;
        public float[] samples = new float[2];

        public void Update()
        {
            if (source != null)
            {
                source.GetOutputData(samples, 0);
                source.GetOutputData(samples, 1);

                rawBeatstrength = samples[0] + samples[1] / 2f;

                if (source.volume > 0f)
                {
                    Beatstrength = rawBeatstrength / 175f * source.volume;
                    EmissionColor = new Color(Beatstrength * 500f, 0f, 0f, 1f);
                }
                else
                {
                    Beatstrength = 0f;
                }

                if (!source.isPlaying)
                {
                    foreach (Transform transform in TargetBones)
                    {
                        transform.localPosition = new Vector3(0f, BoneStandardposition, 0f);
                    }

                    foreach (Renderer renderer in TargetRenderers)
                    {
                        renderer.materials[1].SetColor("_Color", Color.black);
                    }

                    foreach (Light light in TargetLights)
                    {
                        light.enabled = false;
                    }
                }
                else
                {
                    foreach (Transform transform in TargetBones)
                    {
                        transform.localPosition = new Vector3(0f, Mathf.Abs(Beatstrength / 20f) + BoneStandardposition, 0f);
                    }

                    foreach (Renderer renderer in TargetRenderers)
                    {
                        renderer.materials[1].SetColor("_Color", EmissionColor);
                    }

                    foreach (Light light in TargetLights)
                    {
                        light.color = EmissionColor;
                        light.intensity = Beatstrength * 500f;
                    }
                }
            }
            else
            {
                foreach (Transform transform in TargetBones)
                {
                    transform.localPosition = new Vector3(0f, BoneStandardposition, 0f);
                }

                foreach (Renderer renderer in TargetRenderers)
                {
                    renderer.materials[1].SetColor("_Color", Color.black);
                }

                foreach (Light light in TargetLights)
                {
                    light.enabled = false;
                }
            }
        }
    }
}