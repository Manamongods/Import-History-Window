#define FOLDER_BUTTON
#define IGNORE_UNITY_MODIFICATIONS

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

public class ImportHistoryPP : AssetPostprocessor
{
    public static readonly HashSet<string> ignores = new HashSet<string>();

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        ImportHistoryWindow.TryRead();


        //Ignores massive operations
        const int MAX_COUNT = 300;
        int count = deletedAssets.Length + movedAssets.Length + importedAssets.Length;
        if (count > MAX_COUNT)
        {
            ignores.Clear();
            return;
        }


        for (int ii = 0; ii < deletedAssets.Length; ii++)
            ImportHistoryWindow.Remove(deletedAssets[ii]);
        for (int ii = 0; ii < movedFromAssetPaths.Length; ii++)
            ImportHistoryWindow.Remove(movedFromAssetPaths[ii]);

        void Add(string path)
        {
            if (!ignores.Remove(path))
                ImportHistoryWindow.Add(path);
        }

        for (int ii = 0; ii < movedAssets.Length; ii++)
        {
            var moved = movedAssets[ii];

            bool imported = false; //Renaming an asset in Unity will call both movedAssets and importedAssets, but that creates a problem for the ignores
            for (int iii = 0; iii < importedAssets.Length; iii++)
            {
                if (importedAssets[iii] == moved)
                {
                    imported = true;
                    break;
                }
            }

            if (!imported)
                Add(moved);
        }
        for (int ii = 0; ii < importedAssets.Length; ii++)
        {
            Add(importedAssets[ii]);
        }

        ImportHistoryWindow.TryWrite();
    }
}

#if IGNORE_UNITY_MODIFICATIONS
public class ImportHistoryMP : UnityEditor.AssetModificationProcessor
{
    private static readonly HashSet<string> ignores = new HashSet<string>();

    private static void OnWillCreateAsset(string path)
    {
        ignores.Add(path);
    }

    private static string[] OnWillSaveAssets(string[] paths)
    {
        //Ignores assets which will be modified by Unity itself
        for (int i = 0; i < paths.Length; i++)
        {
            var path = paths[i];

            if (!ignores.Remove(path))
            {
                if (path.EndsWith(".meta"))
                    path = path.Substring(0, path.Length - 5);

                ImportHistoryPP.ignores.Add(path);
            }
        }

        return paths;
    }

    private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
    {
        ImportHistoryPP.ignores.Add(destinationPath);

        return AssetMoveResult.DidNotMove;
    }
}
#endif

public class ImportHistoryWindow : EditorWindow, IHasCustomMenu
{
    //Constants
    private static readonly string[] IGNORED_EXTENSIONS = new string[]
    {
        "", //Most often a folder
        //".afdesign",
        ".pxf",
        //".xcf",

        ".prefab",
        ".asset",
        ".mat",
    };

    private const int HISTORY_LENGTH = 32;
    private const float DOUBLE_CLICK_TIME = 0.5f;
    private const float HEIGHT = 20;
    private const int MARGIN = 0;



    //Fields
    public static readonly List<string> history = new List<string>();
    private static bool dirty;

    public Vector2 scrollPosition;
    private GUIStyle pingStyle, folderStyle;
    private Texture folderIcon;
    private UnityEngine.Object previouslyClicked;
    private double clickTime;



    //Methods
    [MenuItem("Window/Import History")]
    static void ShowWindow()
    {
        ImportHistoryWindow window = CreateInstance<ImportHistoryWindow>();
        window.titleContent = new GUIContent("Import History");
        window.Show();
    }

    public void AddItemsToMenu(GenericMenu menu)
    {
        menu.AddItem(EditorGUIUtility.TrTextContent("Clear"), false, Clear);
    }
    private void Clear()
    {
        history.Clear();
        dirty = true;
        TryWrite();
    }

    public static void Add(string path)
    {
        path = path.Replace("\\", "/");

        //Ignores by extension
        var lower = path.ToLowerInvariant();
        for (int i = 0; i < IGNORED_EXTENSIONS.Length; i++)
        {
            var ext = IGNORED_EXTENSIONS[i];

            if (Path.GetExtension(lower) == ext)
                return;
        }

        //Removes Duplicates
        for (int i = history.Count - 1; i >= 0; i--)
        {
            if (history[i] == path)
                history.RemoveAt(i);
        }

        //Adds
        history.Insert(0, path);

        //Removes extra
        while (history.Count > HISTORY_LENGTH)
            history.RemoveAt(HISTORY_LENGTH);

        dirty = true;
    }
    public static void Remove(string path)
    {
        path = path.Replace("\\", "/");

        history.Remove(path);
        dirty = true;
    }

    public static void TryWrite()
    {
        if(dirty)
        {
            dirty = false;

            StringBuilder sb = new StringBuilder(1000);

            for (int i = 0; i < history.Count; i++)
            {
                sb.Append(history[i]);
                if (i != history.Count - 1)
                    sb.Append("\\");
            }

            EditorPrefs.SetString("Import History", sb.ToString());
        }
    }
    public static void TryRead()
    {
        if (history.Count == 0)
            Read();
    }
    private static void Read()
    {
        history.Clear();

        var s = EditorPrefs.GetString("Import History");

        var paths = s.Split('\\');
        for (int i = 0; i < paths.Length; i++)
        {
            var p = paths[i];
            if(!string.IsNullOrEmpty(p))
                history.Add(p);
        }
    }



    //Lifecycle
    private void OnEnable()
    {
        minSize = new Vector2(200, 50);

        folderIcon = Resources.Load<Texture>("IHW Folder Icon");

        TryRead();
    }

    public void OnGUI()
    {
        //Creates Styles
        if (pingStyle == null)
        {
            folderStyle = new GUIStyle(GUI.skin.button);

            folderStyle.margin.bottom = MARGIN;
            folderStyle.margin.top = MARGIN;
            folderStyle.margin.left = MARGIN;
            folderStyle.margin.right = MARGIN;

            pingStyle = new GUIStyle(folderStyle);
            pingStyle.alignment = TextAnchor.MiddleLeft; // MiddleRight;
        }


        //Widths and Height
        float windowWidth = position.width - 13;

#if FOLDER_BUTTON
        const float FOLDER_WIDTH = 50;
        var pingWidth = GUILayout.Width(windowWidth - FOLDER_WIDTH - MARGIN * 2);
        var folderWidth = GUILayout.Width(FOLDER_WIDTH - MARGIN * 2);
#else
        var pingWidth = GUILayout.Width(windowWidth);
#endif
        var height = GUILayout.Height(HEIGHT);


        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);
        {
            for (int i = 0; i < history.Count; i++)
            {
                var path = history[i];
                
                GUILayout.BeginHorizontal(GUILayout.Width(windowWidth));
                {
                    //Ping Button
                    string name = Path.GetFileName(Path.GetDirectoryName(path)) + "/" + Path.GetFileName(path);

                    if (GUILayout.Button(name, pingStyle, pingWidth, height))
                    {
                        //Removes if can't be loaded
                        var asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
                        if (asset == null)
                        {
                            history.RemoveAt(i);
                            i--;
                            dirty = true;
                            continue;
                        }

                        //Pings and selects
                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;

                        var clickDelay = EditorApplication.timeSinceStartup - clickTime;
                        if (previouslyClicked == asset && clickDelay < DOUBLE_CLICK_TIME)
                        {
                            //Double clicked, will open
                            AssetDatabase.OpenAsset(asset);
                            previouslyClicked = null;
                        }
                        else
                        {
                            //Prepares double click
                            previouslyClicked = asset;
                            clickTime = EditorApplication.timeSinceStartup;
                        }
                    }


#if FOLDER_BUTTON
                    //Folder Button
                    if (GUILayout.Button(folderIcon, folderStyle, folderWidth, height))
                    {
                        System.Diagnostics.Process.Start(Path.GetDirectoryName(path));
                    }
#endif
                }
                GUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();


        TryWrite();
    }
}