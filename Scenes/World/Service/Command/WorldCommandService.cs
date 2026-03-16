using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using NeonWarfare.Scenes.World.Service.Chat;
using NeonWarfare.Scenes.World.Service.Command.Impl;
using Humanizer;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.ParentInjection;
using KludgeBox.DI.Requests.SceneServiceInjection;
using Serilog;

namespace NeonWarfare.Scenes.World.Service.Command;

public partial class WorldCommandService : Node
{

    private const string RequireAdminMessage = "Command '{0}' requires admin status.";
    
    public IReadOnlyDictionary<string, ICommandProcessor> CommandProcessorByCommand => _commandProcessorByCommand;
    private readonly Dictionary<string, ICommandProcessor> _commandProcessorByCommand = new();
    private readonly NotFoundCommand _notFoundCommand = new();
    private ChatMessageCommandInterceptor _chatInterceptor;
    
    [Parent] private World _world;
    [SceneService] private WorldChatService _chatService;
    [SceneService] private WorldFacadeService _facadeService;
    [Logger] ILogger _log;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void InitOnServer()
    {
        if (!Net.IsServer()) throw new InvalidOperationException("Can only be executed on the server");
        
        InitCommandProcessors();
        InitInterceptorForChat();
    }

    private void InitCommandProcessors()
    {
        // Get all classes implementing ICommandProcessor
        List<IGrouping<string, ICommandProcessor>> commandGroups = Services.AssemblyCache.GetTypes(Assembly.GetExecutingAssembly())
            .Where(t => typeof(ICommandProcessor).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .Select(t => (ICommandProcessor) Activator.CreateInstance(t))
            .GroupBy(t => t.GetCommand().ToLower())
            .ToList();
        
        // Check ICommandProcessors for duplicate commands
        foreach (IGrouping<string, ICommandProcessor> group in commandGroups)
        {
            // Materialize group to avoid multiple enumerations
            List<ICommandProcessor> processorsList = group.ToList(); 

            if (processorsList.Count > 1)
            {
                string conflictingTypes = string.Join(", ", processorsList.Select(p => p.GetType().Name));
                _log.Warning("Duplicate command '{command}' found in types: {conflictingTypes}.", group.Key, conflictingTypes); 
            }

            _commandProcessorByCommand.Add(group.Key, processorsList.First());
        }
        
        _log.Information("Added {count} chat commands.", _commandProcessorByCommand.Count);
    }

    private void InitInterceptorForChat()
    {
        if (_chatInterceptor != null && _chatService.HasInterceptor(_chatInterceptor))
        {
            _chatService.RemoveInterceptor(_chatInterceptor);
        }
        
        _chatInterceptor = new ChatMessageCommandInterceptor(ProcessCommand);
        _chatService.AddInterceptor(_chatInterceptor);
    }

    private void ProcessCommand(int senderId, string command)
    {
        string commandWithoutParams = command.Split(' ')[0].ToLower();
        if (_commandProcessorByCommand.TryGetValue(commandWithoutParams, out var processor))
        {
            if (processor.IsRequiringAdmin() && !_facadeService.IsAdmin(senderId))
            {
                _chatService.TrySendNewMessage(RequireAdminMessage.FormatWith(commandWithoutParams), senderId);
            }
            else
            {
                processor.ProcessCommand(senderId, command, _world);
            }
        }
        else
        {
            _notFoundCommand.ProcessCommand(senderId, command, _world);
        }
    }
}