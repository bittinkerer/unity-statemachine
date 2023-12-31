﻿#pragma warning disable 0649
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{

    public abstract class TransitionTableBase : ScriptableObject
	{
		[SerializeField] private State _initialState;
		[SerializeField] protected State _anyState;
		[SerializeField] public List<StateToStateTransition> _stateToStateEntries = new();
		[SerializeField] public StateToStateTransition[] _filteredStates;

		private void OnEnable()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
			{
				Initialize(_initialState, _stateToStateEntries);
			}
		}
		protected abstract void Initialize(State initialState, List<StateToStateTransition> sts);
	}

	
}
