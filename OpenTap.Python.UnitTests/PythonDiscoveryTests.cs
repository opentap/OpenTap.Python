using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenTap.Cli;

namespace OpenTap.Python.UnitTests
{
    [Display("test-discovery", Group: "python")]
    public class PythonDiscoveryTests : ICliAction
    {
        public int Execute(CancellationToken cancellationToken)
        {
            // Discovery must never enumerate drives when a preferred installation is usable.
            var preferred = new[] { (library: "python313.dll", pyPath: "Python313") };
            var result = PythonDiscoverer.GetInstallationsWithFallback(preferred,
                () => throw new Exception("Drive fallback should not be called.")).ToArray();
            Assert.IsTrue(result.SequenceEqual(preferred));

            // Preserve all preferred versions, not just the first one.
            var multiple = preferred.Concat(new[] { (library: "python312.dll", pyPath: "Python312") });
            result = PythonDiscoverer.GetInstallationsWithFallback(multiple,
                () => throw new Exception("Drive fallback should not be called.")).ToArray();
            Assert.AreEqual(result.Length, 2);

            // Fall back only after preferred candidates have failed validation.
            int fallbackCalls = 0;
            result = PythonDiscoverer.GetInstallationsWithFallback(
                Enumerable.Empty<(string library, string pyPath)>(), () =>
                {
                    fallbackCalls++;
                    return preferred;
                }).ToArray();
            Assert.AreEqual(fallbackCalls, 1);
            Assert.IsTrue(result.SequenceEqual(preferred));

            result = PythonDiscoverer.GetInstallationsWithFallback(
                Enumerable.Empty<(string library, string pyPath)>(),
                () => Enumerable.Empty<(string library, string pyPath)>()).ToArray();
            Assert.AreEqual(result.Length, 0);
            // PATH is a second stage: a successful result there must also skip drive discovery.
            result = PythonDiscoverer.GetInstallationsWithFallback(
                Enumerable.Empty<(string library, string pyPath)>(),
                () => PythonDiscoverer.GetInstallationsWithFallback(preferred,
                    () => throw new Exception("Drive fallback should not be called after PATH succeeds."))).ToArray();
            Assert.IsTrue(result.SequenceEqual(preferred));

            // If both special-folder and PATH discovery fail, reach the final fallback.
            result = PythonDiscoverer.GetInstallationsWithFallback(
                Enumerable.Empty<(string library, string pyPath)>(),
                () => PythonDiscoverer.GetInstallationsWithFallback(
                    Enumerable.Empty<(string library, string pyPath)>(), () => preferred)).ToArray();
            Assert.IsTrue(result.SequenceEqual(preferred));
            return 0;
        }
    }
}
