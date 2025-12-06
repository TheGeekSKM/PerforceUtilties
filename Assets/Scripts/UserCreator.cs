using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class UserCreator : MonoBehaviour
{
    private class UserData
    {
        public string FirstName;
        public string LastName;
        public string Email;
        public string Password;
    }

    private class CreatedUserEntry
    {
        public string Username;
        public string FullName;
        public string Email;
        public string Password;
        public string Group;
    }

    [Header("UI References")]
    public TMP_InputField groupInput;
    
    [Header("Editable User Fields")]
    public TMP_InputField usernameInput; 
    public TMP_InputField fullNameInput; 
    public TMP_InputField emailInput;    
    public TMP_InputField passwordInput; 

    [Header("Buttons")]
    public ButtonController loadCSVButton;
    public ButtonController continueButton;
    public ButtonController skipButton;
    public ButtonController editButton; 
    
    private List<UserData> usersToCreate = new List<UserData>();
    private List<CreatedUserEntry> successLog = new List<CreatedUserEntry>(); // Store successful creations here
    private int currentUserIndex = -1;
    private string csvPath;

    void Start()
    {
        loadCSVButton.OnClick.AddListener(OnLoadCSV);
        continueButton.OnClick.AddListener(OnContinue);
        skipButton.OnClick.AddListener(OnSkip);
        editButton.OnClick.AddListener(OnEdit); 
        
        csvPath = Path.Combine(Application.persistentDataPath, "perforce_users.csv");
        
        UIManager.Instance.AddLog($"Ready. Will load from {csvPath}.");
        SetDisplayActive(false);
    }

    private void OnLoadCSV()
    {
        if (!File.Exists(csvPath))
        {
            UIManager.Instance.AddLog("CSV file not found. Run Importer.", true);
            return;
        }

        usersToCreate.Clear();
        successLog.Clear(); 
        UIManager.Instance.AddLog($"Loading CSV from {csvPath}...");
        
        try
        {
            var lines = File.ReadAllLines(csvPath);
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var parts = lines[i].Split(',');
                if (parts.Length < 3) continue; 

                var userData = new UserData
                {
                    FirstName = parts[0].Trim(),
                    LastName = parts[1].Trim(),
                    Email = parts[2].Trim()
                };
                if (parts.Length >= 4) userData.Password = parts[3].Trim();

                usersToCreate.Add(userData);
            }
            
            UIManager.Instance.AddLog($"Loaded {usersToCreate.Count} users.");
            currentUserIndex = 0;
            DisplayCurrentUser();
            SetDisplayActive(true);
        }
        catch (System.Exception ex)
        {
            UIManager.Instance.AddLog($"Error loading CSV: {ex.Message}", true);
        }
    }

    private void DisplayCurrentUser()
    {
        if (currentUserIndex < 0 || currentUserIndex >= usersToCreate.Count)
        {
            UIManager.Instance.AddLog("All users processed.");
            SetDisplayActive(false);
            
            SaveCreatedUsersLog();
            return;
        }

        UserData currentUser = usersToCreate[currentUserIndex];
        
        string genUsername = $"{currentUser.FirstName}{currentUser.LastName}".Replace(" ", "");
        string genFullName = $"{currentUser.FirstName} {currentUser.LastName}";
        string genPassword = !string.IsNullOrEmpty(currentUser.Password) ? currentUser.Password : GenerateRandomPassword();
        
        usernameInput.text = genUsername;
        fullNameInput.text = genFullName;
        emailInput.text = currentUser.Email;
        passwordInput.text = genPassword;
        
        SetFieldsInteractable(false);
    }

    private void OnEdit()
    {
        SetFieldsInteractable(true);
        UIManager.Instance.AddLog("Edit mode enabled.");
        usernameInput.Select(); 
    }

    private void OnContinue()
    {
        if (currentUserIndex < 0 || currentUserIndex >= usersToCreate.Count) return;
        if (string.IsNullOrWhiteSpace(groupInput.text))
        {
            UIManager.Instance.AddLog("Group name cannot be empty.", true);
            return;
        }

        string finalUsername = usernameInput.text.Trim();
        string finalFullName = fullNameInput.text.Trim();
        string finalEmail = emailInput.text.Trim();
        string finalPassword = passwordInput.text.Trim();
        string groupName = groupInput.text.Trim();

        if(string.IsNullOrEmpty(finalUsername) || string.IsNullOrEmpty(finalPassword))
        {
            UIManager.Instance.AddLog("Username and Password cannot be empty.", true);
            return;
        }

        UIManager.Instance.AddLog($"Creating user: {finalUsername}...");
        P4.LogMockEvent($"Creating user {finalUsername}...");

        var userSpec = new StringBuilder();
        userSpec.AppendLine($"User: {finalUsername}");
        userSpec.AppendLine($"FullName: {finalFullName}");
        userSpec.AppendLine($"Email: {finalEmail}");
        userSpec.AppendLine("Type: standard");
        
        var (userOut, userErr) = P4.RunCommand("user -i", userSpec.ToString());
        
        bool success = true;
        if (!string.IsNullOrEmpty(userErr))
        {
            UIManager.Instance.AddLog($"Failed to create user {finalUsername}: {userErr}", true);
            success = false;
        }
        else
        {
            UIManager.Instance.AddLog($"User {finalUsername} created.");
        }
        
        string passwordInputStr = $"{finalPassword}\n{finalPassword}\n";
        P4.RunCommand($"passwd -u {finalUsername}", passwordInputStr);
        P4.RunCommand($"group -a -u {finalUsername} {groupName}");

        if (success)
        {
            successLog.Add(new CreatedUserEntry 
            {
                Username = finalUsername,
                FullName = finalFullName,
                Email = finalEmail,
                Password = finalPassword,
                Group = groupName
            });
        }

        currentUserIndex++;
        DisplayCurrentUser();
    }
    
    private void OnSkip()
    {
        if (currentUserIndex < 0 || currentUserIndex >= usersToCreate.Count) return;
        UIManager.Instance.AddLog($"Skipped user: {usernameInput.text}");
        currentUserIndex++;
        DisplayCurrentUser();
    }

    private void SaveCreatedUsersLog()
    {
        if (successLog.Count == 0) return;

        string logPath = Path.Combine(Application.persistentDataPath, "created_users_log.csv");
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("Username,FullName,Email,Password,Group"); 
        
        foreach (var entry in successLog)
        {
            csvBuilder.AppendLine($"{entry.Username},\"{entry.FullName}\",{entry.Email},{entry.Password},{entry.Group}");
        }
        
        try 
        {
            File.WriteAllText(logPath, csvBuilder.ToString());
            UIManager.Instance.AddLog($"<color=green>SUCCESS LOG SAVED:</color> {logPath}");
            
            // Optional: Open the folder so the user sees it
            Application.OpenURL("file://" + Application.persistentDataPath);
        }
        catch (System.Exception ex)
        {
            UIManager.Instance.AddLog($"Failed to save log: {ex.Message}", true);
        }
    }
    
    private void SetDisplayActive(bool isActive)
    {
        usernameInput.transform.parent.gameObject.SetActive(isActive);
        continueButton.Interactable = isActive;
        skipButton.Interactable = isActive;
        editButton.Interactable = isActive;
    }

    private void SetFieldsInteractable(bool isInteractable)
    {
        usernameInput.interactable = isInteractable;
        fullNameInput.interactable = isInteractable;
        emailInput.interactable = isInteractable;
        passwordInput.interactable = isInteractable;
    }

    private string GenerateRandomPassword(int length = 12)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%";
        var sb = new StringBuilder();
        var random = new System.Random();
        for (int i = 0; i < length; i++) sb.Append(validChars[random.Next(validChars.Length)]);
        return sb.ToString();
    }
}