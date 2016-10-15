using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Map.LocationID;
using Assets.Scripts.Map.Utils;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Assets.Scripts.Map.Objects
{
    public class MapObjects
    {
        public static decimal Version = 0.2m;

        public List<Point2<int>> Cities { get; set; }
        public List<Point2<int>> Towns { get; set; }

        private readonly string _saveFileName;

        public MapObjects()
        {
            _saveFileName = Application.persistentDataPath + "/mapObjects-" + GameControl.Map.Seed + "-" +
                            GameControl.Map.Width + "-" + GameControl.Map.Height + "-" +
                            GameControl.Map.LandPercentage + "-" + Version + ".yml";

            Load();
        }

        public void CreateCities()
        {
            if (Cities != null) return;

            Cities = new List<Point2<int>>();
            Towns = new List<Point2<int>>();

            var identification = new Identification();
            identification.IdentifyCities();

            Save();
        }

        private class SaveContainer
        {
            public List<Point2<int>> Cities { get; set; }
            public List<Point2<int>> Towns { get; set; }
        }

        public void Save()
        {
            var mapData = new SaveContainer
            {
                Cities = Cities,
                Towns = Towns
            };

            var serializer = new Serializer();
            Debug.Log("Saving: " + _saveFileName);
            var writer = new StreamWriter(_saveFileName);
            serializer.Serialize(writer, mapData);
            writer.Close();
        }

        public void Load()
        {
            if (!File.Exists(_saveFileName)) return;
            Debug.Log("Loading: " + _saveFileName);
            var reader = new StreamReader(_saveFileName);
            var deserializer = new Deserializer();
            var mapObjects = deserializer.Deserialize<SaveContainer>(reader);
            Cities = mapObjects.Cities;
            Towns = mapObjects.Towns;
        }
    }
}
