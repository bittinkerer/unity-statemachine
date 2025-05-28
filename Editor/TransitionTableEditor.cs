using Packages.Estenis.GameEvent_;
using Packages.Estenis.StateMachine_;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.Progress;

namespace Assets.Esteny.StateMachine_ {

  public static class ReorderableListExtensions {
    public static List<T> toList<T>( this ReorderableList reorderableList ) where T : new() {
      List<T> result = new();
      object temp = new T();
      var props = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

      for ( int i = 0; i < reorderableList.count; ++i ) {
        SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(i);
        foreach ( var prop in props ) {
          var propValue = element.FindPropertyRelative(prop.Name);
          if ( propValue != null ) {
            Type t = Nullable.GetUnderlyingType(prop.FieldType) ?? prop.FieldType;
            if ( t.IsValueType ) continue;
            object safeValue = Convert.ChangeType(propValue.objectReferenceValue, t);
            prop.SetValue( temp, safeValue );
          }
        }
        result.Add( (T) temp );
      }
      return result;
    }
  }

  [CustomEditor( typeof( TransitionTableBase ), true )]
  public class TransitionTableEditor : Editor {
    private TransitionTableBase   _target;
    private SerializedProperty    _initState;
    private ReorderableList       _stateToStateList;
    private ReorderableList       _baseTablesList;
    private ReorderableList       _referencesList;
    private string                _filter;
    private string                _previousFilter;
    private int                   _popupIndex = 0;
    private int                   _previousTransitionType = 0;

    const string                  PREFABS_PATH        = @"Assets/Prefabs/";
    const string                  PREFAB_STATES_PATH  = @"STATES";
    private List<string>          _states             = new();
    private string                _prefabName; // for logging purposes 

    // References
    bool _showReferences = false;
    List<GameObject> _references = new ();

    private enum TransitionType {
      FROM = 0,
      TO,
      EVENT,
      ANY
    }

    protected void OnEnable( ) {
      _target = (TransitionTableBase) serializedObject.targetObject;

      // Get corresponding prefab
      if ( _target.name.Contains( "_TTable" ) ) {
        string prefabGUID =
          AssetDatabase.FindAssets(
            _target.name.Remove( _target.name.IndexOf( "_TTable" ) ), new string[] { PREFABS_PATH } )
          .FirstOrDefault();
        if ( prefabGUID != null ) { // @TODO: Cleanup
          string path = AssetDatabase.GUIDToAssetPath(prefabGUID);
          GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
          _prefabName = prefab.name;
          var statesTransform = prefab.transform.Find(PREFAB_STATES_PATH);
          if ( !statesTransform ) {
            Debug.LogError( $"TTable {target.name} did not find Prefab with '{PREFAB_STATES_PATH}' path, {_prefabName}" );
            //return;
          }
          // build states list for (from,to) states-dropdown
          if ( statesTransform ) {
            _states.Clear();
            _states.Add( "_AnyState" );
            foreach ( Transform tr in statesTransform ) {
              if ( tr.name != "_SharedStates" ) {
                _states.Add( tr.gameObject.name );
              }
            }
          }
        }
      }

      _initState = serializedObject.FindProperty( "_initState" );

      _baseTablesList = new ReorderableList(
        serializedObject,
        serializedObject.FindProperty( "_baseTables" ),
        true, true, true, true ) {
        drawElementCallback = DrawBaseTTableEntry,
        drawHeaderCallback = DrawBaseTTablesHeader,
      };

      // References
      _references = FindPrefabReferences( target.name ).ToList();
      _referencesList = new ReorderableList(
        _references,
        typeof( GameObject ) ) {
        drawElementCallback = DrawReferenceEntry,
        //drawHeaderCallback => EditorGUILayout.LabelField("Gooer")
      };

      _stateToStateList = new ReorderableList(
          serializedObject,
          serializedObject.FindProperty( "_stateToStateEntries2" ),
          true, true, true, true ) {
        drawElementCallback = DrawStateToStateEntry,
        drawHeaderCallback = DrawTTableHeader,
      };

    }

    private void DrawBaseTTablesHeader( Rect rect ) {
      GUI.Label( new Rect( rect.x, rect.y, rect.width, rect.height ), "Base Transition Tables" );
    }

    public override void OnInspectorGUI( ) {
      bool playing = EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying;

      EditorGUI.BeginDisabledGroup( playing );
      serializedObject.Update(); //get the latest target version
      DrawInspector();
      SaveFilteredChanges();
      serializedObject.ApplyModifiedProperties(); //save changes
      EditorGUI.EndDisabledGroup();

      if ( playing ) {
        EditorGUILayout.HelpBox( "Editing during PlayMode currently not supported.", MessageType.Info );
      }
    }

    private void DrawInspector( ) {
      // Draw Initial State
      //_initialState.objectReferenceValue =
      //      EditorGUILayout.ObjectField(
      //          "Initial State: ", _initialState.objectReferenceValue, typeof( Packages.Estenis.StateMachine_.State ), false );

      if ( _states.Count > 0 ) {
        int initIndex =
        EditorGUILayout.Popup(
          "Init State: ",
          Math.Max(0, _states.FindIndex( e => string.IsNullOrEmpty(_initState.stringValue) || _initState.stringValue == e )), _states.ToArray() );

        _initState.stringValue = _states[initIndex];
      }

      EditorGUILayout.Space( 10 );

      // Draw Base Tables
      GUILayout.BeginHorizontal();
      {
        _baseTablesList.DoLayoutList();
      }
      GUILayout.EndHorizontal();
      EditorGUILayout.Space( 7 );

      // Draw References
      _showReferences = EditorGUILayout.Foldout( _showReferences, "Prefab References" );
      if ( _showReferences ) {
        //EditorGUILayout.LabelField( "FOO" );
        _referencesList.DoLayoutList();
      }

      // Draw Filtering Options
      GUILayout.BeginHorizontal();
      {
        var sixthWidth = EditorGUIUtility.currentViewWidth / 6;
        _popupIndex = EditorGUILayout.Popup(
          _popupIndex,
          Enum.GetNames( typeof( TransitionType ) ).Select( s => new GUIContent( s ) ).ToArray(),
          GUILayout.Width( sixthWidth * 1 - 3 ) );
        _filter = EditorGUILayout.TextField( _filter, GUILayout.Width( sixthWidth * 5 - 10 ) );
      }
      GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal();
      {
        // Sort click
        if ( GUILayout.Button( "SORT" ) ) {
          _target._stateToStateEntries =
            _target._stateToStateEntries
              .OrderBy( s2sTransition => ( s2sTransition.FromState == null ? "zzz" : s2sTransition.FromState.name ) )
              .ToList();
        }
        // Save click
        if ( !( EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying ) ) {

          if ( GUILayout.Button( "SAVE" ) ) {
            _target.Initialize(
                /*(Packages.Estenis.StateMachine_.State) */_initState.stringValue
              , _stateToStateList.toList<StateToStateTransition2>() );
          }
        }
      }
      GUILayout.EndHorizontal();

      if ( GUILayout.Button( "TTABLES_SYNC" ) ) {
        //SyncStateTransitions();
      }

      EditorGUILayout.Space( 10 );
      // Draw Transition Table
      if ( !string.IsNullOrWhiteSpace( _filter ) && ( _popupIndex != _previousTransitionType || _filter != _previousFilter ) ) {
        UpdateFiltered();
      }
      else if ( _filter != _previousFilter && string.IsNullOrWhiteSpace( _filter ) ) {
        _stateToStateList = new ReorderableList(
             serializedObject,
             serializedObject.FindProperty( "_stateToStateEntries" ),
             true, true, true, true ) {
          drawElementCallback = DrawStateToStateEntry,
          drawHeaderCallback = DrawTTableHeader
        };
        _target._filteredStates = new StateToStateTransition[0];

      }
      _previousTransitionType = _popupIndex;
      _previousFilter = _filter;

      //_stateToStateList.DoLayoutList();

      // Draw Transition Table 2
      _stateToStateList.DoLayoutList();

    }

    private void UpdateFiltered( ) {
      Func<(StateToStateTransition, int), bool> filter = ((StateToStateTransition s, int i) input) => true;
      switch ( _popupIndex ) {
        case (int) TransitionType.FROM:
          filter = ( (StateToStateTransition s, int i) input ) =>
            input.s.FromState != null && input.s.FromState.name.ToLowerInvariant().StartsWith( _filter.ToLowerInvariant() );
          break;
        case (int) TransitionType.EVENT:
          filter = ( (StateToStateTransition s, int i) input ) =>
            input.s.GameEvent != null && input.s.GameEvent.name.ToLowerInvariant().StartsWith( _filter.ToLowerInvariant() );
          break;
        case (int) TransitionType.TO:
          filter = ( (StateToStateTransition s, int i) input ) =>
            input.s.ToState != null && input.s.ToState.name.ToLowerInvariant().StartsWith( _filter.ToLowerInvariant() );
          break;
        case (int) TransitionType.ANY:
          filter = ( (StateToStateTransition s, int i) input ) =>
                  input.s.FromState.name.ToLowerInvariant().StartsWith( _filter.ToLowerInvariant() )
              || input.s.GameEvent.name.ToLowerInvariant().StartsWith( _filter.ToLowerInvariant() )
              || input.s.ToState.name.ToLowerInvariant().StartsWith( _filter.ToLowerInvariant() );
          break;
        default:
          break;
      }
      _target._filteredStates = _target._stateToStateEntries
          .Select( ( transition, index ) => (transition, index) )
          .Where( filter )
          .Select( t =>
              new StateToStateTransition {
                FromState = t.Item1.FromState,
                ToState = t.Item1.ToState,
                GameEvent = t.Item1.GameEvent,
                Index = t.Item2
              } )
          .ToArray();

      _stateToStateList = new ReorderableList(
              serializedObject,
              serializedObject.FindProperty( "_filteredStates" ),
              true, true, true, true ) {
        drawElementCallback = DrawStateToStateEntry,
        drawHeaderCallback = DrawTTableHeader,
        onChangedCallback = ( list ) => { Debug.Log( $"Changed: Filtered List has {list.count} items." ); },
        // TODO: Trigger event with full list with added/removed from filtered
        // Handlers should always be separate from origin of event
      };
    }

    private void SaveFilteredChanges( ) {
      foreach ( var item in _target._filteredStates ?? new StateToStateTransition[0] ) {
        if ( item.GameEvent != _target._stateToStateEntries[item.Index].GameEvent
            || item.ToState != _target._stateToStateEntries[item.Index].ToState
            || item.FromState != _target._stateToStateEntries[item.Index].FromState ) {
          _target._stateToStateEntries[item.Index] = new StateToStateTransition {
            FromState = item.FromState,
            ToState = item.ToState,
            GameEvent = item.GameEvent,
          };
        }
      }
    }

    private void DrawTTableHeader( Rect rect ) {
      string from = "FROM";
      string to   = "| TO";
      string when = "| WHEN";

      GUI.Label( new Rect( rect.x, rect.y, rect.width / 3, rect.height ), from );
      GUI.Label( new Rect( rect.x + rect.width / 3, rect.y, rect.width / 3, rect.height ), $"{to,10}" );
      GUI.Label( new Rect( rect.x + 2 * ( rect.width / 3 ), rect.y, rect.width / 3, rect.height ), $"{when,10}" );
      GUI.Label( new Rect( rect.x + rect.width - 30, rect.y, 30, rect.height ), $"{_stateToStateList.count,2}" );
    }

    private void DrawStateToStateEntry( Rect rect, int index, bool isActive, bool isFocused ) {
      // get data
      SerializedProperty element = _stateToStateList.serializedProperty.GetArrayElementAtIndex(index);

      SerializedProperty fromStateValue   = element.FindPropertyRelative("FromState");
      SerializedProperty toStateValue     = element.FindPropertyRelative("ToState");
      SerializedProperty gameEvent        = element.FindPropertyRelative("GameEvent");

      // set view settings
      rect.y += 2;
      float fieldWidth = rect.width / 3f;

      Rect fromRect = new(rect.x,                   rect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
      Rect toRect   = new(rect.x + fieldWidth,      rect.y, fieldWidth, EditorGUIUtility.singleLineHeight);
      Rect whenRect = new(rect.x + fieldWidth * 2f, rect.y, fieldWidth, EditorGUIUtility.singleLineHeight);

      float labelWidth = EditorGUIUtility.labelWidth;
      EditorGUIUtility.labelWidth = rect.width / 23f;

      int currentIndex = 0, indexSelected = 0;

      // validate and show UI
      currentIndex = _states.FindIndex( e => fromStateValue.stringValue == string.Empty || fromStateValue.stringValue == e );
      if ( currentIndex < 0 ) {
        Debug.LogWarning( $"TTable for {_prefabName} coudln't find state: {fromStateValue.stringValue}" );
        currentIndex = 0;
      }
      indexSelected = EditorGUI.Popup( fromRect, currentIndex, _states.ToArray() );
      fromStateValue.stringValue = _states[indexSelected];

      currentIndex = _states.FindIndex( e => toStateValue.stringValue == string.Empty || toStateValue.stringValue == e );
      if ( currentIndex < 0 ) {
        Debug.LogError( $"TTable for {_prefabName} coudln't find state: {toStateValue.stringValue}" );
        currentIndex = 0;
      }
      indexSelected = EditorGUI.Popup( toRect, "->", currentIndex, _states.ToArray() );
      toStateValue.stringValue = _states[indexSelected];

      gameEvent.objectReferenceValue =
          EditorGUI.ObjectField(
            whenRect, " :", gameEvent.objectReferenceValue, typeof( GameEventObject ), false );

      // revert view settings
      EditorGUIUtility.labelWidth = labelWidth;
    }

    private void DrawBaseTTableEntry( Rect rect, int index, bool isActive, bool isFocused ) {
      SerializedProperty element = _baseTablesList.serializedProperty.GetArrayElementAtIndex(index);
      Rect ttableRect = new(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
      element.objectReferenceValue =
          EditorGUI.ObjectField(
            ttableRect, "", element.objectReferenceValue, typeof( Packages.Estenis.StateMachine_.TransitionTable ), false );
    }

    private void DrawReferenceEntry( Rect rect, int index, bool isActive, bool isFocused ) {
      Rect ttableRect = new(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
      _references[index] =
          (GameObject) EditorGUI.ObjectField(
            ttableRect, "", _references[index], typeof( GameObject ), false );
    }

    private IEnumerable<GameObject> FindPrefabReferences( string ttableName ) {
      var references = AssetDatabase.FindAssets( "t:Prefab", new string[] { PREFABS_PATH } )
        .Select( guid => {
          string path = AssetDatabase.GUIDToAssetPath( guid );
          var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
          return go;
        } )
        .Where( go => {
          var sm = go.GetComponent<Packages.Estenis.StateMachine_.StateMachine>();
          return sm && sm._transitionTable != null && sm._transitionTable.name == ttableName;
        } )
        .Select( go => go );
      return references;
    }
  }
}