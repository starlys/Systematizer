using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Systematizer.WPF
{
    /// <summary>
    /// encapsulates recent files list
    /// </summary>
    static class RecentFilesList
    {
        /* Documentation on the recent file format:
         * Contains CRLF-delimited lines with recently opened file names.
         * Each line is preceded by ! if that file is open in some instance.
         * 
         * Behavior:
         * Always read entire file, modify this instance's file in memory and write entire file.
         * On startup, open the first non-open file if any, else go to settings dialog.
         */

        class FileEntry
        {
            public string Path;
            public bool IsOpen;
        }

        /// <summary>
        /// get file to auto-open or null if none
        /// </summary>
        /// <returns></returns>
        public static string GetFileToAutoOpen()
        {
            var fs = GetContent();
            if (fs.Count == 0) return null;
            if (fs.Any(f => f.IsOpen)) return null;
            return fs[0].Path;
        }

        /// <summary>
        /// Record the file as open and leave in the same list position
        /// </summary>
        public static void RecordIsOpen(string path)
        {
            var fs = GetContent();
            bool found = false;
            foreach (var f in fs)
            {
                if (string.Compare(path, f.Path, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    found = true;
                    f.IsOpen = true;
                }
            }
            if (!found) fs.Add(new FileEntry { IsOpen = true, Path = path });
            SaveContent(fs);
        }

        /// <summary>
        /// Record the file as closed and move to top of list
        /// </summary>
        public static void RecordIsClosed(string path)
        {
            var fs = GetContent(path);
            fs.Insert(0, new FileEntry { Path = path });
            SaveContent(fs);
        }

        public static void ForgetPath(string path)
        {
            var fs = GetContent(path);
            SaveContent(fs);
        }

        static string RecentFilesFileName() 
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystematizerRecentFiles.txt"
                );
        }

        /// <summary>
        /// Get recent files, optionally omitting the given database path
        /// </summary>
        static List<FileEntry> GetContent(string omitPath = null)
        {
            string path = RecentFilesFileName();
            var lines = new string[0];
            if (File.Exists(path)) lines = File.ReadAllLines(path);
            var fs = lines.Select(line =>
            {
                if (line.StartsWith("!")) return new FileEntry { IsOpen = true, Path = line.Substring(1) };
                return new FileEntry { Path = line };
            }).ToList();
            if (omitPath != null)
               fs = fs.Where(f => string.Compare(omitPath, f.Path, StringComparison.OrdinalIgnoreCase) != 0).ToList();
            return fs;
        }

        public static string[] GetRecentFiles()
        {
            return GetContent().Select(c => c.Path).ToArray();
        }

        static void SaveContent(List<FileEntry> fs)
        {
            var lines = fs.Select(f => 
            {
                if (f.IsOpen) return "!" + f.Path;
                return f.Path;
            });
            File.WriteAllLines(RecentFilesFileName(), lines);
        }
    }
}
