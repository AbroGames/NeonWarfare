using KludgeBox.Core;
using KludgeBox.Core.Random;
using KludgeBox.DI;
using KludgeBox.DI.Requests;
using KludgeBox.Godot.Services;
using KludgeBox.Reflection.Access;
using NeonWarfare.Scripts.GlobalServices;
using NeonWarfare.Scripts.GlobalServices.KnownServers;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;
using NeonWarfare.Scripts.GlobalServices.Settings;
using TypesMappingService = NeonWarfare.Scripts.GlobalServices.TypesMappingService;

namespace NeonWarfare.Scripts;

public static class Services
{
    // Services from KludgeBox
    public static readonly DependencyInjector Di = new(RequestsScanner.CreateDefault());
    public static readonly ExceptionHandlerService ExceptionHandler = new();
    public static readonly RandomService Rand = new();
    public static readonly MathService Math = new();
    public static readonly StringCompressionService StringCompression = new();
    public static readonly NodeTreeService NodeTree = new();
    public static readonly AssemblyCacheService AssemblyCache = new();
    public static readonly I18NService I18N = new();
    public static readonly AutoScalingService AutoScaling = new();
    public static MembersScanner MembersScanner => Di.MembersScanner;
    
    // Services from game, but extended KludgeBox services
    public static readonly TypesMappingService TypesMapping = new();
    
    // Services from game
    public static readonly QuitRequestsService QuitRequests = new();
    public static readonly ProcessService Process = new();
    public static readonly LoadingScreenService LoadingScreen = new();
    public static readonly MainSceneService MainScene = new();
    public static readonly GameSettingsService GameSettings = new();
    public static readonly DedicatedServerSettingsService DedicatedServerSettings = new();
    public static readonly MenuGameSettingsService MenuGameSettings = new();
    public static readonly ResumableGameService LastGame = new();
    public static readonly KnownServersService KnownServers = new();
    public static readonly SaveLoadService SaveLoad = new();
    public static readonly IconsStorageService IconsStorage = new();
    
    public static class Global
    {
        public static DependencyInjector Di => Services.Di;
    }
}