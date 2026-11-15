using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Results;

public class ResultProblemDetails : ProblemDetails
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    public ResultProblemDetails(ProblemDetails source)
    {
        Type = source.Type;
        Title = source.Title;
        Status = source.Status;
        Detail = source.Detail;
        Instance = source.Instance;

        foreach (var (key, value) in source.Extensions)
        {
            Extensions[key] = value;
        }
    }
}
