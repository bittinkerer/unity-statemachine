#pragma warning disable 0649
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{

  public abstract class TransitionTableBase : ScriptableObject
  {
    public const string ANY_STATE_NAME = "_AnyState";

    [SerializeField] private State                        _initialState;
    [SerializeField] private string                       _initState;
    [SerializeField] public List<StateToStateTransition>  _stateToStateEntries = new();
    [SerializeField] public StateToStateTransition[]      _filteredStates;
    [SerializeField] public List<TransitionTable>         _baseTables  = new();

    protected void OnEnable()
    {
#if UNITY_EDITOR
      if ( EditorApplication.isPlayingOrWillChangePlaymode 
        || Application.isPlaying 
        || EditorSettings.enterPlayModeOptions == EnterPlayModeOptions.DisableDomainReload)
      {
#endif
        Initialize(_initialState, _stateToStateEntries);
#if UNITY_EDITOR
      }
#endif

    }
    public abstract void Initialize(State initialState, List<StateToStateTransition> sts);
  }


}
