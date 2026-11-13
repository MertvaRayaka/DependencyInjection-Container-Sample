using System;
using System.Linq;
using System.Reflection;

namespace App
{
    class Program
    {
        static void Main(string[] args)
        {
            TestC();

            Console.ReadLine();
        }

        static void TestA()
        {
            using (var root = new Cat()
                .Register<IFoo, Foo>(Lifetime.Transient)
                .Register<IBar>(_ => new Bar(), Lifetime.Self)
                .Register<IBaz, Baz>(Lifetime.Root)
                .Register(Assembly.GetEntryAssembly()))
            {
                using (var cat = root.CreateChild())
                {
                    cat.GetService<IFoo>();
                    cat.GetService<IBar>();
                    cat.GetService<IBaz>();
                    cat.GetService<IQux>();
                    Console.WriteLine("Child cat is disposed.");
                }
                Console.WriteLine("Root cat is disposed.");
            }
        }

        static void TestB()
        {
            using (var root = new Cat())
            {
                root.Register<IFoo, FooA>(Lifetime.Transient);
                root.Register<IFoo, FooB>(Lifetime.Transient);
                var components = root.GetServices<IFoo>();
                //var components = root.GetService<IEnumberable<IFoo>>(); 
                var fooA = components.Single(c => c is FooA);
                var fooB = components.Single(c => c is FooB);
                Console.WriteLine("Root cat is disposed.");
            }
        }

        static void TestC()
        {
            using (var root = new Cat())
            {
                root.Register<IFoo, Foo>(Lifetime.Transient)
                    .Register<IBar, Bar>(Lifetime.Transient)
                    .Register(typeof(IFoobar<,>), typeof(Foobar<,>), Lifetime.Root);
                using (var child = root.CreateChild())
                {
                    var foobar = child.GetService<IFoobar<IFoo, IBar>>();
                    Console.WriteLine("child cat is disposed.");
                }
                Console.WriteLine("root cat is disposed.");
            }
        }
    }
}
