using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;
using KludgeBox.Collections;

namespace KludgeBox.Ecs;

public partial class EcsEntity : Node
{
	public EcsWorld World { get; init; }
	public ReadOnlyDictionary<Type, EcsComponent> Components => _components.AsReadOnly();
	
	private Dictionary<Type, EcsComponent> _components = new();
	

	public EcsEntity( EcsWorld world)
	{
		World = world;
	}

	public EcsComponent GetComponent(Type type)
	{
		if (_components.TryGetValue(type, out var component))
			return component;

		return null;
	}

	public TComponent GetComponent<TComponent>() where TComponent : EcsComponent
	{
		var componentType = typeof(TComponent);
		
		return GetComponent(componentType) as TComponent;
	}

	public bool TryGetComponent<TComponent>(out TComponent component) where TComponent : EcsComponent
	{
		component = GetComponent<TComponent>();
		return component is not null;
	}

	public bool TryGetComponent(Type type, out EcsComponent component)
	{
		component = GetComponent(type);
		return component is not null;
	}
	
	public bool AddComponent(EcsComponent component)
	{
		if (AddComponentSilently(component))
		{
			World.Notify_EntityChanged(this);
			return true;
		}
		return false;
	}
	
	public bool AddComponentSilently(EcsComponent component)
	{
		Type componentType = component.GetType();
		if (HasComponent(componentType))
			return false;

		_components[componentType] = component;
		component.Entity = this;
		
		component.OnAdded();
		return true;
	}

	public bool RemoveComponent(EcsComponent component)
	{
		if (!RemoveComponentSilently(component))
		{
			return false;
		}
		World.Notify_EntityChanged(this);
		return true;
	}

	public bool RemoveComponent(Type componentType)
	{
		if (!RemoveComponentSilently(componentType))
		{
			return false;
		}
		World.Notify_EntityChanged(this);
		return true;
	}

	public bool RemoveComponent<TComponent>() where TComponent : EcsComponent
	{
		var componentType = typeof(TComponent);
		if (!RemoveComponentSilently(componentType))
		{
			return false;
		}
		World.Notify_EntityChanged(this);
		return true;
	}
	
	public bool RemoveComponentSilently(EcsComponent component)
	{
		Type componentType = component.GetType();
		if (!HasComponent(componentType))
			return false;

		_components.Remove(componentType);

		component.OnRemoved();
		component.Entity = component.Entity == this ? null : component.Entity;
		return true;
	}
	
	public bool RemoveComponentSilently(Type componentType)
	{
		if (!HasComponent(componentType))
			return false;

		var component = _components[componentType];
		
		component.Entity = component.Entity == this ? null : component.Entity;
		_components.Remove(componentType);

		component.OnRemoved();
		return true;
	}
	
	public bool RemoveComponentSilently<TComponent>() where TComponent : EcsComponent
	{
		var componentType = typeof(TComponent);
		if (!HasComponent(componentType))
			return false;

		var component = _components[componentType];
		
		component.Entity = component.Entity == this ? null : component.Entity;
		_components.Remove(componentType);

		component.OnRemoved();
		return true;
	}
	
	public bool HasComponent<TComponent>() where TComponent : EcsComponent
	{
		return _components.ContainsKey(typeof(TComponent));
	}
	
	public bool HasComponent(Type type)
	{
		return _components.ContainsKey(type);
	}
	
	/// <summary>
	/// Выполнит OnRemove на всех компонентах
	/// </summary>
	public void Finalize()
	{
		foreach (var (key, value) in _components)
		{
			value.OnRemoved();
		}
	}
}

public static class EcsEntityExtensions
{
	public static (TComp1, TComp2) GetComponents<TComp1, TComp2>(this EcsEntity entity) 
		where TComp1 : EcsComponent 
		where TComp2 : EcsComponent
	{
		return (entity.GetComponent<TComp1>(), 
			entity.GetComponent<TComp2>());
	}
	
	public static (TComp1, TComp2, TComp3) GetComponents<TComp1, TComp2, TComp3>(this EcsEntity entity) 
		where TComp1 : EcsComponent 
		where TComp2 : EcsComponent
		where TComp3 : EcsComponent
	{
		return (
			entity.GetComponent<TComp1>(),
			entity.GetComponent<TComp2>(),
			entity.GetComponent<TComp3>()
		);
	}
	
	public static (TComp1, TComp2, TComp3, TComp4) GetComponents<TComp1, TComp2, TComp3, TComp4>(this EcsEntity entity) 
		where TComp1 : EcsComponent 
		where TComp2 : EcsComponent
		where TComp3 : EcsComponent
		where TComp4 : EcsComponent
	{
		return (
			entity.GetComponent<TComp1>(),
			entity.GetComponent<TComp2>(),
			entity.GetComponent<TComp3>(),
			entity.GetComponent<TComp4>()
		);
	}
}