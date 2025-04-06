using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaloGUIDManagerWindow : EditorWindow
{
    private GUIStyle listStyle;

    // The variable to control where the scrollview 'looks' into its child elements.
    Vector2 scrollPosition;
    static SaloGUIDManagerWindow instance;
    static bool inited;

    private static GuidDictionary sceneGUIDs;

    int selectedIndex = -1;

    [SerializeField] private bool autoAssignGUID;

    GUIStyle style;
    GUISkin skin;

    // [SerializeField] TreeViewState m_TreeViewState;

    //  SaloGUIDTreeView m_SimpleTreeView;

    [InitializeOnLoadMethod]
    static void Init()
    {
        if (inited)
            return;
        EditorApplication.projectChanged += EditorApplication_projectChanged;
        EditorApplication.hierarchyChanged += EditorApplication_hierarchyChanged;
        EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
        EditorSceneManager.sceneLoaded += EditorSceneManager_sceneLoaded;
        inited = true;
    }


    private Texture2D GetColor2D(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.hideFlags = HideFlags.HideAndDontSave;
        t.SetPixels32(new Color32[] { c });
        t.Apply();
        return t;
    }

    void OnEnable()
    {
        skin = Resources.Load<GUISkin>("SaloStyles/SaloSkin");
        style = new GUIStyle(skin.FindStyle("listView"));
        style.normal.background = Texture2D.whiteTexture;
        style.active.background = Texture2D.whiteTexture;

        listStyle = new GUIStyle(skin.FindStyle("listViewItem"));
        //listStyle.normal.background = GetColor2D(Color.red);
        var hightlightT = GetColor2D(new Color(171 / 255f, 176 / 255f, 201 / 255f, 1.0f));
        listStyle.onNormal.background = hightlightT;
        listStyle.onFocused.background = hightlightT;
        //listStyle.active.background = GetColor2D(Color.blue);
        // Check if we already had a serialized view state (state 
        // that survived assembly reloading)
        /*if (m_TreeViewState == null)
            m_TreeViewState = new TreeViewState();

       m_SimpleTreeView = new SaloGUIDTreeView(m_TreeViewState);
        m_SimpleTreeView.ExpandAll();*/
    }


    private static void EditorSceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (instance != null)
            instance.RefreshGUIDs(false);
    }

    private static void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
    {
        if (obj == PlayModeStateChange.EnteredEditMode)
        {
            if (instance)
                instance.RefreshGUIDs(false);
        }
    }

    private static void EditorApplication_hierarchyChanged()
    {
        if ((SaveLoadManager.InfoLevel & SaLoInfoLevel.Info) > 0)
            Debug.Log("EditorApplication_hierarchyChanged");
        if (instance && instance.autoAssignGUID)
            instance.RefreshGUIDs(true);
    }

    private static void EditorApplication_projectChanged()
    {

        if (instance != null && instance.autoAssignGUID)
        {

            instance.RefreshGUIDs(true);
        }
    }

    [MenuItem("Window/GUID Manager")]
    public static void ShowWindow()
    {
        instance = GetWindow<SaloGUIDManagerWindow>("Salo GUID Manager");

        instance.Show(true);
        if (!Application.isPlaying)
            instance.RefreshGUIDs(false);
    }

    private void RefreshGUIDs(bool addGUIDS)
    {
        if (sceneGUIDs == null)
        {
            sceneGUIDs = new GuidDictionary();
        }
        else
            sceneGUIDs.Clear();

        var scene = EditorSceneManager.GetActiveScene();

        GameObject[] rootObjs = scene.GetRootGameObjects();
        bool isDirty = false;
        foreach (GameObject obj in rootObjs)
        {
            /*int instanceID = obj.GetInstanceID();
            values.Add(instanceID, obj);*/

            foreach (Transform item in obj.GetComponentsInChildren<Transform>(true))
            {
                var guid_comp = item.gameObject.GetComponent<SaloGUID>();
                if (guid_comp == null)
                {
                    if (addGUIDS)
                    {
                        var salo = item.gameObject.AddComponent<SaloGUID>();
                        foreach (var guid in salo.Generate())
                        {
                            sceneGUIDs.Add(guid.Key, guid.Value);
                            //UnityEditor.Undo.RegisterFullObjectHierarchyUndo((UnityEngine.Object)guid.Value, "go");
                        }
                        isDirty = true;
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
                                sceneGUIDs.Add(guid.Key, guid.Value);
                                //UnityEditor.Undo.RegisterFullObjectHierarchyUndo((UnityEngine.Object)guid.Value, "go");
                            }
                            isDirty = true;
                        }
                    }
                    else
                    {
                        if (autoAssignGUID || addGUIDS)
                            if (guid_comp.CheckAndUpdateComponentsGUID())
                                MarkDirty();
                        foreach (var guid in guid_comp.ComponentsGUID)
                        {
                            if (!sceneGUIDs.ContainsKey(guid.Key))
                            {
                                sceneGUIDs.Add(guid.Key, guid.Value);
                                //UnityEditor.Undo.RegisterFullObjectHierarchyUndo((UnityEngine.Object)guid.Value, "go");
                            }
                        }
                    }
                }
            }
        }
        if (isDirty)
            MarkDirty();
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }

    private void InitStyles()
    {

        // if (style == null)
        {
            style = new GUIStyle(GUIStyle.none);
            style.name = "listViewStyle";
            style.normal.background = Texture2D.whiteTexture;

            style.active.background = Texture2D.whiteTexture;
        }

        // if (listStyle == null || listStyle.font.name != "Consolas")
        {

            listStyle = new GUIStyle(GUI.skin.GetStyle("ProjectBrowserGridLabel"));
            listStyle.name = "listItemStyle";
            listStyle.normal.background = Texture2D.whiteTexture;
            try
            {
                listStyle.font = Font.CreateDynamicFontFromOSFont("Consolas", 14);// Font.CreateDynamicFontFromOSFont("Consolas", GUIStyle.none.fontSize);
                                                                                  //Debug.Log(GUIStyle.none.fontSize);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                //listStyle.font = GUIStyle.none.font;
            }
            listStyle.fontSize = 14;
            listStyle.fontStyle = GUIStyle.none.fontStyle;
            listStyle.alignment = TextAnchor.MiddleLeft;
            listStyle.padding = new RectOffset(20, 20, 5, 5);
        }
    }

    //float maxHeight = 0;

    private void OnGUI()
    {
        //InitStyles();
        bool isPlaying = Application.isPlaying;
        if (!isPlaying && sceneGUIDs == null)
            RefreshGUIDs(false);

        var dictionary = isPlaying ? SaloGUIDManager.GetDictionary() : sceneGUIDs;



        GUILayout.Label("All GUID Objects (" + dictionary?.Count + "):");

        //if (style == null || listStyle == null)
        /*var newHeight = m_SimpleTreeView.totalHeight + 4;
        if (newHeight > maxHeight)
            maxHeight = newHeight;

        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.MinHeight(m_SimpleTreeView.totalHeight + 4), GUILayout.ExpandHeight(true));
        
        m_SimpleTreeView.OnGUI(new Rect(6, 22, position.width - 12, m_SimpleTreeView.totalHeight));*/
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, style, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        try
        {
            if (dictionary != null)
            {
                for (int i = 0; i < dictionary.Count; i++)
                {
                    var item = dictionary.ElementAt(i);
                    // This button was doubleclicked within the time specified in the user's system prefs for  double clicking
                    if (GUILayout.Toggle(selectedIndex == i, item.Key + " | " + (item.Value == null ? "null" : item.Value.GetType().Name), listStyle))
                    {
                        if (Event.current.clickCount == 2)
                        {
                            if (item.Value is Object)
                            {
                                EditorGUIUtility.PingObject((Object)item.Value);
                                Selection.activeObject = (Object)item.Value;
                            }
                        }

                        selectedIndex = i;
                    }
                }
            }
        }
        catch { }
        GUILayout.EndScrollView();
        GUILayout.BeginVertical();


        GUI.enabled = !isPlaying;
        autoAssignGUID = GUILayout.Toggle(autoAssignGUID, "Auto assign (refresh) GUIDs");

        if (GUILayout.Button("Add GUIDs", GUILayout.ExpandWidth(true)))
        {
            RefreshGUIDs(true);
            selectedIndex = -1;
        }

        if (GUILayout.Button("Clear GUIDs", GUILayout.ExpandWidth(true)))
        {
            ClearGUIDs();
            selectedIndex = -1;
        }

        if (GUILayout.Button("Update Prefab Path", GUILayout.ExpandWidth(true)))
        {
            UpdatePrefabPath();
        }

        GUI.enabled = true;
        if (GUILayout.Button("Switch GO visibility", GUILayout.ExpandWidth(true)))
        {
            var guidManager = SaloGUIDManager.FindGUIDManager();
            if (guidManager.gameObject.hideFlags == (HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable))
                guidManager.gameObject.hideFlags = HideFlags.NotEditable;
            else
                guidManager.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
            EditorApplication.DirtyHierarchyWindowSorting();
            EditorApplication.RepaintHierarchyWindow();

            Debug.Log(guidManager.hideFlags);
        }

        GUILayout.EndVertical();

    }

    private void UpdatePrefabPath()
    {
        string[] prefabs = Directory.GetDirectories("Assets/", "_Prefabs", SearchOption.AllDirectories);

        foreach (var item in prefabs)
        {
            string[] files = Directory.GetFiles(item, "*.prefab");
            foreach (var pref in files)
            {
                var prefab = PrefabUtility.LoadPrefabContents(pref);
                var salo = prefab.GetComponent<SaloObject>();
                if(salo == null)
                {
                    salo = prefab.AddComponent<SaloObject>();
                    salo.Dynamic = true;
                }
                salo.PrefabPath = Path.Combine("_Prefabs", Path.GetFileNameWithoutExtension(pref));
                PrefabUtility.SaveAsPrefabAsset(prefab, pref);
                PrefabUtility.UnloadPrefabContents(prefab);
            }

        }
    }

    public void ClearGUIDs()
    {

        GameObject[] rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjs)
        {
            /*int instanceID = obj.GetInstanceID();
            values.Add(instanceID, obj);*/

            foreach (SaloGUID item in obj.GetComponentsInChildren<SaloGUID>(true))
            {
                DestroyImmediate(item);
            }
        }
        if (sceneGUIDs == null)
            sceneGUIDs = new GuidDictionary();
        sceneGUIDs.Clear();
        MarkDirty();
    }
}

