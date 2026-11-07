using System;
using System.Linq;
using System.Reflection;

internal static class Program
{
    private static int Main()
    {
        int suites = 0;
        int failures = 0;
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().OrderBy(type => type.Name))
        {
            if (!type.Name.EndsWith("Tests", StringComparison.Ordinal)) continue;
            MethodInfo run = type.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
            if (run == null || run.GetParameters().Length != 0) continue;
            suites++;
            try
            {
                run.Invoke(null, null);
                Console.WriteLine("PASS " + type.Name);
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine("FAIL " + type.Name + ": " + (exception.InnerException ?? exception));
            }
        }
        Console.WriteLine("Suites: " + suites + ", failures: " + failures);
        return suites == 0 || failures != 0 ? 1 : 0;
    }
}
