using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame.Ping;
using NeonWarfare.Scenes.Game.ServerGame;
using NeonWarfare.Scripts.KludgeBox.Events.Global;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;
using NeonWarfare.Scripts.Utils.Components;

namespace NeonWarfare.Scripts.KludgeBox.Networking;

/// <summary>
/// Describes logic for obtaining instance by its identifier.
/// </summary>
public delegate object InstanceResolverDelegate(object instanceIdentifier);
public partial class Network : Node
{
	public static Network Instance { get; private set; }
	
	public const string DefaultHost = "127.0.0.1";
	public const int DefaultPort = 25566;
	
	public const long BroadcastId = 0;
	public const long ServerId = 1;
	
	private Dictionary<Type, InstanceResolverDelegate> _packetTargetResolvers = new();
	private InstanceResolverDelegate _defaultResolver;
	
	public Netmode Mode { get; set; }
	public PacketRegistry PacketRegistry { get; set; } = new PacketRegistry();
	public SceneMultiplayer Api { get; internal set; }

	public ENetMultiplayerPeer Peer
	{
		get => Api.MultiplayerPeer as ENetMultiplayerPeer;
		set => Api.MultiplayerPeer = value;
	}
	
	public enum Netmode
	{
		Client,
		Server
	}

	public bool HasResolver(Type targetInstanceType) => _packetTargetResolvers.ContainsKey(targetInstanceType);
	
	public void AddInstanceResolver(Type targetInstanceType, InstanceResolverDelegate resolver)
	{
		if(resolver is null)
		{
			throw new ArgumentNullException(nameof(resolver));
		}
		
		_packetTargetResolvers.Add(targetInstanceType, resolver);
	}
	
	public void SetDefaultResolver(InstanceResolverDelegate resolver)
	{
		_defaultResolver = resolver;
	}

	public object ResolveInstance(Type targetInstanceType, object instanceIdentifier)
	{
		if (_packetTargetResolvers.TryGetValue(targetInstanceType, out var resolver))
		{
			return resolver(instanceIdentifier);
		}

		if (_defaultResolver != null)
		{
			return _defaultResolver(instanceIdentifier);
		}

		throw new KeyNotFoundException($"Unable to find resolver for type '{targetInstanceType.FullName}'");
	}
	
	
	
	// Called from Root._Ready()
	// Seems like some sort of potential memory leak.
	internal void Initialize(SceneMultiplayer api)
	{
		// Avoid multiple event subscriptions.
		if (Api == api) return;
		
		Instance = this;
		
		Log.Debug("Init network");
		Api = api;
		Api.ServerDisconnected += CloseConnection;
		Api.PeerPacket += OnPacketReceived;
		
		Api.ConnectedToServer += () => EventBus.Publish(new ConnectedToServerEvent());
		Api.ConnectionFailed += () => EventBus.Publish(new ConnectionToServerFailedEvent());
		Api.PeerConnected += id => EventBus.Publish(new PeerConnectedEvent(id));
		Api.PeerDisconnected += id => EventBus.Publish(new PeerDisconnectedEvent(id));
		Api.ServerDisconnected += () => EventBus.Publish(new ServerDisconnectedEvent());
	}
	
	public Error SetServer(int port, int maxClients = 8)
	{
		Log.Debug("Create server network");
		Mode = Netmode.Server;
		PacketRegistry.ScanPackets();

		var peer = new ENetMultiplayerPeer();
		var error = peer.CreateServer(port, maxClients);
		Peer = peer;
		
		return error;
	}

	public Error SetClient(string host, int port)
	{
		Log.Debug("Create client network");
		Mode = Netmode.Client;
		PacketRegistry.ScanPackets();
		
		var peer = new ENetMultiplayerPeer();
		var error = peer.CreateClient(host, port);
		Peer = peer;
		
		return error;
	}

	public void CloseConnection()
	{
		Log.Debug("Close network connection");
		Peer?.Close();
	}
	
	public override void _Notification(int id)
	{
		if (id == NotificationPredelete)
		{
			CloseConnection();
		}
	}
	
	public void SendRaw(long id, byte[] encodedPacketBuffer, MultiplayerPeer.TransferModeEnum mode = MultiplayerPeer.TransferModeEnum.Reliable, int channel = 0)
	{
		Api.SendBytes(encodedPacketBuffer, (int) id, mode, channel);
	}
	
	private void OnPacketReceived(long id, byte[] packet)
	{
		var packetObj = PacketHelper.DecodePacket(packet, PacketRegistry);
		packetObj.SenderId = id;

		Type[] ignoredTypes = [
			typeof(NetworkInertiaComponent.CS_InertiaEntityPacket), 
			typeof(NetworkInertiaComponent.SC_InertiaEntityPacket),
			typeof(ServerGame.CS_PingPacket),
			typeof(PingChecker.SC_PingPacket),
		];
		if (!ignoredTypes.Contains(packetObj.GetType()))
		{
			Log.Info($"Received packet from {id} with {packet.Length - 4} bytes. Received type is {packetObj.GetType().FullName}");
		}

		EventBus.Publish(packetObj);
	}
	
}
