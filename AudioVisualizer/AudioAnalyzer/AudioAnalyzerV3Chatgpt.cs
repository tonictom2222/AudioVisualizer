using NAudio.Dsp;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System;

namespace AudioVisualizer.AudioAnalyzerV3Chatgpt
{
    public class AudioAnalyzerV3Chatgpt
    {
        private WasapiLoopbackCapture _capture;
        private BufferedWaveProvider _bufferedWaveProvider;
        private MediaFoundationResampler _resampler;

        public WaveFormat DesiredFormat { get; private set; }

        public bool AnalyzerRunning { get; private set; }

        public float[] CircularFloatBuffer = new float[4096 * 2];

        public int AmountOfBins = 30;

        private volatile int _writeHead;

        private readonly object _bufferLock = new();

        public void Start()
        {
            if (AnalyzerRunning)
                return;

            DesiredFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

            _capture = new WasapiLoopbackCapture();

            _bufferedWaveProvider = new BufferedWaveProvider(_capture.WaveFormat)
            {
                DiscardOnBufferOverflow = true,
                ReadFully = false
            };

            _resampler = new MediaFoundationResampler(
                _bufferedWaveProvider,
                DesiredFormat)
            {
                ResamplerQuality = 60
            };

            _capture.DataAvailable += OnDataAvailable;
            _capture.StartRecording();

            AnalyzerRunning = true;
        }

        public void Stop()
        {
            if (!AnalyzerRunning)
                return;

            _capture.StopRecording();
            _capture.DataAvailable -= OnDataAvailable;

            _capture.Dispose();
            _resampler.Dispose();

            _capture = null;
            _resampler = null;
            _bufferedWaveProvider = null;

            AnalyzerRunning = false;
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            _bufferedWaveProvider.AddSamples(
                e.Buffer,
                0,
                e.BytesRecorded);

            byte[] resampledBytes = new byte[16384];

            int bytesRead;

            while ((bytesRead =
                _resampler.Read(
                    resampledBytes,
                    0,
                    resampledBytes.Length)) > 0)
            {
                int sampleCount = bytesRead / 4;

                lock (_bufferLock)
                {
                    for (int i = 0; i < sampleCount; i++)
                    {
                        float sample =
                            BitConverter.ToSingle(
                                resampledBytes,
                                i * 4);

                        CircularFloatBuffer[_writeHead] = sample;

                        _writeHead++;

                        if (_writeHead >= CircularFloatBuffer.Length)
                            _writeHead = 0;
                    }
                }
            }
        }

        public float[] FFT()
        {
            const int fftLength = 2048 * 4;
            const int m = 11; // log2(2048)

            Complex[] fftBuffer = new Complex[fftLength];
            float[] fft = new float[AmountOfBins];

            lock (_bufferLock)
            {
                int readHead = _writeHead - fftLength;

                if (readHead < 0)
                    readHead += CircularFloatBuffer.Length;

                for (int i = 0; i < fftLength; i++)
                {
                    int index =
                        (readHead + i) %
                        CircularFloatBuffer.Length;

                    float sample = CircularFloatBuffer[index];

                    sample *= (float)
                        FastFourierTransform.HammingWindow(
                            i,
                            fftLength);

                    fftBuffer[i].X = sample;
                    fftBuffer[i].Y = 0;
                }
            }

            FastFourierTransform.FFT(
                true,
                m,
                fftBuffer);

            int usefulBins = fftLength / 2;

            float sampleRate = DesiredFormat.SampleRate;

            const float minFreq = 20f;
            float maxFreq = sampleRate / 2f;

            for (int band = 0; band < AmountOfBins; band++)
            {
                float startFreq =
                    minFreq *
                    MathF.Pow(
                        maxFreq / minFreq,
                        (float)band / AmountOfBins);

                float endFreq =
                    minFreq *
                    MathF.Pow(
                        maxFreq / minFreq,
                        (float)(band + 1) / AmountOfBins);

                int startIndex =
                    (int)(startFreq * fftLength / sampleRate);

                int endIndex =
                    (int)(endFreq * fftLength / sampleRate);

                startIndex = Math.Clamp(
                    startIndex,
                    0,
                    usefulBins - 1);

                endIndex = Math.Clamp(
                    endIndex,
                    startIndex + 1,
                    usefulBins);

                float maxMagnitude = 0f;

                for (int i = startIndex; i < endIndex; i++)
                {
                    float magnitude =
                        MathF.Sqrt(
                            fftBuffer[i].X * fftBuffer[i].X +
                            fftBuffer[i].Y * fftBuffer[i].Y);

                    if (magnitude > maxMagnitude)
                        maxMagnitude = magnitude;
                }

                // Convert to dB
                float db =
                    20f *
                    MathF.Log10(
                        maxMagnitude + 0.0000000001f);

                // Normalize from -80 dB..0 dB to 0..1
                db = (db + 80f) / 80f;

                fft[band] =
                    Math.Clamp(
                        db,
                        0f,
                        1f);
            }

            return fft;
        }
    }
}