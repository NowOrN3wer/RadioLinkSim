using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using RadioLinkSim.Models;
using RadioLinkSim.Models.OpenElevation;
using RadioLinkSim.Services;

namespace RadioLinkSim.Endpoints;

public static class LinkProfileEndpoints
{
    public static IEndpointRouteBuilder MapLinkProfileEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/link-profile",
                CreateLinkProfileAsync)
            .WithName("CreateLinkProfile")
            .WithTags("Link Profile")
            .WithSummary("İki koordinat arasındaki yükseklik profilini hesaplar.")
            .Produces<LinkProfileResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<Results<Ok<LinkProfileResponse>, ValidationProblem>> CreateLinkProfileAsync(
        LinkProfileRequest request,
        IValidator<LinkProfileRequest> validator,
        LinkProfileService linkProfileService,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.ErrorMessage)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray());

            return TypedResults.ValidationProblem(errors);
        }

        var response = await linkProfileService.CreateProfileAsync(
            request,
            cancellationToken);

        return TypedResults.Ok(response);
    }
}
