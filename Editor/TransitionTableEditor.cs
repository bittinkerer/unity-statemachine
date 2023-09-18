using Packages.Estenis.GameEvent_;
using Packages.Estenis.StateMachine_;
using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Assets.StateMachine
{
    /*
     * UX NOTE:
     * The Asset picker doesn't work because types are of Generics.
     * For example we have DataModelAction : Action<DataModel>.
     * In the inspector we are asking for a Action<DataModel> field, and because of that
     * we can drag a DataModelAction into the field. But if we try to use the asset picker, it will found nothing
     * because it's searching for a Action<DataModel> but can't find it because assets are only DataModelAction.
     * A cool thing would be to display all current asset that are subtypes of Action<DataModel>, but there's no
     * such a feature for the asset picker right now.
     * UX "fix": get a square with "Drag [class name] here to add" and avoid using the asset picker ):
     */
    [CustomEditor(typeof(TransitionTableBase), true)]
    public class TransitionTableEditor : Editor
    {
        private TransitionTableBase _target;
        private SerializedProperty _initialState;
        private SerializedProperty _anyState;
        private ReorderableList _stateToStateList;

        private string _filter;
        private string _previousFilter;
        private int _popupIndex = 0;
        private int _previousTransitionType = 0;

        private enum TransitionType
        {
            FROM = 0,
            TO,
            EVENT,
            ANY
        }

        private void OnEnable()
        {
            _target = (TransitionTableBase)serializedObject.targetObject;

            _initialState = serializedObject.FindProperty("_initialState");
            _anyState = serializedObject.FindProperty("_anyState");

            _stateToStateList = new ReorderableList(
                serializedObject,
                serializedObject.FindProperty("_stateToStateEntries"),
                true, true, true, true)
            {
                drawElementCallback = DrawStateToStateEntry,
                drawHeaderCallback = (rect) => GUI.Label(rect, "State to state transitions:"),
            };
        }

        public override void OnInspectorGUI()
        {
            bool playing = EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying;

            EditorGUI.BeginDisabledGroup(playing);
            serializedObject.Update(); //get the latest target version
            DrawInspector();
            SaveFilteredChanges();
            serializedObject.ApplyModifiedProperties(); //save changes
            //serializedObject.ApplyModifiedPropertiesWithoutUndo(); //add support for undo
            EditorGUI.EndDisabledGroup();

            if (playing)
                EditorGUILayout.HelpBox("Editing during PlayMode currently not supported.", MessageType.Info);
        }

        private void DrawInspector()
        {
            _initialState.objectReferenceValue =
                EditorGUILayout.ObjectField(
                    "Initial State: ", _initialState.objectReferenceValue, typeof(Packages.Estenis.StateMachine_.State), false);

            _anyState.objectReferenceValue =
                EditorGUILayout.ObjectField(
                    "Any State: ", _anyState.objectReferenceValue, typeof(Packages.Estenis.StateMachine_.State), false);

            GUILayout.BeginHorizontal();
            {
                var sixthWidth = EditorGUIUtility.currentViewWidth / 6;
                //EditorGUILayout.LabelField("Filter", GUILayout.Width(sixthWidth));
                _popupIndex = EditorGUILayout.Popup(_popupIndex, Enum.GetNames(typeof(TransitionType)).Select(s => new GUIContent(s)).ToArray(), GUILayout.Width(sixthWidth * 1 - 3));
                //GUILayout.Space(sixthWidth*1.375f);
                _filter = EditorGUILayout.TextField(_filter, GUILayout.Width(sixthWidth * 5 - 10));
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            if (!string.IsNullOrWhiteSpace(_filter) && (_popupIndex != _previousTransitionType || _filter != _previousFilter))
            {
                UpdateFiltered();
            }
            else if (_filter != _previousFilter && string.IsNullOrWhiteSpace(_filter))
            {
                _stateToStateList = new ReorderableList(
                     serializedObject,
                     serializedObject.FindProperty("_stateToStateEntries"),
                     true, true, true, true)
                {
                    drawElementCallback = DrawStateToStateEntry,
                    drawHeaderCallback = (rect) => GUI.Label(rect, "State to state transitions:")
                };
                _target._filteredStates = new StateToStateTransition[0];
                
            }
            _previousTransitionType = _popupIndex;
            _previousFilter = _filter;

            _stateToStateList.DoLayoutList();
        }

        private void UpdateFiltered()
        {
            Func<(StateToStateTransition, int), bool> filter = ((StateToStateTransition s, int i) input) => true;
            switch (_popupIndex)
            {
                case (int)TransitionType.FROM:
                    filter = ((StateToStateTransition s, int i) input) => input.s.FromState.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant());
                    break;
                case (int)TransitionType.EVENT:
                    filter = ((StateToStateTransition s, int i) input) => input.s.GameEvent.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant());
                    break;
                case (int)TransitionType.TO:
                    filter = ((StateToStateTransition s, int i) input) => input.s.ToState.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant());
                    break;
                case (int)TransitionType.ANY:
                    filter = ((StateToStateTransition s, int i) input) =>
                            input.s.FromState.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant())
                        ||  input.s.GameEvent.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant())
                        ||  input.s.ToState.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant());
                    break;
                default:
                    break;
            }
            _target._filteredStates = _target._stateToStateEntries
                .Select((transition, index) => (transition, index))
                .Where(filter)
                .Select(t => 
                    new StateToStateTransition { 
                        FromState = t.Item1.FromState, 
                        ToState = t.Item1.ToState, 
                        GameEvent = t.Item1.GameEvent, 
                        Index = t.Item2})
                .ToArray();

            _stateToStateList = new ReorderableList(
                    serializedObject,
                    serializedObject.FindProperty("_filteredStates"),
                    true, true, true, true)
            {
                drawElementCallback = DrawStateToStateEntry,
                drawHeaderCallback = (rect) => GUI.Label(rect, "State to state transitions:"),
                onChangedCallback = (list) => { Debug.Log($"Changed: Filtered List has {list.count} items."); },  // TODO: Add handler to update full list with added/removed from filtered              
            };
        }

        private void SaveFilteredChanges()
        {
            foreach (var item in _target._filteredStates ?? new StateToStateTransition[0])
            {
                if(item.GameEvent != _target._stateToStateEntries[item.Index].GameEvent
                    || item.ToState != _target._stateToStateEntries[item.Index].ToState
                    || item.FromState != _target._stateToStateEntries[item.Index].FromState)
                {
                    _target._stateToStateEntries[item.Index] = new StateToStateTransition
                    {
                        FromState = item.FromState,
                        ToState=item.ToState,
                        GameEvent=item.GameEvent,
                    };
                }
            }
        }

        private void DrawStateToStateEntry(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = _stateToStateList.serializedProperty.GetArrayElementAtIndex(index);

            SerializedProperty fromState    = element.FindPropertyRelative("FromState");
            SerializedProperty toState      = element.FindPropertyRelative("ToState");
            SerializedProperty gameEvent    = element.FindPropertyRelative("GameEvent");

            rect.y += 2;
            float fieldWidth = rect.width / 3f;

            Rect fromRect   = new Rect(rect.x, rect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
            Rect toRect     = new Rect(rect.x + fieldWidth, rect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
            Rect whenRect   = new Rect(rect.x + fieldWidth * 2f, rect.y, fieldWidth, EditorGUIUtility.singleLineHeight);

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = rect.width / 21f;

            fromState.objectReferenceValue =
                EditorGUI.ObjectField(fromRect, "from", fromState.objectReferenceValue, typeof(Packages.Estenis.StateMachine_.State), false);

            toState.objectReferenceValue =
                EditorGUI.ObjectField(toRect, " to", toState.objectReferenceValue, typeof(Packages.Estenis.StateMachine_.State), false);

            EditorGUIUtility.labelWidth = rect.width / 20f + 5;
            gameEvent.objectReferenceValue =
                EditorGUI.ObjectField(whenRect, " when", gameEvent.objectReferenceValue, typeof(GameEventGameData), false);

            EditorGUIUtility.labelWidth = labelWidth;
        }
    }
}
