using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.Screen;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scenes.Game.ServerGame.ServerCommandsService;

public class CommandsService
{
    public IReadOnlyList<Command> Commands => _commands;
    public IReadOnlyDictionary<string, Command> CommandsByName => _commandsByName;
    public IReadOnlyDictionary<string, Command> CommandsByAlias => _commandsByAlias;
    
    private readonly List<Command> _commands = new();
    private readonly Dictionary<string, Command> _commandsByName = new();
    private readonly Dictionary<string, Command> _commandsByAlias = new(); // У алиасов приоритет ниже чем у основных имен, так что основные имена не могут быть перезаписаны алиасами
    private readonly Color _errorColor = Colors.Red.Lightened(0.1f);

    public void TryExecuteCommand(ChatMessage message)
    {
        CommandArgs commandArgs;
        try
        {
            commandArgs = new CommandArgs(message.MessageText);
        }
        catch (Exception ex)
        {
            ServerRoot.Instance.Game.SendMessageTo(message.SenderInfo.AuthorId, $"[color={_errorColor.ToHtml()}]Ошибка при парсинге команды: '/{ex.Message}'[/color]");
            return;
        }

        if (!_commandsByName.TryGetValue(commandArgs.Command.ToLower(), out var command)
            && !_commandsByAlias.TryGetValue(commandArgs.Command.ToLower(), out command))
        {
            ServerRoot.Instance.Game.SendMessageTo(message.SenderInfo.AuthorId, $"[color={_errorColor.ToHtml()}]Команда '/{commandArgs.Command.ToLower()}' не найдена.[/color]");
            return;
        }

        if (command.IsAdminOnly && !message.SenderInfo.IsAdmin)
        {
            ServerRoot.Instance.Game.SendMessageTo(message.SenderInfo.AuthorId, $"[color={_errorColor.ToHtml()}]У вас нет прав на выполнение команды '/{commandArgs.Command.ToLower()}'.[/color]");
        }
        
        command.Action.Invoke(command, commandArgs);
    }

    
    
    public void RegisterCommand(Command command)
    {
        if (!_commandsByName.TryAdd(command.Name.ToLower(), command))
        {
            Log.Error($"Unable to register command '{command.Name}': Command with the same name already exists.");
            return;
        }
        _commands.Add(command);
        foreach (var alias in command.Aliases)
        {
            var lowerAlias = alias.ToLower();
            if (_commandsByName.ContainsKey(lowerAlias))
            {
                Log.Warning($"Attempt to register the command '{command.Name}' with alias '{lowerAlias}', while the command with main name '{lowerAlias}' is already registered.\n" +
                            $"The alias will be ignored as long as another command with that name exists");
            }

            if (!_commandsByAlias.TryAdd(lowerAlias, command))
            {
                Log.Warning($"Attempt to register the command '{command.Name}' with alias '{lowerAlias}', while that alias is already registered. Ignoring.");
            }
        }
    }
}