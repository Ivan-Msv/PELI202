using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSounds : MonoBehaviour, IPointerEnterHandler
{
    private Button thisButton;
    // Serialized in case you want to use custom sounds for some buttons
    [SerializeField] private SoundType onHover = SoundType.MenuButtonHover;
    [SerializeField] private SoundType onClick = SoundType.MenuButtonClick;

    private void Awake()
    {
        thisButton = GetComponent<Button>();

        // The reason onClick isn't the same way as OnPointerEnter
        // Is because some "toggle" buttons played the sound twice
        thisButton.onClick.AddListener(() => { AudioManager.instance.PlaySoundLocal(onClick); });
    }
    public void OnPointerEnter(PointerEventData ped)
    {
        if (!thisButton.interactable) { return; }

        AudioManager.instance.PlaySoundLocal(onHover);
    }
}
