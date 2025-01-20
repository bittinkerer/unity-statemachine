using Packages.Estenis.GameEvent_;
using System;
using System.Linq;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{
  public class StateManager : EventMonoBehaviour
  {
    [SerializeField] private GameEventObject _stateChangeEvent;
    [SerializeField] private GameObject _statesParentGO;
    [SerializeField] private GameObject _sharedParentGO;
    [SerializeField] private TransitionTable _transitionTable;
    [SerializeField] private GameObject[] _alwaysOnStates;

    protected void Start()
    {
      // disable all state GOs
      DisableAllStatesGO();

      // enable intial state GO
      EnableStateGO(_transitionTable.InitialState.name);

      // register to state change event
      _stateChangeEvent.Register(EventId, (Action<object, object>)OnStateChanged);
    }

    private void OnStateChanged(object sender, object next)
    {
      if (next is not State nextState)
      {
        return;
      }
      // disable all 
      DisableAllStatesGO();
      // enable selected state GO
      EnableStateGO(nextState.name);
    }

    private void EnableStateGO(string state)
    {
      var initialState = _statesParentGO.transform.Find(state);
      if (initialState == null)
      {
        Debug.LogError($"[{this.name}] Could not find Initial State {state}. Aborting.");
        return;
      }
      var stateGO = _statesParentGO.transform.Find(state).gameObject;
      stateGO.SetActive(true);
    }

    private void DisableAllStatesGO()
    {
      var statesParentTransforms = _sharedParentGO == null
          ? _statesParentGO.transform.Cast<Transform>()
          : _sharedParentGO.transform.Cast<Transform>().Union(_statesParentGO.transform.Cast<Transform>());

      foreach (var item in statesParentTransforms)
      {
        if (!_alwaysOnStates.Any(go => go.transform == (Transform)item))
        {
          (item as Transform).gameObject.SetActive(false);
        }
      }
    }
  }
}