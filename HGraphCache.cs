using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HGraph
{
    public static class HGraphCache
    {
        // 类型上携带的特性
        private static Dictionary<Type, Dictionary<Type, List<object>>> _attributes = new();
        // 携带指定特性的类型
        private static Dictionary<Type, List<Type>> _attributeClassMap = new();

        /// <summary>
        /// 构建缓存
        /// </summary>
        
        public static void BuildCache()
        {
            _attributes.Clear();
            _attributeClassMap.Clear();
            
            List<Type> nodeTypes = new();
            Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            // 遍历所有程序集
            foreach (Assembly assembly in assemblies) {
                // 跳过某些dll以提高性能
                string assemblyName = assembly.GetName().Name;
                int index = assemblyName.IndexOf('.');
                if (index != -1) assemblyName = assemblyName.Substring(0, index);
                switch (assemblyName) {
                    case "UnityEditor":
                    case "UnityEngine":
                    case "System":
                    case "mscorlib":
                    case "Microsoft":
                        continue;
                    default:
                        nodeTypes.AddRange(
                            assembly.GetTypes()
                                .Where(t => !t.IsAbstract)
                                .Where(t => t.GetCustomAttributes(typeof(HGraphBaseAttribute), true).Any())
                        );
                        break;
                }
            }
            _add(nodeTypes);
        }
        /// <summary>
        /// 记录所有类型及其特性
        /// </summary>
        /// <param name="addTypes"></param>
        private static void _add(List<Type> addTypes)
        {

            foreach (var type in addTypes)
            {
                var attributes = type.GetCustomAttributes(typeof(HGraphBaseAttribute), true);
                foreach (var attribute in attributes)
                {
                    var attrType = attribute.GetType();
                    // 如果不存在该组合，创建新的列表
                    if (!_attributes.ContainsKey(type))
                        _attributes[type] = new Dictionary<Type, List<object>>();
                    if (!_attributes[type].ContainsKey(attrType))
                        _attributes[type][attrType] = new List<object>();
                    _addAttributeClassMap(attrType, type);
                    // 将特性添加到列表中
                    _attributes[type][attrType].Add(attribute);
                    Debug.Log($"添加HGraph特性记录：{type} - {attrType}");
                }
            }
        }

        /// <summary>
        /// 获取指定类型的特性实例（返回第一个）
        /// </summary>
        /// <param name="key">类型</param>
        /// <typeparam name="T2">特性</typeparam>
        /// <returns></returns>
        public static T2 GetAttribute<T2>(Type key) where T2 : HGraphBaseAttribute
        {
            var key2 = typeof(T2);
            if(!_attributes.ContainsKey(key))
                return null;

            if (!_attributes[key].ContainsKey(key2))
                return null;
            
            var attributeList = _attributes[key][key2];
            return attributeList.Count > 0 ? attributeList[0] as T2 : null;
        }

        /// <summary>
        /// 获取指定类型的所有特性实例（支持多重特性）
        /// </summary>
        /// <param name="key">类型</param>
        /// <typeparam name="T2">特性</typeparam>
        /// <returns></returns>
        public static T2[] GetAttributes<T2>(Type key) where T2 : HGraphBaseAttribute
        {
            var key2 = typeof(T2);
            if (!_attributes.ContainsKey(key))
                return new T2[0];

            if (!_attributes[key].ContainsKey(key2))
                return new T2[0];

            var attributeList = _attributes[key][key2];
            var result = new T2[attributeList.Count];
            for (int i = 0; i < attributeList.Count; i++)
            {
                result[i] = attributeList[i] as T2;
            }
            return result;
        }

        /// <summary>
        /// 根据特性获取类型组
        /// </summary>
        /// <typeparam name="T">特性</typeparam>
        /// <returns></returns>
        public static List<Type> GetTypeListByAttribute<T>() where T : HGraphBaseAttribute
        {
            if (!_attributeClassMap.ContainsKey(typeof(T)))
                return new List<Type>();
            return _attributeClassMap[typeof(T)];
        }

        /// <summary>
        /// 检查类型是否具有某一特性
        /// </summary>
        /// <typeparam name="T">特性</typeparam>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static bool CheckTypeHasAttribute<T>(Type type) where T:HGraphBaseAttribute
        {
            return GetAttribute<T>(type) != null;
        }

        /// <summary>
        /// 获取节点类型映射
        /// </summary>
        /// <returns></returns>
        public static Dictionary<Type,HNodeAttribute> GetNodeTypeMap(Type graphType)
        {
            Dictionary<Type,HNodeAttribute> nodeTypeMap = new ();
            GetTypeListByAttribute<HNodeAttribute>().ForEach(type => {
                var nodeAttr = GetAttribute<HNodeAttribute>(type);
                if(nodeAttr.NodeOf == graphType)
                {
                    nodeTypeMap[type] = nodeAttr;
                }
            });
            return nodeTypeMap;
        }

        private static void _addAttributeClassMap(Type attType, Type classType)
        {
            if (!_attributeClassMap.ContainsKey(attType))
                _attributeClassMap[attType] = new List<Type>();
            // 避免重复添加同一个类型
            if (!_attributeClassMap[attType].Contains(classType))
                _attributeClassMap[attType].Add(classType);
        }
    }
}