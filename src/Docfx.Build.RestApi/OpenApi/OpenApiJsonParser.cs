using Microsoft.OpenApi;
using Microsoft.OpenApi.Readers;

namespace Docfx.Build.RestApi.OpenApi;

public class OpenApiJsonParser
{
    public static OpenApiModel Parse(string fullPath)
    {
        using var fileStream = File.OpenRead(fullPath);
        var reader = new OpenApiStreamReader();
        var document = reader.Read(fileStream, out OpenApiDiagnostic diagnostic);
        if (diagnostic?.SpecificationVersion == OpenApiSpecVersion.OpenApi3_0)
        {
            return new() { Document = document };
        }
        return null;
    }
}
