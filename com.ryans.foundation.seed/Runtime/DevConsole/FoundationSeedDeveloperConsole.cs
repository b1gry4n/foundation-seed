using System;
using System.Collections.Generic;
using System.Linq;
using FoundationSeed.Diagnostics;
using FoundationSeed.Time;
using UnityEngine;

namespace FoundationSeed.DevConsole
{
    [DisallowMultipleComponent]
    public sealed class FoundationSeedDeveloperConsole : MonoBehaviour
    {
        public delegate bool ExternalCommandHandler(string commandText, out string output);
        public delegate string ExternalHelpProvider();

        private sealed class RegisteredCommand
        {
            public string CommandId;
            public string Description;
            public Func<string[], string> Handler;
        }

        private const string InputControlName = "FoundationSeedConsoleInput";
        private const int MaxTranscriptLines = 256;

        private static FoundationSeedDeveloperConsole instance;
        private static readonly Dictionary<string, RegisteredCommand> registeredCommands =
            new Dictionary<string, RegisteredCommand>(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> transcript = new List<string>();
        private bool isOpen;
        private string input = string.Empty;
        private Vector2 scrollPosition;
        private bool focusInputNextFrame;
        private bool scrollToBottomPending;

        public static event ExternalCommandHandler ExternalCommandRequested;
        public static event ExternalHelpProvider ExternalHelpRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime() => EnsureInstance();

        public static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            GameObject go = new GameObject("FoundationSeedDeveloperConsole");
            go.AddComponent<FoundationSeedDeveloperConsole>();
        }

        public static void SetConsoleOpen(bool open)
        {
            EnsureInstance();
            instance?.SetOpen(open);
        }

        public static void ToggleConsole()
        {
            EnsureInstance();
            if (instance != null)
            {
                instance.SetOpen(!instance.isOpen);
            }
        }

        public static void RegisterCommand(string commandId, Func<string[], string> handler)
        {
            RegisterCommand(commandId, string.Empty, handler);
        }

        public static void RegisterCommand(string commandId, string description, Func<string[], string> handler)
        {
            if (string.IsNullOrWhiteSpace(commandId) || handler == null)
            {
                return;
            }

            string id = commandId.Trim();
            registeredCommands[id] = new RegisteredCommand
            {
                CommandId = id,
                Description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim(),
                Handler = handler
            };
        }

        public static void UnregisterCommand(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
            {
                return;
            }

            registeredCommands.Remove(commandId.Trim());
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            AppendSystemLine("Console ready. Type 'help' for commands.");
            FoundationSeedSessionLogRuntime.Trace("DevConsole", "ServiceReady", "Foundation developer console ready.", this);
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            Event currentEvent = Event.current;
            bool submitPressed =
                currentEvent != null
                && currentEvent.type == EventType.KeyDown
                && (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter);

            float areaHeight = Mathf.Min(Screen.height * 0.58f, 420f);
            GUILayout.BeginArea(new Rect(16f, 16f, Screen.width - 32f, areaHeight), GUIContent.none, GUI.skin.window);
            GUILayout.Label("Developer Console");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            for (int i = 0; i < transcript.Count; i += 1)
            {
                GUILayout.Label(transcript[i]);
            }
            GUILayout.EndScrollView();

            if (scrollToBottomPending)
            {
                scrollPosition.y = 100000f;
                scrollToBottomPending = false;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(">", GUILayout.Width(16f));
            GUI.SetNextControlName(InputControlName);
            input = GUILayout.TextField(input ?? string.Empty, GUILayout.ExpandWidth(true));
            bool runClicked = GUILayout.Button("Run", GUILayout.Width(64f));
            GUILayout.EndHorizontal();

            if (focusInputNextFrame)
            {
                GUI.FocusControl(InputControlName);
                focusInputNextFrame = false;
            }

            if (submitPressed || runClicked)
            {
                SubmitCurrentInput();
                if (submitPressed && currentEvent != null)
                {
                    currentEvent.Use();
                }
            }

            GUILayout.EndArea();
        }

        private void SubmitCurrentInput()
        {
            string commandText = input ?? string.Empty;
            input = string.Empty;
            focusInputNextFrame = true;
            Execute(commandText);
        }

        private void SetOpen(bool open)
        {
            if (isOpen == open)
            {
                return;
            }

            isOpen = open;
            FoundationSeedGameTime.SetPaused(open, "FoundationSeedDeveloperConsole");
            FoundationSeedSessionLogRuntime.Trace("DevConsole", open ? "Opened" : "Closed", "Console state changed.", this);
            if (open)
            {
                focusInputNextFrame = true;
                scrollToBottomPending = true;
            }
        }

        private void Execute(string commandText)
        {
            string trimmed = string.IsNullOrWhiteSpace(commandText) ? string.Empty : commandText.Trim();
            AppendCommandLine(trimmed);

            if (trimmed.Length == 0)
            {
                AppendSystemLine("No command entered.");
                return;
            }

            FoundationSeedSessionLogRuntime.Trace("DevConsole", "Command", trimmed, this);

            if (TryExecuteBuiltIn(trimmed, out string commandOutput)
                || TryExecuteRegistered(trimmed, out commandOutput)
                || TryExecuteExternal(trimmed, out commandOutput))
            {
                AppendOutputLines(commandOutput);
                return;
            }

            AppendSystemLine("Unknown command. Type 'help'.");
        }

        private bool TryExecuteBuiltIn(string commandText, out string builtInOutput)
        {
            builtInOutput = string.Empty;
            switch (commandText.ToLowerInvariant())
            {
                case "help":
                case "/help":
                case "?":
                case "commands":
                    builtInOutput = BuildHelpOutput();
                    return true;

                case "clear":
                    transcript.Clear();
                    builtInOutput = "Console cleared.";
                    return true;

                case "log on":
                    FoundationSeedSessionLogRuntime.SetEnabled(true, "console");
                    builtInOutput = "Logging enabled.";
                    return true;

                case "log off":
                    FoundationSeedSessionLogRuntime.SetEnabled(false, "console");
                    builtInOutput = "Logging disabled.";
                    return true;

                case "log status":
                    builtInOutput = "LogEnabled=" + FoundationSeedSessionLogRuntime.IsEnabled +
                                    " JsonlPath=" + FoundationSeedSessionLogRuntime.LatestJsonlPath;
                    return true;

                case "log path":
                    builtInOutput = string.IsNullOrWhiteSpace(FoundationSeedSessionLogRuntime.LatestJsonlPath)
                        ? "No session log path yet."
                        : FoundationSeedSessionLogRuntime.LatestJsonlPath;
                    return true;

                case "pause":
                    FoundationSeedGameTime.SetPaused(true, "FoundationSeedDeveloperConsoleManualPause");
                    builtInOutput = "Gameplay paused.";
                    return true;

                case "resume":
                    FoundationSeedGameTime.SetPaused(false, "FoundationSeedDeveloperConsoleManualPause");
                    builtInOutput = "Gameplay resumed.";
                    return true;

                case "status":
                    builtInOutput = "Paused=" + FoundationSeedGameTime.IsPaused +
                                    " LogEnabled=" + FoundationSeedSessionLogRuntime.IsEnabled;
                    return true;

                case "close":
                    SetOpen(false);
                    builtInOutput = "Console closed.";
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryExecuteRegistered(string commandText, out string result)
        {
            result = string.Empty;
            string[] tokens = commandText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return false;
            }

            if (!registeredCommands.TryGetValue(tokens[0], out RegisteredCommand command) || command?.Handler == null)
            {
                return false;
            }

            try
            {
                string handlerOutput = command.Handler(tokens.Skip(1).ToArray());
                result = string.IsNullOrWhiteSpace(handlerOutput) ? "(ok)" : handlerOutput;
                return true;
            }
            catch (Exception exception)
            {
                result = "Command error: " + exception.Message;
                return true;
            }
        }

        private static bool TryExecuteExternal(string commandText, out string externalOutput)
        {
            externalOutput = string.Empty;
            ExternalCommandHandler handlers = ExternalCommandRequested;
            if (handlers == null)
            {
                return false;
            }

            Delegate[] invocationList = handlers.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i += 1)
            {
                if (((ExternalCommandHandler)invocationList[i]).Invoke(commandText, out externalOutput))
                {
                    if (string.IsNullOrWhiteSpace(externalOutput))
                    {
                        externalOutput = "(ok)";
                    }

                    return true;
                }
            }

            externalOutput = string.Empty;
            return false;
        }

        private static string BuildHelpOutput()
        {
            List<string> lines = new List<string>
            {
                "Built-ins:",
                "  help",
                "  clear",
                "  log on|off|status|path",
                "  pause",
                "  resume",
                "  status",
                "  close"
            };

            if (registeredCommands.Count > 0)
            {
                lines.Add("Project commands:");
                foreach (RegisteredCommand command in registeredCommands.Values.OrderBy(value => value.CommandId, StringComparer.OrdinalIgnoreCase))
                {
                    string suffix = string.IsNullOrWhiteSpace(command.Description) ? string.Empty : " - " + command.Description;
                    lines.Add("  " + command.CommandId + suffix);
                }
            }
            else
            {
                lines.Add("Project commands: none registered.");
                lines.Add("Add one in project code:");
                lines.Add("  FoundationSeedDeveloperConsole.RegisterCommand(\"mycmd\", \"does thing\", args => \"ok\");");
            }

            ExternalHelpProvider helpProviders = ExternalHelpRequested;
            if (helpProviders != null)
            {
                string[] externalLines = helpProviders.GetInvocationList()
                    .Select(handler => ((ExternalHelpProvider)handler).Invoke())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToArray();

                if (externalLines.Length > 0)
                {
                    lines.Add("External:");
                    for (int i = 0; i < externalLines.Length; i += 1)
                    {
                        lines.Add("  " + externalLines[i]);
                    }
                }
            }

            return string.Join("\n", lines);
        }

        private void AppendCommandLine(string commandText)
        {
            AppendTranscriptLine("> " + commandText);
        }

        private void AppendSystemLine(string message)
        {
            AppendTranscriptLine(message);
        }

        private void AppendOutputLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                AppendSystemLine("(ok)");
                return;
            }

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i += 1)
            {
                AppendTranscriptLine(lines[i]);
            }
        }

        private void AppendTranscriptLine(string line)
        {
            transcript.Add(line ?? string.Empty);
            if (transcript.Count > MaxTranscriptLines)
            {
                transcript.RemoveAt(0);
            }

            scrollToBottomPending = true;
        }
    }
}
