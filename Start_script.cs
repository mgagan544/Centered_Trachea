using UnityEngine;

public class Start_script : MonoBehaviour
{
    [SerializeField]
    Collider trigger;

    [SerializeField]
    GameObject robe;

    [SerializeField]
    GameObject button;

    [SerializeField]
    GameObject Markers;

    [SerializeField]
    GameObject panel;

    [SerializeField]
    GameObject hud;

    [SerializeField]
    GameObject skin1;

    [SerializeField]
    GameObject skin2;

    [SerializeField]
    GameObject endsession;

    [SerializeField]
    GameObject selector;

    public bool practiceMode = true; // Set via IntentManager

    void Start()
    {
        if (skin1 != null)
        {
            skin1.SetActive(true);
        }
        if (skin2 != null)
        {
            skin2.SetActive(false);
        }
        if (trigger == null)
        {
            Debug.Log("No collider assigned");
        }
        if (robe != null)
        {
            robe.SetActive(true);
        }
        if (Markers != null)
        {
            Markers.SetActive(false);
        }
        if (panel != null)
        {
            panel.SetActive(false);
        }
        if (endsession != null)
        {
            endsession.SetActive(false);
        }
        if (hud != null)
        {
            hud.SetActive(false);
        }
        if (selector != null)
        {
            selector.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (practiceMode)
        {
            skin2.SetActive(true);
            skin1.SetActive(false);
            Markers.SetActive(true);
            endsession.SetActive(true);
            selector.SetActive(true);
        }
        if (!practiceMode)
        {
            panel.SetActive(true);
        }
        robe.SetActive(false);
        button.SetActive(false);
        hud.SetActive(true);
        //objectManager.SetNextActive(currentIndex);
    }
}
