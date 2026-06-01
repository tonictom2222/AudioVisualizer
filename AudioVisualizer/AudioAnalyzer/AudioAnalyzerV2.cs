using Microsoft.VisualBasic;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AudioVisualizer.AudioAnalyzerV2
{
    public class AudioAnalyzerV2
    {
        private WasapiLoopbackCapture _WasapiInstance;
        private BufferedWaveProvider capturedBytes;
        private MediaFoundationResampler Resampler;
        public WaveFormat desiredFormat;
        public bool AnalyzerRunning = false;
        public float[] CircularFloatBuffer = new float[1024 * 4]; //resolution of the buffer
        public int AmountOfBins = 30;

        public int writeHead;

        public void Start()
        {
            AnalyzerRunning = true;

            desiredFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

            _WasapiInstance = new WasapiLoopbackCapture();

            capturedBytes = new BufferedWaveProvider(desiredFormat)
            {
                DiscardOnBufferOverflow = true,
                ReadFully = false
            };

            _WasapiInstance.DataAvailable += OnDataAvailable;

            Resampler = new MediaFoundationResampler(capturedBytes, desiredFormat);

            _WasapiInstance.StartRecording();
        }
        public void Stop()
        {
            _WasapiInstance.StopRecording();
            _WasapiInstance.Dispose();
            Resampler.Dispose();
            AnalyzerRunning = false;
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)//event listener
        {
            capturedBytes.AddSamples(e.Buffer, 0, e.BytesRecorded);//store bytes in buffer
            byte[] resampledBytes = new byte[Resampler.WaveFormat.AverageBytesPerSecond / 10];//sometimes returns zero, stores the bytes in the correct format
            int numOfBytesRead = Resampler.Read(resampledBytes, 0, resampledBytes.Length);//the actual ammount of data in the buffer

            if (numOfBytesRead > 0)
            {
                int NumOfValidSamples = resampledBytes.Count(x => x != 0) / (desiredFormat.BitsPerSample / 8); //This looks bettter imho
                //int NumOfValidSamples = numOfBytesRead / 4; //ChatGPT answer
                for (int NthSample = 0; NthSample < NumOfValidSamples; NthSample++)
                {
                    CircularFloatBuffer[writeHead] = BitConverter.ToSingle(resampledBytes, NthSample * 4);
                    writeHead++;
                    if (CircularFloatBuffer.Length == writeHead)
                    {
                        writeHead = 0;
                    }

                    // write head should go to zero when circular buffer can no longer fit more samples 
                    //if the writeHead equals the lenght of the buffer reset
                }
            }

            FFT();
        }

        //private void OnDataAvailable(object sender, WaveInEventArgs e)//CHATGPT PROVIDED 
        //{
        //    capturedBytes.AddSamples(e.Buffer, 0, e.BytesRecorded);

        //    byte[] resampledBytes = new byte[16384];

        //    int bytesRead;

        //    while ((bytesRead = Resampler.Read(resampledBytes, 0, resampledBytes.Length)) > 0)
        //    {
        //        int sampleCount = bytesRead / 4;

        //        for (int i = 0; i < sampleCount; i++)
        //        {
        //            float sample = BitConverter.ToSingle(resampledBytes, i * 4);

        //            CircularFloatBuffer[writeHead] = sample;

        //            writeHead++;

        //            if (writeHead >= CircularFloatBuffer.Length)
        //                writeHead = 0;
        //        }
        //    }
        //}



        public float[] FFT()
        {
            int fftLength = 1024 * 2;
            int localWriteHead = writeHead;

            NAudio.Dsp.Complex[] fftBuffer = new NAudio.Dsp.Complex[fftLength];
            float[] fft = new float[AmountOfBins];

            // Read newest samples from circular buffer
            int readHead = localWriteHead - fftLength;

            if (readHead < 0)
                readHead += CircularFloatBuffer.Length;

            for (int i = 0; i < fftLength; i++)
            {
                int index = (readHead + i) % CircularFloatBuffer.Length;

                float windowedSample =
                    CircularFloatBuffer[index] *
                    (float)FastFourierTransform.HammingWindow(i, fftLength);

                fftBuffer[i].X = windowedSample;
                fftBuffer[i].Y = 0;
            }

            // ACTUAL FFT
            FastFourierTransform.FFT(true, (int)Math.Log2(fftLength), fftBuffer);

            int usefulBins = fftLength / 2;
            int samplesPerBin = usefulBins / AmountOfBins;

            for (int bin = 0; bin < AmountOfBins; bin++)
            {
                float max = 0;

                for (int i = 0; i < samplesPerBin; i++)
                {
                    int fftIndex = bin * samplesPerBin + i;

                    float magnitude = (float)Math.Sqrt(
                        fftBuffer[fftIndex].X * fftBuffer[fftIndex].X +
                        fftBuffer[fftIndex].Y * fftBuffer[fftIndex].Y);

                    if (magnitude > max)
                        max = magnitude;
                }

                fft[bin] = max;
            }

            return fft;
        }

        //private void OnDataAvailable(object sender, WaveInEventArgs e)//event listener
        //{
        //    capturedBytes.AddSamples(e.Buffer, 0, e.BytesRecorded);//store bytes in buffer
        //    byte[] resampledBytes = new byte[Resampler.WaveFormat.AverageBytesPerSecond / 10];//convert into desired format bytes
        //    int numOfBytesRead = Resampler.Read(resampledBytes, 0, resampledBytes.Length);//the actual ammount of data in the buffer

        //    if (numOfBytesRead > 0) 
        //    {
        //        int numOfSamples = numOfBytesRead / (desiredFormat.BitsPerSample / 8);
        //        float[] samples = new float[numOfSamples]; //convert bytes into float samples
        //        Buffer.BlockCopy(resampledBytes,0, samples,0,numOfBytesRead); //loads resampled bytes into samples
        //        Console.Clear();
        //        Console.WriteLine(e.BytesRecorded);
        //        Console.WriteLine(resampledBytes.Count(x => x != 0));
        //        Console.WriteLine(numOfBytesRead);
        //    }



        //}
    }
}
