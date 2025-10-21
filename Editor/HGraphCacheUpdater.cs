using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// HGraph缓存自动更新器
    /// 在每次编译完成后自动执行BuildCache
    /// </summary>
    public static class HGraphCacheUpdater
    {
        /// <summary>
        /// 编辑器启动时初始化
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            try
            {
                HGraphCache.BuildCache();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HGraph] 自动更新缓存时发生错误: {ex.Message}");
            }
        }

        
        /// <summary>
        /// 手动构建缓存的菜单项
        /// </summary>
        [MenuItem("工具/HGraph/构建缓存")]
        public static void BuildCacheManually()
        {
            try
            {
                HGraphCache.BuildCache();
                Debug.Log("[HGraph] 手动执行缓存构建完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HGraph] 手动构建缓存时发生错误: {ex.Message}");
            }
        }
    }
}
