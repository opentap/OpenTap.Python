using System.IO;

namespace OpenTap.Python;

static class Utils
{
    public static bool RobustDirectoryExists(string path)
    {
        try
        {
            return Directory.Exists(path); 
        }
        catch
        {
            return false;
        }
    }
}