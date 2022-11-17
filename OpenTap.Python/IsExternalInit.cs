using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// This needs to exist for dotnet being happy about records on .netstandard2.0.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}