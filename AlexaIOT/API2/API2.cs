using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;

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

        public HttpResponseMessage eventsResponse = null;
        public HttpResponseMessage getResponse = null;

        private CoreDispatcher dispatcher;

        private bool expectSpeech = false;
        private bool isSpkeaing = false;
        private bool playURL = false;
        string audioURL = "";

        public API2()
        {
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            eventsURL = new Uri(apiEndpoint, "/" + APIVERSION + "/events");
            directivesURL = new Uri(apiEndpoint, "/" + APIVERSION + "/directives");
            pingURL = new Uri(apiEndpoint, "/" + APIVERSION + "/ping");
        }

        public async Task CreateGETConnection()
        {
            directivesClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Ini.access_token);
            getResponse = await directivesClient.GetAsync(directivesURL);
        }

        public async Task CreatePOSTConnection()
        {
            eventsClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Ini.access_token);
            eventsClient.DefaultRequestHeaders.Add("Connection", "Keep-Alive");

            using (var content = new MultipartFormDataContent())
            {
                string json = "{\"event\":{\"header\":{\"namespace\":\"System\",\"name\":\"SynchronizeState\",\"messageId\":\"fakeMessageID\"},\"payload\":{}}}";
                var stringContent = new StringContent(json);
                stringContent.Headers.Add("Content-Disposition", "form-data; name=\"metadata\"");
                content.Add(stringContent, "metadata");

                eventsResponse = await eventsClient.PostAsync(eventsURL, content);
            }
        }

        public async Task SendRequest(byte[] audioData)
        {
            Debug.WriteLine("API2 - Send Request");

            using (var client = new HttpClient())
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

                    HttpResponseMessage message =null;

                    try
                    {
                        message = await client.PostAsync(eventsURL.ToString(), content);
                        Stream streamResponse = await message.Content.ReadAsStreamAsync();
                        string getDirectives = message.Content.ReadAsStringAsync().Result;

                        var memStream = new MemoryStream();

                        // Convert the stream to the memory stream, because a memory stream supports seeking.
                        await streamResponse.CopyToAsync(memStream);

                        // Set the start position.
                        memStream.Position = 0;

                        StorageFile file = await KnownFolders.MusicLibrary.CreateFileAsync("response.wav", CreationCollisionOption.ReplaceExisting);
                        await Windows.Storage.FileIO.WriteBytesAsync(file, memStream.ToArray());
                        await Audio.PlayAudio(file);

                        using (StringReader reader = new StringReader(getDirectives))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (line.Contains("{\"directive\":{\""))
                                {
                                    if(line.Contains("\"name\":\"Speak\"")) // Alexa speaking
                                    {
                                        isSpkeaing = true;
                                        Audio.fileInput.FileCompleted += FileInput_FileCompleted;
                                    } else if (line.Contains("\"name\":\"ExpectSpeech\"")) // Open mic and record
                                    {
                                        expectSpeech = true;
                                        double timeOut = 8000; // Default
                                        string timeOuts = Ini.GetStringInBetween("\"timeoutInMilliseconds\":", "}}}", line, false, false);
                                        Double.TryParse(timeOuts, out timeOut);
                                    }
                                }
                                else if (line.Contains("\"name\":\"Play\"")) // Play radio
                                {
                                    audioURL = Ini.GetStringInBetween("},\"url\":\"", "\",\"token\"", line, false, false);
                                    playURL = true;
                                }
                            }
                        }
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
            isSpkeaing = false;
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
