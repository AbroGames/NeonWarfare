using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.World.Data.PersistenceData;
using NeonWarfare.Scenes.World.Data.TemporaryData;
using NeonWarfare.Scenes.World.Scenes.ClientScenes;
using NeonWarfare.Scenes.World.Scenes.SyncedScenes;
using NeonWarfare.Scenes.World.Service;
using NeonWarfare.Scenes.World.Service.Command;
using NeonWarfare.Scenes.World.Service.DataSerializer;
using NeonWarfare.Scenes.World.Service.Performance;
using NeonWarfare.Scenes.World.Service.PersistenceFactory;
using NeonWarfare.Scenes.World.Service.StartStop;
using NeonWarfare.Scenes.World.Tree;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai.Impl;
using NeonWarfare.Scenes.World.Service.Chat;
using Serilog;

namespace NeonWarfare.Scenes.World;

/// <summary>
/// Является хранилищем сервисов. Каждый сервис может ссылаться на другие сервисы.
/// Каждый сервис является точкой взаимодействия с системой, и при вызове методов должен гарантировать,
/// что он внесёт изменения и в другие сервисы, чтобы сохранить целостность состояния системы.
/// </summary>
public partial class World : Node2D, IServiceProvider
{
    
    [Child] public WorldTree Tree { get; private set; }
    [Child] public WorldPersistenceData PersistenceData { get; private set; }
    [Child] public WorldTemporaryData TemporaryData { get; private set; }
    
    [Child] public PersistenceNodesFactoryService FactoryService { get; private set; }
    [Child] public WorldMultiplayerSpawnerService MultiplayerSpawnerService { get; private set; }
    [Child] public WorldServerStartStopService ServerStartStopService { get; private set; }
    [Child] public WorldClientStartStopService ClientStartStopService { get; private set; }
    [Child] public WorldSynchronizerService SynchronizerService { get; private set; }
    [Child] public WorldDataSaveLoadService DataSaveLoadService { get; private set; }
    [Child] public WorldDataSerializerService DataSerializerService { get; private set; }
    [Child] public WorldPerformanceService PerformanceService { get; private set; }
    [Child] public WorldChatService ChatService { get; private set; }
    [Child] public WorldCommandService CommandService { get; private set; }
    [Child] public WorldFacadeService FacadeService { get; private set; }
    
    [Child] public SyncedPackedScenes SyncedPackedScenes { get; private set; }
    [Child] public ClientPackedScenes ClientPackedScenes { get; private set; }
    
    private readonly Dictionary<Type, object> _services = new();
    [Logger] private ILogger _log;

    public override void _EnterTree() 
    {
        Di.Process(this);
        
        AddService(Tree);
        AddService(PersistenceData);
        AddService(TemporaryData);
        
        AddService(FactoryService);
        AddService(MultiplayerSpawnerService);
        AddService(ServerStartStopService);
        AddService(ClientStartStopService);
        AddService(SynchronizerService);
        AddService(DataSaveLoadService);
        AddService(DataSerializerService);
        AddService(PerformanceService);
        AddService(ChatService);
        AddService(CommandService);
        AddService(FacadeService);
        
        AddService(SyncedPackedScenes);
        AddService(ClientPackedScenes);
    }

    public object GetService(Type serviceType)
    {
        return _services.GetValueOrDefault(serviceType, null);
    }
    
    private void AddService(object service)
    {
        if (_services.ContainsKey(service.GetType()))
        {
            _log.Warning("Service by type {type} already exists", service.GetType().Name);
            return;
        }
        
        _services.Add(service.GetType(), service);
    }

    //TODO Test methods. Remove after tests.
    public void Test1() => RpcId(ServerId, MethodName.Test1Rpc);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Test1Rpc()
    {
        _log.Warning("Test 1 RPC called");
        
        Tree.MapSurface.AddBotCharacter(100, 100, new AiBattleControllerLogic
        {
            Direction = Vector2.Right
        });
        
        /*Tree.MapSurface.AddChildWithUniqueName(FactoryService.Create<MapPoint, MapPointData>(data =>
        {
            data.PositionX = Random.Shared.Next(0, 600);
            data.PositionY = Random.Shared.Next(0, 600);
        }), "MapPoint");*/
    }
    
    public void Test2() => RpcId(ServerId, MethodName.Test2Rpc);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Test2Rpc()
    {
        _log.Warning("Test 2 RPC called");
        
        Character bot = Tree.MapSurface.AddBotCharacter(1000, 100, new AiBattleControllerLogic
        {
            Direction = Vector2.Left
        });
        bot.Mass *= 10;
        bot.Controller.ForceCoef *= 4;
    }
    
    public void Test3() => RpcId(ServerId, MethodName.Test3Rpc);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Test3Rpc()
    {
        _log.Warning("Test 3 RPC called");
        
        for (int i = 0; i < 50; i++)
        {
            Tree.MapSurface.AddBotCharacter(500, 500, new AiMoveControllerLogic
            {
                TargetPosition = new Vector2(500, 500)
            });
        }
    }
}