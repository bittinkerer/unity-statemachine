using Packages.Estenis.GameData_;
using Packages.Estenis.GameEvent_;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{
    [CreateAssetMenu(menuName = "EventsFSM/State")]
	public class State : State<GameData>
	{
	}

	public class State<T> : ScriptableObject where T: GameData
	{
		//collection of actions to perform once every time the FSM transits INTO this state
		public List<GameEvent<T>> _onEnterActions = new();
		//collection of acts to perform once every time the FSM transits OUT this state
		public List<GameEvent<T>> _onExitActions = new();
		//collection of acts to perform while the FSM is in this state
		public List<GameEvent<T>> _onUpdateActions = new();


		public void OnEnter(T data) =>
			_onEnterActions.ForEach(a => a.Raise(data));

		public void OnExit(T data) =>
			_onExitActions.ForEach(a => a.Raise(data));

		public void OnUpdate(T data) =>
			_onUpdateActions.ForEach(a => a.Raise(data));

	}
}
