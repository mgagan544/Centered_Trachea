using UnityEngine;
using TMPro;
using Oculus.Interaction;

public class MCQButtonHandler_tt : MonoBehaviour
{
    [SerializeField] private TMP_Text label; // button lext
    [SerializeField] private PokeInteractable pokeInteractable; // button poke interactable
    [SerializeField] private Eval_Script manager; // test mode manager script to access the public functions 

    private string optionText; // variable to store the option

    private void Awake()
    {
        pokeInteractable.WhenPointerEventRaised += HandlePointerEvent; //subscribe to point event
    }

    private void OnDestroy()
    {
        pokeInteractable.WhenPointerEventRaised -= HandlePointerEvent; // unsubscribe from point event on destroy
    }

    public void SetOption(string text) //public function that is called by manager to set the option
    {
        optionText = text;
        if (label != null)
        {
            label.text = text;
        }
    }

    private void HandlePointerEvent(PointerEvent evt) //function that detects point event and sends the option to manager
    {
        if (evt.Type == PointerEventType.Unselect)
        {
            manager.OnOptionSelected(optionText);
        }
    }
}

// Shuffle Extension

