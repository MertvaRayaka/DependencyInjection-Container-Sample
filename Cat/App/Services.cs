using System;

namespace App
{
    public interface IFoo { }
    public interface IBar { }
    public interface IBaz { }
    public interface IQux { }
    public interface IFoobar<out T1, out T2> { }
    public class Base : IDisposable
    {
        public Base()
            => Console.WriteLine($"Instance of {GetType().Name} is created.");
        public void Dispose()
            => Console.WriteLine($"Instance of {GetType().Name} is disposed.");
    }

    public class Foo : Base, IFoo { }
    public class Bar : Base, IBar { }
    public class Baz : Base, IBaz { }

    public class FooA : Base, IFoo { }
    public class FooB : Base, IFoo { }

    [MapTo(typeof(IQux), Lifetime.Root)]
    public class Qux : Base, IQux { }
    public class Foobar<T1, T2> : Base, IFoobar<T1, T2>
    {
        public T1 Foo { get; }
        public T2 Bar { get; }
        public Foobar(T1 foo, T2 bar)
        {
            Foo = foo;
            Bar = bar;
        }
    }

}
