using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scripts.KludgeBox.Collections;
using NeonWarfare.Scripts.KludgeBox.Ecs.Systems.Interfaces;

namespace NeonWarfare.Scripts.KludgeBox.Ecs;

public partial class EcsWorld : Node2D
{
	public ReadOnlyHashSet<EcsEntity> Entities => entities.AsReadOnly();
	public IReadOnlyList<EcsSystem> Systems => systems.AsReadOnly();
	public ReadOnlyHashSet<EcsFilter> Filters => filters.AsReadOnly();

	private HashSet<EcsEntity> entities = new();
	private List<EcsSystem> systems = new();
	private HashSet<EcsFilter> filters = new HashSet<EcsFilter>();
	
	public double ProcessTime { get; private set; }
	public double PhysicsProcessTime { get; private set; }

	public bool ProcessPaused { get; set; } = false;
	public bool PhysicsProcessPaused { get; set; } = false;

	public override void _Ready()
	{
		foreach (var processSystem in Systems.OfType<IInitSystem>())
		{
			processSystem.Init();
		}
	}

	public void _Finish()
	{
		foreach (var processSystem in Systems.OfType<IFinishSystem>())
		{
			processSystem.Finish();
		}
	}

	public override void _Process(double delta)
	{
		if (ProcessPaused) return;

		foreach (var processSystem in Systems.OfType<IProcessSystem>())
		{
			processSystem.Process(delta);
		}
		ProcessTime += delta;
	}

	public override void _PhysicsProcess(double delta)
	{
		if(PhysicsProcessPaused) return;
		
		foreach (var processSystem in Systems.OfType<IPhysicsProcessSystem>())
		{
			processSystem.PhysicsProcess(delta);
		}

		PhysicsProcessTime += delta;
	}

	public void AddSystem(EcsSystem system)
	{
		system.World = this;
		systems.Add(system);
	}

	public void RemoveSystem(EcsSystem system)
	{
		system.World = system.World == this ? null : system.World;
		systems.Remove(system);
	}

	/// <summary>
	/// Добавляет новый фильтр и наполняет его уже существующими сущностями
	/// </summary>
	/// <param name="filter"></param>
	public void RegisterFilter(EcsFilter filter)
	{
		if (!filters.Contains(filter))
			filters.Add(filter);
		
		FillFilter(filter);
	}

	/// <summary>
	/// Принудительно запускает проверку для всех фильтров мира на соответствие этой сущности.
	/// Используется для регистрации "тихих" изменений в сущностях.
	/// </summary>
	/// <param name="entity"></param>
	public void RegisterEntity(EcsEntity entity)
	{
		Notify_EntityChanged(entity);
	}

	/// <summary>
	/// Добавляет пустую сущность в мир
	/// </summary>
	/// <returns></returns>
	public EcsEntity AddEntity()
	{
		var entity = new EcsEntity(this);
		entities.Add(entity);
		AddChild(entity);
		return entity;
	}
	
	/// <summary>
	/// Удаляет сущность из мира и фильтров
	/// </summary>
	/// <param name="entity"></param>
	public void RemoveEntity(EcsEntity entity)
	{
		entities.Remove(entity);
		foreach (EcsFilter filter in filters)
		{
			filter.Remove(entity);
		}
		
		entity.Finalize();
		entity.QueueFree();
	}
	
	/// <summary>
	/// Заполняет фильтр уже существующими сущностями. Может быть использован для однократной фильтрации. Без необходимости регистрировать фильтр.
	/// </summary>
	/// <param name="filter"></param>
	public void FillFilter(EcsFilter filter)
	{
		foreach (var ecsEntity in entities)
		{
			filter.Update(ecsEntity);
		}
	}

	/// <summary>
	/// Этот метод отвечает за обновление фильтров
	/// </summary>
	/// <param name="entity"></param>
	public void Notify_EntityChanged(EcsEntity entity)
	{
		foreach (var ecsFilter in filters)
		{
			ecsFilter.Update(entity);
		}
	}

	

	/// <summary>
	/// Удаляет все мусорные сущности
	/// </summary>
	public void CleanUp()
	{
		entities.RemoveWhere(e => !IsInstanceValid(e));
	}
}
