using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.SceneManagement;



public class GuidDictionary : Dictionary<SerializableGUID, object>
{
    public GuidDictionary() : base(new SerializableGUIDComparer()) { }


};

[HideInInspector, DisallowMultipleComponent]
public class SaloGUIDManager : MonoBehaviour
{
    private GuidDictionary guidDictionary;


    static SaloGUIDManager instance;

    public bool Initialized { get { return instance != null; } }

    public void Awake()
    {
        if (instance != null && instance != this)
        {
            DestroyImmediate(this.gameObject);
            return;
        }
        instance = this;
        if (guidDictionary == null)
        {
            guidDictionary = new GuidDictionary(); 
        }
        if (Application.isPlaying)
        {
            RefreshGUIDs(false);
        }
    }

    public static SaloGUIDManager FindGUIDManager()
    {
        if (instance != null)
            return instance;
        
        instance = Resources.FindObjectsOfTypeAll<SaloGUIDManager>().FirstOrDefault();
        if (instance != null)
            return instance;
        GameObject go = new GameObject("GUIDManager");
        go.hideFlags =/* HideFlags.HideInHierarchy | HideFlags.HideInInspector |*/ HideFlags.NotEditable;
        return go.AddComponent<SaloGUIDManager>();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void Remove(SerializableGUID guid)
    {
        if (guid == null)
            return;

        if (instance == null)
            return;
        if (instance.guidDictionary == null)
            return;

        instance.guidDictionary.Remove(guid);
    }


    public static T Get<T>(SerializableGUID guid)
    {
        if (instance == null)
            instance = FindGUIDManager();

        if (instance.guidDictionary == null)
            instance.RefreshGUIDs(false);


        if (!instance.guidDictionary.ContainsKey(guid))
            return default(T);

        return (T)instance.guidDictionary[guid];
    }

    public static SerializableGUID FindGUID(object reference)
    {
        if (instance.guidDictionary == null)
            instance.RefreshGUIDs(true);

        var guidKey = instance.guidDictionary.FirstOrDefault(x => x.Value == reference);

        var guid = guidKey.Key;
        if (guid == null)
            return new SerializableGUID();

        return guid;
    }

    /// <summary>
    /// Добавляет GUID к компонентам без него. В основном это будут динамические компоненты. Поэтому на них должно висеть сохранение
    /// </summary>
    public static void AddGUIDs()
    {
        if (instance == null)
            instance = FindGUIDManager();

        instance.RefreshGUIDs(true);
    }

    public static void Register(SerializableGUID guid, object obj)
    {
        if (instance == null)
            instance = FindGUIDManager();
        if (instance.guidDictionary == null)
            instance.RefreshGUIDs(false);
        if (!guid.IsGuidAssigned()) return;
        if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Info) > 0)
            Debug.Log("Register obj with type " + obj.GetType().Name + " to guid " + guid.ToString());
        if (!instance.guidDictionary.ContainsKey(guid))
            instance.guidDictionary.Add(guid, obj);
    }

    public void Clear()
    {
        if (guidDictionary == null)
            guidDictionary = new GuidDictionary();
        guidDictionary.Clear();
    }

    public void RefreshGUIDs(bool addGUIDS)
    {
        if (guidDictionary == null)
        {
            guidDictionary = new GuidDictionary();
        }

        var scene = SceneManager.GetActiveScene();

        GameObject[] rootObjs = scene.GetRootGameObjects();
 
        foreach (GameObject obj in rootObjs)
        {
            /*int instanceID = obj.GetInstanceID();
            values.Add(instanceID, obj);*/

            foreach (Transform item in obj.GetComponentsInChildren<Transform>(true))
            {
                var guid_comp = item.gameObject.GetComponent<SaloGUID>();
                //Debug.Log("current comp = " + item.name);
                if (guid_comp == null)
                {
                    if (addGUIDS)
                    {
                        var salo = item.gameObject.AddComponent<SaloGUID>();
                        salo.Generate();
                        /*foreach (var guid in salo.Generate())
                        {
                            guidDictionary.Add(guid.Key, guid.Value);
                            //UnityEditor.Undo.RegisterFullObjectHierarchyUndo((UnityEngine.Object)guid.Value, "go");
                        } */
                    }
                }
                else
                {
                    if (!guid_comp.IsInitialized())
                    {
                        if (addGUIDS)
                        {
                            foreach (var guid in guid_comp.Generate())
                            {
                                guidDictionary.Add(guid.Key, guid.Value);
                                //UnityEditor.Undo.RegisterFullObjectHierarchyUndo((UnityEngine.Object)guid.Value, "go");
                            } 
                        }
                    }
                    else
                    {
                        if (Application.isPlaying)
                        {
                            guid_comp.CheckAndUpdateComponentsGUID();
                            foreach (var guid in guid_comp.ComponentsGUID)
                            {
                                if (!guidDictionary.ContainsKey(guid.Key))
                                {
                                    guidDictionary.Add(guid.Key, guid.Value);
                                    //UnityEditor.Undo.RegisterFullObjectHierarchyUndo((UnityEngine.Object)guid.Value, "go");
                                }
                            }
                        }
                    }
                }
            }
        }
         
    }

    public static GuidDictionary GetDictionary()
    {
        if (instance == null)
            return null;

        return instance.guidDictionary;
    }
}
