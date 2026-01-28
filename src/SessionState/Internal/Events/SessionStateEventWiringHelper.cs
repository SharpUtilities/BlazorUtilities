using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SessionState.Core.Events;

namespace SessionState.Internal.Events;

/// <summary>
/// Helper for wiring up event handlers during app configuration.
/// </summary>
internal static class SessionStateEventWiringHelper
{
    public static void WireUpEvents(IServiceProvider services)
    {
        var dispatcher = services.GetRequiredService<SessionStateEventDispatcher>();
        var options = services.GetRequiredService<IOptions<SessionStateEventOptions>>().Value;

        if (options.OnValueSet is not null)
        {
            dispatcher.ValueSet += (_, args) => options.OnValueSet(args);
        }

        if (options.OnValueCleared is not null)
        {
            dispatcher.ValueCleared += (_, args) => options.OnValueCleared(args);
        }

        if (options.OnValueRefreshed is not null)
        {
            dispatcher.ValueRefreshed += (_, args) => options.OnValueRefreshed(args);
        }

        if (options.OnValueChanged is not null)
        {
            dispatcher.ValueChanged += (_, args) => options.OnValueChanged(args);
        }

        if (options.OnBackendOperation is not null)
        {
            dispatcher.BackendOperation += (_, args) => options.OnBackendOperation(args);
        }
    }
}
