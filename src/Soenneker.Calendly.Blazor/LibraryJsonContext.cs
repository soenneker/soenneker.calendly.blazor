using System.Text.Json.Serialization;
using Soenneker.Calendly.Blazor.Options;
using Soenneker.Calendly.Blazor.Dtos;
namespace Soenneker.Calendly.Blazor;
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CalendlyOptions))]
[JsonSerializable(typeof(CalendlyInlineOptions))]
[JsonSerializable(typeof(CalendlyBadgeOptions))]
[JsonSerializable(typeof(CalendlyEvent))]
internal partial class LibraryJsonContext : JsonSerializerContext;
