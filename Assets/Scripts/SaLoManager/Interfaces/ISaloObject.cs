using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


public interface ISaloObject
{
    #region SAVE_LOAD

    string ClassSaveName { get; }

    SerializableGUID GUID { get; }
    /// <summary>
    /// Called before Start / Awake
    /// </summary>
    /// <param name="xElement"></param>
    bool Load(SaloHelper reader);
    /// <summary>
    /// Function called after Load
    /// </summary>
    /// <param name="fromSave"></param>
    void Initialize(SaloHelper reader);
    /// <summary>
    /// Called if scene hasnt loaded from file. Used like Awake/Start in ISaloObject scripts
    /// </summary>
    void Initialize();
    void Save(SaloHelper writer);
    #endregion
}

