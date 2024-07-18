using UnityEngine;
using System.IO;

public class MicrophoneRecorder : MonoBehaviour
{
    private AudioClip audioClip;
    private string microphone;
    private int sampleRate = 16000; // Whisper가 지원하는 샘플레이트

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            microphone = Microphone.devices[0];
        }
        else
        {
            Debug.LogError("No microphone detected!");
        }
    }

    public void StartRecording()
    {
        if (microphone != null)
        {
            audioClip = Microphone.Start(microphone, false, 10, sampleRate);
            Debug.Log("Recording started");
        }
        else
        {
            Debug.LogError("No microphone found to start recording!");
        }
    }

    public void StopRecording()
    {
        if (Microphone.IsRecording(microphone))
        {
            Microphone.End(microphone);
            SaveAudioClip(audioClip);
        }
    }

    void SaveAudioClip(AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);

        var wavFile = ConvertToWav(samples, clip.channels, clip.frequency);
        string filePath = Application.persistentDataPath + "/recorded.wav";
        File.WriteAllBytes(filePath, wavFile);
        Debug.Log("Audio saved as " + filePath);
    }

    byte[] ConvertToWav(float[] samples, int channels, int sampleRate)
    {
        int sampleCount = samples.Length;
        int byteCount = sampleCount * sizeof(short);

        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + byteCount);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * sizeof(short));
                writer.Write((short)(channels * sizeof(short)));
                writer.Write((short)16);
                writer.Write("data".ToCharArray());
                writer.Write(byteCount);

                for (int i = 0; i < sampleCount; i++)
                {
                    var sample = (short)(samples[i] * short.MaxValue);
                    writer.Write(sample);
                }
            }
            return memoryStream.ToArray();
        }
    }
}
