using Packages.Estenis.GameData_;
using Packages.Estenis.GameEvent_;
using Packages.Estenis.ScriptableObjectsData_;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{
    public class StateManager : MonoBehaviour
    {
        [SerializeField] private GameEventGameData _stateChangeEvent;
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
            _stateChangeEvent.Register(this.gameObject.GetHashCode(), OnStateChanged);
        }

        private void OnStateChanged(GameData data)
        {
            //var dataVal = (data as FSMDataString).Data;
            string state = (data as GameDataNamedAggregate).Name;

            if (this.name == "Wolf")
            {
                //Debug.LogWarning($"{Time.time} {this.name} State has changed to {state}");
            }
            if (_stateData != null)
            {
                _stateData.Data = (data as GameDataNamedAggregate).Data;
            }
            if(_transitionEventName != null)
            {
                _transitionEventName.Data = (data as GameDataNamedAggregate).EventName;
            }
            // disable all 
            DisableAllStatesGO();
            // enable selected state GO
            EnableStateGO(state);
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
