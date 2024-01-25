using Docfx.Build.RestApi.OpenApi;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Docfx.Build.RestApi.Tests;

public class OpenApiJsonParserTest
{
    [Fact]
    public void ParseOpenApi3SimpleWorks()
    {
        var swagger = OpenApiJsonParser.Parse("TestData/openapi/simple_openapi3.json");

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
    public void ParseOpenApi3ContactsShouldSucceed()
    {
        var document = OpenApiJsonParser.Parse("TestData/openApi/contacts.json");
        Assert.NotNull(document);
    }
    [Fact]
    public void ParseSwagger2SimpleReturnsNull()
    {
        var document = OpenApiJsonParser.Parse("TestData/swagger/simple_swagger2.json");
        Assert.Null(document);
    }
    [Fact]
    public void ParseSwagger2ContactsThrows()
    {
        Assert.Throws<OpenApiJsonParserException>(() => OpenApiJsonParser.Parse("TestData/swagger/contacts.json"));
    }
}
