using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Media.Capture;
using Windows.Media.Playback;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace AlexaIOT
{
    public partial class MainPage : Page
    {
        Server server;
        private Timer timer;
        private MediaCapture _mediaCapture;
        MediaPlayer player = BackgroundMediaPlayer.Current;
        MediaElement mysong = new MediaElement();

        // The speech recognizer used throughout this sample.
        private SpeechRecognizer speechRecognizer;

        // Keep track of whether the continuous recognizer is currently running, so it can be cleaned up appropriately.
        private bool isListening;

        // Speech events may come in on a thread other than the UI thread, keep track of the UI thread's
        // dispatcher, so we can update the UI in a thread-safe manner.
        private CoreDispatcher dispatcher;

        public MainPage()
        {
            this.InitializeComponent();
            isListening = false;
            try
            {
                _mediaCapture = new MediaCapture();
            }
            catch { }

            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            Ini.IPAddress = Server.GetLocalIp(); // Hard coded until i get a method to detect it.
            ReadConfig();

            // Testing on windows
            if (!DeviceTypeInformation.IsRaspberryPi)
            {
                Ini.access_token = "Atza|IQEBLjAsAhQ6BTrn8WaER8D7UfrxpvZTCRN-UAIUNMsUtWH2ZBYTiDlIK5u8gkEnL834iGhqhhJCHBbabHjl2T79FmWjsEFcdltTHHh6AEd4FgMe1wX7ipDhQN97T5C76fBUmQxRDYs2DOFblSr3eGQEJO0UgIHPPOmqOCrqANUUpQRdkcqvbUzOAH3hfxueci_zP3HdE7d3CJ19r-E7t-OhxoKMi9xqsgKcD1b3Th6xb4ZDtPf7aqHe8QorO1ZOqjzm91MRQPKG2KZjwTYy7TbZ8wfO5UCLb239xCCwMn_2FTrJfZP1sT25SWoqcVsKRYB-WYKYrx4RiEeGzWwJWKg9Ip5iTZnnX1n22l9uet_WANmBsZUHtx8ByeIvU3IVpLfrOt4Ng9Of5wVY2ETegznJuFRZoyy4X3JYIA2gKJ1dcTgV31xSWsCBWTL32R8U8yGdAy3ODc8UXq56yS_OHwdOQ98VmaKUTVN0ezq6H2AmBzt1POvvcJMYWPIvD0vjl9wkUzP-71flwRugyeeImD7h_4_JZ5UJug3u_VKlAul71H38D62JSVeYRJ41RkRM3spF";
            }

            try
            {
                server = new Server();
                server.Start();

                status.Text = "Running - http://" + Ini.IPAddress + ":5000/";
                status.Foreground = new SolidColorBrush(Colors.Green);
            }
            catch
            {
                status.Text = "Not running";
                status.Foreground = new SolidColorBrush(Colors.Red);
            }

            /// JSON Parse response test.
            /// {"messageHeader":{},"messageBody":{"directives":[{"namespace":"SpeechSynthesizer","name":"speak","payload":{"contentIdentifier":"amzn1.as-ct.v1.Domain:Application:Music#ACRI#DeviceTTSRendererV4_0eb7a138-1870-42dc-bcb8-86145318117e","audioContent":"cid:DeviceTTSRendererV4_0eb7a138-1870-42dc-bcb8-86145318117e_1191296483"}}]}}
        }

        public async Task Hello()
        {
            Random random = new Random();
            int randomNumber = random.Next(0, 2);
            StorageFile file;
            if (randomNumber == 0)
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Audio/StartupHello/hello.wav"));
            }
            else if (randomNumber == 1)
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Audio/StartupHello/hi.wav"));
            } else
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Audio/StartupHello/hithere.wav"));
            }
            Audio.PlayAudio(file);
        }

        public async void ReadConfig()
        {
            // Set default audio device on raspberry pi :D
            await Audio.AudioDevices();

            try
            {
                await Ini.ReadConfig();
                textBox.Text = Ini.ProductID;
                textBox_Copy.Text = Ini.Security_Profile_Description;
                textBox_Copy1.Text = Ini.Security_Profile_ID;
                textBox_Copy2.Text = Ini.Client_ID;
                textBox_Copy3.Text = Ini.Client_Secret;
            }
            catch { }
            await Hello();

            await InitializeRecognizer(SpeechRecognizer.SystemSpeechLanguage);

            isListening = true;
            await speechRecognizer.ContinuousRecognitionSession.StartAsync();

            timer = new Timer(timerCallback, null, TimeSpan.FromSeconds(20).Milliseconds, Timeout.Infinite);
        }

        private async void UpdateTextbox()
        {
            await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
() =>
{
    textBox.Text = Ini.ProductID;
    textBox_Copy.Text = Ini.Security_Profile_Description;
    textBox_Copy1.Text = Ini.Security_Profile_ID;
    textBox_Copy2.Text = Ini.Client_ID;
    textBox_Copy3.Text = Ini.Client_Secret;
});
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            Ini.ProductID = textBox.Text;
            Ini.Security_Profile_Description = textBox_Copy.Text;
            Ini.Security_Profile_ID = textBox_Copy1.Text;
            Ini.Client_ID = textBox_Copy2.Text;
            Ini.Client_Secret = textBox_Copy3.Text;
            await Ini.WriteConfig();
            await Ini.ReadConfig();
        }

        private async void timerCallback(object state)
        {
            // do some work not connected with UI
            string result = await server.GetToken(true);
        }

        private async void toggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (toggleButton.Content.ToString() == "Record Audio")
                {
                    toggleButton.Content = "Stop Recording";

                    Audio.StartRecord();
                }
                else
                {
                    toggleButton.Content = "Record Audio";
                    Audio.StopRecord();
                }
            }
            catch (Exception e2)
            {
                Debug.WriteLine("Record - " + e2.ToString());
            }
        }

        private async void button1_Click(object sender, RoutedEventArgs e)
        {
            if (button1.Content.ToString() == "Play Recording")
            {
                button1.Content = "Stop Playing";

                StorageFile file = await KnownFolders.MusicLibrary.GetFileAsync("recording.wav");
                Audio.PlayAudio(file);
            }
            else
            {
                button1.Content = "Play Recording";
                mysong.Pause();
            }
        }

        private async void button2_Click(object sender, RoutedEventArgs e)
        {
            StorageFile file = await KnownFolders.MusicLibrary.GetFileAsync("recording.wav");
            Stream stream = await file.OpenStreamForReadAsync();
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                Debug.WriteLine("Audio byte size - " + ms.Length);
                await server.SendRequest(ms.ToArray());
            }
        }

        /// <summary>
        /// Initialize Speech Recognizer and compile constraints.
        /// </summary>
        /// <param name="recognizerLanguage">Language to use for the speech recognizer</param>
        /// <returns>Awaitable task.</returns>
        private async Task InitializeRecognizer(Language recognizerLanguage)
        {
            if (speechRecognizer != null)
            {
                // cleanup prior to re-initializing this scenario.
                speechRecognizer.StateChanged -= SpeechRecognizer_StateChanged;
                speechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
                speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;
                speechRecognizer.HypothesisGenerated -= SpeechRecognizer_HypothesisGenerated;

                this.speechRecognizer.Dispose();
                this.speechRecognizer = null;
            }

            this.speechRecognizer = new SpeechRecognizer(recognizerLanguage);
            speechRecognizer.Timeouts.EndSilenceTimeout = new TimeSpan(15, 0, 0, 0);

            // Provide feedback to the user about the state of the recognizer. This can be used to provide visual feedback in the form
            // of an audio indicator to help the user understand whether they're being heard.
            speechRecognizer.StateChanged += SpeechRecognizer_StateChanged;
        
            // Apply the dictation topic constraint to optimize for dictated freeform speech.
            var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
            speechRecognizer.Constraints.Add(dictationConstraint);
            SpeechRecognitionCompilationResult result = await speechRecognizer.CompileConstraintsAsync();
            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                //rootPage.NotifyUser("Grammar Compilation Failed: " + result.Status.ToString(), NotifyType.ErrorMessage);
                //btnContinuousRecognize.IsEnabled = false;
            }

            // Handle continuous recognition events. Completed fires when various error states occur. ResultGenerated fires when
            // some recognized phrases occur, or the garbage rule is hit. HypothesisGenerated fires during recognition, and
            // allows us to provide incremental feedback based on what the user's currently saying.
            speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
            speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
            speechRecognizer.HypothesisGenerated += SpeechRecognizer_HypothesisGenerated;
        }

        /// <summary>
        /// Handle events fired when error conditions occur, such as the microphone becoming unavailable, or if
        /// some transient issues occur.
        /// </summary>
        /// <param name="sender">The continuous recognition session</param>
        /// <param name="args">The state of the recognizer</param>
        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                // If TimeoutExceeded occurs, the user has been silent for too long. We can use this to 
                // cancel recognition if the user in dictation mode and walks away from their device, etc.
                // In a global-command type scenario, this timeout won't apply automatically.
                // With dictation (no grammar in place) modes, the default timeout is 20 seconds.
                if (args.Status == SpeechRecognitionResultStatus.TimeoutExceeded)
                {
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await InitializeRecognizer(SpeechRecognizer.SystemSpeechLanguage);

                        isListening = true;
                        await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                    });
                }
                else
                {
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        //Debug.WriteLine("Continuous Recognition Completed: " + args.Status.ToString());
                        isListening = false;
                    });
                }
            }
        }

        /// <summary>
        /// While the user is speaking, update the textbox with the partial sentence of what's being said for user feedback.
        /// </summary>
        /// <param name="sender">The recognizer that has generated the hypothesis</param>
        /// <param name="args">The hypothesis formed</param>
        private async void SpeechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            string hypothesis = args.Hypothesis.Text;
        }

        /// <summary>
        /// Handle events fired when a result is generated. Check for high to medium confidence, and then append the
        /// string to the end of the stringbuffer, and replace the content of the textbox with the string buffer, to
        /// remove any hypothesis text that may be present.
        /// </summary>
        /// <param name="sender">The Recognition session that generated this result</param>
        /// <param name="args">Details about the recognized speech</param>
        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            // We may choose to discard content that has low confidence, as that could indicate that we're picking up
            // noise via the microphone, or someone could be talking out of earshot.
            if (args.Result.Confidence == SpeechRecognitionConfidence.Medium || args.Result.Confidence == SpeechRecognitionConfidence.High)
            {
                //dictatedTextBuilder.Append(args.Result.Text + " ");

                //Debug.WriteLine("Received = " + args.Result.Text);

                if (args.Result.Text.ToLower() == "hello." || args.Result.Text.ToLower() == "alexa.")
                {
                    if (!Audio.IsRecording && !Audio.IsAudioPlaying)
                    {
                        Audio.StartRecord();
                    }
                }

                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    //discardedTextBlock.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                    //dictationTextBox.Text = dictatedTextBuilder.ToString();
                    //btnClearText.IsEnabled = true;
                });
            }
            else
            {
                // In some scenarios, a developer may choose to ignore giving the user feedback in this case, if speech
                // is not the primary input mechanism for the application.
                // Here, just remove any hypothesis text by resetting it to the last known good.
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    //dictationTextBox.Text = dictatedTextBuilder.ToString();
                    string discardedText = args.Result.Text;
                    if (!string.IsNullOrEmpty(discardedText))
                    {
                        discardedText = discardedText.Length <= 25 ? discardedText : (discardedText.Substring(0, 25) + "...");

                        //discardedTextBlock.Text = "Discarded due to low/rejected Confidence: " + discardedText;
                        //discardedTextBlock.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    }
                });
            }
        }

        /// <summary>
        /// Provide feedback to the user based on whether the recognizer is receiving their voice input.
        /// </summary>
        /// <param name="sender">The recognizer that is currently running.</param>
        /// <param name="args">The current state of the recognizer.</param>
        private async void SpeechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {

                if (args.State.ToString() == "SoundEnded")
                {
                    if (Audio.IsRecording)
                    {
                        Audio.StopRecord();
                    }
                }

                //Debug.WriteLine(args.State.ToString());
            });
        }

    }
}
