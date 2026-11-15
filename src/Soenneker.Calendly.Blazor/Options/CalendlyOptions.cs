using System.Collections.Generic;
namespace Soenneker.Calendly.Blazor.Options;
/// <summary>Options shared by Calendly embeds. Use a public HTTPS calendly.com scheduling URL.</summary>
public class CalendlyOptions
{
    /// <summary>The scheduling URL, including any Calendly appearance query parameters.</summary>
    public required string Url { get; set; }
    /// <summary>Optional invitee details and answers.</summary>
    public CalendlyPrefill? Prefill { get; set; }
    /// <summary>Optional campaign attribution.</summary>
    public CalendlyUtm? Utm { get; set; }
    /// <summary>Email addresses to prefill as additional guests.</summary>
    public List<string>? Guests { get; set; }
}
