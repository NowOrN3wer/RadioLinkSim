using Microsoft.AspNetCore.Diagnostics;

namespace RadioLinkSim.ErrorHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var (statusCode, title, detail) = exception switch
        {
            ElevationServiceUnavailableException =>
            (
                StatusCodes.Status503ServiceUnavailable,
                "Yükseklik servisi kullanılamıyor",
                "Yükseklik servisi (Open-Elevation) şu anda yanıt vermiyor, lütfen daha sonra tekrar deneyin."
            ),
            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "Beklenmeyen bir hata oluştu",
                "İstek işlenirken beklenmeyen bir hata oluştu."
            )
        };

        logger.LogError(
            exception,
            "İstek işlenirken hata oluştu. HTTP durum kodu: {StatusCode}",
            statusCode);

        httpContext.Response.StatusCode = statusCode;

        await Results.Problem(
                statusCode: statusCode,
                title: title,
                detail: detail)
            .ExecuteAsync(httpContext);

        return true;
    }
}

public sealed class ElevationServiceUnavailableException : Exception
{
    public ElevationServiceUnavailableException(string message)
        : base(message)
    {
    }

    public ElevationServiceUnavailableException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public ElevationServiceUnavailableException() : base()
    {
    }
}

