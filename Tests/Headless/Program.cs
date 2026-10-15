using System;
using NeonShooter.Tests;
class Program
{
    static int Main()
    {
        int passed = 0, failed = 0;
        GameplayChecks.Run((name, test) => {
            try { test(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
        });
        try { passed += BrowserParity.Check(); } catch(Exception ex) { failed++; Console.WriteLine("FAIL browser parity: "+ex.Message); }
        try { SoakCheck.Run(); passed++; } catch(Exception ex) { failed++; Console.WriteLine("FAIL soak: "+ex.Message); }
        Console.WriteLine($"{passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }
}
