using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docfx.Build.RestApi.OpenApi;
using Docfx.Build.RestApi.Swagger;
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

        Assert.Single(swagger.Paths.Values);
        var contacts = swagger.Paths["/contacts"];
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
}
