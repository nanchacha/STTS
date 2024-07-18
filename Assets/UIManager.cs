using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UIManager : MonoBehaviour
{
    public Button recordButton;
    public TextMeshProUGUI buttonText; // Text or TextMeshProUGUI depending on your setup
    private MicrophoneRecorder microphoneRecorder;
    private WhisperAPI whisperAPI;
    public TextMeshProUGUI responseText; // GPT-4 응답을 표시할 텍스트 UI

    private bool isRecording = false;

    void Start()
    {
        microphoneRecorder = GetComponent<MicrophoneRecorder>();
        whisperAPI = GetComponent<WhisperAPI>();

        if (microphoneRecorder == null)
        {
            Debug.LogError("MicrophoneRecorder component is not assigned or found.");
        }
        if (whisperAPI == null)
        {
            Debug.LogError("WhisperAPI component is not assigned or found.");
        }
        if (recordButton == null)
        {
            Debug.LogError("Record button is not assigned.");
        }
        if (buttonText == null)
        {
            Debug.LogError("Button text is not assigned.");
        }
        
        if (responseText == null)
        {
            Debug.LogError("Response text is not assigned.");
        }
        else
        {
            whisperAPI.responseText = responseText; // WhisperAPI에 텍스트 UI 설정
        }

        recordButton.onClick.AddListener(OnRecordButtonClick);
    }

    void OnRecordButtonClick()
    {
        if (!isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    void StartRecording()
    {
        isRecording = true;
        buttonText.text = "Recording...";
        microphoneRecorder.StartRecording();
    }

    void StopRecording()
    {
        isRecording = false;
        buttonText.text = "Start Recording";
        microphoneRecorder.StopRecording();
        StartCoroutine(whisperAPI.UploadAudioAndTranscribe());
    }
}
