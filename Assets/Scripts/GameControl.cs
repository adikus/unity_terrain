using UnityEngine;

namespace Assets.Scripts
{
    public class GameControl : MonoBehaviour
    {
        public static GameControl Control;

        internal UI.UIControl UI;
        internal Map.Map Map;
        internal Terrain.TerrainControl Terrain;
        internal Paths.Paths Paths;

        //Config
        public int Width;
        public int Height;
        public int LandPercentage;
        public string Seed = "Test Seed 1";

        // Use this for initialization
        private void Start () {
            if (Control == null)
            {
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
            Terrain = new Terrain.TerrainControl();
        }
    }
}
