using System;
using System.Diagnostics;
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
        private static AudioFileInputNode fileInput;

        private static bool _isRecording = false;
        private static bool _isAudioPlaying = false;

        private static MediaCapture _mediaCapture;

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
                _mediaCapture.AudioDeviceController.VolumePercent = 0.75f;
            }
            catch { }
        }

        public static async void PlayAudio(StorageFile file)
        {
            IsAudioPlaying = true;
            CreateAudioFileInputNodeResult fileInputResult = await audioflow.CreateFileInputNodeAsync(file);
            if (AudioFileNodeCreationStatus.Success != fileInputResult.Status)
            {
                // Cannot read input file
                Debug.WriteLine(String.Format("Cannot read input file because {0}", fileInputResult.Status.ToString()));
                return;
            }

            fileInput = fileInputResult.FileInputNode;
            try {
                fileInput.AddOutgoingConnection(deviceOuput);
            } catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            fileInput.StartTime = TimeSpan.FromSeconds(0);

            audioflow.Start();
            IsAudioPlaying = false;
        }

        public static async void StartRecord()
        {
            if (!IsRecording)
            {
                IsAudioPlaying = true;
                Audio.PlayAudio(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Audio/beep.wav")));
                IsAudioPlaying = false;

                IsRecording = true;

                StorageFile recordStorageFile = await Windows.Storage.KnownFolders.MusicLibrary.CreateFileAsync("recording.wav", Windows.Storage.CreationCollisionOption.ReplaceExisting);

                MediaEncodingProfile recordProfile = null;
                recordProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
                recordProfile.Audio.BitsPerSample = 16;
                recordProfile.Audio.ChannelCount = 1;
                recordProfile.Audio.SampleRate = 16000;

                await _mediaCapture.StartRecordToStorageFileAsync(recordProfile, recordStorageFile);
            }
        }

        public static async void StopRecord()
        {
            if (IsRecording)
            {
                IsRecording = false;

                await _mediaCapture.StopRecordAsync();

                IsAudioPlaying = true;
                Audio.PlayAudio(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Audio/beep.wav")));
                IsAudioPlaying = false;
            }
        }
    }
}
