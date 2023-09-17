using Packages.Estenis.GameData_;
using Packages.Estenis.GameEvent_;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{
    public class StateMachine : EventMonoBehaviour
    {
        private bool _transitionDonePerFrame;  // only allow one transition per frame
        public State _currentState;
        public TransitionTable _transitionTable;
        [SerializeField] private GameEventGameData _onStateChangedEvent;

        private void Awake()
        {
            _currentState = _transitionTable.InitialState;
            _transitionTable.OnTransitionEvent.Handler += _transitionTable_OnTransition;
            _transitionTable.Register(EventId);
        }

        private void OnDestroy()
        {
            // unregister...
            _transitionTable.Remove(EventId);
        }

        private void _transitionTable_OnTransition(object sender, GameData data)
        {
            if (!_transitionDonePerFrame && (EventId == data.InstanceId || data.InstanceId == int.MinValue))
            {
                var transition = sender as Transition;

                _currentState.OnExit(new GameData(EventId));
                _currentState = transition.NextState;
                _currentState.OnEnter(data);
                _onStateChangedEvent.Raise(new GameDataNamedAggregate(EventId) { Name = _currentState.name, Data = data, EventName = transition.TransitionEvent.name });
                _transitionTable.OnStateChanged(EventId, transition.NextState);
                _transitionDonePerFrame = true;
            }
        }

        private void LateUpdate()
        {
            _transitionDonePerFrame = false;
        }

    }
}
