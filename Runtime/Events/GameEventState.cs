using UnityEngine;
using Packages.Estenis.GameEvent_;

namespace Packages.Estenis.StateMachine_
{
    [CreateAssetMenu(menuName = "GameEvent/GameEventState")]
    public class GameEventState : GameEvent<GameDataState> { }
}
