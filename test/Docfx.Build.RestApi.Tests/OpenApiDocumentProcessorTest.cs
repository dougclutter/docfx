using System.Collections.Immutable;
using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.DataContracts.RestApi;
using Docfx.Plugins;
using Docfx.Tests.Common;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Docfx.Build.RestApi.Tests;

public class OpenApiDocumentProcessorTest : TestBase
{
    private readonly string _outputFolder;
    private readonly string _inputFolder;
    private readonly FileCollection _defaultFiles;
    private readonly ApplyTemplateSettings _applyTemplateSettings;

    private const string RawModelFileExtension = ".raw.json";
    private const string SwaggerDirectory = "openapi";

    public OpenApiDocumentProcessorTest()
    {
        _outputFolder = GetRandomFolder();
        _inputFolder = GetRandomFolder();
        _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
        _defaultFiles.Add(DocumentType.Article, new[] { "TestData/openapi/contacts.json" }, "TestData/");
        _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder)
        {
            RawModelExportSettings = { Export = true }
        };
    }

    [Fact]
    public void BadFormatReturnsNotSupported()
    {
        var file = GetFile("TestData/openapi/badFormat.json");
        var processor = new OpenApiDocumentProcessor();
        var priority = processor.GetProcessingPriority(file);
        Assert.Equal(ProcessingPriority.NotSupported, priority);
    }
    [Fact]
    public void FileNotFoundReturnsNotSupported()
    {
        var file = GetFile("TestData/openapi/noSuchFile.xyz");
        var processor = new OpenApiDocumentProcessor();
        var priority = processor.GetProcessingPriority(file);
        Assert.Equal(ProcessingPriority.NotSupported, priority);
    }
    [Fact]
    public void GetProcessingPriorityForSimpleOpenApi3ReturnsNormal()
    {
        var file = GetFile("TestData/openapi/simple_openapi3.json");
        var processor = new OpenApiDocumentProcessor();
        var priority = processor.GetProcessingPriority(file);
        Assert.Equal(ProcessingPriority.Normal, priority);
    }
    [Fact]
    public void ProcessSwaggerShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
        BuildDocument(files);

        var outputRawModelPath = GetRawModelFilePath("contacts.json");
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
        Assert.Equal("https://graph.windows.net/myorganization/Contacts/1.0", model.Uid);
        Assert.Equal("https___graph_windows_net_myorganization_Contacts_1_0", model.HtmlId);
        Assert.Equal(10, model.Children.Count);
        Assert.Equal("Hello world!", model.Metadata["meta"]);

        // Verify $ref in path
        var item0 = model.Children[0];
        Assert.Equal("graph.windows.net/myorganization/Contacts/1.0/get contacts", item0.Uid);
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">You can get a collection of contacts from your tenant.</p>\n", item0.Summary);
        Assert.Single(item0.Parameters);
        Assert.Equal("1.6", item0.Parameters[0].Metadata["default"]);
        Assert.Single(item0.Responses);
        Assert.Equal("200", item0.Responses[0].HttpStatusCode);

        // Verify tags of child
        Assert.Equal("contacts", item0.Tags[0]);
        var item1 = model.Children[1];
        Assert.Equal("contacts", item1.Tags[0]);
        Assert.Equal("pet store", item1.Tags[1]);

        // Verify tags of root
        Assert.Equal(3, model.Tags.Count);
        var tag0 = model.Tags[0];
        Assert.Equal("contact", tag0.Name);
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">Everything about the <strong sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">contacts</strong></p>\n", tag0.Description);
        Assert.Equal("contact-bookmark", tag0.HtmlId);
        Assert.Single(tag0.Metadata);
        var externalDocs = (JObject)tag0.Metadata["externalDocs"];
        Assert.NotNull(externalDocs);
        Assert.Equal("Find out more", externalDocs["description"]);
        Assert.Equal("http://swagger.io", externalDocs["url"]);
        var tag1 = model.Tags[1];
        Assert.Equal("pet_store", tag1.HtmlId);

        // Verify path parameters
        // Path parameter applicable for get operation
        Assert.Equal(2, item1.Parameters.Count);
        Assert.Equal("object_id", item1.Parameters[0].Name);
        Assert.Equal("api-version", item1.Parameters[1].Name);
        Assert.Equal(true, item1.Parameters[1].Metadata["required"]);

        // Override ""api-version" parameters by $ref for patch operation
        var item2 = model.Children[2];
        Assert.Equal(3, item2.Parameters.Count);
        Assert.Equal("object_id", item2.Parameters[0].Name);
        Assert.Equal("api-version", item2.Parameters[1].Name);
        Assert.Equal(false, item2.Parameters[1].Metadata["required"]);

        // Override ""api-version" parameters by self definition for delete operation
        var item3 = model.Children[3];
        Assert.Equal(2, item3.Parameters.Count);
        Assert.Equal("object_id", item3.Parameters[0].Name);
        Assert.Equal("api-version", item3.Parameters[1].Name);
        Assert.Equal(false, item3.Parameters[1].Metadata["required"]);

        // When operation parameters is not set, inherit from th parameters for post operation
        var item4 = model.Children[4];
        Assert.Single(item4.Parameters);
        Assert.Equal("api-version", item4.Parameters[0].Name);
        Assert.Equal(true, item4.Parameters[0].Metadata["required"]);

        // When 'definitions' has direct child with $ref defined, should resolve it
        var item5 = model.Children[6];
        var parameter2 = (JObject)item5.Parameters[2].Metadata["schema"];
        Assert.Equal("string", parameter2["type"]);
        Assert.Equal("uri", parameter2["format"]);
        // Verify markup result of parameters
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">The request body <em sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">contains</em> a single property that specifies the URL of the user or contact to add as manager.</p>\n",
            item5.Parameters[2].Description);
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\"><strong sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">uri</strong> description.</p>\n",
            ((string)parameter2["description"]));
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">No Content. Indicates <strong sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">success</strong>. No response body is returned.</p>\n",
            item5.Responses[0].Description);

        // Verify for markup result of securityDefinitions
        var securityDefinitions = (JObject)model.Metadata.Single(m => m.Key == "securityDefinitions").Value;
        var auth = (JObject)securityDefinitions["auth"];
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">securityDefinitions <em sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">description</em>.</p>\n",
            auth["description"].ToString());
    }

    private void BuildDocument(FileCollection files)
    {
        var parameters = new DocumentBuildParameters
        {
            Files = files,
            OutputBaseDir = _outputFolder,
            ApplyTemplateSettings = _applyTemplateSettings,
            Metadata = new Dictionary<string, object>
            {
                ["meta"] = "Hello world!",
            }.ToImmutableDictionary()
        };

        using var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty);
        builder.Build(parameters);
    }

    private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
    {
        yield return typeof(OpenApiDocumentProcessor).Assembly;
    }

    private string GetRawModelFilePath(string fileName)
    {
        return Path.Combine(_outputFolder, SwaggerDirectory, Path.ChangeExtension(fileName, RawModelFileExtension));
    }

    private static FileAndType GetFile(string path)
        => new(Directory.GetCurrentDirectory(), path, DocumentType.Article);
}
