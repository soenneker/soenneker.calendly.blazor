namespace Soenneker.Calendly.Blazor.Options;
/// <summary>Campaign attribution values, each limited to 255 characters by Calendly.</summary>
public sealed class CalendlyUtm
{
    /// <summary>Campaign name.</summary>
    public string? UtmCampaign { get; set; }
    /// <summary>Traffic source.</summary>
    public string? UtmSource { get; set; }
    /// <summary>Marketing medium.</summary>
    public string? UtmMedium { get; set; }
    /// <summary>Campaign content.</summary>
    public string? UtmContent { get; set; }
    /// <summary>Campaign term.</summary>
    public string? UtmTerm { get; set; }
}
