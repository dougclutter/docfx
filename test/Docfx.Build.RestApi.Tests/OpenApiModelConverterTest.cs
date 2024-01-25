using System.Text.Json;
using Docfx.Build.RestApi.OpenApi;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Docfx.Build.RestApi;

public class OpenApiModelConverterTest
{
    private const string PathToContactsJson = "TestData/openapi/contacts.json";

    [Fact]
    public void AreSummaryFieldsSet()
    {
        var model = OpenApiJsonParser.Parse(PathToContactsJson);
        var viewModel = OpenApiModelConverter.FromOpenApiModel(model);

        Assert.Equal("Contacts", viewModel.Name);
        Assert.Equal("https://graph.windows.net/myorganization/Contacts/1.0", viewModel.Uid);
        Assert.Equal("https___graph_windows_net_myorganization_Contacts_1_0", viewModel.HtmlId);
        Assert.Null(viewModel.Description);
        Assert.Null(viewModel.Raw);
    }
    [Fact]
    public void AreTagsSet()
    {
        var model = OpenApiJsonParser.Parse(PathToContactsJson);
        var viewModel = OpenApiModelConverter.FromOpenApiModel(model);

        Assert.Equal(3, viewModel.Tags.Count);

        var contactTag = viewModel.Tags.Single(i => i.Name == "contact");
        Assert.Equal("Everything about the **contacts**", contactTag.Description);
        Assert.Equal("contact-bookmark", contactTag.HtmlId);
        Assert.Equal("https://graph.windows.net/myorganization/Contacts/1.0/tag/contact", contactTag.Uid);

        var contactTagMeta = contactTag.Metadata["externalDocs"];
        var externalDoc = JsonSerializer.Deserialize<OpenApiExternalDocs>(contactTagMeta as string);
        Assert.Equal("Find out more", externalDoc.Description);
        Assert.Equal("http://swagger.io/", externalDoc.Url.ToString());

        var petStoreTag = viewModel.Tags.Single(i => i.Name == "pet store");
        Assert.Equal("Access to Petstore orders", petStoreTag.Description);
        Assert.Equal("https://graph.windows.net/myorganization/Contacts/1.0/tag/pet store", petStoreTag.Uid);

        var userTag = viewModel.Tags.Single(i => i.Name == "user");
        Assert.Equal("Operations about user", userTag.Description);
        Assert.Equal("https://graph.windows.net/myorganization/Contacts/1.0/tag/user", userTag.Uid);
    }
    [Fact]
    public void AreChildrenSet()
    {
        var model = OpenApiJsonParser.Parse(PathToContactsJson);
        var viewModel = OpenApiModelConverter.FromOpenApiModel(model);

        Assert.Equal(10, viewModel.Children.Count);

        var child = viewModel.Children.Single(i => i.OperationId == "get contact by id");
        Assert.Equal("/contacts/{object_id}", child.Path);
        Assert.Equal("Get", child.OperationName);
        Assert.Equal(["contacts", "pet store"], child.Tags);
        Assert.Equal("https___graph_windows_net_myorganization_Contacts_1_0_get_contact_by_id", child.HtmlId);
        Assert.Equal("https://graph.windows.net/myorganization/Contacts/1.0/get contact by id", child.Uid);
        Assert.Equal("<p><i>Required scope</i>: <b><i>Contacts.Read</i></b> or <b><i>Contacts.Write</i></b></p>", child.Description);
        Assert.Equal("Get a contact by using the object ID.", child.Summary);

        var parameter = child.Parameters.Single(i => i.Name == "object_id");
        Assert.Equal("The object ID (GUID) of the target contact.", parameter.Description);
        Assert.Equal("Path", parameter.Metadata["in"]);
        Assert.True((bool)parameter.Metadata["required"]);
        Assert.Equal("string", parameter.Metadata["type"]);
        Assert.Equal("31944231-fd52-4a7f-b32e-7902a01fddf9", parameter.Metadata["default"]);

        var response = child.Responses.Single();
        Assert.Equal("OK. Indicates success. The contact is returned in the response body.", response.Description);
        Assert.Equal("200", response.HttpStatusCode);

        var example = response.Examples.Single();
        Assert.Equal("application/json", example.MimeType);
    }
}
