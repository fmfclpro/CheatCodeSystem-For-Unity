using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

/*
MIT License

Copyright (c) 2023 Filipe Lopes | FMFCLPRO

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

namespace FMFCLPRO.CheatCodeSystem
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class CommandCheatAttribute : Attribute
    {
    }

    [Serializable]
    public class Cheat
    {
        [SerializeField] private string name;
        [SerializeField] private List<string> requirements = new();

        private Action<object[]> _cheatActivate;
        
        public string Name
        {
            get => name;
            set => name = value;
        }

        public List<string> Requirements
        {
            get => requirements;
            set => requirements = value;
        }

        public Action<object[]> OnCheatActivate
        {
            get => _cheatActivate;
            set => _cheatActivate = value;
        }

        public string ReadTypes()
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (var requiredType in Requirements)
            {
                stringBuilder.Append($"{requiredType} ");
            }

            return stringBuilder.ToString();
        }
    }

    public class CheatConsole : MonoBehaviour
    {
        [SerializeField] private List<Cheat> cheats = new List<Cheat>();
        [SerializeField] private KeyCode pressKey;

        private Vector2 _scrollPos = Vector2.zero;
        private List<Cheat> _cheatSuggestion;
        private string _input;
        private bool _turnCheat;
        
        private readonly Dictionary<Type, string> _typeToStr = new Dictionary<Type, string>
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(char), "char" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" }
        };

        private void Awake()
        {
            FindCheats();
        }

        private void OnGUI()
        {
            if (!_turnCheat) return;

            float y = Screen.height - 30;

            GUI_DisplayBox(y);

            GUI_CheatHelper(y);

            GUI_DisplayInput(y);

            var textField = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;

            if (textField != null && _input is { Length: 1 })
            {
                textField.cursorIndex = textField.selectIndex = _input.Length;
            }
        }

        private void GUI_CheatHelper(float y)
        {
            if (_cheatSuggestion == null || _input == "")
            {
                return;
            }

            float scrollViewHeight = _cheatSuggestion.Count * 20f;

            _scrollPos = GUI.BeginScrollView(
                new Rect(10f, y - scrollViewHeight, Screen.width - 20f, scrollViewHeight), _scrollPos,
                new Rect(0, 0, Screen.width - 40f, scrollViewHeight));

            for (int i = 0; i < _cheatSuggestion.Count; i++)
            {
                Rect suggestionRect = new Rect(0, i * 20f, Screen.width - 40f, 20f);

                GUIStyle greenTextStyle = new GUIStyle(GUI.skin.textField);
                greenTextStyle.normal.textColor = Color.green;
                greenTextStyle.focused.textColor = Color.green;
                greenTextStyle.hover.textColor = Color.green;
                string cheatFormat = $"{_cheatSuggestion[i].Name} {_cheatSuggestion[i].ReadTypes()}";

                GUI.Label(suggestionRect, cheatFormat, greenTextStyle);
            }

            GUI.EndScrollView();
        }

        private static void GUI_DisplayBox(float y)
        {
            GUI.backgroundColor = new Color(0, 0, 0, 255);
            GUI.Box(new Rect(0, y, Screen.width, 50), "");
        }


        private void GUI_DisplayInput(float y)
        {
            GUIStyle greenTextStyle = new GUIStyle(GUI.skin.textField);
            greenTextStyle.normal.textColor = Color.green;
            greenTextStyle.focused.textColor = Color.green;
            greenTextStyle.hover.textColor = Color.green;
            greenTextStyle.normal.background = null;
            greenTextStyle.active.background = null;

            bool inputFocused = (GUI.GetNameOfFocusedControl() == "input");

            if (!string.IsNullOrEmpty(_input) || GUIUtility.keyboardControl != 0)
            {
                if (!inputFocused)
                {
                    GUI.FocusControl("input");
                }
            }
            else
            {
                GUIUtility.keyboardControl = 0;
            }

            GUI.SetNextControlName("input");

            _input = GUI.TextField(new Rect(10f, y + 5f, Screen.width - 20f, 20f), _input, greenTextStyle);

            if (cheats != null && cheats.Count > 0 && _input != null && _input.Length > 0)
            {
                var split = _input.Split();

                _cheatSuggestion = cheats.Where(cheat =>
                    {
                        string lower = cheat.Name.ToLower();
                        string l2 = split[0].ToLower();
                        return lower.Contains(l2);
                    })
                    .ToList();
            }

            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return &&
                GUI.GetNameOfFocusedControl() == "input")
            {
                HandleInput();
                if (_cheatSuggestion != null) _cheatSuggestion.Clear();
                _input = "";
            }

            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape &&
                GUI.GetNameOfFocusedControl() == "input")
            {
                _turnCheat = !_turnCheat;
                _input = "";
                return;
            }

            if (Event.current.isKey && Event.current.keyCode == KeyCode.Tab && GUI.GetNameOfFocusedControl() == "input")
            {
                if (_cheatSuggestion != null && _cheatSuggestion.Count > 0)
                {
                    _input = _cheatSuggestion[0].Name;
                    GUI.FocusControl("input");
                    var textField =
                        GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;

                    if (textField != null)
                    {
                        textField.cursorIndex = textField.selectIndex = _input.Length;
                    }
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(pressKey))
            {
                _turnCheat = !_turnCheat;
            }
        }

        private void HandleInput()
        {
            string[] inputParts = _input.Split(' ');
            string cheatName = inputParts[0];
            object[] parameters = new object[inputParts.Length - 1];
            Array.Copy(inputParts, 1, parameters, 0, parameters.Length);

            Cheat cheat = cheats.Find(c => c.Name.Equals(cheatName, StringComparison.OrdinalIgnoreCase));

            if (cheat != null)
            {
                if (cheat.OnCheatActivate != null)
                {
                    cheat.OnCheatActivate(parameters);
                }
                else
                {
                    Debug.Log($"Cheat '{cheatName}' does not have an associated action.");
                }
            }
            else
            {
                Debug.Log($"Cheat '{cheatName}' not found.");
            }
        }

        public void FindCheats()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            var cheatMethods = assembly.GetTypes()
                .SelectMany(type => type.GetMethods())
                .Where(method => method.GetCustomAttributes(typeof(CommandCheatAttribute), true).Any());

            foreach (var cheatMethod in cheatMethods)
            {
                Cheat cheat = new Cheat();

                Type declaringType = cheatMethod.DeclaringType;

                object instance = FindObjectOfType(declaringType);

                if (instance == null) continue;

                cheat.Name = cheatMethod.Name;

                var parameters = cheatMethod.GetParameters();

                List<string> parameterDescriptions = new List<string>();

                foreach (var parameter in parameters)
                {
                    var hasValue = _typeToStr.TryGetValue(parameter.ParameterType, out string value);
                    string txt = hasValue ? value : parameter.ParameterType.ToString();
                    parameterDescriptions.Add($"<{txt}>[{parameter.Name}]");
                }

                cheat.Requirements = parameterDescriptions;

                cheat.OnCheatActivate = (parameterValues) =>
                {
                    if (parameterValues.Length == parameters.Length)
                    {
                        object[] typedParameters = new object[parameters.Length];

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            try
                            {
                                typedParameters[i] =
                                    Convert.ChangeType(parameterValues[i], parameters[i].ParameterType);
                            }
                            catch (Exception)
                            {
                                Debug.LogError($"Invalid parameter type for {cheat.Name}");
                                return;
                            }
                        }

                        cheatMethod.Invoke(instance, typedParameters);
                    }
                    else
                    {
                        Debug.LogError($"Invalid number of parameters for cheat '{cheat.Name}'");
                    }
                };

                cheats.Add(cheat);
            }
        }
    }
}