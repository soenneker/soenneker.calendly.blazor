using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Asyncs.Initializers;
using Soenneker.Blazor.Utils.ResourceLoader.Abstract;
using Soenneker.Calendly.Blazor.Abstract;
using Soenneker.Calendly.Blazor.Dtos;
using Soenneker.Calendly.Blazor.Options;
namespace Soenneker.Calendly.Blazor;
public sealed class Calendly : ICalendly
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IResourceLoader _resourceLoader;
    private readonly AsyncInitializer _initializer;
    private readonly CancellationTokenSource _lifetime = new();
    private IJSObjectReference? _module;
    private IJSObjectReference? _session;
    private DotNetObjectReference<Calendly>? _reference;
    private bool _disposed;
    public event Func<CalendlyEvent, Task>? ProfilePageViewed;
    public event Func<CalendlyEvent, Task>? EventTypeViewed;
    public event Func<CalendlyEvent, Task>? DateAndTimeSelected;
    public event Func<CalendlyEvent, Task>? EventScheduled;

    public Calendly(IJSRuntime jsRuntime, IResourceLoader resourceLoader)
    {
        _jsRuntime = jsRuntime;
        _resourceLoader = resourceLoader;
        _initializer = new AsyncInitializer(Load);
    }

    private async ValueTask Load(CancellationToken token)
    {
        await _resourceLoader.LoadStyle("https://assets.calendly.com/assets/external/widget.css", crossOrigin: null, cancellationToken: token);
        await _resourceLoader.LoadScriptAndWaitForVariable("https://assets.calendly.com/assets/external/widget.js", "Calendly", crossOrigin: null, cancellationToken: token);
        try
        {
            _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", token, "./_content/Soenneker.Calendly.Blazor/js/calendlyinterop.js");
            _reference ??= DotNetObjectReference.Create(this);
            _session ??= await _module.InvokeAsync<IJSObjectReference>("create", token, _reference);
        }
        catch (JSDisconnectedException) { throw; }
        catch (JSException ex) { throw new InvalidOperationException("Unable to initialize the Calendly JavaScript wrapper.", ex); }
    }

    public async ValueTask Initialize(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        await _initializer.Init(linked.Token);
    }

    private async ValueTask Invoke(string method, CancellationToken token, params object?[] args)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
        await _initializer.Init(linked.Token);
        try { await _session!.InvokeVoidAsync(method, linked.Token, args); }
        catch (JSDisconnectedException) { throw; }
        catch (JSException ex) { throw new InvalidOperationException($"Calendly operation '{method}' failed.", ex); }
    }

    public ValueTask CreateInline(string elementId, CalendlyInlineOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        Validate(options);
        return Invoke("createInline", cancellationToken, elementId, JsonSerializer.Serialize(options, LibraryJsonContext.Default.CalendlyInlineOptions));
    }
    public ValueTask DestroyInline(string elementId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        return _session is null ? ValueTask.CompletedTask : Invoke("destroyInline", cancellationToken, elementId);
    }
    public ValueTask OpenPopup(CalendlyOptions options, CancellationToken cancellationToken = default)
    {
        Validate(options);
        return Invoke("openPopup", cancellationToken, JsonSerializer.Serialize(options, LibraryJsonContext.Default.CalendlyOptions));
    }
    public ValueTask ClosePopup(CancellationToken cancellationToken = default) =>
        _session is null ? ValueTask.CompletedTask : Invoke("closePopup", cancellationToken);
    public ValueTask CreateBadge(CalendlyBadgeOptions options, CancellationToken cancellationToken = default)
    {
        Validate(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Text);
        return Invoke("createBadge", cancellationToken, JsonSerializer.Serialize(options, LibraryJsonContext.Default.CalendlyBadgeOptions));
    }
    public ValueTask DestroyBadge(CancellationToken cancellationToken = default) =>
        _session is null ? ValueTask.CompletedTask : Invoke("destroyBadge", cancellationToken);

    private static void Validate(CalendlyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Uri.TryCreate(options.Url, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !(uri.Host.Equals("calendly.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".calendly.com", StringComparison.OrdinalIgnoreCase)) ||
            !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Use an HTTPS calendly.com scheduling URL without credentials or a custom port.", nameof(options));
        if (options.Prefill?.CustomAnswers is { } answers)
            foreach (string key in answers.Keys)
                if (key.Length < 2 || key[0] != 'a' || !int.TryParse(key.AsSpan(1), out int index) || index < 1 || index > 10 || key != $"a{index}")
                    throw new ArgumentException("Custom answer keys must be a1 through a10.", nameof(options));
        if (options.Utm is { } utm)
            foreach (string? value in new[] { utm.UtmCampaign, utm.UtmSource, utm.UtmMedium, utm.UtmContent, utm.UtmTerm })
                if (value?.Length > 255)
                    throw new ArgumentException("UTM values cannot exceed 255 characters.", nameof(options));
    }

    [JSInvokable]
    public async Task ReceiveEvent(string name, string json)
    {
        if (_disposed) return;
        Func<CalendlyEvent, Task>? handlers = name switch
        {
            "calendly.profile_page_viewed" => ProfilePageViewed,
            "calendly.event_type_viewed" => EventTypeViewed,
            "calendly.date_and_time_selected" => DateAndTimeSelected,
            "calendly.event_scheduled" => EventScheduled,
            _ => null
        };
        if (handlers is null) return;
        CalendlyEvent data = JsonSerializer.Deserialize(json, LibraryJsonContext.Default.CalendlyEvent)!;
        foreach (Func<CalendlyEvent, Task> handler in handlers.GetInvocationList())
            await handler(data);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _lifetime.CancelAsync();
        await _initializer.DisposeAsync();
        try
        {
            if (_session is not null)
            {
                await _session.InvokeVoidAsync("dispose");
                await _session.DisposeAsync();
            }
            if (_module is not null) await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        finally
        {
            _reference?.Dispose();
            _lifetime.Dispose();
            ProfilePageViewed = null;
            EventTypeViewed = null;
            DateAndTimeSelected = null;
            EventScheduled = null;
        }
    }
}
