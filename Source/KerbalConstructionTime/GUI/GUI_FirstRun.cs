﻿using UnityEngine;

namespace KerbalConstructionTime
{
    public static partial class KCT_GUI
    {
        private static Rect _firstRunWindowPosition = new Rect((Screen.width - 150) / 2, Screen.height / 5, 300, 50);

        public static void DrawFirstRun(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Follow the steps below to get set up.");

            int step = 1;
            if (PresetManager.Instance.Presets.Count > 1 && GUILayout.Button($"{step++}) Choose a Preset", HighLogic.Skin.button))
            {
                ShowSettings();
            }

            GUILayout.Label($"{step++}) If you want to play from a different site than Cape Canaveral, switch to the Tracking Station and select a new site. You will only get a free launch complex in one site.");

            if (!KCTGameStates.StarterLCSelected)
            {
                GUILayout.Label($"{step++}) Choose a starting Launch Complex:");

                if (GUILayout.Button("1t Capacity (for small rockets)", HighLogic.Skin.button))
                {
                    CreateStartingPad(LCItem.StartingLC1);
                    Utilities.AddFunds(PresetManager.Instance.ActivePreset.GeneralSettings.SmallLCExtraFunds, TransactionReasons.None);
                    KCTGameStates.StarterLCSelected = true;
                }
                GUILayout.Label($"Also gives √{PresetManager.Instance.ActivePreset.GeneralSettings.SmallLCExtraFunds:N0}");

                GUILayout.Label("or", GetLabelCenterAlignStyle());

                if (GUILayout.Button($"15t Capacity (min: {LCItem.CalcMassMin(15):N0}t)", HighLogic.Skin.button))
                {
                    CreateStartingPad(LCItem.StartingLC15);
                    KCTGameStates.StarterLCSelected = true;
                }
            }
            
            if (!IsPrimarilyDisabled && KCTGameStates.UnassignedPersonnel > 0 )
            {
                GUILayout.Label($"{step++}) Assign your {KCTGameStates.UnassignedPersonnel} Applicants");
                if (GUILayout.Button($"Go to Staffing", HighLogic.Skin.button))
                {
                    GUIStates.ShowPersonnelWindow = true;
                }
            }

            GUILayout.Label("");

            if (GUILayout.Button("Understood", HighLogic.Skin.button))
            {
                GUIStates.ShowFirstRun = false;
            }

            GUILayout.EndVertical();
            if (!Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
                GUI.DragWindow();
        }

        private static void CreateStartingPad(LCItem.LCData lcTemplate)
        {
            LCItem starterLC = new LCItem(lcTemplate, KCTGameStates.ActiveKSC)
            {
                IsOperational = true
            };
            KCTGameStates.ActiveKSC.LaunchComplexes.Add(starterLC);
            KCTEvents.OnLCConstructionComplete.Fire(null, starterLC);
        }
    }
}

/*
    KerbalConstructionTime (c) by Michael Marvin, Zachary Eck

    KerbalConstructionTime is licensed under a
    Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

    You should have received a copy of the license along with this
    work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
*/
