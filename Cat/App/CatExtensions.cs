using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace App
{
    /// <summary>
    /// 两个核心Register
    /// 1.Register(this Cat cat, Type from, Type to, Lifetime lifetime)扩展方法
    /// 2.Register(ServiceRegistry registry)
    /// </summary>
    public static class CatExtensions
    {
        public static Cat Register(this Cat cat, Type from, Type to, Lifetime lifetime)
        {
            //右侧lambda表达式表示一个方法，签名与左侧委托类型一致Func<Cat, Type[], object>
            //比较不好理解的部分在于返回值部分的Create这个方法
            //其实是要它的类型为object的返回值，可以增加一些参数参与方法执行
            //_弃元、arguments是形参
            Func<Cat, Type[], object> factory = (_, arguments) => Create(_, to, arguments);
            cat.Register(new ServiceRegistry(from, lifetime, factory));
            return cat;
        }

        public static Cat Register<TFrom, TTo>(this Cat cat, Lifetime lifetime) where TTo : TFrom
            => cat.Register(typeof(TFrom), typeof(TTo), lifetime);

        public static Cat Register(this Cat cat, Type serviceType, object instance)
        {
            Func<Cat, Type[], object> factory = (_, arguments) => instance;
            cat.Register(new ServiceRegistry(serviceType, Lifetime.Root, factory));
            return cat;
        }

        public static Cat Register<TService>(this Cat cat, TService instance)
        {
            Func<Cat, Type[], object> factory = (_, arguments) => instance;
            cat.Register(new ServiceRegistry(typeof(TService), Lifetime.Root, factory));
            return cat;
        }

        public static Cat Register(this Cat cat, Type serviceType,
            Func<Cat, object> factory, Lifetime lifetime)
        {
            cat.Register(new ServiceRegistry(serviceType, lifetime, (_, arguments) => factory(_)));
            return cat;
        }

        public static Cat Register<TService>(this Cat cat,
            Func<Cat, TService> factory, Lifetime lifetime)
        {
            cat.Register(new ServiceRegistry(typeof(TService), lifetime, (_, arguments) => factory(_)));
            return cat;
        }

        public static Cat Register(this Cat cat, Assembly assembly)
        {
            var typedAttributes = from type in assembly.GetExportedTypes()
                                  let attribute = type.GetCustomAttribute<MapToAttribute>()
                                  where attribute != null
                                  select new { ServiceType = type, Attribute = attribute };

            foreach (var typedAttribute in typedAttributes)
            {
                cat.Register(typedAttribute.Attribute.ServiceType, typedAttribute.ServiceType, typedAttribute.Attribute.Lifetime);
            }
            return cat;
        }

        public static Cat CreateChild(this Cat cat) => new Cat(cat);

        public static T GetService<T>(this Cat cat) => (T)cat.GetService(typeof(T));

        public static IEnumerable<T> GetServices<T>(this Cat cat) => cat.GetService<IEnumerable<T>>();

        private static object Create(Cat cat, Type type, Type[] genericArguments)
        {
            if (genericArguments.Length > 0)
            {
                //举例：用List<>构建出List<string>，此时type就是List<>，genericArguments就是string
                type = type.MakeGenericType(genericArguments);
            }
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"Cannot create the instance of {type} which does not have an public constructor.");
            }
            var constructor = constructors.FirstOrDefault(it => it.GetCustomAttributes(false).OfType<InjectionAttribute>().Any());
            constructor ??= constructors.First();
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0)
            {
                return Activator.CreateInstance(type);
            }
            var arguments = new object[parameters.Length];
            for (int index = 0; index < arguments.Length; index++)
            {
                arguments[index] = cat.GetService(parameters[index].ParameterType);
            }
            return constructor.Invoke(arguments);
        }
    }
}
