using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ReflectionTarget;
using System;
using System.Reflection;

/*

Benchmark Results

|                         Method |       Mean |     Error |    StdDev |  Gen 0 | Allocated |
|------------------------------- |-----------:|----------:|----------:|-------:|----------:|
|                 PublicProperty |   3.948 ns | 0.0854 ns | 0.0757 ns | 0.0068 |      32 B |
|                BasicReflection | 166.480 ns | 1.2828 ns | 1.1372 ns | 0.0203 |      96 B |
|       CachedPropertyReflection | 119.821 ns | 1.2866 ns | 1.1405 ns | 0.0203 |      96 B |
| CachedPropertySetterReflection |   5.199 ns | 0.0542 ns | 0.0453 ns | 0.0068 |      32 B |
|     CachedPropertyCreationCost |  42.034 ns | 0.2074 ns | 0.1940 ns |      - |         - |
|       CachedSetterCreationCost | 393.244 ns | 4.4594 ns | 3.9532 ns | 0.0134 |      64 B |

Conforme evidenciado, existe o custo inicial em criar a CachedProperty e o SetPropertyDelegate.
Porém, como são static members, este custo ocorre uma única vez para todo o tempo em que a 
aplicação estiver instanciada em memória (pid).

Se o uso da reflection for pontual, isto é, uma unica vez durante todo o tempo da execução, não
vale a pena criar estes caches. Porém, é mais provável que serão utilizados multiplas vezes e, 
portanto, são um bom recurso para diluir o custo da reflection.

*/

namespace BenchmarkReflection
{
    public static class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<BenchmarkOperations>();
        }
    }

    [MemoryDiagnoser]
    public class BenchmarkOperations
    {
        [Benchmark]
        public void PublicProperty()
        {
            var reflected = new Reflected();
            reflected.PublicProperty = "content";
        }

        [Benchmark]
        public void BasicReflection()
        {
            var reflected = new Reflected();
            var propertyInfo = reflected.GetType().GetProperty("PrivateProperty",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            propertyInfo.SetValue(reflected, "content");
        }

        private static PropertyInfo cachedProperty = 
            typeof(Reflected).GetProperty("PrivateProperty",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        [Benchmark]
        public void CachedPropertyReflection()
        {
            var reflected = new Reflected();
            cachedProperty.SetValue(reflected, "content");
        }

        private static Action<Reflected, string> setPropertyDelegate =
            (Action<Reflected, string>)Delegate.CreateDelegate(
                typeof(Action<Reflected, string>), cachedProperty.GetSetMethod(true));

        [Benchmark]
        public void CachedPropertySetterReflection()
        {
            var reflected = new Reflected();
            setPropertyDelegate(reflected, "content");
        }

        [Benchmark]
        public void CachedPropertyCreationCost()
        {
            _ = typeof(Reflected).GetProperty("PrivateProperty",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Benchmark]
        public void CachedSetterCreationCost()
        {
            _ = (Action<Reflected, string>)Delegate.CreateDelegate(
                typeof(Action<Reflected, string>), cachedProperty.GetSetMethod(true));
        }
    }
}
