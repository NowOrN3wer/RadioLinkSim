using RadioLinkSim.ErrorHandling;
using RadioLinkSim.Models.OpenElevation;
using System.Net;
using System.Text.Json;

namespace RadioLinkSim.Services;

public sealed class OpenElevationClient(
    HttpClient httpClient,
    ILogger<OpenElevationClient> logger) : IElevationProvider
{
    private const int MaximumAttempts = 3;

    public async Task<IReadOnlyList<double>> GetElevationsAsync(
        IReadOnlyList<(double Latitude, double Longitude)> locations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(locations);

        if (locations.Count == 0)
        {
            return Array.Empty<double>();
        }

        var request = new OpenElevationRequest(
            locations
                .Select(location => new OpenElevationLocation(location.Latitude, location.Longitude))
                .ToArray());

        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            try
            {
                using var response = await httpClient.PostAsJsonAsync(
                    "api/v1/lookup",
                    request,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadFromJsonAsync<OpenElevationResponse>(
                        cancellationToken: cancellationToken);

                    if (payload?.Results is null)
                    {
                        throw new ElevationServiceUnavailableException(
                            "Open-Elevation boş veya geçersiz bir cevap döndürdü.");
                    }

                    if (payload.Results.Count != locations.Count)
                    {
                        throw new ElevationServiceUnavailableException(
                            $"Open-Elevation sonuç sayısı eşleşmedi. Beklenen: {locations.Count}, gelen: {payload.Results.Count}.");
                    }

                    return payload.Results
                        .Select(result => result.Elevation)
                        .ToArray();
                }

                if (!IsRetryableStatusCode(response.StatusCode))
                {
                    throw new ElevationServiceUnavailableException(
                        $"Open-Elevation isteği {(int)response.StatusCode} ({response.StatusCode}) durum koduyla reddedildi.");
                }

                lastException = new HttpRequestException(
                    $"Open-Elevation {(int)response.StatusCode} ({response.StatusCode}) döndürdü.",
                    inner: null,
                    response.StatusCode);

                logger.LogWarning(
                    "Open-Elevation denemesi başarısız oldu. Deneme: {Attempt}/{MaximumAttempts}, durum: {StatusCode}",
                    attempt,
                    MaximumAttempts,
                    (int)response.StatusCode);
            }
            catch (HttpRequestException exception)
            {
                lastException = exception;

                logger.LogWarning(
                    exception,
                    "Open-Elevation HTTP isteği başarısız oldu. Deneme: {Attempt}/{MaximumAttempts}",
                    attempt,
                    MaximumAttempts);
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = exception;

                logger.LogWarning(
                    exception,
                    "Open-Elevation isteği zaman aşımına uğradı. Deneme: {Attempt}/{MaximumAttempts}",
                    attempt,
                    MaximumAttempts);
            }
            catch (JsonException exception)
            {
                throw new ElevationServiceUnavailableException(
                    "Open-Elevation geçersiz JSON döndürdü.",
                    exception);
            }

            if (attempt < MaximumAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2.0, attempt));
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new ElevationServiceUnavailableException(
            "Open-Elevation üç denemeden sonra yanıt vermedi.",
            lastException);
    }

    private static bool IsRetryableStatusCode(HttpStatusCode statusCode) =>
        (int)statusCode >= StatusCodes.Status500InternalServerError;
}
