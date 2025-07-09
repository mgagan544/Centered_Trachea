using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class SimpleTriggerDetector : MonoBehaviour
{
    [Header("Highlight Material")]
    public Material highlightMaterial;
    private Material originalMaterial;

    [Header("Linked References")]
    public Start_script startScript; // To check if we're in practice mode
    public Eval_Script evalScript; // For SubmitLandmark call
    public string areaName;

    [Header("Controller Tags")]
    public string leftControllerTag = "newtag1L";
    public string rightControllerTag = "newtag1R";

    [Header("Audio Settings")]
    public AudioClip audioClip;

    [Range(0f, 1f)]
    public float audioVolume = 0.7f;
    public bool loopAudio = false;

    [Header("Haptic Feedback")]
    [Range(0f, 1f)]
    public float hapticStrength = 0.5f;
    private float hapticCooldown = 0.2f;
    private float hapticTimer = 0f;

    private AudioSource audioSource;
    private Renderer objRenderer;
    private bool isControllerInside = false;
    private OVRInput.Controller currentController = OVRInput.Controller.None;

    void Awake()
    {
        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = loopAudio;
            audioSource.volume = audioVolume;
            audioSource.clip = audioClip;
        }

        // Renderer
        objRenderer = GetComponent<Renderer>();
        if (objRenderer == null)
            objRenderer = GetComponentInChildren<Renderer>();

        if (objRenderer != null)
            originalMaterial = objRenderer.material;

        // Collider setup
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        // Rigidbody setup
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (isControllerInside && currentController != OVRInput.Controller.None)
        {
            hapticTimer -= Time.deltaTime;
            if (hapticTimer <= 0f)
            {
                OVRInput.SetControllerVibration(hapticStrength, hapticStrength, currentController);
                hapticTimer = hapticCooldown;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        bool isLeft = other.CompareTag(leftControllerTag);
        bool isRight = other.CompareTag(rightControllerTag);
        if (!isLeft && !isRight)
            return;

        currentController = isLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        isControllerInside = true;

        // Haptic feedback (applies to both modes)
        OVRInput.SetControllerVibration(hapticStrength, hapticStrength, currentController);
        hapticTimer = hapticCooldown;

        // Practice mode effects
        if (startScript != null && startScript.practiceMode)
        {
            if (objRenderer != null && highlightMaterial != null)
                objRenderer.material = highlightMaterial;

            if (audioSource != null && audioClip != null)
            {
                audioSource.time = 0f;
                audioSource.Play();
            }
        }

        // Evaluation logic
        evalScript?.SubmitLandmark(areaName);
    }

    void OnTriggerExit(Collider other)
    {
        bool isLeft = other.CompareTag(leftControllerTag);
        bool isRight = other.CompareTag(rightControllerTag);
        if (!isLeft && !isRight)
            return;

        isControllerInside = false;
        OVRInput.SetControllerVibration(0, 0, currentController);
        currentController = OVRInput.Controller.None;

        // Revert highlight and stop audio in practice mode
        if (startScript != null && startScript.practiceMode)
        {
            if (objRenderer != null && originalMaterial != null)
                objRenderer.material = originalMaterial;

            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }
    }
}
