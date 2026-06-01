using NAudio.Dsp;
using NAudio.Wave;
using NAudio.MediaFoundation;
using System;

namespace AudioVisualizer.AudioAnalyzerV3Chatgpt
{
    public class AudioAnalyzerV3Chatgpt
    {
        private WasapiLoopbackCapture _capture;
        private BufferedWaveProvider _bufferedWaveProvider;
        private MediaFoundationResampler _resampler;

        public WaveFormat DesiredFormat { get; private set; }

        public bool AnalyzerRunning = false;

        public float[] CircularFloatBuffer = new float[4096];
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

            _resampler = new MediaFoundationResampler(_bufferedWaveProvider, DesiredFormat)
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

            AnalyzerRunning = false;
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            _bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);

            byte[] resampledBytes = new byte[16384];

            int bytesRead;

            while ((bytesRead = _resampler.Read(resampledBytes, 0, resampledBytes.Length)) > 0)
            {
                int sampleCount = bytesRead / 4;

                lock (_bufferLock)
                {
                    for (int i = 0; i < sampleCount; i++)
                    {
                        float sample = BitConverter.ToSingle(resampledBytes, i * 4);

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
            const int fftLength = 2048;

            Complex[] fftBuffer = new Complex[fftLength];
            float[] fft = new float[AmountOfBins];

            int localWriteHead;

            lock (_bufferLock)
            {
                localWriteHead = _writeHead;

                int readHead = localWriteHead - fftLength;

                if (readHead < 0)
                    readHead += CircularFloatBuffer.Length;

                for (int i = 0; i < fftLength; i++)
                {
                    int index = (readHead + i) % CircularFloatBuffer.Length;

                    float sample = CircularFloatBuffer[index];

                    sample *= (float)FastFourierTransform.HammingWindow(i, fftLength);

                    fftBuffer[i].X = sample;
                    fftBuffer[i].Y = 0;
                }
            }

            FastFourierTransform.FFT(true, 11, fftBuffer);

            int usefulBins = fftLength / 2;
            int samplesPerBin = usefulBins / AmountOfBins;

            for (int bin = 0; bin < AmountOfBins; bin++)
            {
                float max = 0;

                for (int i = 0; i < samplesPerBin; i++)
                {
                    int fftIndex = bin * samplesPerBin + i;

                    float magnitude =
                        MathF.Sqrt(
                            fftBuffer[fftIndex].X * fftBuffer[fftIndex].X +
                            fftBuffer[fftIndex].Y * fftBuffer[fftIndex].Y);

                    if (magnitude > max)
                        max = magnitude;
                }

                fft[bin] = max;
            }

            return fft;
        }
    }
}