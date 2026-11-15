using System.Collections.Generic;
namespace Soenneker.Calendly.Blazor.Options;
/// <summary>Booking form prefills. Use Name or FirstName/LastName to match the event's form.</summary>
public sealed class CalendlyPrefill
{
    /// <summary>Invitee full name.</summary>
    public string? Name { get; set; }
    /// <summary>Invitee first name for split-name forms.</summary>
    public string? FirstName { get; set; }
    /// <summary>Invitee last name for split-name forms.</summary>
    public string? LastName { get; set; }
    /// <summary>Invitee email address.</summary>
    public string? Email { get; set; }
    /// <summary>Custom answers keyed a1 through a10.</summary>
    public Dictionary<string, string>? CustomAnswers { get; set; }
}
