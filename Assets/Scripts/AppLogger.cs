using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AppLogger : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI logText;
    [SerializeField] ScrollRect scrollRect;

    private StringBuilder logBuilder = new StringBuilder();
    private const int MaxLogLines = 200;

    public void Log(string message, bool isError = false)
    {
        if (isError)
        {
            logBuilder.AppendLine($"<color=red>ERROR: {message}</color>");
            UnityEngine.Debug.LogWarning(message);
        }
        else
        {
            logBuilder.AppendLine(message);
            UnityEngine.Debug.Log(message);
        }

        if (logBuilder.Length > MaxLogLines * 100)
        {
            int cutOff = logBuilder.ToString().IndexOf('\n', logBuilder.Length / 2);
            if (cutOff > 0) logBuilder.Remove(0, cutOff);
        }

        if (logText != null)
        {
            logText.text = logBuilder.ToString();
        }

        if (this.gameObject.activeInHierarchy)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    public void ClearLog()
    {
        logBuilder.Clear();
        if (logText != null) logText.text = "";
    }

    private void OnEnable()
    {
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}