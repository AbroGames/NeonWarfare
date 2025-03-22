using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using NeonWarfare.Scripts.KludgeBox.Godot.Nodes;

namespace NeonWarfare.Scripts.KludgeBox.Godot.Extensions;

public static class NodeExtensions
{
    /// <summary>
    /// Shortcut for GodotObject.IsInstanceValid(object)
    /// </summary>
    /// <param name="gdObj"></param>
    /// <returns></returns>
    public static bool IsValid(this GodotObject gdObj)
    {
        return GodotObject.IsInstanceValid(gdObj);
    }
    
    /// <summary>
    /// Extension method that creates and returns a Destructor node to handle delayed removal of a given node.
    /// If the delay is less than or equal to 0, the node is immediately removed using QueueFree().
    /// </summary>
    /// <param name="node">The node to be destructed.</param>
    /// <param name="delay">The delay (in seconds) before the node is removed. Default is 0.</param>
    /// <returns>The created Destructor node, or null if the delay is 0 or negative.</returns>
    public static Destructor Destruct(this Node node, double delay = 0)
    {
        if (delay <= 0)
        {
            node.QueueFree();
            return null;
        }
        var destructor = new Destructor(delay);
        node.AddChild(destructor);
        return destructor;
    }

    /// <summary>
    /// Gets the absolute size of the sprite, taking into account the texture and global scale.
    /// </summary>
    /// <param name="sprite">The Sprite2D object.</param>
    /// <returns>The absolute size of the sprite.</returns>
    public static Vector2 GetAbsoluteSize(this Sprite2D sprite)
    {
        var texture = sprite.Texture;
        if (!GodotObject.IsInstanceValid(texture))
            return new Vector2();
        var size = sprite.Texture.GetSize();
        var scale = sprite.GlobalScale;

        return new Vector2(size.X * scale.X, size.Y * scale.Y);
    }

    /// <summary>
    /// Sets the absolute scale of the sprite based on the desired size.
    /// </summary>
    /// <param name="sprite">The Sprite2D object.</param>
    /// <param name="size">The desired absolute size of the sprite.</param>
    public static void SetAbsoluteScale(this Sprite2D sprite, Vector2 size)
    {
        var textureSize = sprite.Texture.GetSize();
        sprite.Scale = new Vector2(size.X / textureSize.X, size.Y / textureSize.Y);
    }
    
    /// <summary>
    /// Creates a Dummy2D object dropped at the position of the Node2D object.
    /// </summary>
    /// <param name="node">The Node2D object.</param>
    /// <param name="keepAlive">If true, the Dummy2D object will not be despawned when empty.</param>
    /// <returns>The created Dummy2D object or null if it couldn't be created.</returns>
    public static Dummy2D DropDummy(this Node2D node, bool keepAlive = false)
    {
        var parent = node.GetParent();
        if (!parent.IsValid()) return null;

        var dummy = new Dummy2D();
        dummy.Despawn = !keepAlive;
        parent.AddChild(dummy);

        dummy.Position = node.Position;
        dummy.Rotation = node.Rotation;
        dummy.Scale = node.Scale;
        dummy.Modulate = node.Modulate;

        return dummy;
    }

    /// <summary>
    /// Drops the Node2D object, creating a Dummy2D object at its position and reparenting the Node2D to the Dummy2D.
    /// </summary>
    /// <param name="node">The Node2D object to be dropped.</param>
    public static Dummy2D Drop(this Node2D node, bool directly = false)
    {
        var parent = node.GetParent() as Node2D;
        if(!parent.IsValid()) return null;

        if (!directly)
        {
            var dummy = parent.DropDummy(); 
            node.Reparent(dummy);
            node.Owner = dummy;
            return dummy; 
        }

        parent = parent.GetParent() as Node2D;
        if(!parent.IsValid()) return null;
        
        node.Reparent(parent, true);
        node.Owner = parent;
        
        return null;
    }

    /// <summary>
    /// Extension method that reparents the given node to a new parent node.
    /// If the current parent node is valid, the node is reparented to the new parent node using Reparent().
    /// If the current parent node is invalid but the new parent node is valid, the node is added as a child to the new parent.
    /// </summary>
    /// <param name="node">The node to be reparented.</param>
    /// <param name="newParent">The new parent node to reparent to.</param>
    public static void ParentTo(this Node node, Node newParent)
    {
        var currentParent = node.GetParent();

        if (currentParent.IsValid())
        {
            node.Reparent(newParent, true);
            return;
        }

        if (newParent.IsValid())
        {
            newParent.AddChild(node);
            node.Owner = newParent;
        }
    }
    
    /// <summary>
    /// Sets the node as last in the queue for processing (rendering). As a result, it will be drawn on top of all other nodes.
    /// </summary>
    public static void ToForeground(this Node node)
    {
        var parent = node.GetParent();

        if (GodotObject.IsInstanceValid(parent))
        {
            parent.MoveChild(node, -1);
        }
    }

    /// <summary>
    /// Sets the node as first in the queue for processing (rendering). As a result, all other nodes will be drawn on top of it.
    /// </summary>
    public static void ToBackground(this Node node)
    {
        var parent = node.GetParent();

        if (GodotObject.IsInstanceValid(parent))
        {
            parent.MoveChild(node, 0);
        }
    }
    
    /// <summary>
    /// Returns the first child node of the specified type or null if there is no node of that type.
    /// </summary>
    /// <typeparam name="T">The type of the child node to retrieve.</typeparam>
    /// <param name="node">The parent node to search for the child node.</param>
    /// <returns>The first child node of the specified type, or null if no such node is found.</returns>
    public static T GetChild<T>(this Node node) where T : class
    {
        if (node.GetChildCount() < 1)
            return default;

        List<T> children = node.GetChildren<T>();
        return children.FirstOrDefault();
    }

    
    /// <summary>
    /// Retrieves or creates a child of the specified type and adds it as a child to the calling Node.
    /// If a child of the specified type already exists, it is returned; otherwise, a new instance is created and added.
    /// </summary>
    /// <typeparam name="T">The type of the child Node to retrieve or create.</typeparam>
    /// <param name="node">The Node to which the child will be added.</param>
    /// <remarks>
    /// This method is useful for ensuring that a specific type of child Node always exists as a direct child of the calling Node.
    /// If a child of the specified type is already present, it is returned; otherwise, a new instance is created, added, and returned.
    /// </remarks>
    public static T RequireChild<T>(this Node node) where T : Node, new()
    {
        var found = node.GetChild<T>();
        if (found is not null) return found;

        var child = new T();
        node.AddChild(child);
        return child;
    }

    /// <summary>
    /// Returns a list of child nodes of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the child nodes to retrieve.</typeparam>
    /// <param name="node">The parent node to search for the child nodes.</param>
    /// <returns>A list containing all child nodes of the specified type.</returns>
    public static List<T> GetChildren<T>(this Node node) where T : class
    {
        if (node.GetChildCount() < 1)
            return new List<T>();
        
        Array<Node> children = node.GetChildren();
        return children.OfType<T>().ToList();
    }

    /// <summary>
    /// Attempts to find the nearest parent of the specified type. The type can also be an interface.<br/>
    /// Returns null if such a parent is not encountered up to the root.
    /// </summary>
    public static T GetParent<T>(this Node child) where T : class
    {
        var parent = child.GetParent();
        if (parent is null)
            return default;

        if (parent is T)
            return parent as T;

        return parent.GetParent<T>();
    }

    /// <summary>
    /// Этот метод моет быть полезен, когда при удалении одного узла нужно добавить на его место новый. В этой ситуации
    /// может случиться так, что узел удаляется потому, что его родительский узел тоже удаляется. В этот момент попытка добавить на место узла другой
    /// вызовет ошибку. Этот метод подождет до конца кадра и только после этого ПОПЫТАЕТСЯ добавить узел.
    /// </summary>
    public static void TryAddChildDeferred(this Node parent, Node child, Action callback = null)
    {
        Callable.From(() =>
        {
            if (parent.IsValid())
            {
                parent.AddChild(child);
                callback?.Invoke();
            }
        }).CallDeferred();
    }
    
    /// <summary>
    /// Gets the upward vector relative to the given Node2D's rotation.
    /// </summary>
    public static Vector2 Up(this Node2D node) => Vector2.Up.Rotated(node.Rotation);

    /// <summary>
    /// Gets the downward vector relative to the given Node2D's rotation.
    /// </summary>
    public static Vector2 Down(this Node2D node) => Vector2.Down.Rotated(node.Rotation);

    /// <summary>
    /// Gets the leftward vector relative to the given Node2D's rotation.
    /// </summary>
    public static Vector2 Left(this Node2D node) => Vector2.Left.Rotated(node.Rotation);

    /// <summary>
    /// Gets the rightward vector relative to the given Node2D's rotation.
    /// </summary>
    public static Vector2 Right(this Node2D node) => Vector2.Right.Rotated(node.Rotation);

    /// <summary>
    /// Gets the upward vector relative to the global rotation of the given Node2D.
    /// </summary>
    public static Vector2 GlobalUp(this Node2D node) => Vector2.Up.Rotated(node.GlobalRotation);

    /// <summary>
    /// Gets the downward vector relative to the global rotation of the given Node2D.
    /// </summary>
    public static Vector2 GlobalDown(this Node2D node) => Vector2.Down.Rotated(node.GlobalRotation);

    /// <summary>
    /// Gets the leftward vector relative to the global rotation of the given Node2D.
    /// </summary>
    public static Vector2 GlobalLeft(this Node2D node) => Vector2.Left.Rotated(node.GlobalRotation);

    /// <summary>
    /// Gets the rightward vector relative to the global rotation of the given Node2D.
    /// </summary>
    public static Vector2 GlobalRight(this Node2D node) => Vector2.Right.Rotated(node.GlobalRotation);

    /// <summary>
    /// Gets the direction vector from the 'from' Node2D to the 'to' Node2D.
    /// </summary>
    public static Vector2 DirectionFrom(this Node2D to, Node2D from) => from.Position.DirectionTo(to.Position);

    /// <summary>
    /// Gets the direction vector from the 'from' Node2D to the 'to' Node2D.
    /// </summary>
    public static Vector2 DirectionTo(this Node2D from, Node2D to) => from.Position.DirectionTo(to.Position);

    /// <summary>
    /// Gets the global direction vector from the 'from' Node2D to the 'to' Node2D.
    /// </summary>
    public static Vector2 GlobalDirectionFrom(this Node2D to, Node2D from) => from.GlobalPosition.DirectionTo(to.GlobalPosition);

    /// <summary>
    /// Gets the global direction vector from the 'from' Node2D to the 'to' Node2D.
    /// </summary>
    public static Vector2 GlobalDirectionTo(this Node2D from, Node2D to) => from.GlobalPosition.DirectionTo(to.GlobalPosition);

    
    /// <summary>
    /// Calculates the distance between two Node2D objects.
    /// </summary>
    public static double DistanceTo(this Node2D node, Node2D other) => node.Position.DistanceTo(other.Position);

    /// <summary>
    /// Calculates the distance between a Node2D object and a Vector2 position.
    /// </summary>
    public static double DistanceTo(this Node2D node, Vector2 pos) => node.Position.DistanceTo(pos);

    /// <summary>
    /// Calculates the distance between a Vector2 position and a Node2D object.
    /// </summary>
    public static double DistanceTo(this Vector2 pos, Node2D other) => pos.DistanceTo(other.Position);

    
    /// <summary>
    /// Calculates the global distance between the calling Node2D instance and another Node2D.
    /// </summary>
    public static double GlobalDistanceTo(this Node2D node, Node2D other) => node.GlobalPosition.DistanceTo(other.GlobalPosition);

    /// <summary>
    /// Calculates the global distance between the calling Node2D instance and a specified position.
    /// </summary>
    public static double GlobalDistanceTo(this Node2D node, Vector2 pos) => node.GlobalPosition.DistanceTo(pos);

    /// <summary>
    /// Calculates the global distance between a specified position and the Node2D instance.
    /// </summary>
    public static double GlobalDistanceTo(this Vector2 pos, Node2D other) => pos.DistanceTo(other.GlobalPosition);
}
