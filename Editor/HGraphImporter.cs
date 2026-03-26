using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace HGraph.Editor
{
    /// <summary>
    /// 将 .hgraph 文件注册为 Unity 可识别的资源类型。
    /// 导入时仅提取图类型全名等轻量元数据，完整反序列化在 Inspector 绘制时按需执行。
    /// </summary>
    [ScriptedImporter(1, "hgraph")]
    public class HGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = ScriptableObject.CreateInstance<HGraphAsset>();

            try
            {
                var fileInfo = new FileInfo(ctx.assetPath);
                if (fileInfo.Length == 0)
                {
                    asset.ImportSuccess = false;
                    asset.ErrorMessage = "文件内容为空";
                    ctx.LogImportWarning($"[HGraphImporter] 空文件: {ctx.assetPath}");
                }
                else
                {
                    var data = HGraphPersistenceRegistry.Current.Load(ctx.assetPath);
                    asset.GraphTypeName = data.GetType().FullName;
                    asset.ImportSuccess = true;
                }
            }
            catch (Exception e)
            {
                asset.ImportSuccess = false;
                asset.ErrorMessage = e.Message;
                ctx.LogImportWarning($"[HGraphImporter] 反序列化失败: {ctx.assetPath}\n{e.Message}");
            }

            asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            ctx.AddObjectToAsset("main", asset);
            ctx.SetMainObject(asset);
        }
    }
}
