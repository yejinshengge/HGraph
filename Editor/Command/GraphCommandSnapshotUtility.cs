using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.Serialization;

namespace HGraph.Editor
{
    /// <summary>
    /// 为命令系统提供节点快照、比较与状态回放能力。
    /// </summary>
    public static class GraphCommandSnapshotUtility
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>
        /// 基于 Odin 序列化创建对象深拷贝。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="source">源对象。</param>
        /// <returns>复制结果；源对象为空时返回 null。</returns>
        public static T CreateCopy<T>(T source) where T : class
        {
            return source == null ? null : (T)SerializationUtility.CreateCopy(source);
        }

        /// <summary>
        /// 通过序列化字节流判断两个对象是否等价。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="left">左侧对象。</param>
        /// <param name="right">右侧对象。</param>
        /// <returns>两者状态是否一致。</returns>
        public static bool AreEquivalent<T>(T left, T right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            var leftBytes = SerializationUtility.SerializeValue(left, DataFormat.Binary, (SerializationContext)null);
            var rightBytes = SerializationUtility.SerializeValue(right, DataFormat.Binary, (SerializationContext)null);
            return _byteArrayEquals(leftBytes, rightBytes);
        }

        /// <summary>
        /// 将 source 的字段状态回放到 target。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="target">目标对象。</param>
        /// <param name="source">源状态对象。</param>
        public static void ApplyState<T>(T target, T source) where T : class
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (target.GetType() != source.GetType())
            {
                throw new InvalidOperationException($"无法将 {source.GetType().Name} 的状态应用到 {target.GetType().Name}。");
            }

            foreach (var field in _enumerateFields(target.GetType()))
            {
                if (field.IsInitOnly || field.IsLiteral)
                {
                    continue;
                }

                var sourceValue = field.GetValue(source);
                field.SetValue(target, _cloneValue(sourceValue));
            }
        }

        /// <summary>
        /// 枚举类型及其继承链上的实例字段。
        /// </summary>
        /// <param name="type">目标类型。</param>
        /// <returns>字段枚举序列。</returns>
        private static IEnumerable<FieldInfo> _enumerateFields(Type type)
        {
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (var field in current.GetFields(FieldFlags))
                {
                    yield return field;
                }
            }
        }

        /// <summary>
        /// 复制单个字段值，优先处理常见集合与 Unity 引用类型。
        /// </summary>
        /// <param name="value">原始值。</param>
        /// <returns>复制后的值。</returns>
        private static object _cloneValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            var valueType = value.GetType();
            if (valueType.IsValueType || value is string)
            {
                return value;
            }

            if (value is UnityEngine.Object)
            {
                return value;
            }

            if (value is IList list)
            {
                return _cloneList(list, valueType);
            }

            if (value is IDictionary dictionary)
            {
                return _cloneDictionary(dictionary, valueType);
            }

            return SerializationUtility.CreateCopy(value);
        }

        /// <summary>
        /// 深拷贝列表对象。
        /// </summary>
        /// <param name="source">源列表。</param>
        /// <param name="listType">列表运行时类型。</param>
        /// <returns>复制后的列表。</returns>
        private static object _cloneList(IList source, Type listType)
        {
            var clone = (IList)Activator.CreateInstance(listType);
            foreach (var item in source)
            {
                clone.Add(_cloneValue(item));
            }

            return clone;
        }

        /// <summary>
        /// 深拷贝字典对象。
        /// </summary>
        /// <param name="source">源字典。</param>
        /// <param name="dictionaryType">字典运行时类型。</param>
        /// <returns>复制后的字典。</returns>
        private static object _cloneDictionary(IDictionary source, Type dictionaryType)
        {
            var clone = (IDictionary)Activator.CreateInstance(dictionaryType);
            foreach (DictionaryEntry entry in source)
            {
                clone.Add(_cloneValue(entry.Key), _cloneValue(entry.Value));
            }

            return clone;
        }

        /// <summary>
        /// 比较两个字节数组是否完全一致。
        /// </summary>
        /// <param name="left">左侧数组。</param>
        /// <param name="right">右侧数组。</param>
        /// <returns>是否逐字节相等。</returns>
        private static bool _byteArrayEquals(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
