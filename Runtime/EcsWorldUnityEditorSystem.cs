// ----------------------------------------------------------------------------
// The MIT License
// UnityEditor integration https://github.com/Leopotam/ecslite-unityeditor
// for LeoECS Lite https://github.com/Leopotam/ecslite
// Copyright (c) 2021 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Leopotam.EcsLite.UnityEditor {
    public sealed class EcsWorldUnityEditorSystem : IEcsPreInitSystem, IEcsRunSystem, IEcsWorldEventListener {
        readonly string _worldName;
        readonly Transform _entitiesRoot;
        EcsWorld _world;
        EcsEntityObserver[] _entities;
        Dictionary<int, byte> _dirtyEntities;
        Type[] _typesCache;

        public EcsWorldUnityEditorSystem (string worldName = null) {
            _worldName = worldName;
            var go = new GameObject (_worldName != null ? $"[ECS-WORLD {_worldName}]" : "[ECS-WORLD]");
            Object.DontDestroyOnLoad (go);
            go.hideFlags = HideFlags.NotEditable;
            _entitiesRoot = new GameObject ("Entities").transform;
            _entitiesRoot.gameObject.hideFlags = HideFlags.NotEditable;
            _entitiesRoot.SetParent (go.transform, false);
        }

        public void PreInit (EcsSystems systems) {
            _world = systems.GetWorld (_worldName);
            if (_world == null) { throw new Exception ("Cant find required world."); }
            _entities = new EcsEntityObserver [_world.GetWorldSize ()];
            _dirtyEntities = new Dictionary<int, byte> (_entities.Length);
            _world.AddEventListener (this);
        }

        public void Run (EcsSystems systems) {
            foreach (var pair in _dirtyEntities) {
                var entity = pair.Key;
                var entityName = entity.ToString ("X8");
                if (_world.GetEntityGen (entity) > 0) {
                    var count = _world.GetComponentTypes (entity, ref _typesCache);
                    for (var i = 0; i < count; i++) {
                        entityName = $"{entityName}:{EditorHelpers.GetCleanGenericTypeName (_typesCache[i])}";
                    }
                }
                _entities[entity].name = entityName;
            }
            _dirtyEntities.Clear ();
        }

        public void OnEntityCreated (int entity) {
            if (!_entities[entity]) {
                var go = new GameObject ();
                go.transform.SetParent (_entitiesRoot, false);
                var entityObserver = go.AddComponent<EcsEntityObserver> ();
                entityObserver.Entity = entity;
                entityObserver.World = _world;
                _entities[entity] = entityObserver;
                _dirtyEntities[entity] = 1;
            }
            _entities[entity].gameObject.SetActive (true);
        }

        public void OnEntityDestroyed (int entity) {
            _entities[entity].gameObject.SetActive (false);
        }

        public void OnEntityChanged (int entity) {
            _dirtyEntities[entity] = 1;
        }

        public void OnFilterCreated (EcsFilter filter) { }

        public void OnWorldResized (int newSize) {
            Array.Resize (ref _entities, newSize);
        }

        public void OnWorldDestroyed (EcsWorld world) {
            _world.RemoveEventListener (this);
        }
    }
}