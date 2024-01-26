using Docfx.Common;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Readers;

namespace Docfx.Build.RestApi.OpenApi;

public class OpenApiJsonParser
{
    public static OpenApiModel Parse(string fullPath)
    {
        using var fileStream = File.OpenRead(fullPath);
        var reader = new OpenApiStreamReader(new() { ReferenceResolution = ReferenceResolutionSetting.ResolveLocalReferences });
        var document = reader.Read(fileStream, out OpenApiDiagnostic diagnostic);
        if (diagnostic?.SpecificationVersion == OpenApiSpecVersion.OpenApi3_0)
        {
            LogDiagnostics(fullPath, diagnostic);
            return diagnostic.Errors.Count > 0 ? null : new() { Document = document };
        }
        Logger.LogVerbose($"OpenApi version not supported: {diagnostic?.SpecificationVersion}");
        return null;
    }

    private static void LogDiagnostics(string fullPath, OpenApiDiagnostic diagnostic)
    {
        foreach (var error in diagnostic.Warnings)
        {
            Logger.LogWarning($"OpenApi parse warnings: {error.Message}", file: fullPath);
        }
        foreach (var error in diagnostic.Errors)
        {
            Logger.LogError($"OpenApi parse errors: {error.Message}", file: fullPath);
        }
    }
}
