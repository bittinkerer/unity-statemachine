using Packages.Estenis.GameEvent_;
using UnityEngine;

namespace Packages.Estenis.StateMachine_ {
  public class StateMachine : EventMonoBehaviour {
    public string                             _currentState;
    public TransitionTable                    _transitionTable;
    [SerializeField] private GameEventObject  _onStateChangedEvent;
    [SerializeField] private GameEventObject  _afterStateChangedEvent;
    [SerializeField] private bool             _logStateTransition;

    private bool                              _transitionDonePerFrame;  // only allow one transition per frame

    private void Awake( ) {
      _currentState = _transitionTable.InitState;
      _transitionTable.OnStateChanged( EventId, _transitionTable.InitState, _transitionTable_OnTransition );
    }

    private void _transitionTable_OnTransition( object sender, object data ) {
      if ( _transitionDonePerFrame ) return; // only one transition per frame allowed

      if ( sender is not Transition transition ) {
        Debug.LogError( $"{nameof( sender )} needs to be of type, '{nameof( Transition )}'." );
        return;
      }

      if ( _currentState == transition.NextState ) {
        Debug.LogWarning( 
          $"Trying to transition through same states is NOT supported. Transition from {_currentState}->{transition.NextState} cancelled." );
        return;
      }

      if ( _logStateTransition ) {
        Debug.Log( 
          $"Transition [{Time.time}]: {transition.TransitionEvent.name} : ({transition.CurrentState} -> {transition.NextState})" );
      }

      _currentState = transition.NextState;
      _onStateChangedEvent.Raise( EventId, this.gameObject, _currentState );
      _transitionTable.OnStateChanged( EventId, transition.NextState, _transitionTable_OnTransition );
      if ( _afterStateChangedEvent != null ) {
        _afterStateChangedEvent.Raise( EventId, this.gameObject, data ); //< event with data
      }
      _transitionDonePerFrame = true;
    }

    private void LateUpdate( ) {
      _transitionDonePerFrame = false;
    }

    private void OnDisable( ) {
      _currentState = _transitionTable.InitialState.name;
      _onStateChangedEvent.Raise( EventId, this.gameObject, _currentState );
      _transitionTable.OnStateChanged( EventId, _transitionTable.InitialState.name, _transitionTable_OnTransition );
    }

    private void OnDestroy( ) {
      // unregister...
      _transitionTable.Unregister( EventId, _transitionTable_OnTransition );
    }
  }
}