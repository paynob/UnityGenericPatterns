namespace Paynob.Patterns.Utils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using UnityEngine;

    public static class IniUtils
    {
        public static Dictionary<string, Dictionary<string, string>> ReadIniFile(string filePath)
        {
            var iniData = new Dictionary<string, Dictionary<string, string>>();
            string[] lines = File.ReadAllLines(filePath);
            string currentSection = null;

            foreach (string line in lines)
            {
               ParseLine(line, ref currentSection, iniData);
            }

            return iniData;
        }

        public static Dictionary<string, Dictionary<string, string>> ReadIniText(string text)
        {
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var iniData = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = null;

            foreach (string line in lines)
            {
                ParseLine(line, ref currentSection, iniData);
            }

            return iniData;
        }

        private static void ParseLine(string line, ref string currentSection, Dictionary<string, Dictionary<string, string>> iniData)
        {
            string trimmedLine = line.Trim();

            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                return; // Skip empty lines and comments

            if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
            {
                currentSection = trimmedLine[1..^1].Trim(); // Extract section name
                iniData[currentSection] = new Dictionary<string, string>();
            }
            else if (currentSection != null)
            {
                int separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex != -1)
                {
                    string key = trimmedLine.Substring(0, separatorIndex).Trim();
                    string value = trimmedLine[(separatorIndex + 1)..].Trim();
                    iniData[currentSection][key] = value;
                }
            }
        }
    }
}