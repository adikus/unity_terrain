using UnityEngine;

namespace Assets.Scripts
{
    public class GameControl : MonoBehaviour
    {
        public static GameControl Control;
        public static Map.Map Map;
        public static UI.UIControl UI;
        public static Terrain.TerrainControl Terrain;
        public static Paths.Paths Paths;

        //Config
        public int Width;
        public int Height;
        public int LandPercentage;
        public string Seed = "Test Seed 1";

        // Use this for initialization
        private void Start () {
            if (Control == null)
            {
                Application.runInBackground = true;
                DontDestroyOnLoad(Control);
                Control = this;
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            UI = GetComponent<UI.UIControl>();
            Paths = GetComponent<Paths.Paths>();

            Map = new Map.Map(Width, Height, LandPercentage, Seed);
            Map.Initialize();
            Terrain = new Terrain.TerrainControl();
        }
    }
}
