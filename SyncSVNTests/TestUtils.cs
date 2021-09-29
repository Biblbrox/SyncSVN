using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using SyncSVNTestForms;

namespace SyncSVNTests
{
    class TestUtils
    {
        public static void setAttributesNormal(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories())
                setAttributesNormal(subDir);
            foreach (var file in dir.GetFiles())
                file.Attributes = FileAttributes.Normal;
        }

        public static void CreateEmptyFolder(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            Directory.CreateDirectory(path);
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                folder += Path.DirectorySeparatorChar;

            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }

        public static void WriteToFile(string filePath, string text)
        {
            using (StreamWriter sr = File.AppendText(filePath)) {
                sr.Write(text);
            }
        }

        public static Dictionary<string, bool> ResolveConflictsPull(string message, List<string> conflictList)
        {
            SyncSVNTestForm form = new SyncSVNTestForm(message, conflictList);

            var result = new Dictionary<string, bool>();
            form.checkedListBox1.ItemCheck += (sender, e) => {
                List<string> checkedItems = new List<string>();
                foreach (var item in form.checkedListBox1.CheckedItems)
                    checkedItems.Add(item.ToString());

                if (e.NewValue == CheckState.Checked)
                    checkedItems.Add(form.checkedListBox1.Items[e.Index].ToString());
                else
                    checkedItems.Remove(form.checkedListBox1.Items[e.Index].ToString());

                foreach (string item in checkedItems)
                    result[item] = true;
            };

            form.button1.Click += (sender, e) => form.Close();

            Application.EnableVisualStyles();
            Application.Run(form);

            return result;
        }
    }
}
