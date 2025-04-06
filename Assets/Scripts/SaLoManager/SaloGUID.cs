using UnityEngine;
using System.Collections;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

[DisallowMultipleComponent, System.Serializable]
public class SaloGUID : MonoBehaviour, ISerializationCallbackReceiver, ISaloObject
{
    public string ClassSaveName => "SaloGUID";
    public SerializableGUID GUID => GetComponent<SaloGUID>().FindLocal(this);

    [SerializeField]
    private List<SerializableGUID> guids;
    [SerializeField]
    private List<Object> objectsOnEntity;

    [SerializeField]
    private List<string> temp_NameIID;

#if UNITY_EDITOR
    private bool IsEditingInPrefabMode()
    {
        if (UnityEditor.EditorUtility.IsPersistent(this))
        {
            // if the game object is stored on disk, it is a prefab of some kind, despite not returning true for IsPartOfPrefabAsset =/
            return true;
        }
        else
        {
            // If the GameObject is not persistent let's determine which stage we are in first because getting Prefab info depends on it
            var mainStage = UnityEditor.SceneManagement.StageUtility.GetMainStageHandle();
            var currentStage = UnityEditor.SceneManagement.StageUtility.GetStageHandle(gameObject);
            if (currentStage != mainStage)
            {
                var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject);
                if (prefabStage != null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsAssetOnDisk()
    {
        return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) || IsEditingInPrefabMode();
    }

    public bool IsDisabled()
    {
        return IsAssetOnDisk() || IsEditingInPrefabMode();
    }
#endif

    // We cannot allow a GUID to be saved into a prefab, and we need to convert to byte[]
    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR

        // This lets us detect if we are a prefab instance or a prefab asset.
        // A prefab asset cannot contain a GUID since it would then be duplicated when instanced.
        if (IsAssetOnDisk())
        {
            guids = null;
            guids_dic = null;
            objectsOnEntity = null;
        }
        else
#endif
        {
            /*if (!Application.isPlaying)
            {
                CheckAndUpdateComponentsGUID();
                Debug.LogWarning("!!!IN EDIT MODE!!! " + name + " OnBeforeSerialize()");
            }
            else
            {
               
            }*/
            //CheckAndUpdateComponentsGUID();
        }
    }

    // On load, we can go head a restore our system guid for later use
    public void OnAfterDeserialize()
    {

        if (guids == null)
        {
            if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Warning) > 0)
                Debug.LogWarning("guids is null");
            guids = new List<SerializableGUID>();
        }

        if (guids_dic == null)
        {
            guids_dic = new GuidDictionary();
            ComponentsGUID = new ReadOnlyDictionary<SerializableGUID, object>(guids_dic);
        }
        else
            guids_dic.Clear();



        for (int i = 0; i < objectsOnEntity.Count; i++)
        {
            guids_dic.Add(guids[i], objectsOnEntity[i]);
        }


    }

    void OnValidate()
    {

#if UNITY_EDITOR
        // similar to on Serialize, but gets called on Copying a Component or Applying a Prefab
        // at a time that lets us detect what we are
        if (IsAssetOnDisk())
        {
            guids = null;
            objectsOnEntity = null;
            temp_NameIID = null;
        }
        else
#endif
        {
            //CheckAndUpdateComponentsGUID();
        }

    }

    public bool IsInitialized()
    {
        return objectsOnEntity != null &&
            objectsOnEntity.Count > 0 &&
            guids != null && guids.Count > 0 && guids_dic != null && guids_dic.Count > 0;
    }

    void Awake()
    {
        if (!SaveLoadManager.IsNewGame)
        {
            Generate();
            Register();
        }
    }

    public void Register()
    {
        if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Info) > 0)
            Debug.Log("Saloguid REGISTER");
        if (ComponentsGUID != null)
            foreach (var item in ComponentsGUID)
            {
                SaloGUIDManager.Register(item.Key, item.Value);
            }
    }

    private void FillObjects()
    {
        //if (objectsOnEntity == null)
        {
            var comps = GetComponents<Component>();
            List<Object> temp = new List<Object>();
            temp.Add(gameObject);

            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null)
                    continue;
                temp.Add(comps[i]);
            }
            objectsOnEntity = temp;
            temp_NameIID = temp.Select(x => x.GetType().Name + " - " + x.GetInstanceID()).ToList();
        }
    }









    // let the manager know we are gone, so other objects no longer find this
    public void OnDestroy()
    {
        if (guids == null)
            return;

        foreach (var item in guids)
        {
            SaloGUIDManager.Remove(item);
        }


        if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Info) > 0)
            Debug.Log(name + " guid destroyed and removed");
        objectsOnEntity.Clear();
        temp_NameIID.Clear();
        guids_dic.Clear();
        guids.Clear();

        objectsOnEntity = null;
        temp_NameIID = null;
        guids_dic = null;
        guids = null;
    }

    GuidDictionary guids_dic;
    public ReadOnlyDictionary<SerializableGUID, object> ComponentsGUID { get; private set; }

    public SerializableGUID FindLocal(Object local)
    {
        if (guids_dic == null)
            return SerializableGUID.Empty;


        var guidKey = guids_dic.FirstOrDefault(x => object.ReferenceEquals(x.Value, local));

        var guid = guidKey.Key;
        if (guid == null)
            return new SerializableGUID();

        return guid;
    }

    public ReadOnlyDictionary<SerializableGUID, object> Generate()
    {

        if (IsInitialized())
        {
            if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Warning) > 0)
                Debug.LogWarning("SaloGUID " + name + " was initialized but Generated called. Check And Updat eComponents GUID...");
            CheckAndUpdateComponentsGUID();
            return ComponentsGUID;
        }
        if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Warning) > 0)
            Debug.LogWarning("SaloGUID " + name + " Generated called. Create new guids for components.");

        FillObjects();
        guids_dic = new GuidDictionary();

        InitList(ref guids);

        foreach (var item in objectsOnEntity)
        {
            var guid = SerializableGUID.NewGuid();
            guids.Add(guid);
            guids_dic.Add(guid, item);
        }
        ComponentsGUID = new ReadOnlyDictionary<SerializableGUID, object>(guids_dic);
        return ComponentsGUID;
    }

    public override string ToString()
    {
        return !IsInitialized() ? System.Guid.Empty.ToString() : guids[0].ToString();
    }

    private void InitList<T>(ref List<T> list)
    {
        if (list == null)
            list = new List<T>();
        else
            list.Clear();
    }



    public bool CheckAndUpdateComponentsGUID()
    {
        //Debug.Log(name + " CheckAndUpdateComponentsGUID()");

        if (guids_dic == null || objectsOnEntity == null || guids == null)
        {
            if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Error) > 0)
                Debug.LogError(name + " guids_dic = " + (guids_dic == null ? "null" : guids_dic.Count.ToString()) +
                " guids = " + (guids == null ? "null" : guids.Count.ToString()) +
                "objectsOnEntity = " + (guids == null ? "null" : objectsOnEntity.Count.ToString()));
#if UNITY_EDITOR
            if (!IsAssetOnDisk())
                Generate();
#endif
            return true;
        }

        var current = guids_dic;
        int oldCnt = guids_dic.Count;

        if (objectsOnEntity == null)
        {
            objectsOnEntity = new List<Object>();
            foreach (var item in guids_dic)
            {
                objectsOnEntity.Add((Object)item.Value);
            }
            temp_NameIID = objectsOnEntity.Select(x => x.GetType().Name + " - " + x.GetInstanceID()).ToList();
        }

        var comps = GetComponents<Component>();
        List<Object> temp = new List<Object>();

        Dictionary<Object, SerializableGUID> rev_dic = new Dictionary<Object, SerializableGUID>();
        foreach (var item in guids_dic)
        {
            if (item.Value != null)
                rev_dic.Add((Object)item.Value, item.Key);
        }

        temp.Add(gameObject);
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null)
                continue;
            temp.Add(comps[i]);

        }

        bool edited = false;
        if (temp.Count != oldCnt)
        {
            var objGetted = temp.Select(x => x.GetType().Name + " - " + x.GetInstanceID()).ToList();

            var removedObjs = temp_NameIID.Where(x => !objGetted.Contains(x)).Select(x => "(-)" + x);//выбираем те объекты из старого, которых нет в новом
            var addedObjs = objGetted.Where(x => !temp_NameIID.Contains(x)).Select(x => "(+)" + x);
            var differsTotal = removedObjs.Concat(addedObjs).ToList();
            string diffTypes = differsTotal.Aggregate("", (str, next) => (string.IsNullOrEmpty(str) ? string.Empty : str + ", ") + next);
            if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Warning) > 0)
                Debug.LogWarning((!Application.isPlaying ? "!!!IN  EDIT MODE!!!! " : "") + name + "-" + GetInstanceID() + " has changed components count. From " + oldCnt + " to " + temp.Count + " (" + diffTypes + "). Refreshing...");
            edited = true;
        }
        else
        {
            int cnt = temp.Count;
            for (int i = 0; i < cnt; i++)
            {
                if (objectsOnEntity[i] != temp[i])
                {
                    if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Warning) > 0)
                        Debug.LogWarning((!Application.isPlaying ? "!!!IN EDIT MODE!!!! " : "") + name + " has changed components sequence. Refreshing...");
                    edited = true;
                    break;
                }
            }

        }

        List<SerializableGUID> toRemove = new List<SerializableGUID>();
        foreach (var item in guids_dic)
        {
            if (!temp.Contains(item.Value))
            {
                if (item.Value == null)
                    continue;

                rev_dic.Remove((Object)item.Value);

                //Debug.Log(gameObject?.name + " saloguid " + item.Key + " removed for " + item.Value?.ToString());
                if (Application.isPlaying)
                    SaloGUIDManager.Remove(item.Key);
            }
        }

        foreach (var item in temp)
        {
            if (!rev_dic.ContainsKey(item))
            {
                if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Warning) > 0)
                    Debug.LogWarning((!Application.isPlaying ? "!!!IN EDIT MODE!!!! " : "") + name + " didnt contains guid for " + item.GetType().Name);
                edited = true;
                var guid = SerializableGUID.NewGuid();
                rev_dic.Add(item, guid);
                //objectsOnEntity.Add(item);
                //guids.Add(guid);
                //guids_dic.Add(guid, item);
                if (Application.isPlaying)
                    SaloGUIDManager.Register(guid, item);
            }
        }

        //TODO: перестановка компонентов в эдиторе должна менять порядок GUID


        if (edited)
        {
            if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Warning) > 0)
                Debug.LogWarning((!Application.isPlaying ? "!!!IN EDIT MODE!!!! " : "") + gameObject.name + " guids was edited");
            guids.Clear();
            guids_dic.Clear();
            objectsOnEntity = temp;
            foreach (var item in objectsOnEntity)
            {
                var guid = rev_dic[item];
                guids.Add(guid);
                guids_dic.Add(guid, item);
            }

        }

        //Debug.Log(name + " iid: " + GetInstanceID() + " CheckAndUpdateComponentsGUID() - END");
        return edited;
    }

    public SerializableGUID GetGUID()
    {
        return FindLocal(this);
    }

    public void Save(SaloHelper writer)
    {

        writer.BeginWriteList("GUIDS");
        int cnt = objectsOnEntity.Count;
        for (int i = 0; i < cnt; i++)
        {
            writer.AddCustomData(objectsOnEntity[i].GetType().Name, guids[i].ToString());
        }
        writer.EndList();
    }

    public void Initialize(SaloHelper reader)
    {

    }

    public void Initialize()
    {

    }

    public bool Load(SaloHelper reader)
    {
        if (guids_dic != null)
        {
            if (Application.isPlaying)
                foreach (var item in guids_dic.Keys)
                    SaloGUIDManager.Remove(item);
            guids_dic.Clear();
        }
        else
            guids_dic = new GuidDictionary();

        FillObjects();
        if (guids != null)
            guids.Clear();
        else
            InitList(ref guids);

        int cnt = reader.BeginReadList("GUIDS");
        for (int i = 0; i < cnt; i++)
        {
            var xElement = reader.GetListElement(i);
            if (xElement.Name != objectsOnEntity[i].GetType().Name)
                return false;
            var guid = SerializableGUID.Parse(xElement.Value);
            guids.Add(guid);
            guids_dic.Add(guid, objectsOnEntity[i]);
            if (Application.isPlaying)
                SaloGUIDManager.Register(guid, objectsOnEntity[i]);
        }
        reader.EndList();
        ComponentsGUID = new ReadOnlyDictionary<SerializableGUID, object>(guids_dic);
        return true;
    }
}

