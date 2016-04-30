using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;

namespace AlexaIOT
{
    public class Ini   // revision 10
    {
        public static string IPAddress = "";
        public static string _ProductID = "";
        public static string _Security_Profile_Description = "";
        public static string _Security_Profile_ID = "";
        public static string _Client_ID = "";
        public static string _Client_Secret = "";
        private static string _access_token = "";
        private static string _refresh_token = "";

        public delegate void IniChangeEventHandler(string key, EventArgs e);
        public static event IniChangeEventHandler IniChanged;

        public static string ProductID
        {
            get
            {
                return _ProductID;
            }
            set
            {
                _ProductID = value;
                if (IniChanged != null && !String.IsNullOrEmpty(value))
                {
                    IniChanged("ProductID", new EventArgs());
                }
            }
        }

        public static string Security_Profile_Description
        {
            get
            {
                return _Security_Profile_Description;
            }
            set
            {
                _Security_Profile_Description = value;
                if (IniChanged != null && !String.IsNullOrEmpty(value))
                {
                    IniChanged("Security_Profile_Description", new EventArgs());
                }
            }
        }

        public static string Security_Profile_ID
        {
            get
            {
                return _Security_Profile_ID;
            }
            set
            {
                _Security_Profile_ID = value;
                if (IniChanged != null && !String.IsNullOrEmpty(value))
                {
                    IniChanged("Security_Profile_ID", new EventArgs());
                }
            }
        }

        public static string Client_ID
        {
            get
            {
                return _Client_ID;
            }
            set
            {
                _Client_ID = value;
                if (IniChanged != null && !String.IsNullOrEmpty(value))
                {
                    IniChanged("Client_ID", new EventArgs());
                }
            }
        }

        public static string Client_Secret
        {
            get
            {
                return _Client_Secret;
            }
            set
            {
                _Client_Secret = value;
                if (IniChanged != null && !String.IsNullOrEmpty(value))
                {
                    IniChanged("Client_Secret", new EventArgs());
                }
            }
        }

        public static string refresh_token
        {
            get
            {
                return _refresh_token;
            }
            set
            {
                _refresh_token = value;
                if (IniChanged != null && !String.IsNullOrEmpty(value))
                {
                    IniChanged("refresh_token", new EventArgs());
                }
            }
        }

        public static string access_token
        {
            get
            {
                return _access_token;
            }
            set
            {
                _access_token = value;
                if (IniChanged != null && !String.IsNullOrEmpty(value))
                {
                    IniChanged("access_token", new EventArgs());
                }
            }
        }

        public static async Task WriteConfig()
        {
            StorageFile resultfile;

            if (DeviceTypeInformation.IsRaspberryPi)
            {
                var folders = await KnownFolders.RemovableDevices.GetFoldersAsync();
                resultfile = await folders[2].CreateFileAsync("Config.xml", CreationCollisionOption.OpenIfExists);
            } else
            {
                resultfile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("Config.xml", CreationCollisionOption.OpenIfExists);
            }

            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("AlexaIOT");
            xmlDoc.AppendChild(rootNode);

            XmlNode userNode = xmlDoc.CreateElement("ProductID");
            userNode.InnerText = Ini.ProductID;
            rootNode.AppendChild(userNode);

            XmlNode userNode2 = xmlDoc.CreateElement("Security_Profile_Description");
            userNode2.InnerText = Ini.Security_Profile_Description;
            rootNode.AppendChild(userNode2);

            XmlNode userNode3 = xmlDoc.CreateElement("Security_Profile_ID");
            userNode3.InnerText = Ini.Security_Profile_ID;
            rootNode.AppendChild(userNode3);

            XmlNode userNode4 = xmlDoc.CreateElement("Client_ID");
            userNode4.InnerText = Ini.Client_ID;
            rootNode.AppendChild(userNode4);

            XmlNode userNode5 = xmlDoc.CreateElement("Client_Secret");
            userNode5.InnerText = Ini.Client_Secret;
            rootNode.AppendChild(userNode5);

            XmlNode userNode6 = xmlDoc.CreateElement("refresh_token");
            userNode6.InnerText = Ini.refresh_token;
            rootNode.AppendChild(userNode6);

            string xmlRoot = "";
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                xmlDoc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                xmlRoot = stringWriter.GetStringBuilder().ToString();
            }

            await Windows.Storage.FileIO.WriteTextAsync(resultfile, xmlRoot);
        }

        public static async Task ReadConfig()
        {
            StorageFile resultfile;

            if (DeviceTypeInformation.IsRaspberryPi)
            {
                var folders = await KnownFolders.RemovableDevices.GetFoldersAsync();
                resultfile = await folders[2].CreateFileAsync("Config.xml", CreationCollisionOption.OpenIfExists);
            }
            else
            {
                resultfile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("Config.xml", CreationCollisionOption.OpenIfExists);
            }


            string text = await Windows.Storage.FileIO.ReadTextAsync(resultfile);

                string ProductID = GetStringInBetween("<ProductID>", "</ProductID>", text, false, false);
                Ini.ProductID = ProductID;

                string Security_Profile_Description = GetStringInBetween("<Security_Profile_Description>", "</Security_Profile_Description>", text, false, false);
                Ini.Security_Profile_Description = Security_Profile_Description;

                string Security_Profile_ID = GetStringInBetween("<Security_Profile_ID>", "</Security_Profile_ID>", text, false, false);
                Ini.Security_Profile_ID = Security_Profile_ID;

                string Client_ID = GetStringInBetween("<Client_ID>", "</Client_ID>", text, false, false);
                Ini.Client_ID = Client_ID;

                string Client_Secret = GetStringInBetween("<Client_Secret>", "</Client_Secret>", text, false, false);
                Ini.Client_Secret = Client_Secret;

                string refresh_token = GetStringInBetween("<refresh_token>", "</refresh_token>", text, false, false);
                Ini.refresh_token = refresh_token;
        }

        public static string toString()
        {
            return "IP Address - " + IPAddress + Environment.NewLine + "ProductID - " + ProductID + Environment.NewLine + "Security Description - " + Security_Profile_Description + Environment.NewLine + "Security Profile ID - " + Security_Profile_ID + Environment.NewLine + "ClientID - " + Client_ID + Environment.NewLine + "Client secret - " + Client_Secret + Environment.NewLine + "Refresh Token - " + refresh_token;
        }

        public static string GetStringInBetween(string strBegin, string strEnd, string strSource, bool includeBegin, bool includeEnd)
        {
            string[] result = { string.Empty, string.Empty };
            int iIndexOfBegin = strSource.IndexOf(strBegin);

            if (iIndexOfBegin != -1)
            {
                // include the Begin string if desired 
                if (includeBegin)
                    iIndexOfBegin -= strBegin.Length;

                strSource = strSource.Substring(iIndexOfBegin + strBegin.Length);

                int iEnd = strSource.IndexOf(strEnd);
                if (iEnd != -1)
                {
                    // include the End string if desired 
                    if (includeEnd)
                        iEnd += strEnd.Length;
                    result[0] = strSource.Substring(0, iEnd);
                    // advance beyond this segment 
                    if (iEnd + strEnd.Length < strSource.Length)
                        result[1] = strSource.Substring(iEnd + strEnd.Length);
                }
            }
            else
                // stay where we are 
                result[1] = strSource;
            return result[0];
        }
    }
}
