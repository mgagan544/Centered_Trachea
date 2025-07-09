using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class TracheaTrigger : MonoBehaviour
{
    [Header("Highlight Material")]
    public Material highlightMaterial;
    private Material originalMaterial;

    [Header("Linked References")]
    public Start_script startScript; // Reference to determine practiceMode
    public Eval_Script evalScript; // Called to submit landmarks
    public string areaName;

    [Header("Controller Tags")]
    public string leftControllerTag = "newtag1L";
    public string rightControllerTag = "newtag1R";

    [Header("Audio Settings")]
    public AudioClip audioClip;

    [Range(0f, 1f)]
    public float audioVolume = 0.7f;
    public bool loopAudio = false;

    [Header("Haptic Settings")]
    [Range(0f, 1f)]
    public float hapticStrength = 0.5f;
    public float pulseDuration = 0.1f;
    public float pulseInterval = 0.1f;

    private AudioSource audioSource;
    private Renderer objRenderer;
    private Material currentMaterial;

    private OVRInput.Controller currentController = OVRInput.Controller.None;
    private Coroutine hapticCoroutine;

    private bool isPracticeMode => startScript != null && startScript.practiceMode;

    void Awake()
    {
        // Renderer and Material
        objRenderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        if (objRenderer != null)
            originalMaterial = objRenderer.material;

        // Collider and Rigidbody
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = loopAudio;
            audioSource.volume = audioVolume;
            audioSource.clip = audioClip;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        bool isLeft = other.CompareTag(leftControllerTag);
        bool isRight = other.CompareTag(rightControllerTag);

        if (!isLeft && !isRight)
            return;

        currentController = isLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;

        // Start pulsating haptics
        if (hapticCoroutine == null)
            hapticCoroutine = StartCoroutine(PulsateHaptics());

        // Submit landmark
        evalScript?.SubmitLandmark(areaName);

        // Practice mode extras
        if (isPracticeMode)
        {
            if (objRenderer != null && highlightMaterial != null)
                objRenderer.material = highlightMaterial;

            if (audioSource != null && audioClip != null)
            {
                audioSource.time = 0f;
                audioSource.Play();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        bool isLeft = other.CompareTag(leftControllerTag);
        bool isRight = other.CompareTag(rightControllerTag);

        if (!isLeft && !isRight)
            return;

        // Stop haptics
        if (hapticCoroutine != null)
        {
            StopCoroutine(hapticCoroutine);
            hapticCoroutine = null;
            OVRInput.SetControllerVibration(0, 0, currentController);
            currentController = OVRInput.Controller.None;
        }

        // Practice cleanup
        if (isPracticeMode)
        {
            if (objRenderer != null && originalMaterial != null)
                objRenderer.material = originalMaterial;

            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }
    }

    IEnumerator PulsateHaptics()
    {
        while (true)
        {
            OVRInput.SetControllerVibration(hapticStrength, hapticStrength, currentController);
            yield return new WaitForSeconds(pulseDuration);
            OVRInput.SetControllerVibration(0, 0, currentController);
            yield return new WaitForSeconds(pulseInterval);
        }
    }
}
