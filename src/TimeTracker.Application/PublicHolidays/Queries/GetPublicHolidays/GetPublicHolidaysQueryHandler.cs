using MediatR;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.PublicHolidays.Dtos;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.PublicHolidays.Queries.GetPublicHolidays;

public class GetPublicHolidaysQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPublicHolidaysQuery, Result<IReadOnlyList<PublicHolidayDto>>> {
    public async Task<Result<IReadOnlyList<PublicHolidayDto>>> Handle(
        GetPublicHolidaysQuery request, CancellationToken cancellationToken
    ) {
        if (request.Year is < 2000 or > 2100) {
            return Result<IReadOnlyList<PublicHolidayDto>>.Failure("The year is out of range.");
        }

        var holidays = await unitOfWork.PublicHolidays.GetBetweenAsync(
            new DateOnly(request.Year, 1, 1), new DateOnly(request.Year, 12, 31), cancellationToken);
        return Result<IReadOnlyList<PublicHolidayDto>>.Success(
            holidays.Select(h => new PublicHolidayDto(h.Id, h.Date, h.Name)).ToList());
    }
}
