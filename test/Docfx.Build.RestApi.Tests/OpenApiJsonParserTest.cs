using Docfx.Build.RestApi.OpenApi;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Docfx.Build.RestApi.Tests;

public class OpenApiJsonParserTest
{
    [Theory]
    [InlineData("TestData/swagger/simple_swagger2.json")]
    [InlineData("TestData/openapi/simple_openapi3.json")]
    public void ParseSimpleSwaggerJsonShouldSucceed(string fileName)
    {
        var swagger = OpenApiJsonParser.Parse(fileName);

        Assert.Single(swagger.Document.Paths.Values);
        var contacts = swagger.Document.Paths["/contacts"];

        Assert.Single(contacts.Operations);
        var operation = contacts.Operations[OperationType.Get];
        Assert.NotNull(operation);

        Assert.Single(operation.Parameters);
        var parameter = operation.Parameters[0];
        Assert.Equal(ParameterLocation.Query, parameter.In);

        Assert.Single(operation.Responses);
        var response = operation.Responses["200"];

        var example = response.Content["application/json"];
        Assert.NotNull(example);
    }
    [Fact]
    public void DoesParsingSwagger2Work()
    {
        var document = OpenApiJsonParser.Parse("TestData/swagger/contacts.json");
        Assert.NotNull(document);
    }
}
