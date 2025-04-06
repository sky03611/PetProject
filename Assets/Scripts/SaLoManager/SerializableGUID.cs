using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class SerializableGUIDComparer : IEqualityComparer<SerializableGUID>
{
    public bool Equals(SerializableGUID b1, SerializableGUID b2)
    {
        if (b2 == null && b1 == null)
            return true;
        else if (b1 == null || b2 == null)
            return false;
        else
            return b1.Equals(b2);
    }

    public int GetHashCode(SerializableGUID bx)
    {
        return bx.GetHashCode();
    }
}

[Serializable]
public class SerializableGUID : ISerializationCallbackReceiver, IFormattable, IComparable, IComparable<SerializableGUID>, IComparable<Guid>, IEquatable<SerializableGUID>, IEquatable<Guid>
{
    public static SerializableGUID Empty { get { return new SerializableGUID(); } }
    // System guid we use for comparison and generation
    System.Guid guid = System.Guid.Empty;

    public int Length { get { return serializedGuid.Length; } }
    // Unity's serialization system doesn't know about System.Guid, so we convert to a byte array
    // Fun fact, we tried using strings at first, but that allocated memory and was twice as slow
    [SerializeField, HideInInspector]
    private byte[] serializedGuid;

    private string guidAsString;

    public bool IsGuidAssigned()
    {
        return guid != System.Guid.Empty;
    }

    public static SerializableGUID NewGuid()
    {
        var guid = new SerializableGUID();
        guid.CreateGuid();
        return guid;
    }

    public SerializableGUID()
    {
        guidAsString = guid.ToString();
    }

    public SerializableGUID(byte[] guidData)
    {
        serializedGuid = guidData;
        CreateGuid();
    }

    public static SerializableGUID Parse(string value)
    {
        SerializableGUID guid = new SerializableGUID();
        guid.guid = Guid.Parse(value);
        guid.serializedGuid = guid.guid.ToByteArray();
        guid.guidAsString = value;
        return guid;
    }

    public static bool TryParse(string value, out SerializableGUID guid)
    {
        try
        {
            guid = SerializableGUID.Parse(value);
            return true;
        }
        catch (Exception)
        {
            guid = SerializableGUID.Empty;
            return false;
            throw;
        }
    }

    // When de-serializing or creating this component, we want to either restore our serialized GUID
    // or create a new one.
    void CreateGuid()
    {
        // if our serialized data is invalid, then we are a new object and need a new GUID
        if (serializedGuid == null || serializedGuid.Length != 16)
        {
            guid = System.Guid.NewGuid();
            serializedGuid = guid.ToByteArray();
        }
        else if (guid == System.Guid.Empty)
        {
            // otherwise, we should set our system guid to our serialized guid
            guid = new System.Guid(serializedGuid);
        }

        guidAsString = guid.ToString();
    }

    // We cannot allow a GUID to be saved into a prefab, and we need to convert to byte[]
    public void OnBeforeSerialize()
    {

        if (guid != System.Guid.Empty)
        {
            serializedGuid = guid.ToByteArray();
        }
    }

    // On load, we can go head a restore our system guid for later use
    public void OnAfterDeserialize()
    {
        //Debug.Log((serializedGuid != null) + " " + serializedGuid.Length + " " + BitConverter.ToString(serializedGuid));
        if (serializedGuid != null && serializedGuid.Length == 16)
        {
            guid = new System.Guid(serializedGuid);
            guidAsString = guid.ToString();
        }
    }

    public override string ToString()
    {
        return guid.ToString();
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        return ((IFormattable)guid).ToString(format, formatProvider);
    }

    public int CompareTo(object obj)
    {
        return ((IComparable)guid).CompareTo(obj);
    }

    public int CompareTo(Guid other)
    {
        return ((IComparable<Guid>)guid).CompareTo(other);
    }

    public bool Equals(Guid other)
    {
        return ((IEquatable<Guid>)guid).Equals(other);
    }

    public int CompareTo(SerializableGUID other)
    {
        return ((IComparable<Guid>)guid).CompareTo(other.guid);
    }

    public bool Equals(SerializableGUID other)
    {
        return ((IEquatable<Guid>)guid).Equals(other.guid);
    }

    public override int GetHashCode()
    {
        return guid.GetHashCode();
    }

}
