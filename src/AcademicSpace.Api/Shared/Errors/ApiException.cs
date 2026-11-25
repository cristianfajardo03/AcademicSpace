namespace AcademicSpace.Api.Shared.Errors;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public IDictionary<string, string[]>? Errors { get; }

    public ApiException(int statusCode, string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public static ApiException BadRequest(string message) => new(400, message);
    public static ApiException Unauthorized(string message) => new(401, message);
    public static ApiException Forbidden(string message) => new(403, message);
    public static ApiException NotFound(string message) => new(404, message);
    public static ApiException Conflict(string message) => new(409, message);
}
