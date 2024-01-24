using System.Collections.Immutable;
using System.Composition;
using Docfx.Build.Common;
using Docfx.Build.RestApi.OpenApi;
using Docfx.Common;
using Docfx.Common.Git;
using Docfx.DataContracts.Common;
using Docfx.Exceptions;
using Docfx.Plugins;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers.Exceptions;

namespace Docfx.Build.RestApi;

[Export(typeof(IDocumentProcessor))]
public class OpenApiDocumentProcessor : ReferenceDocumentProcessorBase
{
    private const string OpenApiDocumentType = "RestOpenApi";
    private const string DocumentTypeKey = "documentType";
    private const string OperationIdKey = "operationId";

    // To keep backward compatibility, still support and change previous file endings by first mapping sequence.
    // Take 'a.b_swagger2.json' for an example, the json file name would be changed to 'a.b', then the html file name would be 'a.b.html'.
    private static readonly string[] SupportedFileEndings =
    {
       "_swagger2.json",
       "_swagger.json",
       ".swagger.json",
       ".swagger2.json",
       ".json",
    };

    protected static readonly string[] SystemKeys = {
        "uid",
        "htmlId",
        "name",
        "conceptual",
        "description",
        "remarks",
        "summary",
        "documentation",
        "children",
        "documentType",
        "source",
        // OpenApi Object Fields: https://openApiModel.io/specification
        "openapi",
        "info",
        "servers",
        "schemes",
        "consumes",
        "produces",
        "paths",
        "definitions",
        "parameters",
        "responses",
        "securityDefinitions",
        "security",
        "tags",
        "externalDocs"
    };

    public override string Name => nameof(OpenApiDocumentProcessor);

    [ImportMany(nameof(OpenApiDocumentProcessor))]
    public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

    protected override string ProcessedDocumentType => OpenApiDocumentType;

    public override ProcessingPriority GetProcessingPriority(FileAndType file)
    {
        switch (file.Type)
        {
            case DocumentType.Article:
                if (IsSupportedFile(file.FullPath))
                {
                    return ProcessingPriority.Normal;
                }
                break;
            case DocumentType.Overwrite:
                if (".md".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingPriority.Normal;
                }
                break;
            default:
                break;
        }
        return ProcessingPriority.NotSupported;
    }

    protected override FileModel LoadArticle(FileAndType file, ImmutableDictionary<string, object> metadata)
    {
        var filePath = Path.Combine(file.BaseDir, file.File);
        var openApiModel = OpenApiJsonParser.Parse(filePath);
        openApiModel.Metadata[DocumentTypeKey] = OpenApiDocumentType;
        openApiModel.Raw = EnvironmentContext.FileAbstractLayer.ReadAllText(filePath);
        CheckOperationId(openApiModel.Document, file.File);

        var repoInfo = GitUtility.TryGetFileDetail(filePath);
        if (repoInfo != null)
        {
            openApiModel.Metadata["source"] = new SourceDetail { Remote = repoInfo };
        }

        //openApiModel.Metadata = MergeMetadata(openApiModel.Metadata, metadata);
        var vm = OpenApiModelConverter.FromOpenApiModel(openApiModel);
        vm.Metadata[Constants.PropertyName.SystemKeys] = SystemKeys;
        var displayLocalPath = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, file.FullPath);

        return new FileModel(file, vm)
        {
            Uids = new[] { new UidDefinition(vm.Uid, displayLocalPath) }
                .Concat(from item in vm.Children
                        where !string.IsNullOrEmpty(item.Uid)
                        select new UidDefinition(item.Uid, displayLocalPath))
                .Concat(from tag in vm.Tags
                        where !string.IsNullOrEmpty(tag.Uid)
                        select new UidDefinition(tag.Uid, displayLocalPath))
                .ToImmutableArray(),
            LocalPathFromRoot = displayLocalPath
        };
    }

    private static bool IsSupportedFile(string fullPath)
        => SupportedFileEndings.Any(s => IsSupportedFileEnding(fullPath, s)) && IsOpenApiFile(fullPath);

    private static bool IsSupportedFileEnding(string filePath, string fileEnding)
        => filePath.EndsWith(fileEnding, StringComparison.OrdinalIgnoreCase);

    private static bool IsOpenApiFile(string fullPath)
    {
        try
        {
            var document = OpenApiJsonParser.Parse(fullPath);
            return document is not null;
        }
        catch (FileNotFoundException ex)
        {
            Logger.LogVerbose($"In {nameof(OpenApiDocumentProcessor)}, could not find {fullPath}, exception details: {ex.Message}.");
        }
        catch (OpenApiUnsupportedSpecVersionException ex)
        {
            Logger.LogVerbose($"Unrecognized OpenApi Version exception details: {ex.Message}");
        }
        return false;
    }

    private static void CheckOperationId(OpenApiDocument swagger, string fileName)
    {
        if (swagger.Paths != null)
        {
            foreach (var path in swagger.Paths)
            {
                foreach (var operation in path.Value.Operations)
                {
                    if (string.IsNullOrEmpty(operation.Value.OperationId))
                    {
                        throw new DocfxException($"OperationId should exist in operation '{Enum.GetName(operation.Key)}' of path '{path.Key}' for Open API file '{fileName}'");
                    }
                }
            }
        }
    }
}
