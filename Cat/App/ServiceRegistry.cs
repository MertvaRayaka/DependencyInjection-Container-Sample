using System;
using System.Collections.Generic;

namespace App
{
    public class ServiceRegistry
    {
        public Type ServiceType { get; }
        public Lifetime Lifetime { get; }
        public Func<Cat, Type[], object> Factory { get; }
        /// <summary>
        /// 多个componet继承同一个service，则用链表将其倒序构成链表
        /// </summary>
        internal ServiceRegistry Next { get; set; }

        public ServiceRegistry(Type serviceType, Lifetime lifetime, Func<Cat, Type[], object> factory)
        {
            ServiceType = serviceType;
            Lifetime = lifetime;
            Factory = factory;
        }

        internal IEnumerable<ServiceRegistry> AsEnumerable()
        {
            var list = new List<ServiceRegistry>();
            //链表结构循环写法
            for (var self = this; self != null; self = self.Next)
            {
                list.Add(self);
            }
            return list;
        }
    }
}
