using System.IO;
using System.Reflection;
using Sirenix.Serialization;

namespace HGraph
{
    /// <summary>
    /// 通过Odin序列化
    /// </summary>
    public class OdinHGraphPersistence : IHGraphPersistence
    {
        private const DataFormat dataFormat = DataFormat.Binary;

        /// <summary>
        /// 判断成员是否应被序列化（基础规则，不含 SerializeInEditor 过滤）。
        /// 契约优先级：NotSerialize > ForceSerialize > public 字段。
        /// </summary>
        private static bool _shouldSerializeBase(MemberInfo member)
        {
            if (member.IsDefined(typeof(NotSerializeAttribute), true))
                return false;

            if (member.IsDefined(typeof(ForceSerializeAttribute), true))
                return true;

            if (member.IsDefined(typeof(SerializeInEditorAttribute), true))
                return true;

            if (member is FieldInfo field && field.IsPublic)
                return true;

            return false;
        }

        private static readonly ISerializationPolicy editorPolicy = new CustomSerializationPolicy(
            "HGraphEditor",
            true,
            _shouldSerializeBase
        );

        private static readonly ISerializationPolicy runtimePolicy = new CustomSerializationPolicy(
            "HGraphRuntime",
            true,
            member => !member.IsDefined(typeof(SerializeInEditorAttribute), true) && _shouldSerializeBase(member)
        );

        public HGraphData Load(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var context = new DeserializationContext { Config = { SerializationPolicy = editorPolicy } };
            var data = SerializationUtility.DeserializeValue<HGraphData>(bytes, dataFormat, context);
            return data;
        }

        public void Save(string path, HGraphData graphData, bool isEditor = false)
        {
            var policy = isEditor ? editorPolicy : runtimePolicy;
            var context = new SerializationContext { Config = { SerializationPolicy = policy } };
            var bytes = SerializationUtility.SerializeValue(graphData, dataFormat, context);
            File.WriteAllBytes(path, bytes);
        }
    }
}