using FluentValidation;
using RadioLinkSim.Models;

namespace RadioLinkSim.Validation;

public sealed class LinkProfileRequestValidator : AbstractValidator<LinkProfileRequest>
{
    public LinkProfileRequestValidator()
    {
        RuleFor(request => request.LatA)
            .InclusiveBetween(-90.0, 90.0)
            .WithMessage("latA -90 ile 90 arasında olmalıdır.");

        RuleFor(request => request.LonA)
            .InclusiveBetween(-180.0, 180.0)
            .WithMessage("lonA -180 ile 180 arasında olmalıdır.");

        RuleFor(request => request.LatB)
            .InclusiveBetween(-90.0, 90.0)
            .WithMessage("latB -90 ile 90 arasında olmalıdır.");

        RuleFor(request => request.LonB)
            .InclusiveBetween(-180.0, 180.0)
            .WithMessage("lonB -180 ile 180 arasında olmalıdır.");

        RuleFor(request => request.StepMeters)
            .InclusiveBetween(10.0, 5_000.0)
            .WithMessage("stepMeters 10 ile 5000 metre arasında olmalıdır.");

        RuleFor(request => request)
            .Must(request => request.LatA != request.LatB || request.LonA != request.LonB)
            .WithName("coordinates")
            .WithMessage("A ve B koordinatları aynı nokta olamaz.");
    }
}

