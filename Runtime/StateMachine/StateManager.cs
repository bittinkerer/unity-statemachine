using Packages.Estenis.GameEvent_;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.Estenis.StateMachine_ {
  /// <summary>
  /// Shows the child GameObject named after the machine's current state and hides the rest.
  /// It owns the list of state GameObjects and knows nothing about what they do.
  ///
  /// Why it is shaped this way (performance first): the old version did a LINQ
  /// Cast/Concat/Where pass and two Transform.Find string searches on every state change. Now
  /// the children are gathered once in Start, a state name is mapped to its GameObject the
  /// first time it is seen, and a state change is a loop over an array plus one dictionary read.
  /// </summary>
  public class StateManager : EventMonoBehaviour {
    [SerializeField] public TransitionTable   _transitionTable;
    [SerializeField] private GameEventObject  _stateChangeEvent;
    [SerializeField] private GameObject       _statesParentGO;
    [SerializeField] private GameObject       _sharedParentGO;

    [NonSerialized] private GameObject[]                    _toggled;   // shared + state children
    [NonSerialized] private Dictionary<string, GameObject>  _byState;   // filled the first time a state is seen

    protected void Start( ) {
      BuildToggledList();
      _byState = new Dictionary<string, GameObject>();

      // disable all state GOs
      DisableAllStatesGO();

      // enable intial state GO
      EnableStateGO( _transitionTable.InitState );

      // register to state change event
      _stateChangeEvent.Register( EventId, (Action<object, object>) OnStateChanged );
    }

    private void BuildToggledList( ) {
      var list = new List<GameObject>();
      if ( _sharedParentGO != null ) {
        AddChildren( _sharedParentGO.transform, list, skipName: _sharedParentGO.name );
      }
      AddChildren( _statesParentGO.transform, list, skipName: _sharedParentGO != null ? _sharedParentGO.name : null );
      _toggled = list.ToArray();
    }

    private static void AddChildren( Transform parent, List<GameObject> into, string skipName ) {
      for ( int i = 0; i < parent.childCount; i++ ) {
        var child = parent.GetChild( i );
        if ( skipName == null || child.name != skipName ) { // as before: a child named like the shared parent is left alone
          into.Add( child.gameObject );
        }
      }
    }

    private void OnStateChanged( object sender, object next ) {
      if ( next is not string nextState ) {
        return;
      }
      DisableAllStatesGO();
      EnableStateGO( nextState );
    }

    private void EnableStateGO( string state ) {
      if ( string.IsNullOrEmpty( state ) ) {
        Debug.LogError( $"[{this.name}] TransitionTable '{( _transitionTable != null ? _transitionTable.name : "none" )}' has no state to show (initial state is empty).", this );
        return;
      }
      if ( !_byState.TryGetValue( state, out var stateGO ) ) {
        var child = _statesParentGO.transform.Find( state ); // once per state name, then cached
        if ( child == null ) {
          Debug.LogError( $"[{this.name}] Could not find Initial State {state}. Aborting." );
          return;
        }
        stateGO = child.gameObject;
        _byState[state] = stateGO;
      }
      stateGO.SetActive( true );
    }

    private void DisableAllStatesGO( ) {
      for ( int i = 0; i < _toggled.Length; i++ ) {
        var go = _toggled[i];
        if ( go.activeSelf ) {
          go.SetActive( false );
        }
      }
    }
  }
}
