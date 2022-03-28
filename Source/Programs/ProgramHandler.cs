﻿using ClickThroughFix;
using ContractConfigurator;
using Contracts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RP0.Programs
{
    [KSPScenario((ScenarioCreationOptions)480, new GameScenes[] { GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION })]
    public class ProgramHandler : ScenarioModule
    {
        private static readonly int _windowId = "RP0ProgramsWindow".GetHashCode();

        private bool _showGUI;
        private Rect _windowRect = new Rect(3, 40, 425, 600);
        private GUIContent _gc;
        private Vector2 _scrollPos = new Vector2();
        private readonly List<string> _expandedPrograms = new List<string>();

        public static ProgramHandler Instance { get; private set; }
        public static ProgramHandlerSettings Settings { get; private set; }
        public static List<Program> Programs { get; private set; }

        public List<Program> ActivePrograms { get; private set; } = new List<Program>();

        public List<Program> CompletedPrograms { get; private set; } = new List<Program>();

        public int ActiveProgramLimit => 2;

        public override void OnAwake()
        {
            if (Instance != null)
            {
                Destroy(Instance);
            }
            Instance = this;

            GameEvents.onGUIMissionControlSpawn.Add(ShowMCGUI);
            GameEvents.onGUIMissionControlDespawn.Add(HideMCGUI);
            GameEvents.Contract.onCompleted.Add(OnContractComplete);
        }

        public void OnDestroy()
        {
            GameEvents.onGUIMissionControlSpawn.Remove(ShowMCGUI);
            GameEvents.onGUIMissionControlDespawn.Remove(HideMCGUI);
            GameEvents.Contract.onCompleted.Remove(OnContractComplete);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (Settings == null)
            {
                Settings = new ProgramHandlerSettings();
                foreach (ConfigNode cn in GameDatabase.Instance.GetConfigNodes("PROGRAMHANDLERSETTINGS"))
                    Settings.Load(cn);
            }

            if (Programs == null)
            {
                Programs = new List<Program>();
                foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes("RP0_PROGRAM"))
                {
                    Programs.Add(new Program(n));
                }
            }

            foreach (ConfigNode cn in node.GetNodes("ACTIVEPROGRAM"))
            {
                string progName = cn.GetValue(nameof(Program.name));
                Program programTemplate = Programs.FirstOrDefault(p => p.name == progName);
                var program = new Program(programTemplate);
                program.Load(cn);
                ActivePrograms.Add(program);
            }

            foreach (ConfigNode cn in node.GetNodes("COMPLETEDPROGRAM"))
            {
                string progName = cn.GetValue(nameof(Program.name));
                Program programTemplate = Programs.FirstOrDefault(p => p.name == progName);
                var program = new Program(programTemplate);
                program.Load(cn);
                CompletedPrograms.Add(program);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            foreach (Program program in ActivePrograms)
            {
                var cn = new ConfigNode("ACTIVEPROGRAM");
                program.Save(cn);
                node.AddNode(cn);
            }

            foreach (Program program in CompletedPrograms)
            {
                var cn = new ConfigNode("COMPLETEDPROGRAM");
                program.Save(cn);
                node.AddNode(cn);
            }
        }

        public void ProcessFunding()
        {
            foreach (Program p in ActivePrograms)
            {
                p.ProcessFunding();
            }
        }

        internal void OnGUI()
        {
            if (_showGUI)
            {
                _windowRect = ClickThruBlocker.GUILayoutWindow(_windowId, _windowRect, WindowFunction, "Programs", HighLogic.Skin.window);
                Tooltip.Instance.ShowTooltip(_windowId, contentAlignment: TextAnchor.MiddleLeft);
            }
        }

        private void OnContractComplete(Contract data)
        {
            StartCoroutine(ContractCompleteRoutine());
        }

        private IEnumerator ContractCompleteRoutine()
        {
            // The contract will only be seen as completed after the ContractSystem has run it's next update
            yield return null;

            for (int i = ActivePrograms.Count - 1; i >= 0; --i)
            {
                Program p = ActivePrograms[i];
                if (!p.CanComplete && p.AllObjectivesMet)
                {
                    p.MarkObjectivesComplete();
                }
            }
        }

        private void ShowMCGUI()
        {
            _showGUI = true;
        }

        private void HideMCGUI()
        {
            _showGUI = false;
        }

        private void WindowFunction(int windowID)
        {
            using (var scrollScope = new GUILayout.ScrollViewScope(_scrollPos))
            {
                _scrollPos = scrollScope.scrollPosition;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Active programs: {ActivePrograms.Count}/{ActiveProgramLimit}", HighLogic.Skin.label);
                GUILayout.EndHorizontal();

                foreach (Program p in ActivePrograms)
                {
                    DrawProgramSection(p);
                }

                foreach (Program p in Programs.Where(p => !ActivePrograms.Any(p2 => p.name == p2.name) &&
                                                          !CompletedPrograms.Any(p2 => p.name == p2.name)))
                {
                    DrawProgramSection(p);
                }

                foreach (Program p in CompletedPrograms)
                {
                    DrawProgramSection(p);
                }
            }

            GUI.DragWindow();

            Tooltip.Instance.RecordTooltip(_windowId);
        }

        private void DrawProgramSection(Program p)
        {
            bool isCompleted = p.IsComplete;
            bool isActive = p.IsActive;
            bool canAccept = p.CanAccept;
            bool canComplete = p.CanComplete;

            GUILayout.BeginVertical(HighLogic.Skin.box);
            GUILayout.BeginHorizontal();

            bool oldIsExpanded = _expandedPrograms.Contains(p.name);
            bool newIsExpanded = GUILayout.Toggle(oldIsExpanded, "ⓘ", HighLogic.Skin.button, GUILayout.ExpandWidth(false), GUILayout.Height(20));
            if (oldIsExpanded && !newIsExpanded)
                _expandedPrograms.Remove(p.name);
            if (newIsExpanded && !oldIsExpanded)
                _expandedPrograms.Add(p.name);

            GUILayout.Label(p.title, HighLogic.Skin.label, GUILayout.ExpandWidth(true));

            if (isCompleted)
            {
                GUILayout.Label("Completed", HighLogic.Skin.label);
            }
            else if (canAccept)
            {
                GUI.enabled = ActivePrograms.Count < ActiveProgramLimit;
                if (GUILayout.Button("Accept", HighLogic.Skin.button))
                {
                    ActivePrograms.Add(p.Accept());
                    ContractPreLoader.Instance.ResetGenerationFailure();
                }
                GUI.enabled = true;
            }
            else if (canComplete)
            {
                if (GUILayout.Button("Complete", HighLogic.Skin.button))
                {
                    CompleteProgram(p);
                }
            }
            else if (isActive)
            {
                GUILayout.Label("Active", HighLogic.Skin.label);
            }
            else
            {
                _gc ??= new GUIContent();
                _gc.text = "Unmet requirements";
                _gc.tooltip = p.requirementsPrettyText;
                GUI.enabled = false;
                GUILayout.Button(_gc, HighLogic.Skin.button);
                GUI.enabled = true;
            }

            GUILayout.EndHorizontal();

            if (newIsExpanded)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(p.description, HighLogic.Skin.label);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Objectives: ", HighLogic.Skin.label);
                GUILayout.Label(p.objectivesPrettyText, HighLogic.Skin.label);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Total funds: ", HighLogic.Skin.label);
                GUILayout.Label($"{p.TotalFunding:N0}", HighLogic.Skin.label);
                GUILayout.EndHorizontal();

                if (isActive || isCompleted)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Funds paid out: ", HighLogic.Skin.label);
                    GUILayout.Label($"{p.fundsPaidOut:N0}", HighLogic.Skin.label);
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("Nominal duration: ", HighLogic.Skin.label);
                GUILayout.Label($"{p.nominalDurationYears:0.#} years", HighLogic.Skin.label);
                GUILayout.EndHorizontal();

                if (isActive || isCompleted)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Accepted: ", HighLogic.Skin.label);
                    GUILayout.Label(KSPUtil.dateTimeFormatter.PrintDateCompact(p.acceptedUT, false, false), HighLogic.Skin.label);
                    GUILayout.EndHorizontal();
                }

                if (isCompleted)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Completed: ", HighLogic.Skin.label);
                    GUILayout.Label(KSPUtil.dateTimeFormatter.PrintDateCompact(p.completedUT, false, false), HighLogic.Skin.label);
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }

        private void CompleteProgram(Program p)
        {
            ActivePrograms.Remove(p);
            CompletedPrograms.Add(p);
            p.Complete();
            ContractPreLoader.Instance.ResetGenerationFailure();
        }
    }
}