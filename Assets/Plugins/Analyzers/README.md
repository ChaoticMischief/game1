# Roslyn analyzers

`Microsoft.Unity.Analyzers.dll` is [Microsoft.Unity.Analyzers](https://github.com/microsoft/Microsoft.Unity.Analyzers)
1.27.0 (MIT), from the NuGet package's `analyzers/dotnet/cs/`. Unity runs it on every C#
compile because its `.meta` carries the `RoslynAnalyzer` label and it is excluded from all
platforms. Findings (`UNT####`) appear in the Unity console, Rider, and CI logs.

To update: download the new `.nupkg` from NuGet, replace the DLL, and update the version here.
