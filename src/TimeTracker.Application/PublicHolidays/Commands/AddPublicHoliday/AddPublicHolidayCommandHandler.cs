using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Application.PublicHolidays.Dtos;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.PublicHolidays.Commands.AddPublicHoliday;

public class AddPublicHolidayCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<AddPublicHolidayCommand, Result<PublicHolidayDto>> {
    public async Task<Result<PublicHolidayDto>> Handle(AddPublicHolidayCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result<PublicHolidayDto>.Failure("Only an employer can manage holidays.", ResultErrorType.Forbidden);
        }

        if ((await unitOfWork.PublicHolidays.GetBetweenAsync(request.Date, request.Date, cancellationToken)).Count > 0) {
            return Result<PublicHolidayDto>.Failure("There's already a holiday on this date.", ResultErrorType.Conflict);
        }

        var holiday = PublicHoliday.Create(request.Date, request.Name);
        await unitOfWork.PublicHolidays.AddAsync(holiday, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PublicHolidayDto>.Success(new PublicHolidayDto(holiday.Id, holiday.Date, holiday.Name));
    }
}
