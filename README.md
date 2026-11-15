[![](https://img.shields.io/nuget/v/soenneker.calendly.blazor.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.calendly.blazor/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.calendly.blazor/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.calendly.blazor/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.calendly.blazor.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.calendly.blazor/)
[![](https://img.shields.io/badge/Demo-Live-blueviolet?style=for-the-badge&logo=github)](https://soenneker.github.io/soenneker.calendly.blazor)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.calendly.blazor/codeql.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.calendly.blazor/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Calendly.Blazor
### A Blazor interop library for Calendly scheduling widgets

## Installation

```bash
dotnet add package Soenneker.Calendly.Blazor
```

## Setup

```csharp
using Soenneker.Calendly.Blazor.Registrars;

builder.Services.AddCalendlyAsScoped();
```

The scoped service lazily loads Calendly's hosted JavaScript and stylesheet. No API key or manual script tags are required. Use an interactive Blazor component; JavaScript cannot run during prerendering. Your content security policy must permit Calendly scripts, styles, and frames.

## Popup

```razor
@using Soenneker.Calendly.Blazor.Abstract
@inject ICalendly Calendly

<button @onclick="BookDemo">Book a demo</button>

@code {
    private async Task BookDemo() => await Calendly.OpenPopup(new()
    {
        Url = "https://calendly.com/your-account/demo",
        Prefill = new() { Name = "Jane Doe", Email = "jane@example.com" },
        Utm = new() { UtmSource = "website", UtmCampaign = "demo" }
    });
}
```

Use `ClosePopup()` to dismiss it. The URL may include Calendly appearance parameters such as `hide_event_type_details=1`, `background_color`, `text_color`, and `primary_color`.

## Inline component

```razor
@using Soenneker.Calendly.Blazor.Components
@using Soenneker.Calendly.Blazor.Options
@using Soenneker.Calendly.Blazor.Dtos

<CalendlyInline Options="_options" OnEventScheduled="Booked" />

@code {
    private readonly CalendlyInlineOptions _options = new()
    {
        Url = "https://calendly.com/your-account/demo",
        Resize = true
    };

    private Task Booked(CalendlyEvent booking)
    {
        // booking.EventUri and booking.InviteeUri identify the booking.
        return Task.CompletedTask;
    }
}
```

The component initializes after rendering, refreshes when options change, and removes its widget on disposal. Set `Style` to change its initial dimensions (default: 320px minimum width and 700px height). Only one resizing widget is allowed per page. Its generated HTML ID is reserved.

For manual control, call `CreateInline(elementId, options)` from `OnAfterRenderAsync` with an empty dedicated container, and call `DestroyInline(elementId)` when finished.

## Floating badge

```csharp
await Calendly.CreateBadge(new()
{
    Url = "https://calendly.com/your-account/demo",
    Text = "Book a demo",
    Color = "#0069ff",
    TextColor = "#ffffff",
    Branding = true
});

// On navigation or component disposal:
await Calendly.DestroyBadge();
```

Calendly supports one popup and badge per page. Calls replace the current widget owned by the service; a different service cannot overwrite it. Shared SDK assets remain loaded when widgets are removed.

## Prefills and notifications

`CalendlyPrefill` supports a full name or separate first/last names, email, and custom answers keyed `a1` through `a10`. `Guests` supplies additional guest email addresses. `CalendlyUtm` supports source, medium, campaign, content, and term (255 characters each).

Subscribe to the service's `ProfilePageViewed`, `EventTypeViewed`, `DateAndTimeSelected`, and `EventScheduled` asynchronous events. Unsubscribe when your component is disposed. UI updates from a service event should use the component's `InvokeAsync` dispatcher. Each notification includes the original `Payload` and an `ElementId` for inline widgets (null for popups/badges). The inline component's `OnEventScheduled` callback is already dispatched and filtered to its widget.

Notifications are accepted only from owned Calendly frames with a matching origin. Browser events are UI notifications, not authoritative booking verification; use Calendly webhooks for server-side confirmation.

Scoped service disposal removes owned widgets, message listeners, and .NET callback references. Consumers should remove their widgets on navigation without disposing the shared service.

## Demo and verification

Run the demo project and enter your public Calendly scheduling URL. The demo supports prefills, all three widget types, removal, and booking notifications.

```shell
dotnet run --project test/Soenneker.Calendly.Blazor.Demo
dotnet test --project test/Soenneker.Calendly.Blazor.Tests -- --treenode-filter "/*/*/CalendlyTests/*"
node --test test/Soenneker.Calendly.Blazor.Tests/calendlyinterop.test.mjs
```
