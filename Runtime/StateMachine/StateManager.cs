using Packages.Estenis.GameEvent_;
using Packages.Estenis.ScriptableObjectsData_;
using System;
using System.Linq;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{
    public class StateManager : EventMonoBehaviour
    {
        [SerializeField] private GameEvent<Transition> _stateChangeEvent;
        [SerializeField] private GameObject _statesParentGO;
        [SerializeField] private TransitionTable _transitionTable;
        [SerializeField] private GameDataSOData _stateData;
        [SerializeField] private StringSOData _transitionEventName;
        [SerializeField] private GameObject[] _alwaysOnStates;
        [SerializeField] private GameObject[] _disabledOnTransitionGO;


        private void Start()
        {
            // disable all state GOs
            DisableAllStatesGO();

            // enable intial state GO
            EnableStateGO(_transitionTable.InitialState.name);

            // register to state change event
            _stateChangeEvent.Register(EventId, (Action<object,Transition>)OnStateChanged);
        }

        private void OnStateChanged(object sender, Transition transition)
        {
            
            if(_transitionEventName != null)
            {
                _transitionEventName.Data = transition.TransitionEvent.name;
            }
            // disable all 
            DisableAllStatesGO();
            // enable selected state GO
            EnableStateGO(transition.NextState.name);
        }

        private void EnableStateGO(string state)
        {
            var stateGO = _statesParentGO.transform.Find(state).gameObject;
            stateGO.SetActive(true);
        }

        private void DisableAllStatesGO()
        {
            foreach (var item in _statesParentGO.transform)
            {
                if (!_alwaysOnStates.Any(go => go.transform == (Transform)item))
                {
                    (item as Transform).gameObject.SetActive(false);
                }
            }

            foreach (var go in _disabledOnTransitionGO)
            {
                foreach (var item in go.transform)
                {
                    if (!_alwaysOnStates.Any(go => go.transform == (Transform)item))
                    {
                        (item as Transform).gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
