using System.Text.Json;
namespace Soenneker.Calendly.Blazor.Dtos;
/// <summary>A notification from a widget owned by this service. Browser notifications are not server-side booking verification.</summary>
public sealed class CalendlyEvent
{
    /// <summary>The inline container ID, or null for a popup or badge.</summary>
    public string? ElementId { get; set; }
    /// <summary>The original Calendly event payload.</summary>
    public JsonElement Payload { get; set; }
    /// <summary>The scheduled event URI when supplied by an event-scheduled notification.</summary>
    public string? EventUri => ReadUri("event");
    /// <summary>The invitee URI when supplied by an event-scheduled notification.</summary>
    public string? InviteeUri => ReadUri("invitee");
    private string? ReadUri(string name) => Payload.ValueKind == JsonValueKind.Object &&
        Payload.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Object &&
        value.TryGetProperty("uri", out JsonElement uri) && uri.ValueKind == JsonValueKind.String ? uri.GetString() : null;
}
