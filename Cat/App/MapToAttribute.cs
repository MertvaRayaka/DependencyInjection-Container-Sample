using System;

namespace App
{
    //AllowMultiple = true 标记了我们的定制特性能否被重复放置在同一个程序实体前多次
    //Inherited = true 控制定制特性的继承规则。它标记了我们的特性能否被继承
    //两个属性在继承类的特性添加上起到作用
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class MapToAttribute : Attribute
    {
        public Type ServiceType { get; }
        public Lifetime Lifetime { get; }

        public MapToAttribute(Type serviceType, Lifetime lifetime)
        {
            ServiceType = serviceType;
            Lifetime = lifetime;
        }
    }
}