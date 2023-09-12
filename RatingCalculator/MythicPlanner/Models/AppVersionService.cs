using System.Reflection;

namespace MythicPlanner.Models;

public class AppVersionService : IAppVersionService
{
    public string Version
    {
        get
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"v{version}";
        }
    }
}
