using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


static class EscapeHelper
{

    //Ескейпинг необходим, если у тебя в тексте будут эти спец символы. Иначе XML полетит к херам
    private static readonly char[] s_escapeChars = new char[] { '<', '>', '"', '\'', '&' };
    private static readonly string[] s_escapeStringPairs = new string[] { "<", "&lt;", ">", "&gt;", "\"", "&quot;", "'", "&apos;", "&", "&amp;" };

    /// <summary>
    /// Escapes the specified text.
    /// </summary>
    /// <param name="str">The text to escape.</param>
    /// <returns>An escaped string.</returns>
    public static string Escape(string str)
    {
        if (str == null)
        {
            return null;
        }
        StringBuilder builder = null;
        int length = str.Length;
        int startIndex = 0;
        while (true)
        {
            int num2 = str.IndexOfAny(s_escapeChars, startIndex);
            if (num2 == -1)
            {
                if (builder == null)
                {
                    return str;
                }
                builder.Append(str, startIndex, length - startIndex);
                return builder.ToString();
            }
            if (builder == null)
            {
                builder = new StringBuilder();
            }
            builder.Append(str, startIndex, num2 - startIndex);
            builder.Append(GetEscapeSequence(str[num2]));
            startIndex = num2 + 1;
        }
    }

    private static string GetEscapeSequence(char c)
    {
        int length = s_escapeStringPairs.Length;
        for (int i = 0; i < length; i += 2)
        {
            string str = s_escapeStringPairs[i];
            string str2 = s_escapeStringPairs[i + 1];
            if (str[0] == c)
            {
                return str2;
            }
        }
        return c.ToString();
    }

    public static String Unescape(String str)
    {
        if (str == null)
            return null;

        StringBuilder sb = null;

        int strLen = str.Length;
        int index; // Pointer into the string that indicates the location of the current '&' character
        int newIndex = 0; // Pointer into the string that indicates the start index of the "remainging" string (that still needs to be processed).

        do
        {
            index = str.IndexOf('&', newIndex);

            if (index == -1)
            {
                if (sb == null)
                    return str;
                else
                {
                    sb.Append(str, newIndex, strLen - newIndex);
                    return sb.ToString();
                }
            }
            else
            {
                if (sb == null)
                    sb = new StringBuilder();

                sb.Append(str, newIndex, index - newIndex);
                sb.Append(GetUnescapeSequence(str, index, out newIndex)); // updates the newIndex too

            }
        }
        while (true);

        // C# reports a warning if I leave this in, but I still kinda want to just in case.
        // Contract.Assert( false, "If you got here, the execution engine or compiler is really confused" );
        // return str;
    }

    private static String GetUnescapeSequence(String str, int index, out int newIndex)
    {
        int maxCompareLength = str.Length - index;

        int iMax = s_escapeStringPairs.Length;
        /*if (iMax % 2 == 0)
            Debug.LogWarning("Odd number of strings means the attr/value pairs were not added correctly");*/

        for (int i = 0; i < iMax; i += 2)
        {
            String strEscSeq = s_escapeStringPairs[i];
            String strEscValue = s_escapeStringPairs[i + 1];

            int length = strEscValue.Length;

            if (length <= maxCompareLength && String.Compare(strEscValue, 0, str, index, length, StringComparison.Ordinal) == 0)
            {
                newIndex = index + strEscValue.Length;
                return strEscSeq;
            }
        }

        newIndex = index + 1;
        return str[index].ToString();
    }


}
