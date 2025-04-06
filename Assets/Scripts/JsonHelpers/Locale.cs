using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Locale
{
    public static Language currentLanguage = Language.RUSSIAN;
    public static Language DefaultLanguage = Language.RUSSIAN;

    public static string AppPath = Application.dataPath;
    public static string LangsFolder = "Languages";
    public static CSVParsing parser;

    public static void Initialize()
    {
        parser = new CSVParsing();
        parser.Initialize();
    }

    public static string Get(string phr, bool original = false)
    {
        try
        {
            string phrase = parser.GetDataByKey(phr, currentLanguage).Replace("\\n", "\n");
            return phrase;
        }
        catch
        {
            try
            {
                string phrase = parser.GetDataByKey(phr, Language.ENGLISH).Replace("\\n", "\n");
                return phrase;
            }
            catch
            {
                return phr;
            }
        }
    }

    public static string English(string phr)
    {
        string phrase = parser.GetEnglishByKey(phr).Replace("\\n", "\n");
        return phrase;
    }

    public static string Name (string phr)
    {
        string phrase = parser.GetName(phr, currentLanguage);
        return phrase;
    }

    public static void SetLanguage(int _lang)
    {
        Debug.Log("Lang set: " + (Language)_lang);
        currentLanguage = (global::Language)_lang;
    }
}

public class L
{
    public static void Init()
    {
        Locale.Initialize();
    }
    public static string G(string p)
    {
        return Locale.Get(p);
    }
    public static string E(string p)
    {
        return Locale.English(p);
    }
    public static string N(string p)
    {
        return Locale.Name(p);
    }
}

