#if UNITY_WSA || UNITY_ANDROID || UNITY_WEBGL
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;

public class EncryptManager
{
    //Любая смена этих хешей сделает сохранения (новым алг) от предыдущих версий ЛИХА нечитаемыми! 
    //Хэши для шифрофки/расшифровки файла. К PasswordHash можно добавить SteamID и тогда сейв можно будет запустить только из под аккаунта его создавшего
    static readonly string PasswordHash = "lifehard";
    static readonly string SaltKey = "timur4ik";//Тут
    static readonly string VIKey = "lifeviky";//и тут только 8 символов.

    public static string EncryptToString(string plainText)
    {
        return Convert.ToBase64String(EncryptToBytes(Encoding.UTF8.GetBytes(plainText)));
    }

    public static byte[] EncryptToBytes(string plainText)
    {
        return EncryptToBytes(Encoding.UTF8.GetBytes(plainText));
    }

    public static byte[] EncryptToBytes(byte[] plainTextBytes)
    {
#if !UNITY_STANDALONE && !UNITY_EDITOR
        return plainText;
#else
        byte[] keyBytes = new Rfc2898DeriveBytes(PasswordHash, Encoding.UTF8
                                                 .GetBytes(SaltKey)).GetBytes(256 / 8);
        var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
        var encryptor = symmetricKey.CreateEncryptor(keyBytes, Encoding.Unicode.GetBytes(VIKey));

        byte[] cipherTextBytes;

        using (var memoryStream = new MemoryStream())
        {
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();
                cipherTextBytes = memoryStream.ToArray();
                cryptoStream.Close();
            }
            memoryStream.Close();
        }
        return cipherTextBytes;
#endif
    }

    public static byte[] DecryptToBytes(byte[] encryptedBytes)
    {
#if !UNITY_STANDALONE && !UNITY_EDITOR
        return encryptedText;
#else
        byte[] cipherTextBytes = encryptedBytes;
        byte[] keyBytes = new Rfc2898DeriveBytes(PasswordHash, Encoding.UTF8
                                                 .GetBytes(SaltKey)).GetBytes(256 / 8);
        var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.None };

        var decryptor = symmetricKey.CreateDecryptor(keyBytes, Encoding.Unicode.GetBytes(VIKey));
         
        var memoryStream = new MemoryStream(cipherTextBytes);
        var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        byte[] plainTextBytes = new byte[cipherTextBytes.Length];
        cryptoStream.Read(plainTextBytes, 0, cipherTextBytes.Length);
        

        memoryStream.Close();
        cryptoStream.Close();
        return plainTextBytes;
#endif
    }

    public static byte[] DecryptToBytes(string encryptedText)
    {
        byte[] cipherTextBytes = Convert.FromBase64String(encryptedText);
        return DecryptToBytes(cipherTextBytes);
    }

    public static string Decrypt(byte[] encryptedBytes)
    {
        byte[] data = DecryptToBytes(encryptedBytes);
        string str = Encoding.UTF8.GetString(data, 0, data.Length);
        return str.TrimEnd("\0".ToCharArray());
    }

    public static string Decrypt(string encryptedText)
    {
        byte[] data = DecryptToBytes(encryptedText);
        return Encoding.UTF8.GetString(data, 0, data.Length).TrimEnd("\0".ToCharArray());
    }

}
