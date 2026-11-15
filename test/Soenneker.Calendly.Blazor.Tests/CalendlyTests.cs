using System;
using System.Threading.Tasks;
using Soenneker.Calendly.Blazor.Abstract;
using Soenneker.Calendly.Blazor.Dtos;
using Soenneker.Calendly.Blazor.Options;
using Soenneker.Tests.HostedUnit;
namespace Soenneker.Calendly.Blazor.Tests;
[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class CalendlyTests : HostedUnitTest
{
    private readonly ICalendly _calendly;
    public CalendlyTests(Host host) : base(host) => _calendly = Resolve<ICalendly>(true);

    [Test]
    public async Task RejectsInvalidOptionsBeforeBrowserInterop()
    {
        foreach (string url in new[] { "", "javascript:alert(1)", "http://calendly.com/demo", "https://calendly.com.evil.test/demo", "https://user@calendly.com/demo", "https://calendly.com:8443/demo" })
            await Reject(() => _calendly.OpenPopup(new() { Url = url }));
        await Reject(() => _calendly.CreateInline("", new() { Url = "https://calendly.com/demo" }));
        await Reject(() => _calendly.CreateBadge(new() { Url = "https://calendly.com/demo", Text = " " }));
        await Reject(() => _calendly.OpenPopup(new() { Url = "https://calendly.com/demo", Prefill = new() { CustomAnswers = new() { ["a11"] = "No" } } }));
        await Reject(() => _calendly.OpenPopup(new() { Url = "https://calendly.com/demo", Utm = new() { UtmSource = new string('x', 256) } }));
    }

    [Test]
    public async Task DispatchesBookingPayloadAndAwaitsAllSubscribers()
    {
        int calls = 0;
        Func<CalendlyEvent, Task> first = async data =>
        {
            await Task.Yield();
            if (data.EventUri != "https://api.calendly.com/scheduled_events/123" || data.InviteeUri != "https://api.calendly.com/invitees/456" || data.ElementId != "inline")
                throw new InvalidOperationException("Booking payload was lost.");
            calls++;
        };
        Func<CalendlyEvent, Task> second = _ => { calls++; return Task.CompletedTask; };
        _calendly.EventScheduled += first;
        _calendly.EventScheduled += second;
        try
        {
            await ((Calendly)_calendly).ReceiveEvent("calendly.event_scheduled", """{"elementId":"inline","payload":{"event":{"uri":"https://api.calendly.com/scheduled_events/123"},"invitee":{"uri":"https://api.calendly.com/invitees/456"}}}""");
            await ((Calendly)_calendly).ReceiveEvent("unknown", "{}");
            if (calls != 2) throw new InvalidOperationException("Callbacks were not awaited exactly once.");
        }
        finally { _calendly.EventScheduled -= first; _calendly.EventScheduled -= second; }
    }
    private static async Task Reject(Func<ValueTask> operation)
    {
        try { await operation(); }
        catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid options were accepted.");
    }
}
