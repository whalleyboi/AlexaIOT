using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AlexaIOT
{
    // Research purposes.
    class API2
    {
        public static readonly string APIVERSION = "v20160207";
        public Uri eventsURL;
        public Uri directivesURL;

        public async void CreateConnection(string token)
        {
            // endpoint https://avs-alexa-na.amazon.com

            var apiEndpoint = new Uri("https://avs-alexa-na.amazon.com");
            eventsURL = new Uri(apiEndpoint, "/" + APIVERSION + "/events");
            directivesURL = new Uri(apiEndpoint, "/" + APIVERSION + "/directives");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var getResponse = await client.GetAsync(directivesURL);
                Debug.WriteLine("Response - " + getResponse.RequestMessage);
            }
        }
    }
}
