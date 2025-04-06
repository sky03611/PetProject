using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public enum Language { ENGLISH, RUSSIAN, CHSIMPLIFIED, CHTRADITIONAL, KOREAN, JAPANESE }

public class CSVParsing
{
    public TextAsset csvFile;
    public Dictionary<string, List<string>> translations = new Dictionary<string, List<string>>();

    private char lineSeparator = '\n';

    public void Initialize()
    {
        csvFile = Resources.Load<TextAsset>("Localization/Locale");
        ReadAllData();
    }

    // Read data from CSV file
    private void ReadAllData()
    {
        Regex CSVParser = new Regex("_(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
        string[] records = csvFile.text.Split(lineSeparator);
        List<string> fieldsTmp = new List<string>();
        int line = 0;
        foreach (string record in records)
        {
            string[] fields = CSVParser.Split(record);
            for (int i = 1; i < fields.Length; i++)
            {
                fieldsTmp.Add(fields[i]);
            }
            List<string> copy = fieldsTmp.ToList();
            try
            {
                translations.Add(fields[0], copy);
            }
            catch
            {

            }
            fieldsTmp.Clear();
            line++;
        }
    }

    public string GetName (string key, Language language)
    {
        string data = string.Empty;
        List<string> trans = new List<string>();
        if (key == "")
            return "";
        string findKey = translations.FirstOrDefault(x => x.Value.Contains(key)).Key;
        try
        {
            if (translations.TryGetValue(findKey, out trans))
            {
                try
                {
                    data = trans[(int)language];
                }
                catch
                {
                    try
                    {
                        data = trans[0];
                    }
                    catch
                    {
                        data = "Not found";
                    }
                }
            }
            else
            {
                data = key;
            }
        }
        catch
        {
            if (translations.TryGetValue(key, out trans))
            {
                try
                {
                    data = trans[(int)language];
                }
                catch
                {
                    try
                    {
                        data = trans[0];
                    }
                    catch
                    {
                        data = "Not found";
                    }
                }
            }
            else
            {
                data = key;
            }
        }
        return data;
    }

    public string GetDataByKey (string key, Language language)
    {
        string data = string.Empty;
        List<string> trans = new List<string>();
        if (key == "")
            return "";
        if (translations.TryGetValue (key, out trans))
        {
            try
            {
                data = trans[(int)language];
            }
            catch
            {
                try
                {
                    data = trans[0];
                }
                catch
                {
                    data = "Not found";
                }
            }
        }
        else
        {
            data = key;
        }
        return data;
    }

    public string GetEnglishByKey (string key)
    {
        string data = string.Empty;
        List<string> trans = new List<string>();
        if (key == "")
            return "";
        if (translations.TryGetValue(key, out trans))
        {
            try
            {
                data = trans[(int)0];
            }
            catch
            {
                data = "Not found";
            }
        }
        else
        {
            data = key;
        }
        return data;
    }
}