// IsExternalInit.cs
// Shim per compilare con C#9+ features su netstandard2.0
namespace System.Runtime.CompilerServices
{
    // semplice tipo marker richiesto dal compilatore per init-only setters / record
    public static class IsExternalInit { }
}
