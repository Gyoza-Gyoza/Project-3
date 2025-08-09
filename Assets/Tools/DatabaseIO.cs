using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// This system handles saving and loading CSV files to and from the project's Exports folder.
// Use ExportDatabase to save a string as a CSV file.
// Use ImportDatabase to load a CSV file as a string array.
public static class DatabaseIO
{
    //Use to define the export destination
    private static string folderPath = Application.dataPath + "/Exports";

    /// <summary>
    /// Attempts to create a CSV file
    /// </summary>
    /// <param name="fileName">Name of the exported file</param>
    /// <param name="csv">String that will be converted to a CSV</param>
    public static void ExportDatabase(string fileName, string csv)
    {
        if(!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        
        File.WriteAllText($"{folderPath}/{fileName}.csv", csv);
    }

    /// <summary>
    /// Attempts to import a CSV file for use
    /// </summary>
    /// <param name="fileName">Name of the imported file</param>
    /// <param name="csv">Returns the CSV as a string if it exists, else returns empty</param>
    /// <returns></returns>
    public static bool ImportDatabase(string fileName, out string[] csv)
    {
        string filePath = $"{folderPath}/{fileName}.csv";
        if (File.Exists(filePath))
        {
            csv = File.ReadAllLines(filePath);
            return true;
        }
        else
        {
            csv = new string[0];
            return false;
        }
    }
}
