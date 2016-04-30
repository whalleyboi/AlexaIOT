using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;

namespace AlexaIOT
{
    internal class Server
    {
        private const uint BufferSize = 1024;
        HttpClient httpClient = new HttpClient();

        StreamSocketListener listener = new StreamSocketListener();

        public void Start()
        {
            listener.BindEndpointAsync(new HostName(Ini.IPAddress), "5000").AsTask().Wait();
            listener.ConnectionReceived += Listener_ConnectionReceived;
        }

        public async void Stop()
        {
            await listener.CancelIOAsync();
            listener.Dispose();
        }

        private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                string authCode = null;
                string refresh_token = null;
                string access_token = null;

                bool isIndex = false;
                bool isCode = false;
                bool isSetup = false;
                bool isBeta = false;
                bool setupSave = false;
                StringBuilder request = new StringBuilder();
                string requestString = "";
                using (IInputStream input = args.Socket.InputStream)
                {
                    byte[] data = new byte[BufferSize];
                    IBuffer buffer = data.AsBuffer();
                    uint dataRead = BufferSize;
                    while (dataRead == BufferSize)
                    {
                        await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                        requestString = Encoding.UTF8.GetString(data, 0, data.Length);
                        request.Append(requestString);

                        if (requestString.Contains("/code?code="))
                        {
                            authCode = Ini.GetStringInBetween("/code?code=", "&scope=alexa", requestString, false, false);
                            isCode = true;
                        }
                        else if (requestString.Contains("/setup?ProductID="))
                        {
                            isSetup = true;
                            setupSave = true;
                        }
                        else if (requestString.Contains("/setup"))
                        {
                            isSetup = true;
                        }
                        else if (requestString.Contains("/beta"))
                        {
                            isBeta = true;
                        }
                        else
                        {
                            isIndex = true;
                        }

                        dataRead = buffer.Length;
                    }
                }

                using (IOutputStream output = args.Socket.OutputStream)
                {
                    using (Stream response = output.AsStreamForWrite())
                    {
                        byte[] bodyArray = null;

                        if (isIndex)
                        {
                            string url = "https://www.amazon.com/ap/oa?scope=alexa%3Aall&redirect_uri=http%3A%2F%2F" + Ini.IPAddress + "%3A5000%2Fcode&response_type=code&client_id=" + Ini.Client_ID + "&scope_data=%7B%22alexa%3Aall%22%3A+%7B%22productInstanceAttributes%22%3A+%7B%22deviceSerialNumber%22%3A+%22001%22%7D%2C+%22productID%22%3A+%22" + Ini.ProductID + "%22%7D%7D";
                            bodyArray = Encoding.UTF8.GetBytes("<html><body><a href=\"" + url + "\">Authorize AlexIOT with Amazon</a> <br/> <IMPORTANT> Go to <a href=\"http://" + Ini.IPAddress + ":5000/setup\">Setup AlexaIOT</a> BEFORE you authorize</body></html>");
                        }
                        else if (isBeta)
                        {
                            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Web/beta.html"));
                            Stream stream = await file.OpenStreamForReadAsync();

                            StreamReader reader = new StreamReader(stream);
                            string text = reader.ReadToEnd();
                            text = text.Replace("{ProductID}", Ini.ProductID);
                            bodyArray = Encoding.UTF8.GetBytes(text);
                        }
                        else if (isSetup)
                        {
                            //StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Web/setup.html"));

                            string setupPage = "<html><title>Setup AlexaIOT</title><body><center><h2>Setup AlexaIOT</h2></center>";
                            setupPage += "<form name=\"bizLoginForm\" method=\"get\" action\"\" >";
                            setupPage += "<b>ProductID</b> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; |<input type=\"text\" name=\"ProductID\"  value=\"" + Ini.ProductID + "\"> </br><br/>";
                            setupPage += "<b>Security_Profile_Description</b> &nbsp;&nbsp; |<input type=\"text\" name=\"Security_Profile_Description\" value=\"" + Ini.Security_Profile_Description + "\"> </br><br/>";
                            setupPage += "<b>Security_Profile_ID</b> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; |<input type=\"text\" name=\"Security_Profile_ID\"  value=\"" + Ini.Security_Profile_ID + "\"> </br></br>";
                            setupPage += "<b>Client_ID</b> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; |<input type=\"text\" name=\"Client_ID\"  value=\"" + Ini.Client_ID + "\"> </br></br>";
                            setupPage += "<b>Client_Secret</b> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; |<input type=\"text\" name=\"Client_Secret\"  value=\"" + Ini.Client_Secret + "\"> </br></br>";
                            setupPage += "<input type=\"Submit\" value=\"Save Settings\" />";
                            if (setupSave)
                            {
                                foreach (var myString in requestString.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    if (requestString.Contains("/setup?ProductID="))
                                    {
                                        string ProductID = Ini.GetStringInBetween("?ProductID=", "&Security_Profile_Description", myString, false, false);
                                        string Security_Profile_Description = Ini.GetStringInBetween("&Security_Profile_Description=", "&Security_Profile_ID", myString, false, false);
                                        string Security_Profile_ID = Ini.GetStringInBetween("&Security_Profile_ID=", "&Client_ID=", myString, false, false);
                                        string ClientID = Ini.GetStringInBetween("&Client_ID=", "&Client_Secret=", myString, false, false);
                                        string Client_Secret = Ini.GetStringInBetween("&Client_Secret=", " HTTP/1.1", myString, false, false);

                                        if (String.IsNullOrEmpty(ProductID) || String.IsNullOrEmpty(Security_Profile_Description) || String.IsNullOrEmpty(Security_Profile_ID) || String.IsNullOrEmpty(ClientID) || String.IsNullOrEmpty(Client_Secret))
                                        {
                                            setupPage += "<br /> <br/> <b>Missing paramter! please make sure everything is filled out.</b>";
                                            break;
                                        }
                                        else
                                        {
                                            Ini.ProductID = ProductID;
                                            Ini.Security_Profile_Description = Security_Profile_Description;
                                            Ini.Security_Profile_ID = Security_Profile_ID;
                                            Ini.Client_ID = ClientID;
                                            Ini.Client_Secret = Client_Secret;

                                            await Ini.WriteConfig();
                                            setupPage += "<center><h2>Settings saved</h2></center>";
                                        }
                                        break;
                                    }
                                }
                            }
                            setupPage += "</form>";
                            setupPage += "</body></html>";

                            bodyArray = Encoding.UTF8.GetBytes(setupPage);
                        }
                        else if (isCode && authCode != null)
                        {
                            var requestContent = new FormUrlEncodedContent(new[] {
                                new KeyValuePair<string, string>("redirect_uri", "http://"+Ini.IPAddress+":5000/code"),
                                new KeyValuePair<string, string>("client_id", Ini.Client_ID),
                                new KeyValuePair<string, string>("client_secret", Ini.Client_Secret),
                                new KeyValuePair<string, string>("code", authCode),
                                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                            });

                            HttpResponseMessage response4 = await httpClient.PostAsync("https://api.amazon.com/auth/o2/token", requestContent);
                            HttpContent responseContent = response4.Content;

                            bool gotRefreshToken = false;

                            using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                            {
                                string responseToken = reader.ReadToEndAsync().Result;
                                if (responseToken.Contains("{\"access_token\":\""))
                                {
                                    JsonObject rootObject;
                                    if (JsonObject.TryParse(responseToken, out rootObject))
                                    {
                                        refresh_token = rootObject.GetNamedString("refresh_token");
                                        access_token = rootObject.GetNamedString("access_token");
                                        gotRefreshToken = true;
                                    }
                                }
                            }

                            string status = "";
                            if (gotRefreshToken)
                            {
                                status = "Successfully obtained the refresh token! <br> <br> " + refresh_token;
                                Ini.refresh_token = refresh_token;
                                Ini.access_token = access_token;
                                await Ini.WriteConfig();
                            }
                            else
                            {
                                status = "Failed to obtain refresh token :(";
                            }

                            bodyArray = Encoding.UTF8.GetBytes("<html><body><center><h2>Refresh Token Status:</h2> " + status + "</center><br/></body></html>");
                        }

                        var bodyStream = new MemoryStream(bodyArray);

                        var header = "HTTP/1.1 200 OK\r\n" +
                                    $"Content-Length: {bodyStream.Length}\r\n" +
                                        "Connection: close\r\n\r\n";

                        byte[] headerArray = Encoding.UTF8.GetBytes(header);
                        await response.WriteAsync(headerArray, 0, headerArray.Length);
                        await bodyStream.CopyToAsync(response);
                        await response.FlushAsync();
                    }
                }
            }
            catch (Exception e) {
                Debug.WriteLine("Crashing here?" + Environment.NewLine + e.ToString());
            }

            await args.Socket.CancelIOAsync();
        }

        internal string GetToken()
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetToken(bool refresh)
        {

            Debug.WriteLine("Called: GetToken()");
            if (refresh)
            {
                var requestContent = new FormUrlEncodedContent(new[] {
                                new KeyValuePair<string, string>("client_id", Ini.Client_ID),
                                new KeyValuePair<string, string>("client_secret", Ini.Client_Secret),
                                new KeyValuePair<string, string>("refresh_token", Ini.refresh_token),
                                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                            });

                HttpResponseMessage response4 = await httpClient.PostAsync("https://api.amazon.com/auth/o2/token", requestContent);
                HttpContent responseContent = response4.Content;

                string access_token = "";
                using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                {
                    string responseToken = reader.ReadToEndAsync().Result;
                    if (responseToken.Contains("{\"access_token\":\""))
                    {
                        JsonObject rootObject;
                        if (JsonObject.TryParse(responseToken, out rootObject))
                        {
                            access_token = rootObject.GetNamedString("access_token");
                            Ini.access_token = access_token;
                        }
                    }
                }
                return access_token;
            }
            else if (!String.IsNullOrEmpty(Ini.access_token))
            {
                return Ini.access_token;
            }
            else
            {
                return "nothing";
            }
        }

        public async Task SendRequest(byte[] audioData)
        {
            string alexaURL = "https://access-alexa-na.amazon.com/v1/avs/speechrecognizer/recognize";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Ini.access_token);

                using (var content = new MultipartFormDataContent())
                {
                    string json = "{\"messageHeader\":{\"deviceContext\":[{\"name\":\"playbackState\",\"namespace\":\"AudioPlayer\",\"payload\":{\"streamId\":\"\",\"offsetInMilliseconds\":\"0\",\"playerActivity\":\"IDLE\"}}]},\"messageBody\":{\"profile\":\"alexa-close-talk\",\"locale\":\"en-us\",\"format\":\"audio/L16; rate=16000; channels=1\"}}";
                    var stringContent = new StringContent(json);
                    stringContent.Headers.Add("Content-Disposition", "form-data; name=\"json\"");
                    content.Add(stringContent, "request");

                    ByteArrayContent byteContant = new ByteArrayContent(audioData);
                    byteContant.Headers.Add("Content-Type", "application/octet-stream");
                    byteContant.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"audio\"");
                    content.Add(byteContant, "file", "audio");

                    HttpResponseMessage message = await client.PostAsync(alexaURL, content);

                    Stream streamResponse = await message.Content.ReadAsStreamAsync();

                    var memStream = new MemoryStream();

                    // Convert the stream to the memory stream, because a memory stream supports seeking.
                    await streamResponse.CopyToAsync(memStream);

                    // Set the start position.
                    memStream.Position = 0;

                    StorageFile file = await KnownFolders.MusicLibrary.CreateFileAsync("response.wav", CreationCollisionOption.ReplaceExisting);
                    await Windows.Storage.FileIO.WriteBytesAsync(file, memStream.ToArray());
                    Audio.PlayAudio(file);

                    Debug.WriteLine(message.Content.ReadAsStringAsync().Result);
                }
            }
        }

        public async Task<string> GetDirectives()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Ini.access_token);
            var response = await httpClient.GetAsync("https://access-alexa-na.amazon.com/v20160207/events");

            //will throw an exception if not successful
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            return content;
            //return await Task.Run(() => JsonObject.Parse(content));
        }

        private void ProccessRequest(string Response)
        {
        }

        public static string GetLocalIp()
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();
            if (icp != null
                  && icp.NetworkAdapter != null
                  && icp.NetworkAdapter.NetworkAdapterId != null)
            {
                var name = icp.ProfileName;

                var hostnames = NetworkInformation.GetHostNames();

                foreach (var hn in hostnames)
                {
                    if (hn.IPInformation != null
                        && hn.IPInformation.NetworkAdapter != null
                        && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                                                                   != null
                        && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                                    == icp.NetworkAdapter.NetworkAdapterId
                        && hn.Type == HostNameType.Ipv4)
                    {
                        return hn.CanonicalName;
                    }
                }
            }

            return "---";
        }
    }
}
