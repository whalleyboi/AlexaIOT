using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Data.Json;

namespace AlexaIOT.Utils
{
    class Directives
    {
        public delegate void DirectiveReceivedHandler(string key, EventArgs e);
        public static event DirectiveReceivedHandler DirectiveReceived;

        public static void ParseDirective(string json)
        {
            RootObject parsedJSON = JsonConvert.DeserializeObject<RootObject>(json);
            Directive directive = parsedJSON.Directive;
            Header Header = directive.Header;
            Payload Payload = directive.Payload;

            if (Header.Namespace == Namespace.SpeechRecognizer)
            {
                if (Header.Name == "StopCapture")
                {
                    // Fire stop capture event
                }
            } else if (Header.Namespace == Namespace.Alerts)
            {
                if (Header.Name == "SetAlert")
                {
                    if (Payload.Type == PayloadType.ALARM)
                    {
                        DateTime d2 = DateTime.Parse(Payload.ScheduledTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
                        Debug.WriteLine("SetAlert! - " + d2.ToLocalTime());
                    }
                }
            }

            if (DirectiveReceived != null && !String.IsNullOrEmpty(json))
            {
                DirectiveReceived(json, new EventArgs());
            }
        }

        public static void SendDirective()
        {

        }
    }

    public class RootObject
    {
        public Directive Directive { get; set; }
    }

    public class Directive
    {
        public Header Header { get; set; }
        public Payload Payload { get; set; }
    }

    public class Header
    {
        public Namespace Namespace { get; set; }
        public string Name { get; set; }
        public string MessageId { get; set; }
    }

    public class Payload
    {
        public string Name { get; set; }
        public PayloadType Type { get; set; }
        public string ScheduledTime { get; set; }
        public string Token { get; set; }
    }

    public enum Namespace
    {
        SpeechRecognizer, // 	The core interface for the Alexa Voice Service. Each user utterance leverages the Recognize event.
        Alerts, // The interface for setting, stopping and deleting timers and alarms.
        AudioPlayer, // AudioPlayer
        PlaybackController, // The interface for navigating a playback queue via button press or affordance.
        Speaker, // The interface for volume control of a device or application, including mute and unmute.
        SpeechSynthesizer, // The interface for Alexa speech.
        System // The interface for providing client information to Alexa.
    }

    public enum PayloadType
    {
        ALARM,
        TIMER
    }
}
