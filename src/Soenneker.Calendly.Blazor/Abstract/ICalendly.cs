using System;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Calendly.Blazor.Options;
using Soenneker.Calendly.Blazor.Dtos;
namespace Soenneker.Calendly.Blazor.Abstract;
/// <summary>Scoped Calendly widgets. Call after interactive rendering; scripts and styles load on first use.</summary>
public interface ICalendly : IAsyncDisposable
{
    /// <summary>Raised when a profile is viewed. Unsubscribe when the consumer is disposed.</summary>
    event Func<CalendlyEvent, Task>? ProfilePageViewed;
    /// <summary>Raised when an event type is viewed.</summary>
    event Func<CalendlyEvent, Task>? EventTypeViewed;
    /// <summary>Raised when an invitee selects a time.</summary>
    event Func<CalendlyEvent, Task>? DateAndTimeSelected;
    /// <summary>Raised when a booking completes. Use a server-side webhook for authoritative confirmation.</summary>
    event Func<CalendlyEvent, Task>? EventScheduled;
    /// <summary>Loads Calendly resources without displaying a widget.</summary>
    ValueTask Initialize(CancellationToken cancellationToken = default);
    /// <summary>Creates or replaces a widget in an empty, dedicated container. The caller controls its dimensions.</summary>
    ValueTask CreateInline(string elementId, CalendlyInlineOptions options, CancellationToken cancellationToken = default);
    /// <summary>Removes a widget created by this service, leaving its container intact.</summary>
    ValueTask DestroyInline(string elementId, CancellationToken cancellationToken = default);
    /// <summary>Opens the page's popup. Fails if another service owns a popup or badge.</summary>
    ValueTask OpenPopup(CalendlyOptions options, CancellationToken cancellationToken = default);
    /// <summary>Closes this service's popup, if present.</summary>
    ValueTask ClosePopup(CancellationToken cancellationToken = default);
    /// <summary>Creates or replaces the page's floating badge. Fails if another service owns a popup or badge.</summary>
    ValueTask CreateBadge(CalendlyBadgeOptions options, CancellationToken cancellationToken = default);
    /// <summary>Removes this service's badge and closes its popup.</summary>
    ValueTask DestroyBadge(CancellationToken cancellationToken = default);
}
