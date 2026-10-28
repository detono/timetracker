using MediatR;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Models;
using TimeTracker.Domain.Enums;
using TimeTracker.Domain.Interfaces;

namespace TimeTracker.Application.PublicHolidays.Commands.DeletePublicHoliday;

public class DeletePublicHolidayCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<DeletePublicHolidayCommand, Result> {
    public async Task<Result> Handle(DeletePublicHolidayCommand request, CancellationToken cancellationToken) {
        if (currentUser.Role != UserRole.Employer) {
            return Result.Failure("Only an employer can manage holidays.", ResultErrorType.Forbidden);
        }

        var holiday = await unitOfWork.PublicHolidays.GetByIdAsync(request.Id, cancellationToken);
        if (holiday is null) {
            return Result.Failure("Holiday was not found.", ResultErrorType.NotFound);
        }

        unitOfWork.PublicHolidays.Remove(holiday);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
