using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller.Ai.Impl;
using NeonWarfare.Scenes.World.Data;
using NeonWarfare.Scenes.World.Data.MapPoint;
using NeonWarfare.Scenes.World.PackedScenes;
using NeonWarfare.Scenes.World.Services;
using NeonWarfare.Scenes.World.Services.PersistenceFactory;
using NeonWarfare.Scenes.World.Tree;
using NeonWarfare.Scenes.World.Tree.Entity.Building;
using Serilog;
using WorldStartStopService = NeonWarfare.Scenes.World.Services.StartStop.WorldStartStopService;

namespace NeonWarfare.Scenes.World;

public partial class World : Node2D
{
    
    [Child] public WorldTree Tree { get; private set; }
    [Child] public WorldPersistenceData Data { get; private set; }
    [Child] public PersistenceNodesFactoryService Factory { get; private set; }
    [Child] public WorldTemporaryDataService TemporaryDataService { get; private set; }
    [Child] public WorldStartStopService StartStopService  { get; private set; }
    [Child] public WorldMultiplayerSpawnerService MultiplayerSpawnerService { get; private set; }
    
    [Child] public WorldPackedScenes WorldPackedScenes { get; private set; }
    [Child] public ClientPackedScenes ClientPackedScenes { get; private set; }
    
    public readonly WorldEvents Events = new();
    
    [Logger] private ILogger _log;

    public override void _EnterTree() 
    {
        Di.Process(this);
    }
    
    //TODO Test methods. Remove after tests.
    public void Test1() => RpcId(ServerId, MethodName.Test1Rpc);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Test1Rpc()
    {
        _log.Warning("Test 1 RPC called");
        
        //TODO del
        Tree.MapSurface.AddBotCharacter(100, 100, new AiBattleControllerLogic
        {
            Direction = Vector2.Right
        });
        
        /*Tree.MapSurface.AddChildWithUniqueName(Factory.Create<MapPoint, MapPointData>(data =>
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
        
        //TODO del
        Tree.MapSurface.AddBotCharacter(1000, 100, new AiBattleControllerLogic
        {
            Direction = Vector2.Left
        });
    }
    
    public void Test3() => RpcId(ServerId, MethodName.Test3Rpc);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Test3Rpc()
    {
        _log.Warning("Test 3 RPC called");

        //TODO del
        for (int i = 0; i < 50; i++)
        {
            Tree.MapSurface.AddBotCharacter(500, 500, new AiMoveControllerLogic
            {
                TargetPosition = new Vector2(500, 500)
            });
        }
    }
    
    //TODO Переделать на нормальный метод запроса сохранения с клиента на сервер, с проверкой прав
    public void TestSave(string saveFileName) => RpcId(ServerId, MethodName.TestSaveRpc, saveFileName);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void TestSaveRpc(string saveFileName)
    {
        _log.Warning("TestSave RPC called");
        Data.SaveLoad.Save(saveFileName);
    }
}