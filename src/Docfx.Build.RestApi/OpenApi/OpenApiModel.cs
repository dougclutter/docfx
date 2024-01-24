using Microsoft.OpenApi.Models;

namespace Docfx.Build.RestApi.OpenApi;

public class OpenApiModel
{
    public string Raw { get; set; }

    public OpenApiDocument Document { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = [];
}
