using System;
using System.Collections.Generic;
using KludgeBox.Collections;

namespace KludgeBox.Ecs;

public class EcsFilter
{
    public ReadOnlyHashSet<EcsEntity> Entities => _entities.AsReadOnly();
    private HashSet<EcsEntity> _entities = new();

    private List<EcsFilterRule> _rules = new();

    private EcsFilter(List<EcsFilterRule> rules)
    {
        _rules = rules;
    }

    /// <summary>
    /// Проверяет подходит ли сущность к фильтру
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool Match(EcsEntity entity)
    {
        foreach (var rule in _rules)
        {
            if (!rule.Match(entity)) return false;
        }

        return true;
        //return _rules.All(rule => rule.Match(entity));
    }

    /// <summary>
    /// Проверяет подходит ли сущность к фильтру, после чего удаляет или добавляет её по необходимости
    /// </summary>
    /// <param name="entity"></param>
    public void Update(EcsEntity entity)
    {
        var match = Match(entity);
        
        // Добавить сущность, если она подходит, но не содержится в фильтре
        if (_entities.Contains(entity) && !match)
        {
            _entities.Remove(entity);
        }

        //Удалить сущность, если она есть в фильтре, но больше не подходит
        if (!_entities.Contains(entity) && match)
        {
            _entities.Add(entity);
        }
        
        // В остальных случаях всё ок
    }

    public void Remove(EcsEntity entity)
    {
        _entities.Remove(entity);
    }    
    
    /// <summary>
    /// Возвращает конструктор фильтра.
    /// <remarks>
    /// При добавлении правил рекомендуется добавлять наиболее характерные для системы правила первыми.
    /// </remarks>
    /// </summary>
    /// <returns></returns>
    public static EcsFilterBuilder Create()
    {
        return new EcsFilterBuilder();
    }
    
    
    
    
    public class EcsFilterBuilder
    {
        private List<EcsFilterRule> _rules = new(); 
        public EcsFilterBuilder(){}

        /// <summary>
        /// Сущности в этом фильтре ДОЛЖНЫ содержать компонент указанного типа
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public EcsFilterBuilder With(Type type)
        {
            if(type.IsAssignableTo(typeof(EcsComponent))) 
                _rules.Add(new EcsFilterRule(type, true));
            
            return this;
        }
        
        /// <summary>
        /// Сущности в этом фильтре ДОЛЖНЫ содержать компонент указанного типа.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public EcsFilterBuilder With<TComponent>() where TComponent : EcsComponent, new()
        {
            _rules.Add(new EcsFilterRule(typeof(TComponent), true));
            
            return this;
        }
        
        /// <summary>
        /// Сущности в этом фильтре ДОЛЖНЫ НЕ содержать компонент указанного типа.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public EcsFilterBuilder Without(Type type)
        {
            if(type.IsAssignableTo(typeof(EcsComponent))) 
                _rules.Add(new EcsFilterRule(type, false));
            
            return this;
        }
        
        /// <summary>
        /// Сущности в этом фильтре ДОЛЖНЫ НЕ содержать компонент указанного типа.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public EcsFilterBuilder Without<TComponent>() where TComponent : EcsComponent, new()
        {
            _rules.Add(new EcsFilterRule(typeof(TComponent), false));
            
            return this;
        }

        /// <summary>
        /// Создает фильтр с текущим набором правил.
        /// </summary>
        /// <returns></returns>
        public EcsFilter Build()
        {
            return new EcsFilter(_rules);
        }
    }
}