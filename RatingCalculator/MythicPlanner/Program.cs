using MythicPlanner.Startup;

namespace MythicPlanner;

internal class Program
{
    private static void Main(string[] args)
    {
        WebApplication.CreateBuilder(args)
           .ConfigureServices().Build()
           .ConfigureApp().Run();
    }
}
