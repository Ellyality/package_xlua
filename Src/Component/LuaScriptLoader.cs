using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace XLua.LuaDLL
{

    [AddComponentMenu("Ellyality/Script/Lua Loader")]
    public class LuaScriptLoader : MonoBehaviour
    {
        [SerializeField] TextAsset luaScript;
        [SerializeField] string chunkName = "LuaTestScript";
        [SerializeField] Injection[] injections;

        public static LuaEnv luaEnv = new LuaEnv(); //all lua behaviour shared one luaenv only!
        public static float lastGCTime = 0;
        public const float GCInterval = 1;//1 second 

        Action luaStart;
        Action luaUpdate;
        Action luaOnDestroy;

        LuaTable scriptEnv;

        void Awake()
        {
            scriptEnv = luaEnv.NewTable();

            // 为每个脚本设置一个独立的环境，可一定程度上防止脚本间全局变量、函数冲突
            LuaTable meta = luaEnv.NewTable();
            meta.Set("__index", luaEnv.Global);
            scriptEnv.SetMetaTable(meta);
            meta.Dispose();

            scriptEnv.Set("self", this);
            foreach (var injection in injections)
            {
                scriptEnv.Set(injection.name, injection.value);
            }

            luaEnv.DoString(luaScript.text, chunkName, scriptEnv);

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
            if (Time.time - LuaScriptLoader.lastGCTime > GCInterval)
            {
                luaEnv.Tick();
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
