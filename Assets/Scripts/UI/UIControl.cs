using System.Collections.Generic;

using UnityEngine;

namespace Assets.Scripts.UI
{
    public class UIControl : MonoBehaviour
    {
        public CameraControl CameraControl;
        public MouseControl MouseControl;
        public FPSCounter FPSCounter;
        public Dictionary<int, string> DebugLines;

        private GUIStyle _debugStyle;
        private GUIStyle _fpsStyle;
        private const string FPSFormat = "{0} FPS";

        private void Awake ()
        {
            DebugLines = new Dictionary<int, string>();
            _debugStyle = new GUIStyle
            {
                fontSize = 16,
                normal = {textColor = Color.white}
            };
            _fpsStyle = new GUIStyle(_debugStyle) {alignment = TextAnchor.UpperRight};

            CameraControl = new CameraControl();
            MouseControl = new MouseControl();
            FPSCounter = new FPSCounter();
        }

        // Update is called once per frame
        private void Update ()
        {
            MouseControl.Update();
            CameraControl.Update();
            FPSCounter.Update();
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(Screen.width - 210, 10, 200, 20), string.Format(FPSFormat, FPSCounter.FPS), _fpsStyle);
            foreach (var i in DebugLines.Keys)
            {
                GUI.Label(new Rect(10, 10 + i*30, 200, 20), DebugLines[i], _debugStyle);
            }
        }
    }
}
