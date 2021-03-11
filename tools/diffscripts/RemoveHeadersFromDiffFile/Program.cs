using System;
using System.IO;
using System.Text.RegularExpressions;

namespace RemoveHeadersFromDiffFile
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // Take in the file name
            string fileName = args[1];

            // Get the existing contents
            string fileContents = File.ReadAllText(fileName);

            // Replace ```\r\n\r\n
            string headerReplaceRegex = "^```\\s+^";
            // Replace ## Microsoft...\r\n\r\n
            string titleReplaceRegex = "^##.*\\s+^";
            // Replace ``` diff\r\n
            string diffHeaderReplaceRegex = "^``` diff\\s+^";

            
            string headerReplacement = Regex.Replace(fileContents, headerReplaceRegex, string.Empty, RegexOptions.Multiline);

            Regex titleRegex = new Regex(titleReplaceRegex, RegexOptions.Multiline);
            // Start regex AFTER the existing header so we don't remove it.
            string titleReplacement = titleRegex.Replace(headerReplacement, string.Empty, Int32.MaxValue, fileContents.IndexOf("```C#"));

            string diffReplacement = Regex.Replace(titleReplacement, diffHeaderReplaceRegex, string.Empty, RegexOptions.Multiline);

            File.WriteAllText(fileName, string.Concat(diffReplacement, "```\r\n"));
        }
    }
}