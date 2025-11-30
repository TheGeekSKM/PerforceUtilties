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

    [Header("UI References")]
    public TMP_InputField groupInput;
    public Button fetchButton;
    
    [Space(10)]
    public GameObject userDisplayGroup; 
    public TextMeshProUGUI confirmationLabel; 
    
    public Button deleteButton;
    public Button skipButton;

    private List<P4UserDetail> usersToDelete = new List<P4UserDetail>();
    private List<P4UserDetail> deletedUsersLog = new List<P4UserDetail>();
    private int currentUserIndex = -1;

    void Start()
    {
        fetchButton.onClick.AddListener(OnFetchUsers);
        deleteButton.onClick.AddListener(OnDelete);
        skipButton.onClick.AddListener(OnSkip);
        
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

        UIManager.Instance.AddLog($"Fetching users from group: {groupName}...");
        var (output, error) = P4.RunCommand($"users -g {groupName}");

        if (!string.IsNullOrEmpty(error))
        {
            UIManager.Instance.AddLog($"Error fetching users: {error}", true);
            return;
        }
        
        usersToDelete.Clear();
        deletedUsersLog.Clear();
        var lines = output.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var userDetail = new P4UserDetail();
            
            // P4 'users' format is: "username <email> (FullName) accessed..."

            int firstSpace = line.IndexOf(' ');
            if (firstSpace > 0) userDetail.Username = line.Substring(0, firstSpace);

            int emailStart = line.IndexOf('<');
            int emailEnd = line.IndexOf('>');
            if (emailStart > -1 && emailEnd > emailStart) 
            {
                userDetail.Email = line.Substring(emailStart + 1, emailEnd - emailStart - 1);
            }
            else
            {
                userDetail.Email = "Unknown Email"; 
            }
            
            int nameStart = line.IndexOf('(');
            int nameEnd = line.IndexOf(')');
            if (nameStart > -1 && nameEnd > nameStart)
            {
                userDetail.FullName = line.Substring(nameStart + 1, nameEnd - nameStart - 1);
            }
            else
            {
                userDetail.FullName = "Unknown Name";
            }
            
            if (!string.IsNullOrEmpty(userDetail.Username))
            {
                usersToDelete.Add(userDetail);
            }
        }
        
        if (usersToDelete.Count > 0)
        {
            UIManager.Instance.AddLog($"Found {usersToDelete.Count} users.");
            currentUserIndex = 0;
            DisplayCurrentUser();
            userDisplayGroup.SetActive(true);
        }
        else
        {
            UIManager.Instance.AddLog("No users found in that group.");
            userDisplayGroup.SetActive(false);
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
        
        confirmationLabel.text = $"Do you want to delete {user.Username}?\n<size=20>Full Name: {user.FullName}\nEmail: {user.Email}</size>";
    }

    private void OnDelete()
    {
        if (currentUserIndex < 0 || currentUserIndex >= usersToDelete.Count) return;
        
        P4UserDetail user = usersToDelete[currentUserIndex];
        string username = user.Username;

        UIManager.Instance.AddLog($"Deleting user: {username}...");
        
        var (output, error) = P4.RunCommand($"user -d -f {username}");
        
        if (!string.IsNullOrEmpty(error))
        {
            UIManager.Instance.AddLog($"Failed to delete {username}: {error}", true);
        }
        else
        {
            UIManager.Instance.AddLog($"Successfully deleted {username}.");
            deletedUsersLog.Add(user); // Add the full detail object to the log
        }

        currentUserIndex++;
        DisplayCurrentUser();
    }

    private void OnSkip()
    {
        if (currentUserIndex < 0 || currentUserIndex >= usersToDelete.Count) return;

        UIManager.Instance.AddLog($"Skipped user: {usersToDelete[currentUserIndex].Username}");
        currentUserIndex++;
        DisplayCurrentUser();
    }

    private void SaveLog()
    {
        string logPath = Path.Combine(Application.persistentDataPath, "deleted_users_log.csv");
        var csvBuilder = new StringBuilder();
        // Updated header to include all details
        csvBuilder.AppendLine("Username,FullName,Email"); 
        
        foreach (var user in deletedUsersLog)
        {
            csvBuilder.AppendLine($"{user.Username},\"{user.FullName}\",{user.Email}");
        }
        
        File.WriteAllText(logPath, csvBuilder.ToString());
        UIManager.Instance.AddLog($"Log of deleted users saved to {logPath}");
    }
}