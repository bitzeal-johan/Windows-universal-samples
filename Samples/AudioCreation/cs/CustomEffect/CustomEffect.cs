using System;
using System.Collections.Generic;
using Windows.Media.Effects;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.MediaProperties;
using System.Runtime.InteropServices;
using Windows.Foundation.Metadata;

namespace CustomEffect
{
    // Using the COM interface IMemoryBufferByteAccess allows us to access the underlying byte array in an AudioFrame
    [ComImport]
    [System.Runtime.InteropServices.Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public sealed class AudioEchoEffect : IBasicAudioEffect
    {
        private AudioEncodingProperties _currentEncodingProperties;
        private readonly List<AudioEncodingProperties> _supportedEncodingProperties;

        private float[] _echoBuffer;
        private int _currentActiveSampleIndex;
        private IPropertySet _propertySet;

        // Mix does not have a set - all updates should be done through the property set.
        private float Mix
        {
            get
            {
                object val = null;
                if (_propertySet?.TryGetValue("Mix", out val) == true)
                {
                    return (float)val;
                }
                return .5f;
            }
        }

        public bool UseInputFrameForOutput { get { return false; } }

        // Set up constant members in the constructor
        public AudioEchoEffect()
        {
            // Support 44.1kHz and 48kHz mono float
            _supportedEncodingProperties = new List<AudioEncodingProperties>();
            AudioEncodingProperties encodingProps1 = AudioEncodingProperties.CreatePcm(44100, 1, 32);
            encodingProps1.Subtype = MediaEncodingSubtypes.Float;
            AudioEncodingProperties encodingProps2 = AudioEncodingProperties.CreatePcm(48000, 1, 32);
            encodingProps2.Subtype = MediaEncodingSubtypes.Float;

            _supportedEncodingProperties.Add(encodingProps1);
            _supportedEncodingProperties.Add(encodingProps2);
        }
        
        public IReadOnlyList<AudioEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                return _supportedEncodingProperties;
            }
        }

        public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
        {
            _currentEncodingProperties = encodingProperties;

            // Create and initialize the echo array
            _echoBuffer = new float[encodingProperties.SampleRate]; // exactly one second delay
            _currentActiveSampleIndex = 0;
        }

        public unsafe void ProcessFrame(ProcessAudioFrameContext context)
        {
            AudioFrame inputFrame = context.InputFrame;
            AudioFrame outputFrame = context.OutputFrame;

            using (AudioBuffer inputBuffer = inputFrame.LockBuffer(AudioBufferAccessMode.Read),
                                outputBuffer = outputFrame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference inputReference = inputBuffer.CreateReference(),
                                            outputReference = outputBuffer.CreateReference())
            {
                ((IMemoryBufferByteAccess)inputReference).GetBuffer(out var inputDataInBytes, out _);
                ((IMemoryBufferByteAccess)outputReference).GetBuffer(out var outputDataInBytes, out _);

                float* inputDataInFloat = (float*)inputDataInBytes;
                float* outputDataInFloat = (float*)outputDataInBytes;

                // Process audio data
                int dataInFloatLength = (int)inputBuffer.Length / sizeof(float);

                for (int i = 0; i < dataInFloatLength; i++)
                {
                    // var inputData = inputDataInFloat[i] * (1.0f - Mix);
                    var inputData = inputDataInFloat[i];
                    outputDataInFloat[i] = ProcessFilterSample(inputData);
                }
            }
        }

        private const int MemoryLength = 5;
        private readonly double[] xv = new double[MemoryLength];
        private readonly double[] yv = new double[MemoryLength];

        public float ProcessFilterSample(float inputvalue)
        {
            const double GAIN = 5.631380800e+06;
            double outputv = 0.0f;

            xv[0] = xv[1]; xv[1] = xv[2]; xv[2] = xv[3];
            xv[3] = inputvalue / GAIN;
            yv[0] = yv[1]; yv[1] = yv[2]; yv[2] = yv[3];
            yv[3] = (xv[0] + xv[3]) + 3 * (xv[1] + xv[2])
                                    + (0.9859857072 * yv[0]) + (-2.9717213683 * yv[1])
                                    + (2.9857342405 * yv[2]);

            outputv = yv[3];

            return (float)outputv;
        }

        public void Close(MediaEffectClosedReason reason)
        {
            // Clean-up any effect resources
            // This effect doesn't care about close, so there's nothing to do
        }

        public void DiscardQueuedFrames()
        {
            // Reset contents of the samples buffer
            Array.Clear(_echoBuffer, 0, _echoBuffer.Length - 1);
            _currentActiveSampleIndex = 0;
        }

        public void SetProperties(IPropertySet configuration)
        {
            _propertySet = configuration;
        }
    }
}