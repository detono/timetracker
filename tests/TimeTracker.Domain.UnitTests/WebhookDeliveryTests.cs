using FluentAssertions;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;
using Xunit;

namespace TimeTracker.Domain.UnitTests;

public class WebhookDeliveryTests {
    private static readonly DateTime Now = new(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);

    private static WebhookDelivery NewDelivery() =>
        WebhookDelivery.Create(Guid.NewGuid(), Guid.NewGuid(), "leave.approved", "Eva", "{}", Now);

    [Fact]
    public void Failures_AreRetriedWithGrowingGaps_ThenGivenUp() {
        var delivery = NewDelivery();
        var waits = new List<TimeSpan>();

        for (var i = 0; i < WebhookDelivery.MaxAttempts - 1; i++) {
            delivery.RecordFailure(Now, 503, "down");
            delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
            waits.Add(delivery.NextAttemptAtUtc!.Value - Now);
        }

        waits.Should().Equal(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(2), TimeSpan.FromHours(6));

        delivery.RecordFailure(Now, 503, "still down");
        delivery.Status.Should().Be(WebhookDeliveryStatus.Failed);
        delivery.Attempts.Should().Be(6);
        delivery.NextAttemptAtUtc.Should().BeNull();
    }

    [Fact]
    public void SendAgain_StartsOverRightAway() {
        var delivery = NewDelivery();
        delivery.RecordFailure(Now, 400, "bad", retry: false);
        delivery.Status.Should().Be(WebhookDeliveryStatus.Failed);

        delivery.Retry(Now.AddHours(1));

        delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        delivery.Attempts.Should().Be(0);
        delivery.NextAttemptAtUtc.Should().Be(Now.AddHours(1));
    }

    [Fact]
    public void LongErrors_AreCut() {
        var delivery = NewDelivery();
        delivery.RecordFailure(Now, 500, new string('x', 5000));
        delivery.LastError.Should().HaveLength(WebhookDelivery.MaxErrorLength);
    }
}
