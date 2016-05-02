using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage;

namespace AlexaIOT
{
    public class Audio
    {
        private static AudioGraph audioflow;
        private static AudioDeviceOutputNode deviceOuput;
        private static AudioGraphSettings settings;
        public static AudioFileInputNode fileInput;

        private static bool _isRecording = false;
        private static bool _isAudioPlaying = false;
        private static bool _isExpecting = false;
        private static MediaCapture _mediaCapture;

        private static StorageFile recordStorageFile = null;
        private static StorageFile beep = null;

        public static MemoryStream str = new MemoryStream();

        public static bool IsRecording
        {
            get
            {
                return _isRecording;
            }
            set
            {
                _isRecording = value;
            }
        }

        public static bool IsAudioPlaying
        {
            get
            {
                return _isAudioPlaying;
            }
            set
            {
                _isAudioPlaying = value;
            }
        }

        public static async Task AudioDevices()
        {
            if (beep == null)
            {
                beep = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Audio/beep.wav"));
            }

            DeviceInformationCollection devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Media.Devices.MediaDevice.GetAudioRenderSelector());
            DeviceInformation selectedDevice = null;

            // Sometimes usb mics are confused as speakers.. so add check to make Speakers (Raspberry Pi 2 audio) the default <-- Confirmed works on rpi3
            for (int i = 0; i <= devices.Count; i++)
            {
                if (i != devices.Count)
                {
                    if (DeviceTypeInformation.IsRaspberryPi)
                    {
                        if (devices[i].Name == "Speakers (Raspberry Pi 2 audio)")
                        {
                            selectedDevice = devices[i];
                            break;
                        }
                    }
                    else
                    {
                        selectedDevice = devices[i];
                        break;
                    }
                }
            }

            settings = new AudioGraphSettings(AudioRenderCategory.Media);
            settings.PrimaryRenderDevice = selectedDevice;
            CreateAudioGraphResult resultg = await AudioGraph.CreateAsync(settings);

            audioflow = resultg.Graph;

            CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await audioflow.CreateDeviceOutputNodeAsync();
            CreateAudioDeviceInputNodeResult deviceInputNodeResult = await audioflow.CreateDeviceInputNodeAsync(MediaCategory.Media);

            deviceOuput = deviceOutputNodeResult.DeviceOutputNode;

            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();
                _mediaCapture.Failed += _mediaCapture_Failed;
                _mediaCapture.AudioDeviceController.VolumePercent = 0.75f;
            }
            catch {
                Debug.WriteLine("Failed to setup microphone is one connected?");
            }
        }

        private static void _mediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Debug.WriteLine(errorEventArgs.Message);
        }

        public static async Task PlayAudio(StorageFile file)
        {
            await AudioDevices();

            _isAudioPlaying = true;
            CreateAudioFileInputNodeResult fileInputResult = await audioflow.CreateFileInputNodeAsync(file);
            if (AudioFileNodeCreationStatus.Success != fileInputResult.Status)
            {
                // Cannot read input file
                Debug.WriteLine(String.Format("Cannot read input file because {0}", fileInputResult.Status.ToString()));
                _isAudioPlaying = false;
                return;
            }

            if (!_isAudioPlaying)
            {
                Debug.WriteLine("Error detected!");
                return;
            }

            fileInput = fileInputResult.FileInputNode;
            fileInput.FileCompleted += FileInput_FileCompleted;
            try {
                fileInput.AddOutgoingConnection(deviceOuput);
            } catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            fileInput.StartTime = TimeSpan.FromSeconds(0);
            audioflow.Start();
            _isAudioPlaying = false;
        }

        private static void FileInput_FileCompleted(AudioFileInputNode sender, object args)
        {
            audioflow.Stop();
        }

        public static async Task StartRecord()
        {
            if (!IsRecording)
            {
                IsRecording = true;
                _isAudioPlaying = true;
                await Audio.PlayAudio(beep);
                _isAudioPlaying = false;

                if (recordStorageFile == null)
                {
                    recordStorageFile = await Windows.Storage.KnownFolders.MusicLibrary.CreateFileAsync("recording.wav", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                }

                MediaEncodingProfile recordProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
                recordProfile.Audio.BitsPerSample = 16;
                recordProfile.Audio.ChannelCount = 1;
                recordProfile.Audio.SampleRate = 16000;

                try {
                    await str.FlushAsync();
                    await _mediaCapture.StartRecordToStreamAsync(recordProfile, str.AsRandomAccessStream());
                } catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
                //await _mediaCapture.StartRecordToStorageFileAsync(recordProfile, recordStorageFile);
                recordStorageFile = null;
            }
        }

        public static async Task StopRecord()
        {
            if (IsRecording)
            {
                await _mediaCapture.StopRecordAsync();

                _isAudioPlaying = true;
                await Audio.PlayAudio(beep);
                _isAudioPlaying = false;
                IsRecording = false;
            }
        }
    }
}
