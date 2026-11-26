using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The shape of an <c>[EventHandler]</c>: the dispatcher calls it by reflection, so nothing else may, and it
/// is collected only from the Presentation. One name for all of them keeps "who handles this event" a search
/// for the event type alone.
/// </summary>
[Collection(GameAssembly.Collection)]
public class EventHandlerTests
{
    private const string EventBase = WorldLayers.EventBase;
    private const string HandlerName = "Handle";

    [Fact]
    public void EventHandlers_ArePrivateHandleOfOneEvent()
    {
        FailureReport report = new("[EventHandler] methods of the wrong shape");

        List<MethodDefinition> handlers = Handlers().ToList();
        Assert.NotEmpty(handlers);
        foreach (MethodDefinition method in handlers)
        {
            string where = GameAssembly.Describe(method);
            if (!method.IsPrivate)
            {
                report.Add($"{where}: is not private");
            }
            if (method.IsStatic)
            {
                report.Add($"{where}: is static");
            }
            if (method.Name != HandlerName)
            {
                report.Add($"{where}: is not named {HandlerName}");
            }
            if (method.Parameters.Count != 1)
            {
                report.Add($"{where}: has {method.Parameters.Count} parameters instead of one");
            }
            else if (!IsEventType(method.Parameters[0].ParameterType))
            {
                report.Add($"{where}: takes {method.Parameters[0].ParameterType.FullName}, not a concrete " +
                           $"top-level subclass of {EventBase}");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void EventHandlers_AreDeclaredInPresentation()
    {
        FailureReport report = new("[EventHandler] methods outside the Presentation");

        foreach (MethodDefinition method in Handlers())
        {
            if (WorldLayers.DeclaredLayer(method.DeclaringType) != Layer.Presentation)
            {
                report.Add($"{GameAssembly.Describe(method)}: {GameAssembly.ShortName(method.DeclaringType)} " +
                           "is not marked [Presentation]");
            }
        }

        report.AssertEmpty();
    }

    // The same rule WorldServicesBuilder uses to collect the event types: any other parameter makes Register throw
    private static bool IsEventType(TypeReference type) =>
        GameAssembly.Instance.Find(type) is { IsNested: false, IsAbstract: false } definition
        && WorldLayers.DerivesFrom(definition, EventBase);

    private static IEnumerable<MethodDefinition> Handlers() =>
        GameAssembly.Instance.Types.SelectMany(type => type.Methods).Where(WorldLayers.IsEventHandler);
}
