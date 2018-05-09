using System;
using System.Collections.Generic;
using System.Linq;
using Commonwealth.Script.EditorUtils;
using CW.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CW.Editor
{
    [CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
    public class EnumFlagsAttributeDrawer : PropertyDrawer
    {
        private string[] _enumNames;
        private readonly Dictionary<string, int> _enumNameToValue = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _enumNameToDisplayName = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _enumNameToTooltip = new Dictionary<string, string>();
        private readonly List<string> _activeEnumNames = new List<string>();

        private SerializedProperty _serializedProperty;
        private ReorderableList _reorderableList;

        private bool _firstTime = true;

        private Type EnumType
        {
            get { return fieldInfo.FieldType; }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _serializedProperty = property;
            SetupIfFirstTime();
            return _reorderableList.GetHeight();
        }

        private void SetupIfFirstTime()
        {
            if (!_firstTime)
            {
                return;
            }

            _enumNames = _serializedProperty.enumNames;

            CacheEnumMetadata();
            ParseActiveEnumNames();

            _reorderableList = GenerateReorderableList();
            _firstTime = false;
        }

        private void CacheEnumMetadata()
        {
            for (var index = 0; index < _enumNames.Length; index++)
            {
                _enumNameToDisplayName[_enumNames[index]] = _serializedProperty.enumDisplayNames[index];
            }

            foreach (string enumName in _enumNames)
            {
                _enumNameToTooltip[enumName] = EnumType.Name + "." + enumName;
            }

            foreach (string name in _enumNames)
            {
                _enumNameToValue.Add(name, (int) Enum.Parse(EnumType, name));
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginDisabledGroup(_serializedProperty.hasMultipleDifferentValues);
            _reorderableList.DoList(position);
            EditorGUI.EndDisabledGroup();
        }

        private ReorderableList GenerateReorderableList()
        {
            return new ReorderableList(_activeEnumNames, typeof(string), false, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect,
                        new GUIContent(_serializedProperty.displayName, "EnumType: " + EnumType.Name));
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.y += 2;
                    EditorGUI.LabelField(
                        new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        new GUIContent(_enumNameToDisplayName[_activeEnumNames[index]],
                            _enumNameToTooltip[_activeEnumNames[index]]),
                        EditorStyles.label);
                },
                onAddDropdownCallback = (buttonRect, l) =>
                {
                    var menu = new GenericMenu();
                    foreach (string enumName in _enumNames)
                    {
                        if (_activeEnumNames.Contains(enumName) == false)
                        {
                            menu.AddItem(new GUIContent(_enumNameToDisplayName[enumName]),
                                false, data =>
                                {
                                    if (_enumNameToValue[(string) data] == 0)
                                    {
                                        _activeEnumNames.Clear();
                                    }

                                    _activeEnumNames.Add((string) data);
                                    SaveActiveValues();
                                    ParseActiveEnumNames();
                                },
                                enumName);
                        }
                    }

                    menu.ShowAsContext();
                },
                onRemoveCallback = l =>
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(l);
                    SaveActiveValues();
                    ParseActiveEnumNames();
                }
            };
        }


        private void ParseActiveEnumNames()
        {
            _activeEnumNames.Clear();
            foreach (string enumValue in _enumNames)
            {
                if (IsFlagSet(enumValue))
                {
                    _activeEnumNames.Add(enumValue);
                }
            }
        }

        private bool IsFlagSet(string enumValue)
        {
            if (_enumNameToValue[enumValue] == 0)
            {
                return _serializedProperty.intValue == 0;
            }

            return (_serializedProperty.intValue & _enumNameToValue[enumValue]) == _enumNameToValue[enumValue];
        }

        private void SaveActiveValues()
        {
            _serializedProperty.intValue = ConvertActiveNamesToInt();
            _serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private int ConvertActiveNamesToInt()
        {
            return _activeEnumNames.Aggregate(0, (current, activeEnumName) => current | _enumNameToValue[activeEnumName]);
        }
    }
}