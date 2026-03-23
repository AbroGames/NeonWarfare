using System;
using Godot;

namespace NeonWarfare.Scenes.World.Service.PersistenceFactory;

public interface IPersistenceNodeFactory
{
    public Type CreatedType();
    public void InitFactory(World world);
}

public interface IPersistenceNodeFactory<TNode, TData> : IPersistenceNodeFactory where TNode : Node
{
    public TNode Create(Action<TData> init);
}