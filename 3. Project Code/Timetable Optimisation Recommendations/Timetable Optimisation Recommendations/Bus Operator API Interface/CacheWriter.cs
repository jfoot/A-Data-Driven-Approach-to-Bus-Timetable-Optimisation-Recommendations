// Copyright (c) Jonathan Foot. All Rights Reserved. 
// See the LICENSE file in the project root for more information.
// Private and confidential until the date of 18/06/2021

using System;
using System.IO;

namespace Timetable_Optimisation_Recommendations.Bus_Operator_API_Interface
{

    /// <summary>
    /// Used to write a cache files to the disk.
    /// </summary>
    public class CacheWriter
    {
        /// <summary>
        /// Saves a cache file to local disk, and hides the folder so the user cannot see it.
        /// </summary>
        /// <param name="fileLoc">The location for the cache file.</param>
        /// <param name="fileName">The name of the file</param>
        /// <param name="content">The contents of the file.</param>
        public static void WriteToCache(string fileLoc, string fileName, string? content)
        {
            //Checks that the directory exists or not, if not make it.
            if (!Directory.Exists(fileLoc))
            {
                Directory.CreateDirectory(fileLoc);
                _ = new DirectoryInfo(fileLoc) { Attributes = FileAttributes.Hidden };
            }
            //Then actually save the file.
            Console.WriteLine("cache write: " + fileLoc + "/" + fileName);
            File.WriteAllText(fileLoc + "/" + fileName, content);
        }
    }
}
