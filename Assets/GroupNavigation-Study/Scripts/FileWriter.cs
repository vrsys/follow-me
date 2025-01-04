using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRSYS.Core.Logging;

public static class FileWriter
{
    public static void WriteCsvFile(string fileName, List<string> csvFile)
    {
        if(string.IsNullOrEmpty(fileName))
            return;
        
        // Make sure to remove files with same name to properly overwrite them
        if (File.Exists(Application.persistentDataPath + fileName + ".csv"))
        {
            File.Delete(Application.persistentDataPath + fileName + ".csv");
        }
        
        using (StreamWriter writer = new StreamWriter(Application.persistentDataPath + fileName + ".csv", true))
        {
            for(int i = 0; i < csvFile.Count; i++)
                writer.WriteLine(csvFile[i]);
            writer.Close();
            
            ExtendedLogger.LogInfo("FileWriter", "File " + fileName + " was saved to " + Application.persistentDataPath + fileName + ".csv");
        }
    }
}
