using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
[Flags]
public enum SaLoInfoLevel
{
    None = 0,
    Info = 1,
    Warning = 2,
    Error = 4
}

public enum SaveLoadState
{
    Success, InProgress, IOError, Currupted, FileMissed, UnknownError
}

public enum SlotType
{
    NewGame,
    OldType,
    NewType
}

/// <summary>
/// Игнорирование полем или свойством сохранения или загрузки
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Property | AttributeTargets.Field,
                   AllowMultiple = false, Inherited = true)]
public class SaLoIgnorable : Attribute { }

public static class SaveLoadManager
{

    public static SaLoInfoLevel InfoLevel { get; set; }

    public static bool IsNewGame { get; set; } = true;

    /// <summary>
    /// Максимальное количество сохранений.
    /// </summary>
    public const int MAX_SLOTS = 7;
    public const string SAVE_FOLDER = "Saves";
    public const string FILENAME = "slot{0}.hah";
    public static string SavesDir = string.Empty;

    public static Encoding Encoding { get; set; } = Encoding.UTF8;
    /// <summary>
    /// Текущий слот сохранения
    /// </summary>
    public static int CurrentSlotID { get; private set; }
    /// <summary>
    /// Используем новую версию загрузчика или нет
    /// </summary>
    public static SlotType CurrentSlotType { get; private set; }

    /// <summary>
    /// Проверка наличия папки для сохранений. Вызывается при любом вызове других методов обращающихся к папке,
    /// чтобы исключить возможность удаления папки во время игры
    /// </summary>
    private static void CheckDirectory()
    {
        if (SavesDir == string.Empty)
            SavesDir = Path.Combine(Environment.CurrentDirectory, SAVE_FOLDER);
        if (!Directory.Exists(SavesDir))
            Directory.CreateDirectory(SavesDir);
    }

    public static bool Delete(int slotID)
    {
        CheckDirectory();
        try
        {
            File.Delete(Path.Combine(SavesDir, string.Format(FILENAME, slotID)));
            return true;
        }
        catch
        {
            //на случай если файл сделали защищенным или еще чего.
            return false;
        }
    }

    public static bool HasSave(int slotID)
    {
        CheckDirectory();
        return File.Exists(Path.Combine(SavesDir, string.Format(FILENAME, slotID)));
    }

    public static SlotType GetSlotType(int slotID)
    {
        bool hasNewSave = HasSave(slotID);
        if (!hasNewSave)
            return SlotType.NewGame;
        if (hasNewSave) //если мы имеем новое сохранение с текущим ID грузим его
            return SlotType.NewType;
        return SlotType.NewGame;
    }

    public static void SetActiveSlot(int slotID, SlotType type)
    {
        CurrentSlotID = slotID;
        CurrentSlotType = type;
    }

    private static byte[] NullRemover(byte[] DataStream)
    {
        int i;
        byte[] temp = new byte[DataStream.Length];
        for (i = 0; i < DataStream.Length; i++)
        {
            if (DataStream[i] == 0x00) break;
            temp[i] = DataStream[i];
        }
        byte[] NullLessDataStream = new byte[i];
        for (i = 0; i < NullLessDataStream.Length; i++)
        {
            NullLessDataStream[i] = temp[i];
        }
        return NullLessDataStream;
    }

    public static SaveLoadState LoadXWorld(out XDocument xworld, bool decrypt)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-us");
        CheckDirectory();
        xworld = null;
        string fileName = Path.Combine(SavesDir, string.Format(FILENAME, CurrentSlotID));
        if (!File.Exists(fileName))
            return SaveLoadState.FileMissed;

        try
        {
            string data = "";
            if (decrypt)
                data = EncryptManager.Decrypt(File.ReadAllBytes(fileName));
            else
                data = File.ReadAllText(fileName);

            xworld = XDocument.Parse(data, LoadOptions.PreserveWhitespace);
            IsNewGame = false;
            return SaveLoadState.Success;
        }
        catch (IOException ex)
        {
            if ((InfoLevel & SaLoInfoLevel.Error) != 0)
                Debug.LogError(ex.ToString());
            return SaveLoadState.IOError;
        }
        catch (Exception ex)
        {
            if ((InfoLevel & SaLoInfoLevel.Error) != 0)
                Debug.LogError(ex.ToString());
            return SaveLoadState.UnknownError;
        }

    }

    public struct SaveInfo
    {
        public int slot;
        public bool UseDefName;
        public string Date;
        public string Name;
        public bool IsTutorial;
        public override string ToString()
        {
            return UseDefName ? Name : Date + " : " + "Slot " + Name;
        }
    }

    public static SaveInfo GetSaveInfo(int slot, string defaultName)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-us");
        CheckDirectory();
        SaveInfo info = new SaveInfo() { slot = slot, UseDefName = true, Name = defaultName };
        string fileName = Path.Combine(SavesDir, string.Format(FILENAME, slot));
        info.Date = DateTime.Now.ToShortDateString();

        if (!File.Exists(fileName))
            return info;
        info.Date = File.GetLastWriteTime(fileName).ToShortDateString();
        XDocument xworld_header = null;
        try
        {

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    string xmlHeader = sr.ReadLine();
                    xmlHeader = (xmlHeader[xmlHeader.Length - 2] == '/') ? xmlHeader : xmlHeader.Remove(xmlHeader.Length - 1, 1) + "/>";
                    xworld_header = XDocument.Parse(xmlHeader);

                    var xTutorial = xworld_header.Root.Attribute("IsTutorial");
                    if (xTutorial != null)
                        info.IsTutorial = xTutorial.Value == "1";

                    var xDate = xworld_header.Root.Attribute("Date");
                    if (xDate != null)
                        info.Date = xDate.Value;

                    var xTimeOfDay = xworld_header.Root.Attribute("TimeOfDay");
                    if (xTimeOfDay != null)
                    {
                        info.Name = xTimeOfDay.Value;
                        info.UseDefName = false;
                    }
                    return info;
                }
            }
        }
        catch (Exception ex)
        {
            if ((InfoLevel & SaLoInfoLevel.Info) != 0)
                Debug.Log("Slot " + slot + " read header. Try to decode. " + ex);
        }

        try
        {
            string text = EncryptManager.Decrypt(File.ReadAllBytes(fileName));
            byte[] data = EncryptManager.DecryptToBytes(File.ReadAllBytes(fileName));
            using (MemoryStream inputStream = new MemoryStream(data))
            using (StreamReader sr = new StreamReader(inputStream))
            {
                string xmlHeader = sr.ReadLine();
                xmlHeader = (xmlHeader[xmlHeader.Length - 2] == '/') ? xmlHeader : xmlHeader.Remove(xmlHeader.Length - 1, 1) + "/>";
                xworld_header = XDocument.Parse(xmlHeader);

                var xTutorial = xworld_header.Root.Attribute("IsTutorial");
                if (xTutorial != null)
                    info.IsTutorial = xTutorial.Value == "1";

                var xDate = xworld_header.Root.Attribute("Date");
                if (xDate != null)
                    info.Date = EscapeHelper.Unescape(xDate.Value);

                var xTimeOfDay = xworld_header.Root.Attribute("TimeOfDay");
                if (xTimeOfDay != null)
                {
                    info.Name = EscapeHelper.Unescape(xTimeOfDay.Value);
                    info.UseDefName = false;
                }
                return info;
            }
        }
        catch (Exception ex)
        {
            if ((InfoLevel & SaLoInfoLevel.Error) != 0)
                Debug.LogError("Slot " + slot + " cant read header. Use defaults." + ex);
        }

        return info;
    }

    public static SaveLoadState SaveXWorld(XDocument xworld, bool encrypt)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-us");

        if (xworld == null)
            return SaveLoadState.Currupted;

        CheckDirectory();
        //сразу перезаписываем файл
        string xmlString = xworld.ToString();

        try
        {
            string path = Path.Combine(SavesDir, string.Format(FILENAME, CurrentSlotID));
            if (File.Exists(path))
                File.Delete(path);

            System.Diagnostics.Stopwatch st = new System.Diagnostics.Stopwatch();
            st.Start();
            if (encrypt)
                File.WriteAllBytes(path, EncryptManager.EncryptToBytes(xmlString));
            else
                File.WriteAllText(path, xmlString);
            st.Stop();
            Debug.Log(st.Elapsed.TotalSeconds);

            /* using (MemoryStream output = new MemoryStream())
             {
                 using (GZipStream gZip = new GZipStream(output, CompressionMode.Compress))
                 using (MemoryStream input = new MemoryStream(Encoding.GetBytes(xmlString)))
                     CopyTo(input, gZip);
                 //Если нужна скорость, то можно убрать EncryptManager и просто убрать первые 2 байта в memoryStream и добавлять их при загрузке.
                 //Скорость сохранения/загрузки возрастет (плюс размер файла чуть меньше станет), однако защита файла сведется к 0, если потенциальный взломщик определит формат файла.
                 //Но это все не имеет значения, т.к. можно легко посмотреть код unity игры через какой-нибудь IL Spy: узнать и пароли и метод сохранения файлов

                 if (encrypt)
                     File.WriteAllText(path, EncryptManager.Encrypt(output.GetBuffer()));
                 else
                     File.WriteAllBytes(path, output.GetBuffer());
             }*/
            SetActiveSlot(CurrentSlotID, SlotType.NewType);
            return SaveLoadState.Success;
        }
        catch (Exception e)
        {
            if ((InfoLevel & SaLoInfoLevel.Error) != 0)
                Debug.LogError("SaLoMa: " + e.ToString());
            return SaveLoadState.IOError;
        }
    }

    private static long CopyTo(Stream source, Stream destination)
    {
        byte[] buffer = new byte[2048];
        int bytesRead;
        long totalBytes = 0;
        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            destination.Write(buffer, 0, bytesRead);
            totalBytes += bytesRead;
        }
        return totalBytes;
    }



}
