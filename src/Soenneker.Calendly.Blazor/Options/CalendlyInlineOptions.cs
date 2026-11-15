namespace Soenneker.Calendly.Blazor.Options;
/// <summary>Options for an inline widget.</summary>
public sealed class CalendlyInlineOptions : CalendlyOptions
{
    /// <summary>Enables Calendly auto-resizing. Only one auto-resizing widget is supported per page.</summary>
    public bool Resize { get; set; }
}
