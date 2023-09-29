using Packages.Estenis.GameEvent_;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{
    public class StateMachine : EventMonoBehaviour
    {
        private bool _transitionDonePerFrame;  // only allow one transition per frame
        public State _currentState;
        public TransitionTable _transitionTable;
        [SerializeField] private GameEvent<Transition> _onStateChangedEvent;

        private void Awake()
        {
            _currentState = _transitionTable.InitialState;
            _transitionTable.Register(EventId, _transitionTable_OnTransition);
        }

        private void OnDestroy()
        {
            // unregister...
            _transitionTable.Unregister(EventId, _transitionTable_OnTransition);
        }

        private void _transitionTable_OnTransition(object sender, object data)
        {
            if (!_transitionDonePerFrame)
            {
                if (sender is not Transition transition)
                {
                    Debug.LogError($"{data.GetType()} needs to be a {nameof(Transition)} type.");
                    return;
                }
                _currentState = transition.NextState;
                _onStateChangedEvent.Raise(EventId, this, transition);
                _transitionTable.OnStateChanged(EventId, transition.NextState, _transitionTable_OnTransition);
                _transitionDonePerFrame = true;
            }
        }

        private void LateUpdate()
        {
            _transitionDonePerFrame = false;
        }

    }
}
