using Docfx.Exceptions;
using Microsoft.OpenApi.Models;

namespace Docfx.Build.RestApi.OpenApi;

public class OpenApiJsonParserException : DocfxException
{
    public OpenApiJsonParserException()
    {
    }

    public OpenApiJsonParserException(string message) : base(message)
    {
    }

    public OpenApiJsonParserException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public OpenApiJsonParserException(IList<OpenApiError> errors, IList<OpenApiError> warnings)
        : base(string.Join(Environment.NewLine, errors))
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
    }

    /// <summary>
    /// List of all errors.
    /// </summary>
    public IList<OpenApiError> Errors { get; set; } = new List<OpenApiError>();

    /// <summary>
    /// List of all warnings
    /// </summary>
    public IList<OpenApiError> Warnings { get; set; } = new List<OpenApiError>();

}
