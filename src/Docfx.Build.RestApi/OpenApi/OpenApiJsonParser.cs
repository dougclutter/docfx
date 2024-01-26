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
        if (diagnostic.Errors.Any())
        {
            foreach (var error in diagnostic.Warnings)
            {
                Logger.LogWarning($"OpenApi parse warnings: {error.Message}", file: fullPath);
            }
            foreach (var error in diagnostic.Errors )
            {
                Logger.LogError($"OpenApi parse errors: {error.Message}", file: fullPath);
            }
            return null;
        }
        if (diagnostic?.SpecificationVersion == OpenApiSpecVersion.OpenApi3_0)
        {
            return new() { Document = document };
        }
        Logger.LogWarning($"OpenApi version not supported: {diagnostic?.SpecificationVersion}");
        return null;
    }
}
