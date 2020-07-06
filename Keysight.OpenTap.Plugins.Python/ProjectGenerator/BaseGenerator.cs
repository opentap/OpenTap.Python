using System;
using OpenTap;
using System.Text.RegularExpressions;
using System.IO;
using OpenTap.Cli;
using System.Threading;
using System.Text;
using System.Reflection;

namespace Keysight.OpenTap.Plugins.Python.ProjectGenerator
{
    public abstract class BaseGenerator : ICliAction
    {
        public const string resources = "Keysight.OpenTap.Plugins.Python.Resources";
        public const string initFile = "__init__.py";
        public TraceSource log = Log.CreateSource("New");

        public abstract int Execute(CancellationToken cancellationToken);

        public bool GetBoolInput(string question)
        {
            log.Info(question);
            var request = new BoolRequest();
            UserInput.Request(request, true);
            return request.BoolInput == BoolEnum.Yes ? true : false;
        }

        public bool NameValidator(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                log.Warning("Name cannot be empty.");
                return false;
            }
            Regex regex = new Regex(@"^[a-zA-Z_][\w]*$");
            Match match = regex.Match(s);
            if (match.Success)
                return true;

            log.Warning("Invalid name format. Please follow python naming conventions.");
            return false;
        }

        public bool VersionValidator(string s)
        {
            Regex regex = new Regex(@"^\d+\.\d+\.\d+$");
            Match match = regex.Match(s);
            if (match.Success)
                return true;

            log.Warning("Invalid version format. Please follow this format [0.0.0].");
            return false;
        }

        public void WriteFile(string filepath, string content, bool force = false)
        {
            if (File.Exists(filepath) && force == false)
            {
                log.Error("File already exists: '{0}'", Path.GetFileName(filepath));
                if (!GetBoolInput("Do you want to override?"))
                {
                    log.Info("File was not overridden.");
                    return;
                }
            }
            if (!Directory.Exists(Path.GetDirectoryName(filepath)) && string.IsNullOrWhiteSpace(Path.GetDirectoryName(filepath)) == false)
                Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            File.WriteAllText(filepath, content);
            log.Info($"Generated file: '{filepath}'");
        }

    }

    public class BoolRequest
    {
        [Layout(LayoutMode.FloatBottom | LayoutMode.FullRow)]
        [Submit]
        public BoolEnum BoolInput { get; set; }
    }
}
