//SaveEntropy 2.2
//Gluschenko
//2-08-2016
//SaveEntropy 3.0
//Hellmapper
//13-09-2017

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CM = System.ComponentModel;
using System.Xml.Linq;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[System.Serializable]
public class UnityIntEvent : UnityEvent<int> { }
[System.Serializable]
public class UnitySLSEvent : UnityEvent<SaveLoadState> { }

public class SaloScene : MonoBehaviour
{
    public SaLoInfoLevel LogInfoLevel = SaLoInfoLevel.Error;
    private static SaloScene link;
    public static bool IsSaving { get; private set; }
    public static bool IsLoading { get; private set; }
    /// <summary>
    /// Сколько объектов будет обработано за 1 кадр в корутине
    /// </summary>
    public int PerFrameObjects = 5;
    private int objEncouter;


    [SerializeField]
    private bool useCryptography;

    /// <summary>
    /// Сохранять и загружать Свойства объектов
    /// Если False то стандартные классы юнити не будут сохранены. Трансформ уж точно :)
    /// </summary>
    public bool UseProperties = true;
    private bool _useProperties;
    /// <summary>
    /// Удалять объекты компоненты которых повреждены во время загрузки
    /// </summary>
    public bool RemoveCorruptedObjects;
    private bool _removeCorruptedObjects;
    /// <summary>
    /// Останавливать загрузку поврежденных карт
    /// </summary>
    public bool BreakOnCurrupted;
    private bool _breakOnCurrupted;

    [SerializeField]
    private UnityEvent SaveCompleted;
    [SerializeField]
    private UnityIntEvent SaveProgressChanged;
    [SerializeField]
    private UnitySLSEvent SaveError;

    [SerializeField]
    private UnityIntEvent LoadProgressChanged;
    [SerializeField]
    private UnityEvent LoadCompleted;
    [SerializeField]
    private UnitySLSEvent LoadError;

    private List<SaloObject> saloObjects;

    private SaveLoadState saveState;

    public bool IsSaved { get; private set; }

    static List<LoadObject> heapInitObjects;

    private void Awake()
    {
        link = this;
        SaveLoadManager.InfoLevel = LogInfoLevel;
    }

    public void Start()
    {
        if (SaveLoadManager.IsNewGame)
        {
            var sceneObjects = GetAllSaloObjects(false);
            foreach (var item in sceneObjects)
            {
                item.Initialize();
            }
            SaveLoadManager.IsNewGame = false;
        }

    }

    void OnDestroy()
    {
        link = null;
    }



    public void SaveGame(string timeofday)
    {
        if (IsSaving) //мы в процессе сохранения
        {
            SaveError.Invoke(SaveLoadState.InProgress);
            return;
        }

        objEncouter = 0;
        _useProperties = UseProperties;
        perSaveGUID = new List<SerializableGUID>();
        StartCoroutine(SaveWorldCourutine(timeofday));
    }

    public void LoadGame()
    {
        if (IsLoading)
        {
            LoadError.Invoke(SaveLoadState.InProgress);
            return;
        }
        SaveLoadManager.IsNewGame = false;
        //т.к. у нас используются коурутины то мы не должны позволять менять эти значения во время их выполнения
        _breakOnCurrupted = BreakOnCurrupted;
        _removeCorruptedObjects = RemoveCorruptedObjects;
        _useProperties = UseProperties;
        XDocument xworld = null;
        SaveLoadState state = SaveLoadManager.LoadXWorld(out xworld, useCryptography);
        if (state == SaveLoadState.Success)
            StartCoroutine(LoadWorldCourutine(xworld));
        else
            LoadError.Invoke(state);
    }



    private IEnumerator SaveWorldCourutine(string timeofday)
    {
        IsSaving = true;
        saveState = SaveLoadState.InProgress;
        //список уже сохраненных тегов

        XDocument xworld = new XDocument();
        xworld.Add(new XElement("World"));
        xworld.Root.Add(new XAttribute("IsTutorial", SceneManager.GetActiveScene().buildIndex == 2));
        xworld.Root.Add(new XAttribute("Date", EscapeHelper.Escape(DateTime.Now.ToString("dd/MM/yy"))));
        xworld.Root.Add(new XAttribute("TimeOfDay", EscapeHelper.Escape(timeofday)));


        XElement xStatic = new XElement("Static");
        XElement xDynamic = new XElement("Dynamic");
        XElement xHeap = new XElement("Heap");
        SaloGUIDManager.AddGUIDs();

        yield return WaitFor.Frames(5);

        GuidDictionary totalObjects = SaloGUIDManager.GetDictionary();

        int maxSaloObjects = totalObjects.Count(
            x =>
            {
                var type = x.Value.GetType();
                return type.GetInterface("ISaloObject") != null || type.IsSubclassOf(typeof(SaloObject));
            }
        );
        int percEncounter = 0;
        foreach (var item in totalObjects)
        {
            var type = item.Value.GetType();

            if (type.GetInterface("ISaloObject") != null && !type.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Info) > 0)
                    Debug.Log("Saving to heap: " + type.Name + " with guid " + item.Key);
                //Сохраняем классы и структуры реализующие ISaloObject но не являющиеся объектами в юнити в кучу.
                SaloHelper heapHelper = new SaloHelper(xHeap, HelperState.Save);
                var obj = (ISaloObject)item.Value;
                heapHelper.SetComponent(obj);
                obj.Save(heapHelper);
                heapHelper.EndSave();
                percEncounter++;//вынес вперед чтобы правильно просчитывались пропущенные объекты без загромождения кода
                int progress = (int)(((float)percEncounter / (float)maxSaloObjects) * 100);
                SaveProgressChanged.Invoke(progress);//для дебага фриза

                objEncouter++;
                if (objEncouter >= PerFrameObjects)
                {
                    objEncouter = 0;
                    yield return null;
                }

            }
            else
                if (type.Equals(typeof(SaloObject)))
            {
                var salo = (SaloObject)item.Value;

                if (salo == null)
                {
                    continue;
                }

                if (salo.Dynamic)
                    xDynamic.Add(salo.Save());
                else
                    xStatic.Add(salo.Save());
                percEncounter++;//вынес вперед чтобы правильно просчитывались пропущенные объекты без загромождения кода
                int progress = (int)(((float)percEncounter / (float)maxSaloObjects) * 100);
                SaveProgressChanged.Invoke(progress);//для дебага фриза
                objEncouter++;
                if (objEncouter >= PerFrameObjects)
                {
                    objEncouter = 0;
                    yield return null;
                }
            }
        }

        xworld.Root.Add(xStatic);
        xworld.Root.Add(xDynamic);
        xworld.Root.Add(xHeap);
        if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Info) > 0)
            Debug.Log("Gathering object completed. Begin save thread.");
        //выносим в отдельный поток сохранение затенения (т.к. можем) и сохранение в файл.
        Thread writeSaveThread = new Thread(() =>
        {
            try
            {
                //XElement xHaze = Haze.Save();
                /*if (xHaze != null)
                    xworld.Root.Add(xHaze);*/
                saveState = SaveLoadManager.SaveXWorld(xworld, useCryptography);
                if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Info) > 0)
                    Debug.Log("Save state = " + saveState);
            }
            catch (Exception e)
            {
                saveState = SaveLoadState.UnknownError;
                if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Error) > 0)
                    Debug.LogError("Error in save final thread: " + e.ToString());
            }
        });
        writeSaveThread.Start();
    }


    internal static void AddHeapInitializer<T>(T go, SaloHelper loader) where T : ISaloObject
    {
        heapInitObjects.Add(new LoadObject() { obj = go, helper = loader });
    }

    static List<SaloObject> GetAllSaloObjects(bool onlyRoot)
    {
        List<SaloObject> values = new List<SaloObject>();
        GameObject[] rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjs)
        {
            if (onlyRoot)
            {
                var comp = obj.GetComponent<SaloObject>();
                if (comp)
                    values.Add(comp);
            }
            else
            {
                foreach (SaloObject item in obj.GetComponentsInChildren<SaloObject>(true))
                {
                    values.Add(item);
                }
            }
        }
        return values;
    }

    private void Update()//нам незачем проверять стату каждый кадр, пусть будет как у физики
    {
        if (IsSaving && saveState != SaveLoadState.InProgress)
        {
            IsSaving = false;
            SaveProgressChanged.Invoke(100);//для красивого завершения процентов
            if (saveState == SaveLoadState.Success)
                SaveCompleted.Invoke();
            else
                SaveError.Invoke(saveState);
        }

    }



    private IEnumerator LoadWorldCourutine(XDocument xworld)
    {
        if (xworld == null)
        {
            LoadError.Invoke(SaveLoadState.Currupted);
            yield break;
        }

        IsLoading = true;
        //список уже сохраненных тегов
        List<string> TagsIndex = new List<string>();
        //xdoc.Root.Add(new XAttribute("ID", GPrefs.WorldID));//Ид мира. Как я понял это слот карты в загрузке. Т.к. мы позволим игрокам менять сейвы местами то игнориуем данное поле
        int percEncounter = 0;
        //будем считать проценты только для обработанных тегов. 

        //Сохраняем объекты из кучи сюда для последующей инициализации
        heapInitObjects = new List<LoadObject>();

        saloObjects = GetAllSaloObjects(false);
        int cnt = saloObjects.Count;
        for (int i = 0; i < cnt; i++)
        {
            var item = saloObjects[i];
            if (item.Dynamic)
            {
                DestroyImmediate(item.GetComponent<SaloGUID>());
                saloObjects.RemoveAt(i);
                cnt--;
                i--;
            }
        }

        var guidManager = SaloGUIDManager.FindGUIDManager();
        guidManager.Clear();
        guidManager.RefreshGUIDs(false);
        //Разбираем статические объекты
        var xStatic = xworld.Root.Element("Static");
        var xStaticObjects = xStatic.Elements("SaloObject");
        var xDynamic = xworld.Root.Element("Dynamic");
        var xDynamicObjects = xDynamic.Elements("SaloObject");
        var xHeap = xworld.Root.Element("Heap");
        XSaveHelper.xTempHeap = xHeap;

        var xHeapObjects = xHeap.Elements();
        int maxPercEncounter = xStaticObjects.Count() + xDynamicObjects.Count();
        foreach (var xStaticSalo in xStaticObjects)
        {
            var xIID = xStaticSalo.Attribute("GUID");
            var saloObject = SaloGUIDManager.Get<SaloObject>(SerializableGUID.Parse(xIID.Value));
            if (saloObject)
            {
                saloObject.Load(xStaticSalo);
            }
            else
            {
                var xGO = xStaticSalo.Element("Components").Element("GameObject");
                var xGO_name = xGO.Element("name");
                var xGO_name_a_value = xGO_name.Attribute("value");
                if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Error) > 0)
                    Debug.LogError("Loading error. Static Object (" + xGO_name_a_value?.Value + ") of type " + xStaticSalo.Name + " with GUID = " + xIID?.Value + " hasn't got scene reference. GUIDS may have been updated incorrectly in edit mode.");
                if (ProceedCorrupted(null))
                    yield break;
                continue;
            }

            percEncounter++;
            int Progress = (int)(((float)percEncounter / (float)maxPercEncounter) * 100);
            LoadProgressChanged.Invoke(Progress);
        }

        //Разбираем динамические объекты


        foreach (var xDynamicSalo in xDynamicObjects)
        {
            var prefab = Resources.Load<GameObject>(xDynamicSalo.Attribute("PrefabPath").Value);
            var dynGameObject = Instantiate(prefab);
            dynGameObject.AddComponent<SaloGUID>();
            var xGUID = xDynamicSalo.Attribute("GUID");


            SerializableGUID dynGIID = SerializableGUID.Parse(xGUID.Value);
            var dynSalo = dynGameObject.GetComponent<SaloObject>();
            dynSalo.Load(xDynamicSalo);
            saloObjects.Add(dynSalo);

            percEncounter++;
            int Progress = (int)(((float)percEncounter / (float)maxPercEncounter) * 100);
            LoadProgressChanged.Invoke(Progress);
        }

        foreach (var item in heapInitObjects)
        {
            item.obj.Initialize(item.helper);
        }

        foreach (var item in saloObjects)
        {
            item.Initialize();
        };


#warning Look comment below
        //---------------------------------------------------------------------------------------------------------------------//
        // Для активации покадровой загрузки раскомментировать строчку yield return null.                                      //
        // Метод покадровой загрузки лучше всего реализовывать когда от игрока скрыта сцена и выводится прогресс загрузки.     //
        // Обновление сцены и объектов (т.е. методов Update FixedUpdate и LateUpdate) так же не должны происходить.            //
        // Это необходимо, чтобы не происходили ошибки из-за неназначенных переменных.                                         //
        // В качестве реализации можно в самое начало каждого такого метода дописать.                                          //
        // if (SaveLoadManager.IsLoading) return;                                                                              //
        //---------------------------------------------------------------------------------------------------------------------//
        //yield return null;

        //Haze.Load(xworld.Root);
        IsLoading = false;
        LoadCompleted.Invoke();
    }

    internal static void SaveGUID(SerializableGUID guid)
    {
        if (perSaveGUID == null)
            perSaveGUID = new List<SerializableGUID>();
        perSaveGUID.Add(guid);
    }

    static List<SerializableGUID> perSaveGUID;

    internal static bool IsGUIDSaved(SerializableGUID guid)
    {
        if (perSaveGUID == null)
            return false;

        return perSaveGUID.Contains(guid);
    }


    /// <summary>
    /// Обработка поврежденного объекта
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>True - требуется прервать дальнейшее выполнение коурутины</returns>
    private bool ProceedCorrupted(GameObject obj)
    {
        if (obj != null && (_removeCorruptedObjects || _breakOnCurrupted))
            Destroy(obj);

        if (_breakOnCurrupted)
        {
            LoadError.Invoke(SaveLoadState.Currupted);
            IsLoading = false;
            return true;
        }

        return false;
    }


    public void LogError(string reason)
    {
        if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Info) > 0)
            Debug.LogError("SaloScene error: " + reason);
    }


}
