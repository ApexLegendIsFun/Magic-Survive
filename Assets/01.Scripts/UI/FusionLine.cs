using UnityEngine;
using UnityEngine.UI;

public class FusionLine : MonoBehaviour
{
    [SerializeField] private Image lineImage;
    [SerializeField] private Button lineButton;

    [SerializeField] private Color activeColor;
    [SerializeField] private Color lockedColor;

    public void SetActive(bool active)
    {
        lineImage.color = active ? activeColor : lockedColor;
    }
}