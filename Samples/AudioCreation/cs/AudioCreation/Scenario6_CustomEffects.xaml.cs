//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using CustomEffect;
using SDKTemplate;
using System;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Effects;
using Windows.Media.Render;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace AudioCreation
{
    /// <summary>
    /// Using custom effects with the AudioGraph API.
    /// </summary>
    public sealed partial class Scenario6_CustomEffects : Page
    {
        private MainPage _rootPage;
        private AudioGraph _graph;
        private AudioFileInputNode _fileInputNode;
        private AudioDeviceOutputNode _deviceOutputNode;
        private AudioDeviceInputNode _deviceInputNode;
        private int _framesCounter = 0;

        public Scenario6_CustomEffects()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _rootPage = MainPage.Current;
            await CreateAudioGraph();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_graph != null)
            {
                _graph.Dispose();
            }
        }

        private async void File_Click(object sender, RoutedEventArgs e)
        {
            await SelectInputFile();
        }

        private async Task SelectInputFile()
        {
            //if (_graph == null)
            //    return;

            //// If another file is already loaded into the FileInput node
            //if (_fileInputNode != null)
            //{
            //    // Release the file and dispose the contents of the node
            //    _fileInputNode.Dispose();
            //    // Stop playback since a new file is being loaded. Also reset the button UI
            //    if (graphButton.Content.Equals("Stop Graph"))
            //    {
            //        TogglePlay();
            //    }
            //}

            //FileOpenPicker filePicker = new FileOpenPicker();
            //filePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            //filePicker.FileTypeFilter.Add(".mp3");
            //filePicker.ViewMode = PickerViewMode.Thumbnail;
            //StorageFile file = await filePicker.PickSingleFileAsync();

            //// File can be null if cancel is hit in the file picker
            //if (file == null)
            //{
            //    return;
            //}

            //CreateAudioFileInputNodeResult fileInputNodeResult = await _graph.CreateFileInputNodeAsync(file);
            //if (fileInputNodeResult.Status != AudioFileNodeCreationStatus.Success)
            //{
            //    // Cannot read file
            //    _rootPage.NotifyUser(String.Format("Cannot read input file because {0}", fileInputNodeResult.Status.ToString()), NotifyType.ErrorMessage);
            //    return;
            //}

            //_fileInputNode = fileInputNodeResult.FileInputNode;
            // _fileInputNode.AddOutgoingConnection(_deviceOutputNode);

            _deviceInputNode.AddOutgoingConnection(_deviceOutputNode);
            fileButton.Background = new SolidColorBrush(Colors.Green);

            // Event Handler for file completion
            // _fileInputNode.FileCompleted += FileInput_FileCompleted;

            // Enable the button to start the graph
            graphButton.IsEnabled = true;

            // Create the custom effect and apply to the FileInput node
//            AddCustomEffect(_fileInputNode);
            // AddCustomEffect(_deviceInputNode);
        }

        private void Graph_Click(object sender, RoutedEventArgs e)
        {
            TogglePlay();
        }

        private void TogglePlay()
        {
            // Toggle playback
            if (graphButton.Content.Equals("Start Graph"))
            {
                _graph.Start();
                graphButton.Content = "Stop Graph";
                audioPipe.Fill = new SolidColorBrush(Colors.Blue);
            }
            else
            {
                _graph.Stop();
                graphButton.Content = "Start Graph";
                audioPipe.Fill = new SolidColorBrush(Color.FromArgb(255, 49, 49, 49));
            }
        }

        private async Task CreateAudioGraph()
        {
            // Create an AudioGraph with default settings
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);
            settings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.LowestLatency;
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                // Cannot create graph
                _rootPage.NotifyUser(String.Format("AudioGraph Creation Error because {0}", result.Status.ToString()), NotifyType.ErrorMessage);
                return;
            }

            _graph = result.Graph;

            // Create a device output node
            CreateAudioDeviceOutputNodeResult deviceOutputResult = await _graph.CreateDeviceOutputNodeAsync();

            if (deviceOutputResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                // Cannot create device output
                _rootPage.NotifyUser(String.Format("Audio Device Output unavailable because {0}", deviceOutputResult.Status.ToString()), NotifyType.ErrorMessage);
                speakerContainer.Background = new SolidColorBrush(Colors.Red);
                return;
            }

            _deviceOutputNode = deviceOutputResult.DeviceOutputNode;
            _rootPage.NotifyUser("Device Output Node successfully created", NotifyType.StatusMessage);
            speakerContainer.Background = new SolidColorBrush(Colors.Green);

            // Create a device input node using the default audio input device
            CreateAudioDeviceInputNodeResult deviceInputNodeResult = await _graph.CreateDeviceInputNodeAsync(MediaCategory.Media);

            if (deviceInputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                // Cannot create device input node
                _rootPage.NotifyUser(String.Format("Audio Device Input unavailable because {0}", deviceInputNodeResult.Status.ToString()), NotifyType.ErrorMessage);
                return;
            }

            _deviceInputNode = deviceInputNodeResult.DeviceInputNode;

            var frameOutputNode = _graph.CreateFrameOutputNode();
            _graph.QuantumProcessed += AudioGraph_QuantumProcessed;
            _deviceInputNode.AddOutgoingConnection(frameOutputNode);
        }

        private async void AudioGraph_QuantumProcessed(AudioGraph sender, object args)
        {
            _framesCounter++;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                frames.Text = _framesCounter.ToString();
            });
        }

        private void AddCustomEffect(IAudioInputNode audioFileInputNode)
        {
            // Create a property set and add a property/value pair
            PropertySet echoProperties = new PropertySet();
            echoProperties.Add("Mix", 0.5f);

            // Instantiate the custom effect defined in the 'CustomEffect' project
            AudioEffectDefinition echoEffectDefinition = new AudioEffectDefinition(typeof(AudioEchoEffect).FullName, echoProperties);
            audioFileInputNode.EffectDefinitions.Add(echoEffectDefinition);
        }

        // Event handler for file completion event
        private async void FileInput_FileCompleted(AudioFileInputNode sender, object args)
        {
            // File playback is done. Stop the graph
            _graph.Stop();

            // Reset the file input node so starting the graph will resume playback from beginning of the file
            sender.Reset();

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                _rootPage.NotifyUser("End of file reached", NotifyType.StatusMessage);
                graphButton.Content = "Start Graph";
            });
        }
    }
}
