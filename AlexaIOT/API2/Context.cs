using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AlexaIOT
{
    // Returns the current context of the AlexaDevice.
    //See https://developer.amazon.com/public/solutions/alexa/alexa-voice-service/reference/context for more information.
    class Context
    {
        public static string GetDeviceContext()
        {
            ContextJSON json = new ContextJSON();
            return null;
        }
    }

    [DataContract]
    internal class ContextJSON
    {
        [DataMember]
        internal string context = "context";

        [DataMember]
        internal Header[] header;
    }

    [DataContract]
    internal class Header
    {
        [DataMember]
        internal string Namespace;

        [DataMember]
        internal string name;

        [DataMember]
        internal string messageId;

        [DataMember]
        internal string dialogRequestId;
    }

    [DataContract]
    internal class Payload
    {
        [DataMember]
        internal string name;
    }
}
