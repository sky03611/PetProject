using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;

public enum HelperState
{
    Save, Load
}

struct LoadObject
{
    public ISaloObject obj;
    public SaloHelper helper;
}

public class SaloHelper
{

    public enum State
    {
        Saving, Loading, Completed
    }

    const string SaveError = "Add methods works only in 'Saving' state";
    const string SaveDefError = "Default save method works only in 'Saving' state";
    const string LoadDefError = "Default load method works only in 'Loading' state";
    const string BeginError = "Begin methods can be called only in 'Ready' state.";
    const string SetCompError = "You need to call 'SetComponent' before begin save any data.";
    const string LoadError = "Get methods works only in 'Loading' state";

    private XElement xRoot;

    public State CurrentState { get { return currentState; } }

    private State currentState;

    private XElement xComp;
    private XElement xFieldRoot;
    private XElement xPropRoot;
    private XElement xLinksRoot;
    private XElement xListRoot;
    private XElement xClassData;

    private XElement xCustomData;

    bool ready = false;

    private ISaloObject comp;

    private bool useRoot;

    public SaloHelper(XElement root, HelperState state)
    {
        Initialize(root, state, false);
    }

    public SaloHelper(XElement root, HelperState state, bool useRoot)
    {
        Initialize(root, state, useRoot);
    }

    private void Initialize(XElement root, HelperState state, bool useRoot)
    {
        xRoot = root;
        this.useRoot = useRoot;

        switch (state)
        {
            case HelperState.Save:
                xFieldRoot = new XElement("Fields");
                xPropRoot = new XElement("Properties");
                xLinksRoot = new XElement("Links");
                xListRoot = new XElement("Lists");
                xClassData = new XElement("ClassData");
                xCustomData = new XElement("CustomData");

                currentState = State.Saving;
                break;
            case HelperState.Load:
                currentState = State.Loading;
                break;
            default:
                break;
        }

        xComp = null;
        comp = null;
    }

    public bool IsSaving
    {
        get
        {
            return currentState == State.Saving && ready;
        }
    }

    public bool IsLoading
    {
        get
        {
            return currentState == State.Loading && ready;
        }
    }

    public SerializableGUID RootGUID { get { return rootGUID == null ? SerializableGUID.Empty : rootGUID; } }

    private SerializableGUID rootGUID;

    public bool SetComponent<T>(T obj) where T : ISaloObject
    {
        return SetComponent(obj, SerializableGUID.Empty, string.Empty);
    }

    public bool SetComponent<T>(T obj, SerializableGUID guid) where T : ISaloObject
    {
        return SetComponent(obj, guid, string.Empty);
    }

    public bool SetComponent<T>(T obj, SerializableGUID guid, string customClassName) where T : ISaloObject
    {
        if (ready == true)
        {
            throw new InvalidOperationException(BeginError);
            return false;
        }

        string name = EscapeHelper.Escape(customClassName.Trim());
        bool isCustomName = name.Length > 0;

        string xName = isCustomName ? name : obj.GetType().Name;

        comp = obj;

        if (currentState == State.Saving)
        {
            if (!useRoot)
            {
                rootGUID = guid.IsGuidAssigned() ? guid : obj.GUID;
                string s_guid = rootGUID.ToString();
                xComp = new XElement(xName, new XAttribute("GUID", s_guid));
                xComp.Add(xFieldRoot);
                xComp.Add(xPropRoot);
                if(obj is Behaviour)
                    xComp.Add(new XAttribute("enabled", ((Behaviour)(object)obj).enabled ? "1" : "0"));
            }
            else
            {
                rootGUID = guid.IsGuidAssigned() ? guid : obj.GUID;
                xRoot.Add(xFieldRoot);
                xRoot.Add(xPropRoot);
                if (obj is Behaviour)
                    xRoot.Add(new XAttribute("enabled", ((Behaviour)(object)obj).enabled ? "1" : "0"));
            }
            SaloScene.SaveGUID(guid);
        }
        else
        {
            if (!useRoot)
            {
                if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Info) > 0)
                    Debug.Log("Element1 " + xName + "!useRoot current guid = " + guid);
                if (guid.IsGuidAssigned())
                {
                    rootGUID = guid;
                    var s_guid = guid.ToString();
                    try
                    {

                        var elems = xRoot.Elements();
                        xComp = elems.FirstOrDefault(
                            x => 
                            x.Attribute("GUID")?.Value == s_guid);
                        if (xComp == null)
                        {
                            if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Error) > 0)
                                Debug.LogError("Element " + xName + " cant find reference GUID " + guid + " in root " + xRoot.Name);
                            return false;
                        }
                    }
                    catch(Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }

                }
                else
                    xComp = xRoot.Element(xName);

                if (xComp == null)
                {
                    if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Warning) > 0)
                        Debug.LogWarning("No suitable element is save file with ClassName = " + xName + " and GUID = " + guid.ToString());
                    return false;
                }

                if (obj is Behaviour)
                {
                    var xEnabled = xComp.Attribute("enabled");
                    if (xEnabled != null)
                        ((Behaviour)(object)obj).enabled = xEnabled.Value == "1";
                }

                xComp.Remove();//убираем из списка в глобале, чтобы можно было юзать несколько одинаковых компонентов

                xFieldRoot = xComp.Element("Fields");
                xPropRoot = xComp.Element("Properties");
                xLinksRoot = xComp.Element("Links");
                xListRoot = xComp.Element("Lists");
                xClassData = xComp.Element("ClassData");
                xCustomData = xComp.Element("CustomData");
            }
            else
            {
                rootGUID = SerializableGUID.Parse(xRoot.Attribute("GUID").Value);
                xComp = xRoot;

                if (obj is Behaviour)
                {
                    var xEnabled = xComp.Attribute("enabled");
                    if (xEnabled != null)
                        ((Behaviour)(object)obj).enabled = xEnabled.Value == "1";
                }


                xFieldRoot = xRoot.Element("Fields");
                xPropRoot = xRoot.Element("Properties");
                xLinksRoot = xRoot.Element("Links");
                xListRoot = xRoot.Element("Lists");
                xClassData = xRoot.Element("ClassData");
                xCustomData = xRoot.Element("CustomData");
            }
        }

        ready = true;
        return true;
    }

    public void DefaultSaveComponent()
    {
        if (currentState != State.Saving)
            throw new InvalidOperationException(SaveDefError);
        if (!ready)
            throw new InvalidOperationException(SetCompError);
        XSaveHelper.ObjectToXml(xComp, comp, false, true);
    }


    XElement xListElement;
    bool listStarted;
    public void BeginWriteList(string name)
    {
        if (listStarted)
            return;

        xListElement = new XElement(name);
        xListRoot.Add(xListElement);
        listStarted = true;
    }

    public void ReadComponentListField<T>(string name, ref List<T> list) where T : Component
    {
        var xListElement = xFieldRoot.Element(name);
        if (xListElement == null)
            return;
        object temp = null;
        XSaveHelper.StrToListComponent(xListElement, list.GetType(), xListElement.Elements().Count().ToString(), out temp);
        list = (List<T>)temp;
    }

    public void ReadGameObjectListField(string name, ref List<GameObject> list)
    {
        var xListElement = xFieldRoot.Element(name);
        if (xListElement == null)
            return;
        XSaveHelper.StrToListGameObject(xListElement, xListElement.Elements().Count().ToString(), out list);
    }

    public void ReadSaloListField<T>(string name, ref List<T> list) where T : ISaloObject
    { 
        var xListElement = xFieldRoot.Element(name);
        if (xListElement == null)
            return;
        object temp = null;
        XSaveHelper.StrToListSalo(xListElement, list.GetType(), xListElement.Elements().Count().ToString(), out temp);
        list = (List<T>)temp;
    }

    public int BeginReadList(string name)
    {
        if (listStarted)
            return -1;

        xListElement = xListRoot.Element(name);
        if (xListElement == null)
            return -1;
        listStarted = true;
        return xListElement.Elements().Count();
    }

    public XElement GetListElement(int index)
    {
        if (!listStarted)
            return null;

        var elements = xListElement.Elements();
        if (elements.Count() <= index)
            return null;

        return elements.ElementAt(index);
    }

    public void EndList()
    {
        listStarted = false;
    }

    /// <summary>
    /// Сохраняет базовые типы, Component и GameObject а так же их списки.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public void AddField(string name, object value)
    {
        if (!IsSaving)
            throw new InvalidOperationException(SaveError);
        var activeRoot = listStarted ? xListElement : xFieldRoot;
        activeRoot.Add(XSaveHelper.ObjectToXml(name, value));
    }

    /// <summary>
    /// Сохраняет ссылку или значение класса или структуры реализующие интерфейс ISaLoObject
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="obj"></param>
    internal void AddSaloField<T>(string name, T obj) where T : ISaloObject
    {
        if (!IsSaving)
            throw new InvalidOperationException(SaveError);

        var activeRoot = listStarted ? xListElement : xLinksRoot;
        if (obj == null)
            activeRoot.Add(new XElement(name, new XAttribute("GUID", "")));
        else
        {
            activeRoot.Add(new XElement(name, new XAttribute("GUID", obj.GUID)));

            /*var guid = obj.GUID;
            Type t = typeof(T);
            if (t.IsSubclassOf(typeof(UnityEngine.Object)))//юнитовские объекты сохраняются только по GUID
            {
                activeRoot.Add(new XElement(name, new XAttribute("GUID", guid)));
                return;
            }

            if (!SaloScene.IsGUIDSaved(guid))
                AddSaloObject(guid, obj);

            activeRoot.Add(new XElement(name, new XAttribute("GUID", guid)));*/

        }
    }

    private void AddSaloObject<T>(SerializableGUID guid, T value) where T : ISaloObject
    {
        if (!IsSaving)
            throw new InvalidOperationException(SaveError);


        SaloHelper saloHelper = new SaloHelper(xClassData, HelperState.Save);
        saloHelper.SetComponent(value, value.GUID);
        value.Save(saloHelper);
        if (saloHelper.IsSaving)
            saloHelper.EndSave();



    }

    public void AddCustomData(string name, string value)
    {
        if (!IsSaving)
            throw new InvalidOperationException(SaveError);
        var activeRoot = listStarted ? xListElement : xCustomData;
        activeRoot.Add(new XElement(name, EscapeHelper.Escape(value)));
    }

    public void EndSave()
    {
        if (currentState == State.Completed)
            return;

        if (!useRoot)
        {
            xComp.Add(xLinksRoot);
            xComp.Add(xListRoot);
            xComp.Add(xClassData);
            xComp.Add(xCustomData);
            xRoot.Add(xComp);
        }
        else
        {
            xRoot.Add(xLinksRoot);
            xRoot.Add(xListRoot);
            xRoot.Add(xClassData);
            xRoot.Add(xCustomData);
        }
        currentState = State.Completed;

        xRoot = null;
        xComp = null;
        xFieldRoot = null;
        xLinksRoot = null;
        xListRoot = null;
        xClassData = null;

        comp = null;
    }


    public bool DefaultLoadComponent()
    {
        if (currentState != State.Loading)
            throw new InvalidOperationException(LoadDefError);
        if (!ready)
            throw new InvalidOperationException(SetCompError);


        return XSaveHelper.XmlToComponent(xComp, comp, true, true);
    }

    public bool GetField<T>(string name, ref T value)
    {
        if (!IsLoading)
            throw new InvalidOperationException(LoadError);
        var activeRoot = listStarted ? xListElement : xFieldRoot;
        XElement xValue = activeRoot.Element(name);
        if (xValue == null)
            return false;

        return XSaveHelper.XmlTo(xValue, ref value);
    }

    /// <summary>
    /// Метод для получения поля ссылочного типа или структуры, реализующие интерфейс ISaLoObject
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool GetSaloField<T>(string name, ref T value) where T : ISaloObject
    {
        if (!IsLoading)
            throw new InvalidOperationException(LoadError);
        var activeRoot = listStarted ? xListElement : xLinksRoot;
        XElement xValue = activeRoot.Element(name);
        if (xValue == null)
            return false;


        if (!SerializableGUID.TryParse(xValue.Attribute("GUID").Value, out SerializableGUID guid))
        {
            value = default(T);
            return true;
        }

        T instance = SaloGUIDManager.Get<T>(guid);
        if (instance == null)
            value = default(T);
        else
            value = instance;
        return true;
    }

    private bool GetSaloObject<T>(SerializableGUID guid, ref T value) where T : ISaloObject
    {
        if (!IsLoading)
            throw new InvalidOperationException(LoadError);

        value = (T)Activator.CreateInstance(typeof(T));
        SaloHelper loader = new SaloHelper(xClassData, HelperState.Load);
        if (!loader.SetComponent(value, guid))
            return false;
        if (value != null)
            return value.Load(loader);
        return false;
    }

    public string GetCustomData(string name)
    {
        if (!IsLoading)
            throw new InvalidOperationException(LoadError);
        var activeRoot = listStarted ? xListElement : xCustomData;
        XElement xValue = activeRoot.Element(name);
        if (xValue == null)
            return string.Empty;
        return xValue.Value;
    }


    public void EndLoad()
    {
        currentState = State.Completed;
        xRoot = null;
        xComp = null;
        xFieldRoot = null;
        comp = null;
    }
}

