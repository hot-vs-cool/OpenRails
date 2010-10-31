/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace MSTS
{

    /// <summary>
    /// Utility class to avoid loading multiple copies of the same file.
    /// </summary>
    public class SharedSMSFileManager
    {
        private static Dictionary<string, SMSFile> SharedSMSFiles = new Dictionary<string, SMSFile>();

        public static SMSFile Get(string path)
        {
            if (!SharedSMSFiles.ContainsKey(path))
            {
                SMSFile smsFile = new SMSFile(path);
                SharedSMSFiles.Add(path, smsFile);
                return smsFile;
            }
            else
            {
                return SharedSMSFiles[path];
            }
        }
    }

	/// <summary>
	/// Represents the hiearchical structure of the SMS File
	/// </summary>
	public class SMSFile
	{
		public Tr_SMS Tr_SMS;

		public SMSFile( string filePath )
		{
            ReadFile(filePath);  
        }

        private void ReadFile(string filePath)
        {
            using(STFReader f = new STFReader(filePath, false))
                while (!f.EOF)
                    switch(f.ReadItem().ToLower())
                    {
                        case "tr_sms": Tr_SMS = new Tr_SMS(f); break;
                        case "(": f.SkipRestOfBlock(); break;
                    }
        }

	} // class SMSFile

    public class Tr_SMS
    {
        public List<ScalabiltyGroup> ScalabiltyGroups = new List<ScalabiltyGroup>();
        
        public Tr_SMS(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "scalabiltygroup": ScalabiltyGroups.Add(new ScalabiltyGroup(f)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
    } // class Tr_SMS

    public partial class ScalabiltyGroup
    {
        public int DetailLevel;
        public SMSStreams Streams = null;
        public float Volume = 1.0f;
        public bool Stereo = false;
        public bool Ignore3D = false;
        public Activation Activation;
        public Deactivation Deactivation;

        public ScalabiltyGroup(STFReader f)
        {
            f.MustMatch("(");
            DetailLevel = f.ReadInt(STFReader.UNITS.None, null);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "activation": Activation = new Activation(f); break;
                    case "deactivation": Deactivation = new Deactivation(f); break;
                    case "streams": Streams = new SMSStreams(f, Volume); break;
                    case "volume": Volume = f.ReadFloatBlock(STFReader.UNITS.None, null); break;
                    case "stereo": Stereo = f.ReadBoolBlock(true); break;
                    case "ignore3d": Ignore3D = f.ReadBoolBlock(true); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
    } // class ScalabiltyGroup

    public class Activation
    {
        public bool ExternalCam = false;
        public bool CabCam = false;
        public bool PassengerCam = false;
        public float Distance = 10000;  // by default we are 'in range' to hear this
        public int TrackType = -1;

        public Activation(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
                switch(f.ReadItem().ToLower())
                {
                    case "externalcam": ExternalCam = f.ReadBoolBlock(true); break;
                    case "cabcam": CabCam = f.ReadBoolBlock(true); break;
                    case "passengercam": PassengerCam = f.ReadBoolBlock(true); break;
                    case "distance": Distance = f.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                    case "tracktype": TrackType = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
    }

    public class Deactivation: Activation
    {
        public Deactivation(STFReader f): base( f )
        {
        }
    }

    public class SMSStreams : List<SMSStream>
    {
        public SMSStreams(STFReader f, float VolumeOfScGroup)
        {
            f.MustMatch("(");

            int count = f.ReadInt(STFReader.UNITS.None, null);

            while( !f.EndOfBlock() )
                switch(f.ReadItem().ToLower())
                {
                    case "stream": Add(new SMSStream(f, VolumeOfScGroup)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }

            if (count != this.Count)
            {
                STFException.TraceWarning(f, "Stream count mismatch found :" + this.Count.ToString() + ", expected :" + count.ToString());
                //Strings: All of the code below should be removed once I locate a bug in STF parsing
                foreach (var i in this)
                    Trace.WriteLine(String.Format("stream {0} {1} {2}->{3}", i.Priority, i.Volume, i.Triggers.Count, string.Join(",", i.Triggers.Select(t => t.GetType().Name).ToArray())));
            }
        }
    }

    public class SMSStream
    {
        public int Priority = 0;
        public Triggers Triggers;
        public float Volume = 1.0f;
        public VolumeCurve VolumeCurve = null;
        public FrequencyCurve FrequencyCurve = null;

        public SMSStream(STFReader f, float VolumeOfScGroup)
        {
            f.MustMatch("(");

            Volume = VolumeOfScGroup;

            while (!f.EndOfBlock())
                switch(f.ReadItem().ToLower())
                {
                    case "priority": Priority = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "triggers":  Triggers = new Triggers(f); break;
                    case "volumecurve": VolumeCurve = new VolumeCurve(f); break; 
                    case "frequencycurve": FrequencyCurve = new FrequencyCurve(f); break;
                    case "volume": Volume = f.ReadFloatBlock(STFReader.UNITS.None, null); break;
                    case "(": f.SkipRestOfBlock(); break;
                }

            if (Volume > 1)
                Volume /= 100;
        }
    }

    public struct CurvePoint
    {
        public float X, Y;
    }

    public class VolumeCurve
    {
        public enum Controls { None, DistanceControlled, SpeedControlled, Variable1Controlled, Variable2Controlled, Variable3Controlled };

        public Controls Control = Controls.None;
        public float Granularity = 1.0f;

        public CurvePoint[] CurvePoints;

        public VolumeCurve(STFReader f)
        {
            f.MustMatch("(");
            switch (f.ReadItem().ToLower())
            {
                case "distancecontrolled": Control = Controls.DistanceControlled; break;
                case "speedcontrolled": Control = Controls.SpeedControlled; break;
                case "variable1controlled": Control = Controls.Variable1Controlled; break;
                case "variable2controlled": Control = Controls.Variable2Controlled; break;
                case "variable3controlled": Control = Controls.Variable3Controlled; break;
                case "(": f.SkipRestOfBlock(); break;
            }
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "curvepoints":
                        f.MustMatch("(");
                        int count = f.ReadInt(STFReader.UNITS.None, null);
                        CurvePoints = new CurvePoint[count];
                        for (int i = 0; i < count; ++i)
                        {
                            CurvePoints[i].X = f.ReadFloat(STFReader.UNITS.None, null);
                            CurvePoints[i].Y = f.ReadFloat(STFReader.UNITS.None, null);
                        }
                        f.SkipRestOfBlock();
                        break;
                    case "granularity": Granularity = f.ReadFloatBlock(STFReader.UNITS.None, null); break;
                    case "(": f.SkipRestOfBlock(); break;
                }

            if (Control == Controls.Variable2Controlled && CurvePoints[CurvePoints.Length - 1].X <= 1)
            {
                for (int i = 0; i < CurvePoints.Length; i++)
                {
                    CurvePoints[i].X *= 100f;
                }
            }
        }
    }

    public class FrequencyCurve: VolumeCurve
    {
        public FrequencyCurve(STFReader f)
            : base(f)
        {
        }
    }


    public class Triggers : List<Trigger>
    {
        public Triggers(STFReader f)
        {
            f.MustMatch("(");
            int count = f.ReadInt(STFReader.UNITS.None, null);

            while(!f.EndOfBlock())
                switch(f.ReadItem().ToLower())
                {
                    case "dist_travelled_trigger": Add(new Dist_Travelled_Trigger(f)); break;   
                    case "discrete_trigger": Add(new Discrete_Trigger(f)); break;       
                    case "random_trigger": Add(new Random_Trigger(f)); break; 
                    case "variable_trigger": Add(new Variable_Trigger(f)); break; 
                    case "initial_trigger": Add(new Initial_Trigger(f)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }

            foreach (Trigger trigger in this)
                if (trigger.SoundCommand == null)
                    STFException.TraceError( f, "Trigger lacks a sound command");
        }
    }

    public class Trigger
    {
        public SoundCommand SoundCommand = null;

        int playcommandcount = 0;

        protected void ParsePlayCommand( STFReader f, string lowertoken )
        {
            switch (lowertoken)
            {
                case "playoneshot": 
                case "startloop":
                case "releaselooprelease": 
                case "startlooprelease":
                case "releaseloopreleasewithjump": 
                case "disabletrigger": 
                case "enabletrigger": 
                case "setstreamvolume":
                    ++playcommandcount;
                    if (playcommandcount > 1)
                        STFException.TraceWarning( f, "Found multiple Play Commands");
                    break;
                default:
                    break;
            }

            switch (lowertoken)
            {
                case "playoneshot": SoundCommand = new PlayOneShot(f); break;
                case "startloop": SoundCommand = new StartLoop(f); break;
                case "releaselooprelease":  SoundCommand = new ReleaseLoopRelease(f); break; 
                case "startlooprelease":  SoundCommand = new StartLoopRelease( f ); break; 
                case "releaseloopreleasewithjump": SoundCommand = new ReleaseLoopReleaseWithJump( f ); break; 
                case "disabletrigger": SoundCommand = new DisableTrigger( f); break; 
                case "enabletrigger": SoundCommand = new EnableTrigger( f); break;
                case "setstreamvolume": SoundCommand = new SetStreamVolume(f); break;
                case "(": f.SkipRestOfBlock(); break;
            }
        }
    }

    public class Initial_Trigger : Trigger
    {

        public Initial_Trigger(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
                ParsePlayCommand(f, f.ReadItem().ToLower());
        }
    }

    public class Discrete_Trigger : Trigger
    {

        public int TriggerID;

        public Discrete_Trigger(STFReader f)
        {
            f.MustMatch("(");
            TriggerID = f.ReadInt(STFReader.UNITS.None, null);
            while (!f.EndOfBlock())
                ParsePlayCommand(f, f.ReadItem().ToLower());
        }
    }

    public class Variable_Trigger : Trigger
    {
        public enum Events { Speed_Inc_Past, Speed_Dec_Past, Distance_Inc_Past, Distance_Dec_Past,
        Variable1_Inc_Past, Variable1_Dec_Past, Variable2_Inc_Past, Variable2_Dec_Past, Variable3_Inc_Past, Variable3_Dec_Past   };

        public Events Event;
        public float Threshold;

        public Variable_Trigger(STFReader f)
        {
            f.MustMatch("(");

            string eventString = f.ReadItem();

            switch (eventString.ToLower())
            {
                case "speed_inc_past": Event = Events.Speed_Inc_Past; break;
                case "speed_dec_past": Event = Events.Speed_Dec_Past; break;
                case "distance_inc_past": Event = Events.Distance_Inc_Past; break;
                case "distance_dec_past": Event = Events.Distance_Dec_Past; break;
                case "variable1_inc_past": Event = Events.Variable1_Inc_Past; break;
                case "variable1_dec_past": Event = Events.Variable1_Dec_Past; break;
                case "variable2_inc_past": Event = Events.Variable2_Inc_Past; break;
                case "variable2_dec_past": Event = Events.Variable2_Dec_Past; break;
                case "variable3_inc_past": Event = Events.Variable3_Inc_Past; break;
                case "variable3_dec_past": Event = Events.Variable3_Dec_Past; break;
            }

            Threshold = f.ReadFloat(STFReader.UNITS.None, null);

            while (!f.EndOfBlock())
                ParsePlayCommand(f, f.ReadItem().ToLower());
        }
    }

    public class Dist_Travelled_Trigger : Trigger
    {
        public float Dist_Min = 80;
        public float Dist_Max = 100;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Dist_Travelled_Trigger(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadItem().ToLower();
                switch (lowtok)
                {
                    case "dist_min_max": f.MustMatch("("); Dist_Min = f.ReadFloat(STFReader.UNITS.Distance, null); Dist_Max = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    case "volume_min_max": f.MustMatch("("); Volume_Min = f.ReadFloat(STFReader.UNITS.None, null); Volume_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Random_Trigger : Trigger
    {
        public float Delay_Min = 80;
        public float Delay_Max = 100;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Random_Trigger(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadItem().ToLower();
                switch (lowtok)
                {
                    case "delay_min_max": f.MustMatch("("); Delay_Min = f.ReadFloat(STFReader.UNITS.None, null); Delay_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    case "volume_min_max": f.MustMatch("("); Volume_Min = f.ReadFloat(STFReader.UNITS.None, null); Volume_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }
    public class SoundCommand
    {
        public enum SelectionMethods { RandomSelection, SequentialSelection };
    }

    public class SetStreamVolume : SoundCommand
    {
        public float Volume;

        public SetStreamVolume(STFReader f)
        {
            f.MustMatch("(");
            Volume = f.ReadFloat(STFReader.UNITS.None, null);
            f.SkipRestOfBlock();
        }
    }

    public class DisableTrigger : SoundCommand
    {
        public int TriggerID;

        public DisableTrigger(STFReader f)
        {
            f.MustMatch("(");
            TriggerID = f.ReadInt(STFReader.UNITS.None, null);
            f.SkipRestOfBlock();
        }
    }

    public class EnableTrigger : DisableTrigger
    {
        public EnableTrigger(STFReader f)
            : base(f)
        {
        }
    }

    public class ReleaseLoopRelease : SoundCommand
    {
        public ReleaseLoopRelease(STFReader f)
        {
            f.MustMatch("(");
            f.SkipRestOfBlock();
        }
    }

    public class ReleaseLoopReleaseWithJump : SoundCommand
    {
        public ReleaseLoopReleaseWithJump(STFReader f)
        {
            f.MustMatch("(");
            f.SkipRestOfBlock();
        }
    }

    public class SoundPlayCommand: SoundCommand
    {
        public string[] Files;
        public SelectionMethods SelectionMethod = SelectionMethods.SequentialSelection;
    }

    public class PlayOneShot : SoundPlayCommand
    {
        
        public PlayOneShot(STFReader f)
        {
            f.MustMatch("(");
            int count = f.ReadInt(STFReader.UNITS.None, null);
            Files = new string[count];
            int iFile = 0;
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "file":
                        if (iFile < count)
                        {
                            f.MustMatch("(");
                            Files[iFile++] = f.ReadItem();
                            f.ReadInt(STFReader.UNITS.None, null);
                            f.SkipRestOfBlock();
                        }
                        else  // MSTS skips extra files
                        {
                            STFException.TraceWarning(f, "File count mismatch");
                            f.SkipBlock();
                        }
                        break;
                    case "selectionmethod":
                        f.MustMatch("(");
                        string s = f.ReadItem();
                        switch (s.ToLower())
                        {
                            case "randomselection": SelectionMethod = SelectionMethods.RandomSelection; break;
                            case "sequentialselection": SelectionMethod = SelectionMethods.SequentialSelection; break;
                            default: STFException.TraceWarning(f, "Unknown selection method " + s); break;
                        }
                        f.SkipRestOfBlock();
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
    }// PlayOneShot

    public class StartLoop : PlayOneShot
    {
        public StartLoop( STFReader f ): base(f)
        {
        }
    }

    public class StartLoopRelease : PlayOneShot
    {
        public StartLoopRelease(STFReader f)
            : base(f)
        {
        }
    }


} // namespace
