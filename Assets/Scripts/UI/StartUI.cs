using System;
using UnityEngine;
using UnityEngine.UI;

public class StartUI : MonoBehaviour
{
    public RawImage signImage;
    public GameObject closeButton;
    public AudioSource backgroundVoice;
    public AudioClip voiceClip;

    void Awake()
    {
        gameObject.SetActive(true);

        if (signImage == null)
            signImage = gameObject.AddComponent<RawImage>();
        signImage.enabled = true;
        
        if (backgroundVoice == null)
            backgroundVoice = gameObject.AddComponent<AudioSource>();

        backgroundVoice.clip = voiceClip;
        backgroundVoice.loop = false;
        
    }

    public void Initialize()
    {
        backgroundVoice.Play();
        Invoke(nameof(Close), voiceClip.length);

        GameController.SetPaused(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    private void Close()
    {
        Destroy(gameObject);
        if  (Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
        }
    }

    public void CloseSign()
    {
        signImage.enabled = false;
        closeButton.SetActive(false);
        GameController.SetPaused(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }
}
