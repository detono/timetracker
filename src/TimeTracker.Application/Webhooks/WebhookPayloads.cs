using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Application.Webhooks;

/// <summary>
/// The JSON webhooks send. Every event has the same outer shape: <c>id</c> (the same on
/// retries, so receivers can skip duplicates), <c>event</c>, <c>occurredAtUtc</c>, <c>actor</c>
/// (who did it) and <c>employee</c> (who it's about), plus a block for the thing itself, like
/// <c>leave</c>. New events add a new block; existing fields don't change.
/// </summary>
public static class WebhookPayloads {
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        // Names like "Congé payé" stay readable. The body is JSON sent to an API, never put into HTML.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public record Person(Guid Id, string Name);

    public record Employee(Guid Id, string? ExternalId, string FirstName, string LastName, string Email) {
        public static Employee From(User user) => new(user.Id, user.ExternalId, user.FirstName, user.LastName, user.Email);
    }

    public record LeaveType(Guid Id, string? ExternalId, IReadOnlyDictionary<string, string> Names) {
        public static LeaveType From(HourType type) => new(type.Id, type.ExternalId, type.LocalizedNames);
    }

    /// <param name="Days">
    /// The working days booked, each with its hours from the work schedule (weekends and public
    /// holidays left out). Only on leave.approved, the moment they're booked; null on other events.
    /// </param>
    public record Leave(
        Guid Id, string Status, LeaveType Type, DateOnly StartDate, DateOnly EndDate,
        string FirstDayPart, string LastDayPart,
        decimal Hours, string? Comment, string? ReviewComment, IReadOnlyList<BookedDay>? Days);

    /// <summary>One working day of leave, local time.</summary>
    public record BookedDay(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime) {
        public static BookedDay From(LeaveDay day) => new(day.Date, day.StartTime, day.EndTime);
    }

    public static object LeaveEvent(
        Guid eventId, string eventType, DateTime occurredAtUtc, User actor,
        LeaveRequest request, User requester, HourType type, IReadOnlyList<LeaveDay>? days = null
    ) => new {
        id = eventId,
        @event = eventType,
        occurredAtUtc,
        actor = new Person(actor.Id, $"{actor.FirstName} {actor.LastName}"),
        employee = Employee.From(requester),
        leave = new Leave(
            request.Id, request.Status.ToString(), LeaveType.From(type), request.StartDate, request.EndDate,
            request.FirstDayPart.ToString(), request.LastDayPart.ToString(),
            request.Hours, request.Comment, request.ReviewComment, days?.Select(BookedDay.From).ToList())
    };

    /// <summary>"Send test": shaped like a leave event, with made-up values.</summary>
    public static object TestEvent(Guid eventId, DateTime occurredAtUtc, string? actorName) => new {
        id = eventId,
        @event = WebhookEvents.Test,
        occurredAtUtc,
        actor = new Person(Guid.Empty, actorName ?? "Test"),
        employee = new Employee(Guid.Empty, "EMP-0042", "Jonathan", "Example", "jonathan@example.com"),
        leave = new Leave(
            Guid.Empty, "Approved",
            new LeaveType(Guid.Empty, "PTO", new Dictionary<string, string> { ["en"] = "Paid time off" }),
            DateOnly.FromDateTime(occurredAtUtc), DateOnly.FromDateTime(occurredAtUtc).AddDays(4),
            "Full", "Full", 38m, null, null,
            Enumerable.Range(0, 5)
                .Select(i => new BookedDay(DateOnly.FromDateTime(occurredAtUtc).AddDays(i), new TimeOnly(9, 0), new TimeOnly(17, 0)))
                .ToList())
    };

    /// <summary>Language-neutral, for the "Recent calls" list: "Eva Peeters · 2026-11-16 – 2026-11-20".</summary>
    public static string LeaveSummary(User requester, LeaveRequest request) {
        string Day(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var period = request.StartDate == request.EndDate
            ? Day(request.StartDate)
            : $"{Day(request.StartDate)} – {Day(request.EndDate)}";
        return $"{requester.FirstName} {requester.LastName} · {period}";
    }

    public static string Serialize(object payload) => JsonSerializer.Serialize(payload, Json);
}
