using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("References")]
    RectTransform _buttonRectTransform;
    Image _buttonImage;
    [SerializeField] TMPro.TextMeshProUGUI _buttonTMPro;

    [Header("Settings")]
    [SerializeField] string _buttonText = "Default";
    
    [Header("Animation Settings")]
    [SerializeField] float _hoverScale = 1.1f;
    [SerializeField] float _animationDuration = 0.2f;
    [SerializeField] Ease _easeType = Ease.OutBack;

    [Header("Events")]
    [SerializeField] UnityEngine.Events.UnityEvent _onClick;
    public UnityEngine.Events.UnityEvent OnClick
    {
        get { return _onClick; }
    }
    bool interactable = true;
    public bool Interactable
    {
        get { return interactable; }
        set { 
            interactable = value; 
            if (_buttonImage != null)
            {
                _buttonImage.raycastTarget = interactable;
                _buttonImage.color = interactable ? Color.white : Color.gray;
            }
        }
    }

    private void OnValidate() 
    {
        if (_buttonTMPro != null)
        {
            _buttonTMPro.text = _buttonText;
        }
    }

    private void Awake() {
        _buttonRectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _buttonRectTransform.DOScale(_hoverScale, _animationDuration).SetEase(_easeType);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _buttonRectTransform.DOScale(1f, _animationDuration).SetEase(_easeType);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _buttonRectTransform.DOScale(_hoverScale * 0.95f, _animationDuration / 2).SetEase(_easeType);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _buttonRectTransform.DOScale(_hoverScale, _animationDuration / 2).SetEase(_easeType);

        if (RectTransformUtility.RectangleContainsScreenPoint(_buttonRectTransform, eventData.position, eventData.enterEventCamera))
        {
            _onClick?.Invoke();
        }
    }
}
