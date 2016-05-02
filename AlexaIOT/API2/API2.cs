using AlexaIOT.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.Web.Http;

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

        private CoreDispatcher dispatcher;

        private bool expectSpeech = false;
        private bool playURL = false;

        public API2()
        {
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            eventsURL = new Uri(apiEndpoint, "/" + APIVERSION + "/events");
            directivesURL = new Uri(apiEndpoint, "/" + APIVERSION + "/directives");
            pingURL = new Uri(apiEndpoint, "/" + APIVERSION + "/ping");

            CreateDownchannel();
        }

        /// <summary>
        /// GET for establishing the downchannel using the directives path
        /// Refer
        /// </summary>
        public async void CreateDownchannel()
        {
            using (System.Net.Http.HttpClient get = new System.Net.Http.HttpClient())
            {
                get.DefaultRequestHeaders.Add("Authorization", "Bearer " + Ini.access_token);
                var response = await (get.GetAsync(directivesURL, System.Net.Http.HttpCompletionOption.ResponseHeadersRead));

                var stream = await response.Content.ReadAsStreamAsync();
                var buffer = new byte[2048];

                while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0)
                {
                    // Report progress and write to a different stream
                    string directive = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                    using (StringReader reader = new StringReader(directive))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains("{\"directive\":{\""))
                            {
                                Directives.ParseDirective(line);
                            }
                        }
                    }
                    Array.Clear(buffer, 0, buffer.Length);
                }
            }
        }

        /// <summary>
        /// POST for all events sent to the Alexa Voice Service using the events path
        /// Refer ro : https://developer.amazon.com/public/solutions/alexa/alexa-voice-service/docs/avs-http2-requests
        /// </summary>
        /// <returns></returns>
        public async Task CreatePOSTConnection()
        {
            Windows.Web.Http.HttpClient post = new Windows.Web.Http.HttpClient();
            post.DefaultRequestHeaders.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Bearer", Ini.access_token);
            post.DefaultRequestHeaders.Add("Keep-Alive", "true");

            var content = new Windows.Web.Http.HttpMultipartFormDataContent();

            string json = "{\"event\":{\"header\":{\"namespace\":\"System\",\"name\":\"SynchronizeState\",\"messageId\":\"fakeMessageID\"},\"payload\":{}}}";
            var stringContent = new Windows.Web.Http.HttpStringContent(json);
            stringContent.Headers.Add("Content-Disposition", "form-data; name=\"metadata\"");

            content.Add(stringContent, "metadata");

            Debug.WriteLine("Confirm just before POST");
            var response = await (post.PostAsync(eventsURL, content));
        }

        public async Task SendRequest(byte[] audioData)
        {
            Debug.WriteLine("API2 - Send Request");

            using (var client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Ini.access_token);

                using (var content = new MultipartFormDataContent())
                {
                    string json = "{\"event\":{\"header\":{\"namespace\":\"SpeechRecognizer\",\"name\":\"Recognize\",\"messageId\":\"message-123\",\"dialogRequestId\":\"dialog-123\"},\"payload\":{\"profile\":\"CLOSE_TALK\",\"format\":\"AUDIO_L16_RATE_16000_CHANNELS_1\"}}}";
                    var stringContent = new StringContent(json);
                    stringContent.Headers.Add("Content-Disposition", "form-data; name=\"json\"");
                    content.Add(stringContent, "request");

                    ByteArrayContent byteContant = new ByteArrayContent(audioData);
                    byteContant.Headers.Add("Content-Type", "application/octet-stream");
                    byteContant.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"audio\"");
                    content.Add(byteContant, "file", "audio");

                    System.Net.Http.HttpResponseMessage message = await client.PostAsync(eventsURL.ToString(), content);
                    Stream streamResponse = await message.Content.ReadAsStreamAsync();
                    string getDirectives = message.Content.ReadAsStringAsync().Result;

                    try
                    {
                        var memStream = new MemoryStream();
                        await streamResponse.CopyToAsync(memStream);
                        memStream.Position = 0;

                        StorageFile file = await KnownFolders.MusicLibrary.CreateFileAsync("response.wav", CreationCollisionOption.ReplaceExisting);
                        await Windows.Storage.FileIO.WriteBytesAsync(file, memStream.ToArray());
                        await Audio.PlayAudio(file);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                }
            }
        }

        private async void FileInput_FileCompleted(Windows.Media.Audio.AudioFileInputNode sender, object args)
        {
            Debug.WriteLine("Completed speech");
            if (expectSpeech)
            {
                Audio.fileInput.FileCompleted -= FileInput_FileCompleted;
                await Audio.StartRecord();
                Debug.WriteLine("Start Record expectSpeech");
                Task.Delay(TimeSpan.FromMilliseconds(8000)).Wait();
                await Audio.StopRecord();
                Debug.WriteLine("Stop Record expectSpeech");
                expectSpeech = false;
            } else if (playURL)
            {
                Debug.WriteLine("Playing external audio not supported yet!");
                playURL = false;
            }
        }
    }
}
