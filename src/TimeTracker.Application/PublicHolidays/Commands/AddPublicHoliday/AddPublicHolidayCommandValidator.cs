using FluentValidation;

namespace TimeTracker.Application.PublicHolidays.Commands.AddPublicHoliday;

public class AddPublicHolidayCommandValidator : AbstractValidator<AddPublicHolidayCommand> {
    public AddPublicHolidayCommandValidator() {
        RuleFor(x => x.Date).NotEqual(default(DateOnly));
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
