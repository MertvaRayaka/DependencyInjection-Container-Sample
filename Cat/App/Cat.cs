using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace App
{
    public class Cat : IServiceProvider, IDisposable
    {
        internal readonly Cat _root;
        internal readonly ConcurrentDictionary<Type, ServiceRegistry> _registries;
        /// <summary>
        /// 定义成_services，但其实是已经创建的component
        /// </summary>
        private readonly ConcurrentDictionary<Key, object> _services;
        private readonly ConcurrentBag<IDisposable> _disposables;
        private volatile bool _disposed;

        public Cat()
        {
            _registries = new ConcurrentDictionary<Type, ServiceRegistry>();
            _root = this;
            _services = new ConcurrentDictionary<Key, object>();
            _disposables = new ConcurrentBag<IDisposable>();
        }

        internal Cat(Cat parent)
        {
            _root = parent._root;
            _registries = _root._registries;
            _services = new ConcurrentDictionary<Key, object>();
            _disposables = new ConcurrentBag<IDisposable>();
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Cat");
            }
        }

        public Cat Register(ServiceRegistry registry)
        {
            EnsureNotDisposed();
            if (_registries.TryGetValue(registry.ServiceType, out var existing))
            {
                //以下两步将registry插入表头，后注册的组件先resolve
                _registries[registry.ServiceType] = registry;
                registry.Next = existing;
                //existing.Next = registry;将registry插入表尾，先注册的组件先resolve
            }
            else
            {
                _registries[registry.ServiceType] = registry;
            }
            return this;
        }

        public object GetService(Type serviceType)
        {
            EnsureNotDisposed();

            if (serviceType == typeof(Cat) || serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            ServiceRegistry registry;
            //IEnumerable<T>
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = serviceType.GetGenericArguments()[0];
                if (!_registries.TryGetValue(elementType, out registry))
                {
                    return Array.CreateInstance(elementType, 0);
                }

                var registries = registry.AsEnumerable();
                var services = registries.Select(it => GetServiceCore(it, Type.EmptyTypes)).ToArray();
                Array array = Array.CreateInstance(elementType, services.Length);
                services.CopyTo(array, 0);
                return array;
            }

            //Generic
            if (serviceType.IsGenericType && !_registries.ContainsKey(serviceType))
            {
                var definition = serviceType.GetGenericTypeDefinition();
                return _registries.TryGetValue(definition, out registry)
                    ? GetServiceCore(registry, serviceType.GetGenericArguments())
                    : null;
            }

            //Normal
            return _registries.TryGetValue(serviceType, out registry)
                    ? GetServiceCore(registry, Type.EmptyTypes)
                    : null;
        }

        public void Dispose()
        {
            _disposed = true;
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
            _disposables.Clear();
            _services.Clear();
        }

        private object GetServiceCore(ServiceRegistry registry, Type[] genericArguments)
        {
            var key = new Key(registry, genericArguments);
            var serviceType = registry.ServiceType;

            switch (registry.Lifetime)
            {
                case Lifetime.Root: return GetOrCreate(_root._services, _root._disposables);
                case Lifetime.Self: return GetOrCreate(_services, _disposables);
                default:
                    {
                        var service = registry.Factory(this, genericArguments);
                        if (service is IDisposable disposable && disposable != this)
                        {
                            //ConcurrentBag的Add会把元素插到bag最前方
                            _disposables.Add(disposable);
                        }
                        return service;
                    }
            }

            object GetOrCreate(ConcurrentDictionary<Key, object> services, ConcurrentBag<IDisposable> disposables)
            {
                //此处的key在外部方法中实例化
                if (services.TryGetValue(key, out var service))
                {
                    return service;
                }
                service = registry.Factory(this, genericArguments);
                services[key] = service;
                if (service is IDisposable disposable)
                {
                    disposables.Add(disposable);
                }
                return service;
            }
        }
    }
}