using System;
using Godot;
using KludgeBox.DI.Requests.DependencyCreation;

namespace NeonWarfare.Scenes.World.Services.PersistenceFactory;

public interface IPersistenceNodeFactory
{
    public Type CreatedType();
    public void InitFactory(World world);
}

public interface IPersistenceNodeFactory<TNode, TData> : IPersistenceNodeFactory where TNode : Node
{
    public TNode Create(Action<TData> init);
}