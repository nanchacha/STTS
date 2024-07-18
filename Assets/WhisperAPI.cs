using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq; // JSON 파싱을 위해 Newtonsoft.Json 패키지 사용
using TMPro;

// elevenlabs api key = sk_3f6bb06ac27bb51e26c45cdcf0954024b4eac327d37a0ea6
// elevenlabs voice_id = XrExE9yKIg1WjnnlVkGX

public class WhisperAPI : MonoBehaviour
{
    private string whisperApiKey = "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"; // Whisper API 키
    private string gptApiKey = "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"; // GPT-4 API 키
    public TextMeshProUGUI responseText; // GPT-4 응답을 표시할 텍스트 UI
    private string elevenLabsApiKey = "sk_3f6bb06ac27bb51e26c45cdcf0954024b4eac327d37a0ea6"; // ElevenLabs API 키
    private string voiceId = "YOrYO796QMeGe8zn4CLy"; // YOrYO796QMeGe8zn4CLy - Natasha - Valley girl, uk7kXcoBjHAb8ioRtYNO - Funny Jackie Lee
    private string inShort = "영어로 3줄로 대답해줘";
    public AudioSource audioSource; // AudioSource 컴포넌트
    public IEnumerator UploadAudioAndTranscribe()
    {
        string filePath = Application.persistentDataPath + "/recorded.wav";
        Debug.Log("Looking for audio file at: " + filePath);

        if (!File.Exists(filePath))
        {
            Debug.LogError("Audio file not found!");
            yield break;
        }

        byte[] fileData = System.IO.File.ReadAllBytes(filePath);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, "recorded.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        UnityWebRequest www = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", form);
        www.SetRequestHeader("Authorization", "Bearer " + whisperApiKey);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            Debug.Log("Transcription: " + www.downloadHandler.text);
            string transcription = ExtractTranscription(www.downloadHandler.text) + inShort; // 텍스트 결과 추출
            if (!string.IsNullOrEmpty(transcription))
            {
                StartCoroutine(CallChatGPT(transcription)); // GPT-4 API 호출
            }
            else
            {
                Debug.LogError("Transcription is empty.");
            }
        }
    }

    private string ExtractTranscription(string jsonResponse)
    {
        // jsonResponse에서 텍스트 추출
        var json = JObject.Parse(jsonResponse);
        return json["text"]?.ToString();
    }

    private IEnumerator CallChatGPT(string userMessage)
    {
        string gptUrl = "https://api.openai.com/v1/chat/completions";
        var requestData = new
        {
            model = "gpt-4o",
            messages = new[]
            {
                new { role = "system", content = "You are a English tutor for kids." },
                new { role = "user", content = userMessage }
            }
        };
        string json = JObject.FromObject(requestData).ToString(); // Newtonsoft.Json 사용

        UnityWebRequest www = new UnityWebRequest(gptUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + gptApiKey);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            // Debug.Log("GPT-4 Response: " + www.downloadHandler.text);
            string responseContent = ExtractGptResponseContent(www.downloadHandler.text);
            Debug.Log("GPT-4 Content: " + responseContent);

            
            if (responseText != null)
            {
                responseText.text = responseContent; // TextMeshProUGUI UI에 GPT-4 응답 표시
                StartCoroutine(CallElevenLabsTTS(responseContent)); // TTS 호출
            }
        }
    }

    private static string ExtractGptResponseContent(string jsonResponse)
    {
        // jsonResponse에서 content 추출
        var json = JObject.Parse(jsonResponse);
        var choices = json["choices"] as JArray;
        if (choices != null && choices.Count > 0)
        {
            var message = choices[0]["message"];
            if (message != null)
            {
                return message["content"]?.ToString();
            }
        }
        return null;
    }

    private IEnumerator CallElevenLabsTTS(string text)
    {
        string ttsUrl = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}"; // ElevenLabs TTS API URL
        var requestData = new
        {
            text = text,
            model_id = "eleven_multilingual_v2",
            voice_settings = new
            {
                stability = 0.5,
                similarity_boost = 0.5,
                style = 0.5,
                use_speaker_boost = true
            },
            format = "mpeg"
        };

        string json = JObject.FromObject(requestData).ToString(); // Newtonsoft.Json 사용
        UnityWebRequest www = new UnityWebRequest(ttsUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("xi-api-key", elevenLabsApiKey);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            Debug.Log("ElevenLabs TTS Response: " + www.downloadHandler.text);
            Debug.Log("API response correctly");
            byte[] audioData = www.downloadHandler.data;
            StartCoroutine(PlayAudioClip(audioData));
        }
    }

     private IEnumerator PlayAudioClip(byte[] audioData)
    {
        string tempPath = Path.Combine(Application.persistentDataPath, "tempAudio.mpeg");
        File.WriteAllBytes(tempPath, audioData);
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                if (audioSource != null)
                {
                    audioSource.clip = audioClip;
                    audioSource.Play();
                }
                else
                {
                    Debug.LogError("AudioSource is not assigned.");
                }
            }
        }
    }
}
