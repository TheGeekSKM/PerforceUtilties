using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using System.Text;
using OfficeOpenXml; 
using SFB; 

public class ExcelImporter : MonoBehaviour
{
    [Header("UI References")]
    public Button selectFileButton;
    public TextMeshProUGUI statusLabel;
    
    private string outputCsvPath;

    void Start()
    {        
        selectFileButton.onClick.AddListener(OnSelectFile);
        outputCsvPath = Path.Combine(Application.persistentDataPath, "perforce_users.csv");
        statusLabel.text = $"Ready. Converted file will be saved to:\n{outputCsvPath}";
    }

    private void OnSelectFile()
    {
        var extensions = new[] { new ExtensionFilter("Excel Files", "xlsx", "xls") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Excel Roster", "", extensions, false);

        if (paths.Length > 0)
        {
            string excelPath = paths[0];
            statusLabel.text = $"Selected: {Path.GetFileName(excelPath)}. Converting...";
            UIManager.Instance.AddLog($"Starting conversion for {excelPath}");
            bool success = ConvertExcelToCsv(excelPath, outputCsvPath);

            if (success)
            {
                statusLabel.text = $"Success! Converted to:\n{outputCsvPath}";
                UIManager.Instance.AddLog($"Successfully converted and saved to {outputCsvPath}");
            }
        }
        else
        {
            statusLabel.text = "File selection cancelled.";
            UIManager.Instance.AddLog("File selection cancelled.");
        }
    }

    private bool ConvertExcelToCsv(string excelPath, string csvPath)
    {
        try
        {
            var fileInfo = new FileInfo(excelPath);
            using (var package = new ExcelPackage(fileInfo))
            {
                var worksheet = package.Workbook.Worksheets[0];
                
                if (worksheet.Cells[2, 2].Value?.ToString() != "First" || 
                    worksheet.Cells[2, 3].Value?.ToString() != "Last"  ||
                    worksheet.Cells[2, 4].Value?.ToString() != "Email")
                {
                    string error = "Invalid Excel format. Expected headers 'First', 'Last', 'Email' not found in columns B2, C2, D2.";
                    statusLabel.text = error;
                    UIManager.Instance.AddLog(error, true);
                    return false;
                }
                
                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("FirstName,LastName,Email,Password"); 
                
                for (int row = 3; row <= worksheet.Dimension.Rows; row++)
                {
                    string firstName = worksheet.Cells[row, 2].Value?.ToString() ?? "";
                    string lastName = worksheet.Cells[row, 3].Value?.ToString() ?? "";
                    string email = worksheet.Cells[row, 4].Value?.ToString() ?? "";
                    
                    if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
                    {
                        continue;
                    }
                    
                    string password = GenerateRandomPassword();

                    csvBuilder.AppendLine($"{firstName.Trim()},{lastName.Trim()},{email.Trim()},{password}");
                }
                
                File.WriteAllText(csvPath, csvBuilder.ToString());
                return true;
            }
        }
        catch (System.Exception ex)
        {
            statusLabel.text = $"Error: {ex.Message}";
            UIManager.Instance.AddLog($"Excel Conversion Error: {ex.Message}", true);
            return false;
        }
    }

    private string GenerateRandomPassword(int length = 12)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%";
        var sb = new StringBuilder();
        var random = new System.Random();
        for (int i = 0; i < length; i++)
        {
            sb.Append(validChars[random.Next(validChars.Length)]);
        }
        return sb.ToString();
    }
}