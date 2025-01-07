//  __                            __                       __   __   __    ___ .  . ___
// |__)  /\  |  | |__|  /\  |  | /__`    |  | |\ | | \  / |__  |__) /__` |  |   /\   |  
// |__) /~~\ \__/ |  | /~~\ \__/ .__/    \__/ | \| |  \/  |___ |  \ .__/ |  |  /~~\  |  
//
//       ___               __                                                           
// |  | |__  |  |\/|  /\  |__)                                                          
// |/\| |___ |  |  | /~~\ |  \                                                                                                                                                                                     
//
// Copyright (c) 2024 Virtual Reality and Visualization Group
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//-----------------------------------------------------------------
//   Authors:        Tony Zoeppig, Anton Lammert
//   Date:           2024
//-----------------------------------------------------------------

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
