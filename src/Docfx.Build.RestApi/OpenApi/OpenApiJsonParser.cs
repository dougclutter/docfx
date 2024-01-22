﻿using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Docfx.Build.RestApi.OpenApi;

public class OpenApiJsonParser
{
    public static OpenApiDocument Parse(string fullPath)
    {
        using var fileStream = File.OpenRead(fullPath);
        var reader = new OpenApiStreamReader();
        var document = reader.Read(fileStream, out OpenApiDiagnostic diagnostic);
        return document;
    }
}
