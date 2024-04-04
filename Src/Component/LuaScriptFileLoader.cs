using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using XLua;

namespace Ellyality.xlua
{
    [AddComponentMenu("Ellyality/Script/Lua File Loader")]
    public class LuaScriptFileLoader : MonoBehaviour
    {
        [System.Serializable]
        enum FileType
        {
            URL,
            Absolute,
            DataPath,
            StreamingAssetsPath,
            PersistentDataPath,
            TemporaryCachePath
        }

        [SerializeField] FileType fileType = FileType.Absolute;
        [SerializeField] string filepath;
        [SerializeField] string chunkName = "LuaTestScript";
        [SerializeField] Injection[] injections;

        Action luaStart;
        Action luaUpdate;
        Action luaOnDestroy;

        LuaTable scriptEnv;

        void Awake()
        {
            scriptEnv = LuaScriptLoader.luaEnv.NewTable();

            // 为每个脚本设置一个独立的环境，可一定程度上防止脚本间全局变量、函数冲突
            LuaTable meta = LuaScriptLoader.luaEnv.NewTable();
            meta.Set("__index", LuaScriptLoader.luaEnv.Global);
            scriptEnv.SetMetaTable(meta);
            meta.Dispose();

            scriptEnv.Set("self", this);
            foreach (var injection in injections)
            {
                scriptEnv.Set(injection.name, injection.value);
            }

            switch (fileType)
            {
                case FileType.URL:
                    StartCoroutine(NetStringGetter());
                    break;
                case FileType.Absolute:
                    Fetch(File.ReadAllText(filepath));
                    break;
                case FileType.DataPath:
                    Fetch(File.ReadAllText(Path.Combine(Application.dataPath, filepath)));
                    break;
                case FileType.StreamingAssetsPath:
                    Fetch(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, filepath)));
                    break;
                case FileType.PersistentDataPath:
                    Fetch(File.ReadAllText(Path.Combine(Application.persistentDataPath, filepath)));
                    break;
                case FileType.TemporaryCachePath:
                    Fetch(File.ReadAllText(Path.Combine(Application.temporaryCachePath, filepath)));
                    break;
            }
        }

        IEnumerator NetStringGetter()
        {
            UnityWebRequest www = UnityWebRequest.Get(filepath);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Fetch(www.downloadHandler.text);
            }
        }

        void Fetch(string script)
        {
            LuaScriptLoader.luaEnv.DoString(script, chunkName, scriptEnv);
            Action luaAwake = scriptEnv.Get<Action>("awake");
            scriptEnv.Get("start", out luaStart);
            scriptEnv.Get("update", out luaUpdate);
            scriptEnv.Get("ondestroy", out luaOnDestroy);

            if (luaAwake != null)
            {
                luaAwake();
            }
        }

        // Use this for initialization
        void Start()
        {
            if (luaStart != null)
            {
                luaStart();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (luaUpdate != null)
            {
                luaUpdate();
            }
            if (Time.time - LuaScriptLoader.lastGCTime > LuaScriptLoader.GCInterval)
            {
                LuaScriptLoader.luaEnv.Tick();
                LuaScriptLoader.lastGCTime = Time.time;
            }
        }

        void OnDestroy()
        {
            if (luaOnDestroy != null)
            {
                luaOnDestroy();
            }
            luaOnDestroy = null;
            luaUpdate = null;
            luaStart = null;
            scriptEnv.Dispose();
            injections = null;
        }
    }
}
