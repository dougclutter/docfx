// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Docfx.Build.RestApi.OpenApi;
using Docfx.Build.RestApi.Swagger;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.RestApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Docfx.Build.RestApi;

public static class OpenApiModelConverter
{
    public static RestApiRootItemViewModel FromOpenApiModel(OpenApiModel openApi)
    {
        var uid = GetUid(openApi.Document);
        var vm = new RestApiRootItemViewModel
        {
            Name = openApi.Document.Info.Title,
            Uid = uid,
            HtmlId = GetHtmlId(uid),
            Metadata = openApi.Metadata,
            Description = openApi.Document.Info.Description,
            //Summary = openApi.Summary,        // I don't believe the Spec includes Summary
            Children = [],
            Raw = openApi.Raw,
            Tags = []
        };
        if (openApi.Document.Tags != null)
        {
            foreach (var tag in openApi.Document.Tags)
            {
                var htmlId = tag.Extensions.TryGetValue("x-bookmark-id", out IOpenApiExtension extension)
                    && extension is OpenApiString apiString
                    && !string.IsNullOrEmpty(apiString.Value)
                    ? apiString.Value : GetHtmlId(tag.Name);

                var tagViewModel = new RestApiTagViewModel
                {
                    Name = tag.Name,
                    Description = tag.Description,
                    HtmlId = htmlId,
                    Metadata = [],
                    Uid = GetUidForTag(uid, tag)
                };
                if (tag.ExternalDocs != null)
                {
                    tagViewModel.Metadata.Add("externalDocs", JsonUtility.Serialize(tag.ExternalDocs));
                }
                vm.Tags.Add(tagViewModel);

            }
        }
        if (openApi.Document.Paths != null)
        {
            foreach (var path in openApi.Document.Paths)
            {
                var commonParameters = path.Value.Parameters;
                foreach (var op in path.Value.Operations)
                {
                    var operationName = Enum.GetName(op.Key);
                    if (OperationNames.Contains(operationName, StringComparer.OrdinalIgnoreCase))
                    {
                        var operation = op.Value;
                        var parameters = GetParametersForOperation(operation.Parameters, commonParameters);
                        var itemUid = GetUidForOperation(uid, operation);
                        var itemVm = new RestApiChildItemViewModel
                        {
                            Path = path.Key,
                            OperationName = operationName,
                            Tags = operation.Tags.Select(i => i.Name).ToList(),
                            OperationId = operation.OperationId,
                            HtmlId = GetHtmlId(itemUid),
                            Uid = itemUid,
                            //Metadata = operation.Metadata,
                            Description = operation.Description,
                            Summary = operation.Summary,
                            Parameters = parameters?.Select(s =>
                            {
                                var paramViewModel = new RestApiParameterViewModel
                                {
                                    Description = s.Description,
                                    Name = s.Name,
                                    Metadata = []
                                };
                                paramViewModel.Metadata.Add("in", Enum.GetName(s.In.Value));
                                paramViewModel.Metadata.Add("required", s.Required);
                                if (s.Schema.Default is OpenApiString openApiString)
                                {
                                    paramViewModel.Metadata.Add("default", openApiString.Value);
                                    paramViewModel.Metadata.Add("type", s.Schema.Type);
                                }
                                return paramViewModel;
                            }).ToList(),
                            Responses = operation.Responses?.Select(s => new RestApiResponseViewModel
                            {
                                //Metadata = s.Value.Metadata,
                                Description = s.Value.Description,
                                //Summary = s.Value.Summary,    // I don't believe the Spec includes Summary
                                HttpStatusCode = s.Key,
                                Examples = s.Value.Content?.SelectMany(
                                    content => content.Value.Examples,
                                    (content, example) => new RestApiResponseExampleViewModel
                                    {
                                        MimeType = content.Key,
                                        Content = example.Value != null ? JsonUtility.Serialize(example.Value) : null,
                                    }).ToList(),
                            }).ToList(),
                        };

                        // TODO: line number
                        itemVm.Metadata[Constants.PropertyName.Source] = openApi.Metadata.TryGetValue(Constants.PropertyName.Source, out object value) ? value : null;
                        vm.Children.Add(itemVm);
                    }
                }
            }
        }

        return vm;
    }

    #region Private methods

    private static readonly Regex HtmlEncodeRegex = new(@"\W", RegexOptions.Compiled);
    private const string TagText = "tag";
    private static readonly string[] OperationNames = ["get", "put", "post", "delete", "options", "head", "patch"];

    /// <summary>
    /// TODO: merge with the one in XrefDetails
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private static string GetHtmlId(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return HtmlEncodeRegex.Replace(id, "_");
    }

    private static string GetUid(OpenApiDocument openApi)
    {
        return GenerateUid(openApi.Servers[0].Url, openApi.Info.Title, openApi.Info.Version);
    }

    private static string GetUidForOperation(string parentUid, OpenApiOperation item)
    {
        return GenerateUid(parentUid, item.OperationId);
    }

    private static string GetUidForTag(string parentUid, OpenApiTag tag)
    {
        return GenerateUid(parentUid, TagText, tag.Name);
    }

    /// <summary>
    /// UID is joined by '/', if segment ends with '/', use that one instead
    /// </summary>
    /// <param name="segments">The segments to generate UID</param>
    /// <returns></returns>
    private static string GenerateUid(params string[] segments)
    {
        return string.Join("/", segments.Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim('/')));
    }

    /// <summary>
    /// Merge operation's parameters with path's parameters.
    /// </summary>
    /// <param name="operationParameters">Operation's parameters</param>
    /// <param name="pathParameters">Path's parameters</param>
    /// <returns></returns>
    private static IEnumerable<OpenApiParameter> GetParametersForOperation(IList<OpenApiParameter> operationParameters, IList<OpenApiParameter> pathParameters)
    {
        if (pathParameters == null || pathParameters.Count == 0)
        {
            return operationParameters;
        }
        if (operationParameters == null || operationParameters.Count == 0)
        {
            return pathParameters;
        }

        // Path parameters can be overridden at the operation level.
        var uniquePathParams = pathParameters.Where(
            p => !operationParameters.Any(o => IsParameterEquals(p, o))).ToList();

        return operationParameters.Union(uniquePathParams).ToList();
    }

    /// <summary>
    /// Judge whether two ParameterObject equal to each other. according to value of 'name' and 'in'
    /// Define 'Equals' here instead of inside ParameterObject, since ParameterObject is either self defined or referenced object which 'name' and 'in' needs to be resolved.
    /// </summary>
    /// <param name="left">Fist ParameterObject</param>
    /// <param name="right">Second ParameterObject</param>
    private static bool IsParameterEquals(OpenApiParameter left, OpenApiParameter right)
    {
        if (left == null || right == null)
        {
            return false;
        }
        return string.Equals(left.Name, right.Name) && left.In == right.In;
    }

    private static string GetMetadataStringValue(ParameterObject parameter, string metadataName)
    {
        if (parameter.Metadata.TryGetValue(metadataName, out object metadataValue))
        {
            return (string)metadataValue;
        }
        return null;
    }
    #endregion
}
