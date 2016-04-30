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
        Uri apiEndpoint = new Uri("https://avs-alexa-na.amazon.com");
        public static readonly string APIVERSION = "v20160207";
        public Uri eventsURL;
        public Uri directivesURL;
        public Uri pingURL;

        // endpoint https://avs-alexa-na.amazon.com


        public HttpClient directivesClient = new HttpClient();
        public HttpClient pingClient = new HttpClient();
        public HttpClient eventsClient = new HttpClient();

        public API2()
        {
            eventsURL = new Uri(apiEndpoint, "/" + APIVERSION + "/events");
            directivesURL = new Uri(apiEndpoint, "/" + APIVERSION + "/directives");
            pingURL = new Uri(apiEndpoint, "/" + APIVERSION + "/ping");
            CreateGETConnection();
            CreatePOSTConnection();
            SendPing();
        }

        public async void CreateGETConnection()
        {
            Debug.WriteLine("Create GET channel");

            directivesClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Ini.access_token);
            var getResponse = await directivesClient.GetAsync(directivesURL);

            Debug.WriteLine("Failed to create GET connection..");
        }

        public async void CreatePOSTConnection()
        {
            Debug.WriteLine("Create POST channel");
            eventsClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Ini.access_token);

            using (var content = new MultipartFormDataContent())
            {
                string json = "{\"event\":{\"header\":{\"namespace\":\"System\",\"name\":\"SynchronizeState\",\"messageId\":\"fakeMessageID\"},\"payload\":{}}}";
                var stringContent = new StringContent(json);
                stringContent.Headers.Add("Content-Disposition", "form-data; name=\"metadata\"");
                content.Add(stringContent, "metadata");

                var getResponse = await eventsClient.PostAsync(eventsURL, content);

                Debug.WriteLine(getResponse.Content.ReadAsStringAsync().Result);
            }
        }

        public async void SendPing()
        {
            pingClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Ini.access_token);
            var getResponse = await pingClient.GetAsync(pingURL);

            Debug.WriteLine(getResponse.Content.ReadAsStringAsync().Result);
        }
    }
}
