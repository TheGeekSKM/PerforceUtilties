// Filename: UIManager.cs
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private void Awake() 
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return; 
        }
    }

    [SerializeField] RectTransform _rootPanel;
    [SerializeField] RectTransform _mainMenuPanel;
    [SerializeField] RectTransform _loginPanel;
    [SerializeField] RectTransform _excelImporterPanel;
    [SerializeField] RectTransform _userCreationPanel;
    [SerializeField] RectTransform _userDeleterPanel;
    [SerializeField] List<AppLogger> _loggers; 

    void Start()
    {
        ShowMainMenu();
    }

    public void AddLog(string message, bool isError = false)
    {
        if (_loggers == null) return;

        foreach (var logger in _loggers)
        {
            if (logger != null) 
            {
                logger.Log(message, isError);
            }
        }
    }

    void HideAllPanels()
    {
        _mainMenuPanel.gameObject.SetActive(false);
        _loginPanel.gameObject.SetActive(false);
        _excelImporterPanel.gameObject.SetActive(false);
        _userCreationPanel.gameObject.SetActive(false);
        _userDeleterPanel.gameObject.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        _mainMenuPanel.gameObject.SetActive(true);
        _rootPanel.DOAnchorPosX(0, 0.25f).SetEase(Ease.OutCubic);
    }

    public void ShowExcelImporter()
    {
        HideAllPanels();
        _excelImporterPanel.gameObject.SetActive(true);
        _rootPanel.DOAnchorPosX(-600, 0.25f).SetEase(Ease.OutCubic);
    }

    public void ShowUserCreation()
    {
        HideAllPanels();
        _userCreationPanel.gameObject.SetActive(true);
        _rootPanel.DOAnchorPosX(-1200, 0.25f).SetEase(Ease.OutCubic);
    }

    public void ShowUserDeleter()
    {
        HideAllPanels();
        _userDeleterPanel.gameObject.SetActive(true);
        _rootPanel.DOAnchorPosX(-1800, 0.25f).SetEase(Ease.OutCubic);
    }

    public void ShowLogin()
    {
        HideAllPanels();
        _loginPanel.gameObject.SetActive(true);
        _rootPanel.DOAnchorPosX(600, 0.25f).SetEase(Ease.OutCubic);
    }

    public void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}