﻿using Packages.Estenis.GameEvent_;
using System;
using System.Linq;
using UnityEngine;

namespace Packages.Estenis.StateMachine_ {
  public class StateManager : EventMonoBehaviour {
    [SerializeField] public TransitionTable   _transitionTable;
    [SerializeField] private GameEventObject  _stateChangeEvent;
    [SerializeField] private GameObject       _statesParentGO;
    [SerializeField] private GameObject       _sharedParentGO;

    protected void Start( ) {
      // disable all state GOs
      DisableAllStatesGO();

      // enable intial state GO
      EnableStateGO( _transitionTable.InitState );

      // register to state change event
      _stateChangeEvent.Register( EventId, (Action<object, object>) OnStateChanged );
    }

    private void OnStateChanged( object sender, object next ) {
      if ( next is not string nextState ) {
        return;
      }
      // disable all 
      DisableAllStatesGO();
      // enable selected state GO
      EnableStateGO( nextState );
    }

    private void EnableStateGO( string state ) {
      var initialState = _statesParentGO.transform.Find(state);
      if ( initialState == null ) {
        Debug.LogError( $"[{this.name}] Could not find Initial State {state}. Aborting." );
        return;
      }
      var stateGO = _statesParentGO.transform.Find(state).gameObject;
      stateGO.SetActive( true );
    }

    private void DisableAllStatesGO( ) {
      var statesParentTransforms = _sharedParentGO == null
                ? _statesParentGO.transform.Cast<Transform>()
                : _sharedParentGO.transform.Cast<Transform>()
                  .Concat(_statesParentGO.transform.Cast<Transform>())
                  .Where(st => st.name != _sharedParentGO.name);

      foreach ( var item in statesParentTransforms ) {
        ( item as Transform ).gameObject.SetActive( false );
      }
    }
  }
}