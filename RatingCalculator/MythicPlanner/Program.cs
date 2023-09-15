using MythicPlanner.Startup;
using Serilog;

namespace MythicPlanner;

internal class Program
{
    private static void Main(string[] args)
    {

        Log.Logger = CreateLogger();
        try
        {
            WebApplication.CreateBuilder(args)
                .UseSerilog()
                .ConfigureServices().Build()
                .ConfigureApp().Run();
        }
        catch (Exception ex) { Log.Fatal(ex, "Application did not start correctly."); }
        finally { Log.CloseAndFlush(); }
    }

    public static Serilog.ILogger CreateLogger() =>
       new LoggerConfiguration().ReadFrom
           .Configuration(new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build()).CreateLogger();
}
