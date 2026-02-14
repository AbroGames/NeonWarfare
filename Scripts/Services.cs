using KludgeBox.Core;
using KludgeBox.Core.Random;
using KludgeBox.DI;
using KludgeBox.Godot.Services;
using KludgeBox.Reflection.Access;
using NeonWarfare.Scripts.Service;
using NeonWarfare.Scripts.Service.Settings;
using CmdArgsService = NeonWarfare.Scripts.Service.CmdArgs.CmdArgsService;
using NetworkService = NeonWarfare.Scripts.Service.NetworkService;

namespace NeonWarfare.Scripts;

public static class Services
{
    // Services from KludgeBox
    public static readonly DependencyInjector Di = new DependencyInjector();
    public static readonly ExceptionHandlerService ExceptionHandler = new ExceptionHandlerService();
    public static readonly RandomService Rand = new RandomService();
    public static readonly MathService Math = new MathService();
    public static readonly StringCompressionService StringCompression = new StringCompressionService();
    public static readonly NodeTreeService NodeTree = new NodeTreeService();
    public static readonly TypesStorageService TypesStorage = new TypesStorageService();
    public static MembersScanner MembersScanner => Di.MembersScanner;
    
    // Services from game, but extended KludgeBox services
    public static readonly CmdArgsService CmdArgs = new CmdArgsService();
    public static readonly NetworkService Net = new NetworkService();
    
    // Services from game
    public static readonly ProcessService Process = new ProcessService();
    public static readonly LoadingScreenService LoadingScreen = new LoadingScreenService();
    public static readonly MainSceneService MainScene = new MainSceneService();
    public static readonly PlayerSettingsService PlayerSettings = new PlayerSettingsService();
    
    public static class Global
    {
        public static DependencyInjector Di => Services.Di;
        public static NetworkService Net => Services.Net;
    }
}