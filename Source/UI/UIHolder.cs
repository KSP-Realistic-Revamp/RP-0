using System;
using UnityEngine;
using KSP.UI.Screens;

namespace RP0
{
    [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, false)]
    public class UIHolder : MonoBehaviour
    {
        public static GUISkin RescaledSkin
        {
            get
            {
                if (_rescaledSkin == null)
                    _rescaledSkin = RescaleSkin(_originalSkin);
                
                return _rescaledSkin;
            }
        }
        private static GUISkin _rescaledSkin = null;
        private static GUISkin _originalSkin = HighLogic.Skin;

        public static float UIScale => GameSettings.UI_SCALE * GameSettings.UI_SCALE_APPS;

        private bool _isGuiEnabled = false;
        private ApplicationLauncherButton _button;
        private TopWindow _tw;

        protected void Awake()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
            GameEvents.onGameSceneLoadRequested.Add(OnSceneChange);
            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);
        }

        protected void Start()
        {
            _tw = new TopWindow();
            _tw.Start();

            // force reset rescaling of the UI
            _rescaledSkin = null;
            Tooltip.RecreateInstance();    // Need to make sure that a new Tooltip instance is created after every scene change
        }

        protected void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiAppLauncherReady);
            GameEvents.onGameSceneLoadRequested.Remove(OnSceneChange);
            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);
            if (_button != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_button);
            }
        }

        protected void OnGUI()
        {
            if (_isGuiEnabled)
                _tw.OnGUI();
        }

        private void ShowWindow()
        {
            _isGuiEnabled = true;
        }

        private void HideWindow()
        {
            _isGuiEnabled = false;
        }

        private void OnSceneChange(GameScenes s)
        {
            if (s == GameScenes.FLIGHT)
                HideWindow();
        }

        private void OnGuiAppLauncherReady()
        {
            _button = ApplicationLauncher.Instance.AddModApplication(
                ShowWindow,
                HideWindow,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
                GameDatabase.Instance.GetTexture("RP-0/maintecost", false));
        }

        private void OnGameSettingsApplied()
        {
            _rescaledSkin = null;
            TopWindow.ResetUISize();
        }
        
        // Code ripped from principia
        // https://github.com/mockingbirdnest/Principia/blob/master/ksp_plugin_adapter/window_renderer.cs#L32-L82
        private static GUISkin RescaleSkin(GUISkin template)
        {
            GUISkin skin = UnityEngine.Object.Instantiate(template);

            // Creating a dynamic font as is done below results in Unity producing
            // incorrect character bounds and everything looks ugly.  They even
            // "document" it in their source code, see
            // https://github.com/Unity-Technologies/UnityCsReference/blob/57f723ec72ca50427e5d17cad0ec123be2372f67/Modules/GraphViewEditor/Views/GraphView.cs#L262.
            // So here I am, sizing a pangram to get an idea of the shape of things and
            // nudging pixels by hand.  It's the 90's, go for it!
            var pangram = new GUIContent("Portez ce vieux whisky au juge blond qui fume.");
            float buttonHeight = skin.button.CalcHeight(pangram, width : 1000);
            float labelHeight = skin.label.CalcHeight(pangram, width : 1000);
            float textAreaHeight = skin.textArea.CalcHeight(pangram, width : 1000);
            float textFieldHeight = skin.textField.CalcHeight(pangram, width : 1000);
            float toggleHeight = skin.toggle.CalcHeight(pangram, width : 1000);

            skin.font = Font.CreateDynamicFontFromOSFont(
                skin.font.fontNames,
                (int)(skin.font.fontSize * UIScale));

            skin.button.alignment = TextAnchor.MiddleCenter;
            skin.button.contentOffset = new Vector2(0, -buttonHeight * UIScale / 10);
            skin.button.fixedHeight = buttonHeight * UIScale;
            skin.horizontalSlider.fixedHeight = 21 * UIScale;
            skin.horizontalSliderThumb.fixedHeight = 21 * UIScale;
            skin.horizontalSliderThumb.fixedWidth = 12 * UIScale;
            skin.label.alignment = TextAnchor.MiddleLeft;
            skin.label.contentOffset = new Vector2(0, -labelHeight * UIScale / 20);
            skin.label.fixedHeight = labelHeight * UIScale;
            skin.textArea.alignment = TextAnchor.MiddleLeft;
            skin.textArea.contentOffset = new Vector2(0, -textAreaHeight * UIScale / 20);
            skin.textArea.fixedHeight = textAreaHeight * UIScale;
            skin.textField.alignment = TextAnchor.MiddleLeft;
            skin.textField.contentOffset = new Vector2(0, -textAreaHeight * UIScale / 20);
            skin.textField.fixedHeight = textFieldHeight * UIScale;
            skin.toggle.fixedHeight = toggleHeight * UIScale;
            skin.toggle.contentOffset = new Vector2(0, -toggleHeight * (UIScale - 1) / 3);
            skin.toggle.alignment = TextAnchor.UpperLeft;
            skin.toggle.margin = new RectOffset(
                (int)(skin.toggle.margin.left * UIScale),
                skin.toggle.margin.right,
                (int)(skin.toggle.margin.top * 1.7 * UIScale),
                skin.toggle.margin.bottom);
            return skin;
        }
        
        public static GUILayoutOption Width(float units)
        {
            return GUILayout.Width(units * UIScale);
        }
    }
}
