namespace AMS.Application.Common;

public class Result<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static Result<T> SuccessResult(T data, string message = "Operation successful")
    {
        return new Result<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static Result<T> FailureResult(string message, List<string>? errors = null)
    {
        return new Result<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }

    public static Result<T> FailureResult(List<string> errors)
    {
        return new Result<T>
        {
            Success = false,
            Message = "Operation failed",
            Errors = errors
        };
    }
}