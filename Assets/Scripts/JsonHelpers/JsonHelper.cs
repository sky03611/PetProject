using System;
using System.IO;
using UnityEngine;

[Serializable]
public class Wrapper<T>
{
    public T[] Items;
}

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }
}
public class TypeArrayDeserializer
{
    public static T[] LoadResourceFromJson<T>(string path)
    {
        TextAsset targetFile = Resources.Load<TextAsset>(path);
        if (targetFile == null)
        {
            Debug.Log("TextAsset is NULL at path: " + path);
            return null;
        }

        try
        {
            var output = JsonHelper.FromJson<T>(targetFile.text);

            if (output == null)
                Debug.Log("JsonHelper.FromJson returned NULL!");

            return output;
        }
        catch (Exception e)
        {
            Debug.Log("Exception while parsing JSON: " + e);
            return null;
        }

    }
}