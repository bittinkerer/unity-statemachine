using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Packages.Estenis.StateMachine_
{
    
    public abstract class StateBase : ScriptableObject
	{
		[SerializeField] private List<Object> _onEnter	= new();
		[SerializeField] private List<Object> _onExit	= new();
		[SerializeField] private List<Object> _onUpdate = new();

		private void OnEnable()
		{
#if UNITY_EDITOR
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
                Initialize(_onEnter, _onExit, _onUpdate);
#else
			Initialize(_onEnter, _onExit, _onUpdate);
#endif
        }

        protected abstract void Initialize(List<Object> onEnter, List<Object> onExit, List<Object> onUpdate);

		public abstract Type GetActionType();
	}
}
