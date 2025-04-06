using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SaloObject : MonoBehaviour
{
    public bool Dynamic;
    public string PrefabPath;
    [SerializeField]
    private Transform[] SubObjectsRoot;

    private XElement initializedObject;

    private List<LoadObject> LoadedObjects;

    private List<ChildObject> childsInit;

    private struct ChildObject
    {
        public Transform childTransformNode;
        public XElement childTransformXNode;
        public List<LoadObject> objs;
    }

 

    public XElement Save()
    {
        XElement xSaloObject = new XElement("SaloObject", new XAttribute("GUID", FindLocalGUID(this)),
            new XAttribute("Dynamic", Dynamic),
            new XAttribute("PrefabPath", PrefabPath));

        XElement xComponents = new XElement("Components");

        XElement xGameObject = new XElement("GameObject", new XAttribute("GUID", SaloGUIDManager.FindGUID(gameObject)));
        xGameObject.Add(DefaultFieldToXml("String", "name", gameObject.name));
        xGameObject.Add(DefaultFieldToXml("Boolean", "activeInHierarchy", gameObject.activeInHierarchy));
        xGameObject.Add(DefaultFieldToXml("Int32", "layer", gameObject.layer));
        xGameObject.Add(DefaultFieldToXml("String", "tag", gameObject.tag));
        xComponents.Add(xGameObject);

        XElement xTransform = new XElement("Transform", new XAttribute("GUID", SaloGUIDManager.FindGUID(transform)));
        xTransform.Add(DefaultFieldToXml("Vector3", "localPosition", XSaveHelper.Vector3ToXml(transform.localPosition)));
        xTransform.Add(DefaultFieldToXml("Quaternion", "localRotation", XSaveHelper.QuaternionToXml(transform.localRotation)));
        xTransform.Add(DefaultFieldToXml("Vector3", "localScale", XSaveHelper.Vector3ToXml(transform.localScale)));
        xTransform.Add(DefaultFieldToXml("Component", "parent", transform.parent ? SaloGUIDManager.FindGUID(transform.parent).ToString() : ""));
        xComponents.Add(xTransform);
        var components = GetComponents<Behaviour>();

        foreach (var item in components)
        {
            if (item is ISaloObject)
            {
                var salo = (ISaloObject)item;
                //each script must have this.InstanceID()
                SaloHelper helper = new SaloHelper(xComponents, HelperState.Save);
                helper.SetComponent(salo);
                salo.Save(helper);
                if (helper.CurrentState == SaloHelper.State.Saving)
                    helper.EndSave();
            }
            else
            {
                XElement xDefComp = new XElement(item.GetType().Name, new XAttribute("enabled", item.enabled ? 1 : 0));
                xComponents.Add(xDefComp);
            }
        }
        xSaloObject.Add(xComponents);

        if (Dynamic)
        {
            XElement xChilds = new XElement("Childs");
            foreach (var item in SubObjectsRoot)
            {
                if (item == null)
                    continue;

                xChilds.Add(XSaveChild(item));
                int childs = item.childCount;
                for (int i = 0; i < childs; i++)
                    xChilds.Add(XSaveChild(item.GetChild(i)));

            }
            xSaloObject.Add(xChilds);
        }
        return xSaloObject;
    }

    private XElement XSaveChild(Transform item)
    {
        //TODO: сохранение и рут ноды в чилдах
        XElement xChild = new XElement("Child", new XAttribute("Name", item.name));

        var childGO = item.gameObject;

        SaloGUID guid = item.GetComponent<SaloGUID>();
        if(guid == null)
        {
            guid = item.gameObject.AddComponent<SaloGUID>();
            guid.Generate();
            guid.Register();
        }

        XElement xGameObject = new XElement("GameObject", new XAttribute("GUID", SaloGUIDManager.FindGUID(childGO)));
        xGameObject.Add(DefaultFieldToXml("String", "name", childGO.name));
        xGameObject.Add(DefaultFieldToXml("Boolean", "activeInHierarchy", childGO.activeInHierarchy));
        xGameObject.Add(DefaultFieldToXml("Int32", "layer", childGO.layer));
        xGameObject.Add(DefaultFieldToXml("String", "tag", childGO.tag));
        xChild.Add(xGameObject);

        XElement xTransform = new XElement("Transform", new XAttribute("GUID", SaloGUIDManager.FindGUID(item)));
        xTransform.Add(DefaultFieldToXml("Vector3", "localPosition", XSaveHelper.Vector3ToXml(item.localPosition)));
        xTransform.Add(DefaultFieldToXml("Quaternion", "localRotation", XSaveHelper.QuaternionToXml(item.localRotation)));
        xTransform.Add(DefaultFieldToXml("Vector3", "localScale", XSaveHelper.Vector3ToXml(item.localScale)));
        xTransform.Add(DefaultFieldToXml("Component", "parent", item.parent ? SaloGUIDManager.FindGUID(item.parent).ToString() : ""));
        xChild.Add(xTransform);

        var childAllComps = item.GetComponents<Behaviour>();
        foreach (var childComp in childAllComps)
        {
            if (!(childComp is ISaloObject))
            {
                //Сохраняем только состояние включен ли компонент
                XElement xDefComp = new XElement(childComp.GetType().Name, new XAttribute("enabled", childComp.enabled ? 1 : 0));
                xChild.Add(xDefComp);
            }
            else
            {
                var saloComp = (ISaloObject)childComp;
                SaloHelper helper = new SaloHelper(xChild, HelperState.Save);
                helper.SetComponent(saloComp);
                saloComp.Save(helper);
                if (helper.CurrentState == SaloHelper.State.Saving)
                    helper.EndSave();
            }

        }
        return xChild;
    }


    public void Load(XElement xSaloObject)
    {
        childsInit = new List<ChildObject>();
        var xaDynamic = xSaloObject.Attribute("Dynamic");
        if (xaDynamic == null)
            return;

        Dynamic = Boolean.Parse(xaDynamic.Value);
        PrefabPath = xSaloObject.Attribute("PrefabPath").Value;

        var xComponents = xSaloObject.Element("Components");

        var xGameObject = xComponents.Element("GameObject");

        gameObject.name = xGameObject.Element("name").Attribute("value").Value;

        var xActiveInHierarchy = xGameObject.Element("activeInHierarchy");
        gameObject.SetActive(Boolean.Parse(xActiveInHierarchy.Attribute("value").Value));

        var xLayer = xGameObject.Element("layer");
        gameObject.layer = int.Parse(xLayer.Attribute("value").Value);

        gameObject.tag = xGameObject.Element("tag").Attribute("value").Value;

        initializedObject = xComponents.Element("Transform");

        var defComps = GetComponents<Behaviour>();
        LoadedObjects = new List<LoadObject>();
        foreach (var item in defComps)
        {
            if (item is ISaloObject)
            {
                var element = new LoadObject
                { helper = new SaloHelper(xComponents, HelperState.Load), obj = item as ISaloObject };
                LoadedObjects.Add(element);
                if (element.helper.SetComponent(element.obj, Dynamic ? SerializableGUID.Empty : element.obj.GUID))
                    element.obj.Load(element.helper);
            }
            else
            {
                var xComp = xComponents.Element(item.GetType().Name);
                if (xComp != null)
                {
                    var xEnabled = xComp.Attribute("enabled");
                    if (xEnabled != null)
                        item.enabled = xEnabled.Value == "1";
                }
            }
        }

       

        if (Dynamic)
        {
            var xChilds = xSaloObject.Element("Childs").Elements();
            int enc = 0;
            int cnt = xChilds.Count();
            foreach (var item in SubObjectsRoot)
            {
                if (enc >= cnt)
                    break;

                XLoadChild(item, xChilds.ElementAt(enc++));

                int childs = item.childCount;
                for (int i = 0; i < childs; i++)
                {
                    if (enc >= cnt)
                        break;
                    XLoadChild(item.GetChild(i), xChilds.ElementAt(enc++));
                }

            }
        }
    }

    private void XLoadChild(Transform item, XElement xChild)
    {
        //у нас на дочерних элементах уже есть сало которое будет вызвано само собой
        if (item.GetComponent<SaloObject>() != null)
            return;

        if (item.GetComponent<SaloGUID>() == null)
            item.gameObject.AddComponent<SaloGUID>();
 
        var c_loadObjects = new List<LoadObject>();
        ChildObject c_Object = new ChildObject();
        c_Object.childTransformNode = item;
        c_Object.childTransformXNode = xChild.Element("Transform");
        c_Object.objs = c_loadObjects;

        var defComps = item.GetComponents<Behaviour>();

        foreach (var defCmp in defComps)
        {
            if (defCmp is ISaloObject)
            {
                SaloHelper helper = new SaloHelper(xChild, HelperState.Load);
                if (helper.SetComponent(defCmp as ISaloObject))
                {
                    ((ISaloObject)defCmp).Load(helper);
                    c_loadObjects.Add(new LoadObject()
                    {
                        helper = helper,
                        obj = defCmp as ISaloObject
                    });
                }
            }
            else
            {
                continue;
                var xComp = xChild.Element(item.GetType().Name);
                if (xComp != null)
                {
                    var xEnabled = xComp.Attribute("enabled");
                    if (xEnabled != null)
                        defCmp.enabled = xEnabled.Value == "1";
                }
            }
        } 
        childsInit.Add(c_Object);
    }


    public void Initialize()
    {
        if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Warning) > 0)
            Debug.LogWarning("Initialize of " + name);
        if (initializedObject != null)
        {
            InitTranform(transform, initializedObject);

            foreach (var item in LoadedObjects)
            {
                item.obj.Initialize(item.helper);
                if (item.helper.IsLoading)
                    item.helper.EndLoad();
            }

            foreach (var item in childsInit)
            {
                InitTranform(item.childTransformNode, item.childTransformXNode);
                foreach (var chld in item.objs)
                {
                    chld.obj.Initialize(chld.helper);
                    if (chld.helper.IsLoading)
                        chld.helper.EndLoad();
                }
            }
        }
        else
        {
            foreach (var item in GetComponents<ISaloObject>())
            {
                item.Initialize();
            }
        }
    }

    private void InitTranform(Transform trnsf, XElement xTransformNode)
    {
        if (trnsf == null || xTransformNode == null)
            return;
        XSaveHelper.StrToVector3(xTransformNode.Element("localPosition").Attribute("value").Value, out Vector3 locPos);
        XSaveHelper.StrToQuaternion(xTransformNode.Element("localRotation").Attribute("value").Value, out Quaternion locRot);
        XSaveHelper.StrToVector3(xTransformNode.Element("localScale").Attribute("value").Value, out Vector3 locScl);

        string s_parentGUID = xTransformNode.Element("parent").Attribute("value").Value;

        SerializableGUID parentGUID = null;
        SerializableGUID.TryParse(s_parentGUID, out parentGUID);
        var parentTransform = SaloGUIDManager.Get<Transform>(parentGUID);
        trnsf.SetParent(parentTransform);

        trnsf.localPosition = locPos;
        trnsf.localRotation = locRot;
        trnsf.localScale = locScl;
    }

    private SaloGUID guidComponent;
    private SerializableGUID FindLocalGUID(UnityEngine.Object obj)
    {
        if (guidComponent == null)
            guidComponent = GetComponent<SaloGUID>();
        if (!guidComponent)
            return SerializableGUID.Empty;
        return guidComponent.FindLocal(obj);
    }

    private static XElement DefaultFieldToXml(string type, string name, object value)
    {
        var xField = new XElement(name);
        xField.Add(new XAttribute("type", type));
        xField.Add(new XAttribute("value", value.ToString())); //перевод значения поля в рассматриваемом компоненте в строковое представление
        return xField;
    }
}

