using Packages.Estenis.GameEvent_;
using Packages.Estenis.StateMachine_;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Assets.StateMachine
{

  public static class ReorderableListExtensions2 {
    public static List<T> toList<T>(this ReorderableList reorderableList) where T : new() {
      List<T> result = new();
      object temp = new T();
      var props = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

      for (int i = 0; i < reorderableList.count; ++i) {
        SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(i);
        foreach (var prop in props) {
          var propValue = element.FindPropertyRelative(prop.Name);
          if (propValue != null) {            
            Type t = Nullable.GetUnderlyingType(prop.FieldType) ?? prop.FieldType;
            if (t.IsValueType) continue;
            object safeValue = Convert.ChangeType(propValue.objectReferenceValue, t);
            prop.SetValue(temp, safeValue);
          }
        }
        result.Add((T)temp);
      }
      return result;
    }
  }

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

    protected void OnEnable()
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
        drawHeaderCallback = DrawTTableHeader,
      };
    }
    
    private void DrawTTableHeader(Rect rect)
    {
      string from = "FROM";
      string to = "| TO";
      string when = "| WHEN";
      
      GUI.Label(new Rect(rect.x, rect.y, rect.width/3, rect.height), from);
      GUI.Label(new Rect(rect.x + rect.width / 3, rect.y, rect.width / 3, rect.height), $"{to, 10}");
      GUI.Label(new Rect(rect.x + 2 * (rect.width / 3), rect.y, rect.width / 3, rect.height), $"{when,10}");
      GUI.Label(new Rect(rect.x + rect.width - 30, rect.y, 30, rect.height), $"{_stateToStateList.count, 2}");
    }

    public override void OnInspectorGUI()
    {
      bool playing = EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying;

      EditorGUI.BeginDisabledGroup(playing);
      serializedObject.Update(); //get the latest target version
      DrawInspector();
      SaveFilteredChanges();
      serializedObject.ApplyModifiedProperties(); //save changes
      EditorGUI.EndDisabledGroup();

      if (playing) {
        EditorGUILayout.HelpBox("Editing during PlayMode currently not supported.", MessageType.Info);
      }
    }

    private void DrawInspector()
    {
      _initialState.objectReferenceValue =
          EditorGUILayout.ObjectField(
              "Initial State: ", _initialState.objectReferenceValue, typeof(Packages.Estenis.StateMachine_.State), false);

      _anyState.objectReferenceValue =
          EditorGUILayout.ObjectField(
              "Any State: ", _anyState.objectReferenceValue, typeof(Packages.Estenis.StateMachine_.State), false);

      // Draw Filtering Options
      GUILayout.BeginHorizontal();
      {
        var sixthWidth = EditorGUIUtility.currentViewWidth / 6;
        //EditorGUILayout.LabelField("Filter", GUILayout.Width(sixthWidth));
        _popupIndex = EditorGUILayout.Popup(
          _popupIndex, 
          Enum.GetNames(typeof(TransitionType)).Select(s => new GUIContent(s)).ToArray(), 
          GUILayout.Width(sixthWidth * 1 - 3));
        //GUILayout.Space(sixthWidth*1.375f);
        _filter = EditorGUILayout.TextField(_filter, GUILayout.Width(sixthWidth * 5 - 10));
      }
      GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal();
      {
        if(GUI.Button(new Rect(18,70,50,25), "Sort"))
        {
          _target._stateToStateEntries = 
            _target._stateToStateEntries
              .OrderBy(s2sTransition => (s2sTransition.FromState == null ? "zzz" : s2sTransition.FromState.name))
              .ToList();          
        }
        if (!(EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)) {

          if (GUI.Button(new Rect(EditorGUIUtility.currentViewWidth - 60, 70, 50, 25), "Save")) {
            _target.Initialize(
                (Packages.Estenis.StateMachine_.State)_initialState.objectReferenceValue
              , _stateToStateList.toList<StateToStateTransition>());
          }
        }
      }
      GUILayout.EndHorizontal();

      EditorGUILayout.Space(40);
      // Draw Transition Table
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
          drawHeaderCallback = DrawTTableHeader
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
          filter = ((StateToStateTransition s, int i) input) => 
            input.s.FromState != null && input.s.FromState.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant());
          break;
        case (int)TransitionType.EVENT:
          filter = ((StateToStateTransition s, int i) input) =>
            input.s.GameEvent != null && input.s.GameEvent.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant());
          break;
        case (int)TransitionType.TO:
          filter = ((StateToStateTransition s, int i) input) => 
            input.s.ToState != null && input.s.ToState.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant());
          break;
        case (int)TransitionType.ANY:
          filter = ((StateToStateTransition s, int i) input) =>
                  input.s.FromState.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant())
              || input.s.GameEvent.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant())
              || input.s.ToState.name.ToLowerInvariant().StartsWith(_filter.ToLowerInvariant());
          break;
        default:
          break;
      }
      _target._filteredStates = _target._stateToStateEntries
          .Select((transition, index) => (transition, index))
          .Where(filter)
          .Select(t =>
              new StateToStateTransition
              {
                FromState = t.Item1.FromState,
                ToState = t.Item1.ToState,
                GameEvent = t.Item1.GameEvent,
                Index = t.Item2
              })
          .ToArray();

      _stateToStateList = new ReorderableList(
              serializedObject,
              serializedObject.FindProperty("_filteredStates"),
              true, true, true, true)
      {
        drawElementCallback = DrawStateToStateEntry,
        drawHeaderCallback = DrawTTableHeader,
        onChangedCallback = (list) => { Debug.Log($"Changed: Filtered List has {list.count} items."); },  // TODO: Trigger event with full list with added/removed from filtered
                                                                                                          // Handlers should always be separate from origin of event
      };
    }

    private void SaveFilteredChanges()
    {
      foreach (var item in _target._filteredStates ?? new StateToStateTransition[0])
      {
        if (item.GameEvent != _target._stateToStateEntries[item.Index].GameEvent
            || item.ToState != _target._stateToStateEntries[item.Index].ToState
            || item.FromState != _target._stateToStateEntries[item.Index].FromState)
        {
          _target._stateToStateEntries[item.Index] = new StateToStateTransition
          {
            FromState = item.FromState,
            ToState = item.ToState,
            GameEvent = item.GameEvent,
          };
        }
      }
    }

    private void DrawStateToStateEntry(Rect rect, int index, bool isActive, bool isFocused)
    {
      SerializedProperty element = _stateToStateList.serializedProperty.GetArrayElementAtIndex(index);

      SerializedProperty fromState  = element.FindPropertyRelative("FromState");
      SerializedProperty toState    = element.FindPropertyRelative("ToState");
      SerializedProperty gameEvent  = element.FindPropertyRelative("GameEvent");

      rect.y += 2;
      float fieldWidth = rect.width / 3f;

      Rect fromRect = new(rect.x,                   rect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
      Rect toRect   = new(rect.x + fieldWidth,      rect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
      Rect whenRect = new(rect.x + fieldWidth * 2f, rect.y, fieldWidth, EditorGUIUtility.singleLineHeight);

      float labelWidth = EditorGUIUtility.labelWidth;
      EditorGUIUtility.labelWidth = rect.width / 23f;

      fromState.objectReferenceValue =
          EditorGUI.ObjectField(
            fromRect, "", fromState.objectReferenceValue, typeof(Packages.Estenis.StateMachine_.State), false);

      toState.objectReferenceValue =
          EditorGUI.ObjectField(
            toRect, "->", toState.objectReferenceValue, typeof(Packages.Estenis.StateMachine_.State), false);

      gameEvent.objectReferenceValue =
          EditorGUI.ObjectField(
            whenRect, " :", gameEvent.objectReferenceValue, typeof(GameEventObject), false);

      EditorGUIUtility.labelWidth = labelWidth;
    }
  }
}