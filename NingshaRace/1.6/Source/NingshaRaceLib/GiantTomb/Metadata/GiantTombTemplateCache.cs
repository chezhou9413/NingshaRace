using System;
using System.Collections.Generic;
using ChezhouLib.CustomMission.MapTemplates;
using NingshaRaceLib.GiantTomb.Layout;

namespace NingshaRaceLib.GiantTomb.Metadata
{
    //类职责：在当前游戏进程内缓存已经严格校验的墓葬模板、连接点和结构掩码。
    internal static class GiantTombTemplateCache
    {
        private static readonly Dictionary<ClMapTemplateDef, GiantTombModule> modules = new Dictionary<ClMapTemplateDef, GiantTombModule>();

        //函数职责：尝试取得已经完成二进制、metadata和结构掩码校验的模板模块。
        public static bool TryGet(ClMapTemplateDef def, out GiantTombModule module)
        {
            if (def == null)
            {
                throw new ArgumentNullException(nameof(def));
            }
            return modules.TryGetValue(def, out module);
        }

        //函数职责：登记一个完成全部严格校验且可供后续墓葬生成复用的模板模块。
        public static void Add(ClMapTemplateDef def, GiantTombModule module)
        {
            if (def == null)
            {
                throw new ArgumentNullException(nameof(def));
            }
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }
            modules.Add(def, module);
        }

        //函数职责：清除墓葬模板缓存，供开发期热重载资源后主动刷新。
        public static void Clear()
        {
            modules.Clear();
        }
    }
}
