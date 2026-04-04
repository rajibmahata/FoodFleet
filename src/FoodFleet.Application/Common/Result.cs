namespace FoodFleet.Application.Common;

public record Result<T>(bool IsSuccess, T? Data, string? Error, int StatusCode = 200)
{
    public static Result<T> Success(T data, int statusCode = 200) => new(true, data, null, statusCode);
    public static Result<T> Failure(string error, int statusCode = 400) => new(false, default, error, statusCode);
    public static Result<T> NotFound(string error = "Not found") => new(false, default, error, 404);
    public static Result<T> Unauthorized(string error = "Unauthorized") => new(false, default, error, 401);
    public static Result<T> Forbidden(string error = "Forbidden") => new(false, default, error, 403);
    public static Result<T> UnprocessableEntity(string error) => new(false, default, error, 422);
}

public record Result(bool IsSuccess, string? Error, int StatusCode = 200)
{
    public static Result Success(int statusCode = 200) => new(true, null, statusCode);
    public static Result Failure(string error, int statusCode = 400) => new(false, error, statusCode);
    public static Result NotFound(string error = "Not found") => new(false, error, 404);
}
