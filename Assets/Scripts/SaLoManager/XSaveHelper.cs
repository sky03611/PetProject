using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

static class XSaveHelper
{
    public static XElement xTempHeap;
    public static bool IsValidType(Type _type)
    {
        string type = _type.Name;
        if (type == "List`1")
        {
            if (_type.IsGenericType && _type.GetGenericTypeDefinition()
                    == typeof(List<>))
            {

                Type itemType = _type.GetGenericArguments()[0]; // use this...
                return IsBaseType(itemType);
            }
        }
        bool isEnum = _type.IsSubclassOf(typeof(Enum));
        bool isComponent = _type.IsSubclassOf(typeof(Component));
        //Типы, которые мы можем сохранить из классов
        return IsBaseType(_type);
    }

    private static bool IsBaseType(Type type)
    {
        //Можно было и через массив и цикл, но так дебаг нагляднее
        //Здесь идут struct и sealed классы
        if (type == typeof(String))
            return true;
        if (type == typeof(Boolean))
            return true;
        if (type == typeof(Byte))
            return true;
        if (type == typeof(SByte))
            return true;
        if (type == typeof(UInt16))
            return true;
        if (type == typeof(UInt32))
            return true;
        if (type == typeof(UInt64))
            return true;
        if (type == typeof(Int16))
            return true;
        if (type == typeof(Int32))
            return true;
        if (type == typeof(Int64))
            return true;
        if (type == typeof(Single))
            return true;
        if (type == typeof(Double))
            return true;
        if (type == typeof(Vector2))
            return true;
        if (type == typeof(Vector2Int))
            return true;
        if (type == typeof(Vector3))
            return true;
        if (type == typeof(Vector3Int))
            return true;
        if (type == typeof(Vector4))
            return true;
        if (type == typeof(Quaternion))
            return true;
        if (type == typeof(GameObject))
            return true;

        //Здесь любые наследования и SaLo
        if (type.IsSubclassOf(typeof(Component)))
            return true;

        if (type.IsEnum || type.IsSubclassOf(typeof(Enum)))
            return true;

        if (IsSaloHeapObject(type))
            return true;

        return false;
    }

    private static string GetCorrectBaseTypeName(Type _type)
    {
        string type = _type.Name;

        if (_type.IsEnum)
            return "Enum";
        if (_type.IsSubclassOf(typeof(Component)))
            return "Component";

        if (IsSaloHeapObject(_type))
            return _type.Name;

        if (_type.IsGenericType && _type.GetGenericTypeDefinition()
            == typeof(List<>))
            return "List`1";

        if (IsBaseType(_type))
            return type;

        return "null";
    }

    private static string EnumToXml(Enum e)
    {
        return Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType())).ToString();
    }

    private static bool XmlToEnum<T>(string xmlValue, out T en) where T : Enum
    {
        int v = 0;
        en = (T)Enum.GetValues(typeof(T)).GetValue(0);
        if (!int.TryParse(xmlValue, out v))
            return false;
        if (!Enum.IsDefined(typeof(T), v))
            return false;
        en = (T)Enum.ToObject(typeof(T), v);
        return true;
    }

    public static string ComponentToXml(Component go)
    {
        if (go == null)
            return "0";
        return SaloGUIDManager.FindGUID(go).ToString();
    }

    public static bool StrToComponent(string xmlValue, out Component go)
    {
        SerializableGUID instanceID;
        bool parsed = SerializableGUID.TryParse(xmlValue, out instanceID);
        go = null;
        if (!parsed)
            return false;
        go = SaloGUIDManager.Get<Component>(instanceID);
        return true;
    }

    private static string GameObjectToXml(GameObject go)
    {
        if (go == null)
            return SerializableGUID.Empty.ToString();
        return SaloGUIDManager.FindGUID(go).ToString();
    }

    private static bool StrToGameObject(string xmlValue, out GameObject go)
    {
        SerializableGUID instanceID;
        bool parsed = SerializableGUID.TryParse(xmlValue, out instanceID);
        go = null;
        if (!parsed)
            return false;
        go = SaloGUIDManager.Get<GameObject>(instanceID);
        return true;
    }

    private static string SaLoToXml(ISaloObject go)
    {
        if (go == null)
            return SerializableGUID.Empty.ToString();


        if (go.GUID == null)
        {
            if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Error) > 0)
                Debug.LogError(go.ClassSaveName + " GUID is NULL");

            return "";
        }


        return go.GUID.ToString();
    }

    private static bool StrToSalo(string xmlValue, Type destType, out object go)
    {
        if (xmlValue == "")
        {
            go = null;
            return true;
        }
        SerializableGUID instanceID;
        bool parsed = SerializableGUID.TryParse(xmlValue, out instanceID);
        go = null;
        if (!parsed)
            return false;
        go = SaloGUIDManager.Get<object>(instanceID);
        if (go == null)
        {
            var xHeapObject = xTempHeap.Elements().FirstOrDefault(x => x.Attribute("GUID").Value == xmlValue);

            if (xHeapObject != null)
            {
                if (destType.IsAbstract)
                    destType = Assembly.GetExecutingAssembly().GetType(xHeapObject.Name.LocalName);
                //go = Activator.CreateInstance(destType);
                go = Activator.CreateInstance(destType);
                var inf = (ISaloObject)go;
                SaloHelper loader = new SaloHelper(xHeapObject, HelperState.Load, true);
                loader.SetComponent(inf);
                inf.Load(loader);
                SaloScene.AddHeapInitializer(inf, loader);

            }
        }
        return true;
    }

    #region Vector 2
    public static string Vector2ToXml(Vector2 v)
    {
        return v.x + ";" + v.y;
    }

    public static string Vector2ToXml(Vector2Int v)
    {
        return v.x + ";" + v.y;
    }

    public static bool StrToVector2(string xmlValue, out Vector2 v)
    {
        v = Vector2.zero;
        //Все эксепшены можно убрать и добавить игнорирование данного вектора при загрузке
        string[] data = xmlValue.Split(';');
        if (data.Length != 2)
            return false; //не хватает компонент вектора
        for (int i = 0; i < 2; i++)
        {
            float value = 0;
            if (!float.TryParse(data[i], out value))
                return false; //не смогли спарсить значение
            v[i] = value;
        }
        return true;
    }

    public static bool StrToVector2(string xmlValue, out Vector2Int v)
    {
        v = Vector2Int.zero;
        //Все эксепшены можно убрать и добавить игнорирование данного вектора при загрузке
        string[] data = xmlValue.Split(';');
        if (data.Length != 2)
            return false; //не хватает компонент вектора
        for (int i = 0; i < 2; i++)
        {
            int value = 0;
            if (!int.TryParse(data[i], out value))
                return false; //не смогли спарсить значение
            v[i] = value;
        }
        return true;
    }
    #endregion

    #region Vector 3
    public static string Vector3ToXml(Vector3 v)
    {
        return v.x + ";" + v.y + ";" + v.z;
    }

    public static string Vector3ToXml(Vector3Int v)
    {
        return v.x + ";" + v.y + ";" + v.z;
    }

    public static bool StrToVector3(string xmlValue, out Vector3 v)
    {
        v = Vector3.zero;
        //Все эксепшены можно убрать и добавить игнорирование данного вектора при загрузке
        string[] data = xmlValue.Split(';');
        if (data.Length != 3)
            return false; //не хватает компонент вектора
        Vector3 temp = Vector3.zero;
        for (int i = 0; i < 3; i++)
        {
            float value = 0;
            if (!float.TryParse(data[i], out value))
                return false; //не смогли спарсить значение
            v[i] = value;
        }
        return true;
    }

    public static bool StrToVector3(string xmlValue, out Vector3Int v)
    {
        v = Vector3Int.zero;
        //Все эксепшены можно убрать и добавить игнорирование данного вектора при загрузке
        string[] data = xmlValue.Split(';');
        if (data.Length != 3)
            return false; //не хватает компонент вектора
        Vector3Int temp = Vector3Int.zero;
        for (int i = 0; i < 3; i++)
        {
            int value = 0;
            if (!int.TryParse(data[i], out value))
                return false; //не смогли спарсить значение
            v[i] = value;
        }
        return true;
    }
    #endregion

    #region Vector 4
    public static string Vector4ToXml(Vector4 v)
    {
        return v.x + ";" + v.y + ";" + v.z + ";" + v.w;
    }

    public static bool StrToVector4(string xmlValue, out Vector4 v)
    {
        v = Vector4.zero;
        //Все эксепшены можно убрать и добавить игнорирование данного вектора при загрузке
        string[] data = xmlValue.Split(';');
        if (data.Length != 4)
            return false; //не хватает компонент вектора

        for (int i = 0; i < 4; i++)
        {
            float value = 0;
            if (!float.TryParse(data[i], out value))
                return false; //не смогли спарсить значение
            v[i] = value;
        }
        return true;
    }
    #endregion

    #region Quaternion
    public static string QuaternionToXml(Quaternion v)
    {
        return v.x + ";" + v.y + ";" + v.z + ";" + v.w;
    }

    public static bool StrToQuaternion(string xmlValue, out Quaternion obj)
    {
        obj = Quaternion.identity;
        //Все эксепшены можно убрать и добавить игнорирование данного вектора при загрузке
        string[] data = xmlValue.Split(';');
        if (data.Length != 4)
            return false; //не хватает компонент вектора
        Quaternion temp = Quaternion.identity;
        for (int i = 0; i < 4; i++)
        {
            float value = 0;
            if (!float.TryParse(data[i], out value))
                return false; //несмогли спарсить значение
            obj[i] = value;
        }
        return true;
    }
    #endregion

    private static string ListToXml(XElement parent, object list)
    {
        var _type = list.GetType();
        if (_type.IsGenericType && _type.GetGenericTypeDefinition()
                    == typeof(List<>))
        {
            Type itemType = list.GetType().GetGenericArguments()[0];
            parent.Add(new XAttribute("list_type", GetCorrectBaseTypeName(itemType)));
            //Можно было бы сократить, но ведь мы не в Net 4.0

            switch (itemType.Name)
            {
                case "GameObject":
                    var lst_iid = (List<GameObject>)list;
                    foreach (var item in lst_iid)
                    {
                        parent.Add(new XElement("i", new XAttribute("value", item == null ? "" : SaloGUIDManager.FindGUID(item).ToString())));
                    }
                    return lst_iid.Count.ToString();
                default:
                    if (itemType.IsEnum)
                    {
                        var lst_enum = (System.Collections.IList)list;
                        foreach (var item in lst_enum)
                        {

                            parent.Add(new XElement("i", new XAttribute("value", item == null ? "" : EnumToXml((Enum)item))));
                        }
                        return lst_enum.Count.ToString();
                    }

                    if (itemType.GetInterface("ISaloObject") != null)
                    {
                        var lst_mono = (System.Collections.IList)list;

                        foreach (var item in lst_mono)
                        {
                            try
                            {
                                parent.Add(new XElement("i", new XAttribute("value", item == null ? "" : ((ISaloObject)item).GUID.ToString())));
                            }

                            catch (Exception e)
                            {
                                var guid = ((ISaloObject)item).GUID;
                                if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Error) > 0)
                                    Debug.LogError(item + " with GUID " + (guid == null ? " NULL " : guid + " ") + e.ToString());
                            }
                        }
                        return lst_mono.Count.ToString();
                    }

                    var lst_generic = (System.Collections.IList)list;
                    foreach (var item in lst_generic)
                    {
                        if (item is Vector2)
                        {
                            parent.Add(new XElement("i", new XAttribute("value", Vector2ToXml((Vector2)item))));
                            continue;
                        }
                        if (item is Vector2Int)
                        {
                            parent.Add(new XElement("i", new XAttribute("value", Vector2ToXml((Vector2Int)item))));
                            continue;
                        }
                        if (item is Vector3)
                        {
                            parent.Add(new XElement("i", new XAttribute("value", Vector3ToXml((Vector3)item))));
                            continue;
                        }
                        if (item is Vector3Int)
                        {
                            parent.Add(new XElement("i", new XAttribute("value", Vector3ToXml((Vector3Int)item))));
                            continue;
                        }
                        if (item is Vector4)
                        {
                            parent.Add(new XElement("i", new XAttribute("value", Vector4ToXml((Vector4)item))));
                            continue;
                        }
                        if (item is Quaternion)
                        {
                            parent.Add(new XElement("i", new XAttribute("value", QuaternionToXml((Quaternion)item))));
                            continue;
                        }
                        parent.Add(new XElement("i", new XAttribute("value", EscapeHelper.Escape(item.ToString()))));
                    }
                    return lst_generic.Count.ToString();
            }
        }
        return "";
    }

    private static bool StrToListString(XElement node, string size, out List<string> list)
    {
        int capacity = 4;
        int.TryParse(size, out capacity);
        list = new List<string>(capacity);
        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;
            list.Add(EscapeHelper.Unescape(xValue.Value));
        }
        return true;
    }

    private static bool StrToListVector2(XElement node, string size, out List<Vector2> list)
    {
        int capacity = 4;
        int.TryParse(size, out capacity);
        list = new List<Vector2>(capacity);
        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;
            StrToVector2(xValue.Value, out Vector2 temp);
            list.Add(temp);
        }
        return true;
    }

    private static bool StrToListVector2i(XElement node, string size, out List<Vector2Int> list)
    {
        int capacity = 4;
        int.TryParse(size, out capacity);
        list = new List<Vector2Int>(capacity);
        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;
            StrToVector2(xValue.Value, out Vector2Int temp);
            list.Add(temp);
        }
        return true;
    }

    private static bool StrToListVector3(XElement node, string size, out List<Vector3> list)
    {
        int capacity = 4;
        int.TryParse(size, out capacity);
        list = new List<Vector3>(capacity);
        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;
            StrToVector3(xValue.Value, out Vector3 temp);
            list.Add(temp);
        }
        return true;
    }

    private static bool StrToListVector3i(XElement node, string size, out List<Vector3Int> list)
    {
        int capacity = 4;
        int.TryParse(size, out capacity);
        list = new List<Vector3Int>(capacity);
        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;
            StrToVector3(xValue.Value, out Vector3Int temp);
            list.Add(temp);
        }
        return true;
    }

    private static bool StrToListVector4(XElement node, string size, out List<Vector4> list)
    {
        int capacity = 4;
        int.TryParse(size, out capacity);
        list = new List<Vector4>(capacity);
        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;
            StrToVector4(xValue.Value, out Vector4 temp);
            list.Add(temp);
        }
        return true;
    }

    private static bool StrToListQuaternion(XElement node, string size, out List<Quaternion> list)
    {
        int capacity = 4;
        int.TryParse(size, out capacity);
        list = new List<Quaternion>(capacity);
        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;
            StrToQuaternion(xValue.Value, out Quaternion temp);
            list.Add(temp);
        }
        return true;
    }

    private static bool StrToListConvertable(XElement node, string size, Type _type, out object list)
    {

        int capacity = 4;
        int.TryParse(size, out capacity);
        var ilist = Activator.CreateInstance(_type);
        var listType = _type.GetGenericArguments()[0];
        var converter = System.ComponentModel.TypeDescriptor.GetConverter(listType);
        //var parse = _type.GetGenericArguments()[0].GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);

        var dynList = (System.Collections.IList)ilist;

        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;

            //dynList.Add(parse.Invoke(null, new object[] { xValue.Value }));
            dynList.Add(converter.ConvertFrom(xValue.Value));
        }
        list = ilist;
        return true;
    }

    public static bool StrToListGameObject(XElement node, string size, out List<GameObject> list)
    {
        int capacity = 4;
        int.TryParse(size, out capacity);
        list = new List<GameObject>(capacity);
        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;
            SerializableGUID val;
            if (SerializableGUID.TryParse(xValue.Value, out val))
                list.Add(SaloGUIDManager.Get<GameObject>(val));
        }
        return true;
    }

    public static bool StrToListEnum(XElement node, string size, Type _type, out object list)
    {
        int capacity = 4;
        int.TryParse(size, out capacity);
        var ilist = Activator.CreateInstance(_type);

        var dynList = (System.Collections.IList)ilist;
        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;
            int val = 0;
            if (int.TryParse(xValue.Value, out val))
                dynList.Add(Enum.Parse(_type.GetGenericArguments()[0], val.ToString()));
        }
        list = ilist;
        return true;
    }

    public static bool StrToListComponent(XElement node, Type _type, string size, out object obj)
    {

        bool isgeneric = _type.IsGenericType;
        var GetGenericTypeDefinition = _type.GetGenericTypeDefinition();
        var genType = _type.GetGenericArguments()[0];
        bool isSubclass = genType.IsSubclassOf(typeof(Component));
        if (!isgeneric || GetGenericTypeDefinition
                != typeof(List<>) || !isSubclass)
            throw new InvalidCastException();

        int capacity = 4;
        int.TryParse(size, out capacity);


        var list = (System.Collections.IList)Activator.CreateInstance(_type);
        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;
            SerializableGUID val;
            if (SerializableGUID.TryParse(xValue.Value, out val))
                list.Add(Convert.ChangeType(SaloGUIDManager.Get<Component>(val), _type.GetGenericArguments()[0]));
        }
        obj = list;
        return true;
    }


    public static bool StrToListSalo(XElement node, Type _type, string size, out object obj)
    {

        bool isgeneric = _type.IsGenericType;
        var GetGenericTypeDefinition = _type.GetGenericTypeDefinition();
        var genType = _type.GetGenericArguments()[0];
        bool isSubclass = IsSaloHeapObject(genType);
        if (!isgeneric || GetGenericTypeDefinition
                != typeof(List<>) || !isSubclass)
            throw new InvalidCastException();

        int capacity = 4;
        int.TryParse(size, out capacity);


        var list = (System.Collections.IList)Activator.CreateInstance(_type);
        foreach (var item in node.Elements())
        {
            var xValue = item.Attribute("value");
            if (xValue == null)
                continue;

            object list_item = null;
            var genArgType = _type.GetGenericArguments()[0];
            //Debug.Log("genArgType = " + genArgType);
            StrToSalo(xValue.Value, genArgType, out list_item);

            list.Add(list_item/*Convert.ChangeType(list_item, genArgType)*/);
        }
        obj = list;
        return true;
    }



    public static XElement ObjectToXml(XElement root, object obj, bool saveProperties, bool savePrivate)
    {
        //сохраняет имя и номер компонента на случай если на объекте несколько одинаковых компонентов
        var xcomp = root;
        if (saveProperties)
        {
            XElement xProps = root.Element("Properties");
            //xcomp.Add(xProps);
            PropertyInfo[] props = obj.GetType().GetProperties();
            foreach (var prop in props)
            {
                //игнорируем свойства типы, которых не поддерживаются парсером
                if (!IsValidType(prop.PropertyType))
                    continue;

                if (Attribute.GetCustomAttribute(prop, typeof(SaLoIgnorable)) != null)
                {
                    continue;
                }

                //пропускаем свойства, которым не можем задать или получить значения.
                if (prop.GetGetMethod() == null || prop.GetSetMethod() == null)
                    continue;

                XElement xProp = new XElement(prop.Name);
                object value = prop.GetValue(obj, null);



                xProp.Add(new XAttribute("type", GetCorrectBaseTypeName(prop.PropertyType)));

                if (value == null)
                {
                    xProp.Add(new XAttribute("value", ""));
                }
                else
                    xProp.Add(new XAttribute("value", ObjectToString(xProp, value)));
                xProps.Add(xProp);
            }
        }

        XElement xFields = root.Element("Fields");
        //xcomp.Add(xFields);
        FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | (savePrivate ? BindingFlags.NonPublic : BindingFlags.Default));
        foreach (var field in fields)
        {
            if (!IsValidType(field.FieldType))
                continue;

            if (Attribute.GetCustomAttribute(field, typeof(SaLoIgnorable)) != null)
            {
                //Debug.LogWarning("Save field: " + component.name + "." + field.Name + " is SaLoIgnorable - skipping");
                continue;
            }

            XElement xField = new XElement(field.Name);
            xField.Add(new XAttribute("type", GetCorrectBaseTypeName(field.FieldType)));

            object value = field.GetValue(obj);
            if (value == null)
            {
                xField.Add(new XAttribute("value", ""));
                //Debug.LogWarning("Save field: " + component.name + "." + field.Name + "." + field.FieldType.Name + " value is null - skipping");
                //continue;
            }
            else
                xField.Add(new XAttribute("value", ObjectToString(xField, value))); //перевод значения поля в рассматриваемом компоненте в строковое представление
            xFields.Add(xField);
        }
        return xcomp;
    }

    public static bool XmlToComponent(XElement xComponent, object component, bool loadProperties, bool loadPrivate)
    {
        //сохраняет имя и номер компонента на случай если на объекте несколько одинаковых компонентов
        if (xComponent == null || component == null)
            return false;

        /*if (xComponent.Name.LocalName != component.GetType().Name)
            return false;*/
        XElement xProps = xComponent.Element("Properties");
        if (loadProperties && xProps != null)
        {
            PropertyInfo[] props = component.GetType().GetProperties();
            //continue - игнор поврежденного свойства
            foreach (var prop in props)
            {
                //игнорируем свойства типов, которых не поддерживаются парсером
                if (!IsValidType(prop.PropertyType))
                    continue;
                //пропускаем свойства, которым не можем задать или получить значения.
                if (prop.GetGetMethod() == null || prop.GetSetMethod() == null)
                    continue;

                if (Attribute.GetCustomAttribute(prop, typeof(SaLoIgnorable)) != null)
                {
                    continue;
                }

                var element = xProps.Element(prop.Name);
                if (element == null)
                    continue;

                var xAttr = element.Attribute("type");
                if (xAttr == null)
                    continue;

                var xValue = element.Attribute("value");
                if (xValue == null)
                    continue;

                object xObjValue = null;
                bool parsed = StringTo(element, prop.PropertyType, xValue.Value, out xObjValue);

                if (!parsed)
                    continue;
                prop.SetValue(component, xObjValue, null);
            }
        }

        XElement xFields = xComponent.Element("Fields");
        FieldInfo[] fields = component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | (loadPrivate ? BindingFlags.NonPublic : BindingFlags.Default));
        if (xFields != null)
            foreach (var field in fields)
            {
                //игнорируем свойства типы, которых не поддерживаются парсером
                if (!IsValidType(field.FieldType))
                    continue;

                if (Attribute.GetCustomAttribute(field, typeof(SaLoIgnorable)) != null)
                {
                    continue;
                }

                var element = xFields.Element(field.Name);
                if (element == null)
                    continue;

                var xAttr = element.Attribute("type");
                if (xAttr == null)
                    continue;

                var xValue = element.Attribute("value");
                if (xValue == null)
                    continue;

                object xObjValue = null;
                bool parsed = StringTo(element, field.FieldType, xValue.Value, out xObjValue);

                if (!parsed)
                    continue;

                field.SetValue(component, xObjValue);
            }
        return true;
    }

    public static XElement ObjectToXml(string name, object value)
    {
        if (value == null)
        {
            return new XElement(name, new XAttribute("type", "null"), new XAttribute("value", 0));
        }
        XElement xElement = new XElement(name, new XAttribute("type", GetCorrectBaseTypeName(value.GetType())));
        string val = ObjectToString(xElement, value);
        xElement.Add(new XAttribute("value", val));
        return xElement;
    }

    private static string ObjectToString(XElement parent, object value)
    {
        var _type = value.GetType();
        string type = GetCorrectBaseTypeName(_type);
        if (!IsValidType(_type))
            throw new ArgumentException("Cant cast " + type + " to string value");

        //Обрабатываем отдельно только сложные структуры
        switch (type)
        {
            case "Vector2":
                return Vector2ToXml((Vector2)value);
            case "Vector2Int":
                return Vector2ToXml((Vector2Int)value);
            case "Vector3":
                return Vector3ToXml((Vector3)value);
            case "Vector3Int":
                return Vector3ToXml((Vector3Int)value);
            case "Vector4":
                return Vector4ToXml((Vector4)value);
            case "Quaternion":
                return Vector3ToXml(((Quaternion)value).eulerAngles);
            case "GameObject":
                return GameObjectToXml((GameObject)value);
            case "Component":
                return ComponentToXml((Component)value);
            case "Enum":
                return EnumToXml((Enum)value);
            case "List`1":
                return ListToXml(parent, value);
            default:
                if (IsSaloHeapObject(_type))
                {
                    return SaLoToXml((ISaloObject)value);
                }
                else
                    return value.ToString();
        }
    }

    private static bool IsSaloHeapObject(Type _type)
    {
        return _type.GetInterface("ISaloObject") != null && !_type.IsSubclassOf(typeof(UnityEngine.Object));
    }

    private static bool StringTo(XElement node, Type _type, string value, out object dest)
    {
        string type = GetCorrectBaseTypeName(_type);
        if (!IsValidType(_type))
            throw new ArgumentException("Can't cast string " + value + " to type " + type + ".");
        bool parsed = true;
        switch (type)
        {
            case "String":
                dest = value;
                break;
            case "Int32":
                int i = 0;
                parsed = int.TryParse(value, out i);
                dest = i;
                break;
            case "Byte":
                byte bt = 0;
                parsed = byte.TryParse(value, out bt);
                dest = bt;
                break;
            case "Single":
                float f = 0;
                parsed = float.TryParse(value, out f);
                dest = f;
                break;
            case "Boolean":
                bool bl = false;
                parsed = bool.TryParse(value, out bl);
                dest = bl;
                break;
            case "Vector2":
                parsed = StrToVector2(value, out Vector2 v2);
                dest = v2;
                break;
            case "Vector2Int":
                parsed = StrToVector2(value, out Vector2Int v2i);
                dest = v2i;
                break;
            case "Vector3":
                parsed = StrToVector3(value, out Vector3 v3);
                dest = v3;
                break;
            case "Vector3Int":
                parsed = StrToVector3(value, out Vector3Int v3i);
                dest = v3i;
                break;
            case "Vector4":
                parsed = StrToVector4(value, out Vector4 v4);
                dest = v4;
                break;
            case "Quaternion":
                Quaternion q;
                parsed = StrToQuaternion(value, out q);
                dest = q;
                break;
            case "GameObject":
                GameObject go;
                parsed = StrToGameObject(value, out go);
                dest = go;
                break;
            case "Component":
                Component mo;
                parsed = StrToComponent(value, out mo);
                if (parsed)
                    dest = Convert.ChangeType(mo, _type);
                else
                    dest = null;
                break;
            case "Enum":
                dest = Enum.Parse(_type, value);
                break;
            case "List`1":
                var xListGeneralType = node.Attribute("list_type");
                if (xListGeneralType != null)
                {
                    switch (xListGeneralType.Value)
                    {
                        case "String":
                            parsed = StrToListString(node, value, out List<string> list_str);
                            dest = list_str;
                            break;
                        case "Boolean":
                        case "Single":
                        case "Int64":
                        case "Int32":
                        case "Int16":
                        case "UInt64":
                        case "UInt32":
                        case "UInt16":
                        case "Byte":
                        case "SByte":
                            parsed = StrToListConvertable(node, value, _type, out dest);
                            break;
                        case "Vector2":
                            parsed = StrToListVector2(node, value, out List<Vector2> lst_v2);
                            dest = lst_v2;
                            break;
                        case "Vector2Int":
                            parsed = StrToListVector2i(node, value, out List<Vector2Int> lst_v2i);
                            dest = lst_v2i;
                            break;
                        case "Vector3":
                            parsed = StrToListVector3(node, value, out List<Vector3> lst_v3);
                            dest = lst_v3;
                            break;
                        case "Vector3Int":
                            parsed = StrToListVector3i(node, value, out List<Vector3Int> lst_v3i);
                            dest = lst_v3i;
                            break;
                        case "Vector4":
                            parsed = StrToListVector4(node, value, out List<Vector4> lst_v4);
                            dest = lst_v4;
                            break;
                        case "Quaternion":
                            parsed = StrToListQuaternion(node, value, out List<Quaternion> lst_qt);
                            dest = lst_qt;
                            break;
                        case "Enum":
                            parsed = StrToListEnum(node, value, _type, out dest);
                            break;
                        case "GameObject":
                            List<GameObject> lst_gos;
                            parsed = StrToListGameObject(node, value, out lst_gos);
                            dest = lst_gos;
                            break;
                        case "Component":
                            parsed = StrToListComponent(node, _type, value, out dest);
                            break;
                        default:
                            parsed = StrToListSalo(node, _type, value, out dest);
                            break;
                    }
                }
                else
                {
                    dest = null;
                    return false;
                }
                break;
            default:
                {
                    Debug.LogWarning("def = " + node.ToString() + " to type = " + _type.FullName);
                    if (IsSaloHeapObject(_type))
                    {
                        return StrToSalo(value, _type, out dest);
                    }
                    else
                    {
                        dest = null;
                        return false;
                    }
                }
        }
        return parsed;
    }

    private static List<T> CloneListAs<T>(IList<object> source)
    {
        // Here we can do anything we want with T
        // T == source[0].GetType()
        return source.Cast<T>().ToList();
    }

    public static bool XmlTo<T>(XElement xValue, ref T value)
    {
        object o;
        Type type = typeof(T);
        var result = StringTo(xValue, type, xValue.Attribute("value").Value, out o);
        value = (T)Convert.ChangeType(o, type);
        return result;
    }


}

