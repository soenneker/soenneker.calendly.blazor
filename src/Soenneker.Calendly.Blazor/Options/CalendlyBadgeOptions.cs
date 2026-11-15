namespace Soenneker.Calendly.Blazor.Options;
/// <summary>Options for the floating scheduling badge.</summary>
public sealed class CalendlyBadgeOptions : CalendlyOptions
{
    /// <summary>Visible button label.</summary>
    public string Text { get; set; } = "Book a demo";
    /// <summary>Button background color.</summary>
    public string Color { get; set; } = "#0069ff";
    /// <summary>Button text color.</summary>
    public string TextColor { get; set; } = "#ffffff";
    /// <summary>Whether to show Calendly branding, subject to the Calendly plan.</summary>
    public bool Branding { get; set; } = true;
}
