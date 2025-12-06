using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class UserDeleter : MonoBehaviour
{
    private class P4UserDetail
    {
        public string Username;
        public string FullName;
        public string Email;
    }

    // New struct to track the action taken for the final log
    private struct ProcessedUserEntry
    {
        public P4UserDetail User;
        public string Action; // "Deleted" or "Skipped"
    }

    [Header("UI References")]
    public TMP_InputField groupInput;
    public ButtonController fetchButton;
    
    [Space(10)]
    public GameObject userDisplayGroup; 
    public TextMeshProUGUI confirmationLabel; 
    public TextMeshProUGUI fullNameLabel;     
    public TextMeshProUGUI emailLabel;        
    
    public ButtonController deleteButton;
    public ButtonController skipButton;

    private List<P4UserDetail> usersToDelete = new List<P4UserDetail>();
    private List<ProcessedUserEntry> processedUsersLog = new List<ProcessedUserEntry>();
    private int currentUserIndex = -1;

    void Start()
    {
        fetchButton.OnClick.AddListener(OnFetchUsers);
        deleteButton.OnClick.AddListener(OnDelete);
        skipButton.OnClick.AddListener(OnSkip);
        
        userDisplayGroup.SetActive(false); 
    }

    private void OnFetchUsers()
    {
        string groupName = groupInput.text.Trim();
        if (string.IsNullOrWhiteSpace(groupName))
        {
            UIManager.Instance.AddLog("Group name cannot be empty.", true);
            return;
        }

        UIManager.Instance.AddLog($"Fetching group spec: {groupName}...");
        
        var (output, error) = P4.RunCommand($"group -o {groupName}");

        if (!string.IsNullOrEmpty(error) && !error.ToLower().Contains("group not found")) 
        {
            UIManager.Instance.AddLog($"Error fetching group: {error}", true);
            return;
        }

        List<string> usernames = new List<string>();
        bool inUsersSection = false;
        var lines = output.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("Users:"))
            {
                inUsersSection = true;
                continue;
            }

            if (inUsersSection)
            {
                if (!line.StartsWith("\t") && !line.StartsWith(" "))
                {
                    inUsersSection = false; 
                    continue;
                }
                usernames.Add(line.Trim());
            }
        }

        if (usernames.Count == 0)
        {
            UIManager.Instance.AddLog("No users found in this group.");
            return;
        }

        UIManager.Instance.AddLog($"Found {usernames.Count} members. Fetching details...");
        string userListStr = string.Join(" ", usernames);
        
        var (usersOut, usersErr) = P4.RunCommand($"users {userListStr}");
        
        usersToDelete.Clear();
        processedUsersLog.Clear();
        
        var userLines = usersOut.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in userLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var userDetail = new P4UserDetail();
            
            int firstSpace = line.IndexOf(' ');
            if (firstSpace > 0) userDetail.Username = line.Substring(0, firstSpace);

            int emailStart = line.IndexOf('<');
            int emailEnd = line.IndexOf('>');
            if (emailStart > -1 && emailEnd > emailStart)
                userDetail.Email = line.Substring(emailStart + 1, emailEnd - emailStart - 1);
            else
                userDetail.Email = "Unknown Email";
            
            int nameStart = line.IndexOf('(');
            int nameEnd = line.IndexOf(')');
            if (nameStart > -1 && nameEnd > nameStart)
                userDetail.FullName = line.Substring(nameStart + 1, nameEnd - nameStart - 1);
            else
                userDetail.FullName = "Unknown Name";
            
            if (!string.IsNullOrEmpty(userDetail.Username)) usersToDelete.Add(userDetail);
        }
        
        if (usersToDelete.Count > 0)
        {
            UIManager.Instance.AddLog($"Ready to process {usersToDelete.Count} users.");
            currentUserIndex = 0;
            DisplayCurrentUser();
            userDisplayGroup.SetActive(true);
        }
        else
        {
            UIManager.Instance.AddLog("Could not parse user details.");
        }
    }

    private void DisplayCurrentUser()
    {
        if (currentUserIndex < 0 || currentUserIndex >= usersToDelete.Count)
        {
            UIManager.Instance.AddLog("All users processed. Saving log...");
            userDisplayGroup.SetActive(false);
            SaveLog();
            return;
        }
        
        P4UserDetail user = usersToDelete[currentUserIndex];
        
        confirmationLabel.text = $"Do you want to delete {user.Username}?";
        fullNameLabel.text = $"Full Name: {user.FullName}";
        emailLabel.text = $"Email: {user.Email}";
    }

    private void OnDelete()
    {
        if (currentUserIndex < 0 || currentUserIndex >= usersToDelete.Count) return;
        
        P4UserDetail user = usersToDelete[currentUserIndex];
        string username = user.Username;

        UIManager.Instance.AddLog($"Deleting user: {username}...");
        
        P4.LogMockEvent($"Deleting user {username}...");

        var (output, error) = P4.RunCommand($"user -d -f {username}");
        
        if (!string.IsNullOrEmpty(error))
        {
            UIManager.Instance.AddLog($"Failed to delete {username}: {error}", true);
        }
        else
        {
            UIManager.Instance.AddLog($"Successfully deleted {username}.");
            processedUsersLog.Add(new ProcessedUserEntry { User = user, Action = "Deleted" });
        }

        currentUserIndex++;
        DisplayCurrentUser();
    }

    private void OnSkip()
    {
        if (currentUserIndex < 0 || currentUserIndex >= usersToDelete.Count) return;

        P4UserDetail user = usersToDelete[currentUserIndex];
        string username = user.Username;

        UIManager.Instance.AddLog($"Skipped user: {username}");
        
        P4.LogMockEvent($"Skipped user: {username}");

        processedUsersLog.Add(new ProcessedUserEntry { User = user, Action = "Skipped" });

        currentUserIndex++;
        DisplayCurrentUser();
    }

    private void SaveLog()
    {
        string logPath = Path.Combine(Application.persistentDataPath, "deleted_users_log.csv");
        var csvBuilder = new StringBuilder();
        
        csvBuilder.AppendLine("Username,FullName,Email,Action"); 
        
        foreach (var entry in processedUsersLog)
        {
            csvBuilder.AppendLine($"{entry.User.Username},\"{entry.User.FullName}\",{entry.User.Email},{entry.Action}");
        }
        
        File.WriteAllText(logPath, csvBuilder.ToString());
        UIManager.Instance.AddLog($"Log of processed users saved to {logPath}");

        Application.OpenURL(Path.GetDirectoryName(logPath));
    }
}