using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class P4LoginManager : MonoBehaviour
{
    [SerializeField] TMP_InputField serverInput;
    [SerializeField] TMP_InputField userInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] ButtonController loginButton;
    [SerializeField] TextMeshProUGUI statusLabel;


    void Start() 
    {
        loginButton.OnClick.AddListener(OnLoginAttempt);
    }

    void OnLoginAttempt()
    {
        string server = serverInput.text.Trim();
        string user = userInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            UIManager.Instance.AddLog("Server, User, and Password fields cannot be empty.", true);
            statusLabel.text = "Login failed: Missing fields.";
            return;
        }

        P4.P4Port = server;
        P4.P4User = user;

        statusLabel.text = "Attempting to log in...";
        UIManager.Instance.AddLog($"Logging in to {server} as {user}.");

        var (output, error) = P4.RunCommand("login", password + "\n");

        if (!string.IsNullOrEmpty(error) && !error.Contains("Password invalid or expired"))
        {
            statusLabel.text = $"Login Failed: {error}";
            UIManager.Instance.AddLog($"P4 Login Failed: {error}", true);
        }
        else if (output.Contains("User") && output.Contains("logged in."))
        {
            statusLabel.text = "Login Successful! Ready to proceed.";
            UIManager.Instance.AddLog("P4 Login Successful. Environment variables set. Go back to Main Menu, and proceed with other operations.");
        }
        else
        {
            statusLabel.text = "Login Failed. Check user, password, or server address.";
            UIManager.Instance.AddLog("P4 Login Failed. Check credentials/connection.", true);
        }
        
    }
}
