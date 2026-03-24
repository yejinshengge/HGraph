using System;
using System.Collections.Generic;
using System.Linq;

namespace HGraph
{
    /// <summary>
    /// 序列化器注册中心。
    /// 自动发现所有 <see cref="IHGraphPersistence"/> 实现并注册实例，
    /// 提供默认序列化器，支持用户切换。
    /// </summary>
    public static class HGraphPersistenceRegistry
    {
        private static readonly Dictionary<Type, IHGraphPersistence> _registered = new();
        private static IHGraphPersistence _current;
        private static bool _initialized;

        /// <summary>
        /// 当前使用的序列化器。首次访问时自动初始化。
        /// </summary>
        public static IHGraphPersistence Current
        {
            get
            {
                EnsureInitialized();
                return _current;
            }
        }

        /// <summary>
        /// 所有已注册的序列化器类型。
        /// </summary>
        public static IReadOnlyCollection<Type> RegisteredTypes
        {
            get
            {
                EnsureInitialized();
                return _registered.Keys;
            }
        }

        /// <summary>
        /// 设置当前使用的序列化器（按类型）。
        /// </summary>
        public static void SetCurrent<T>() where T : IHGraphPersistence
        {
            SetCurrent(typeof(T));
        }

        /// <summary>
        /// 设置当前使用的序列化器（按类型）。
        /// </summary>
        public static void SetCurrent(Type type)
        {
            EnsureInitialized();
            if (!_registered.TryGetValue(type, out var instance))
                throw new ArgumentException($"未注册的序列化器类型: {type.FullName}");
            _current = instance;
        }

        /// <summary>
        /// 获取指定类型的序列化器实例。
        /// </summary>
        public static T Get<T>() where T : class, IHGraphPersistence
        {
            EnsureInitialized();
            return _registered.TryGetValue(typeof(T), out var instance) ? (T)instance : null;
        }

        /// <summary>
        /// 手动注册一个序列化器实例（覆盖自动发现的同类型实例）。
        /// </summary>
        public static void Register(IHGraphPersistence instance)
        {
            EnsureInitialized();
            _registered[instance.GetType()] = instance;
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            var interfaceType = typeof(IHGraphPersistence);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t => t.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t));

            foreach (var type in types)
            {
                var instance = (IHGraphPersistence)Activator.CreateInstance(type);
                _registered[type] = instance;
            }

            // 默认取第一个
            if (_registered.Count > 0)
                _current = _registered.Values.First();
        }
    }
}
