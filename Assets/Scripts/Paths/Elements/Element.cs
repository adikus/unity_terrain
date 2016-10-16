using System.Collections.Generic;

namespace Assets.Scripts.Paths.Elements
{
    public interface IElement
    {
        int X { get; set; }
        int EndX { get; set; }
        int Y { get; set; }
        int EndY { get; set; }
        float Z { get; set; }
        float EndZ { get; set; }
        int Direction { get; set; }
        int EndDirection { get; set; }

        float Cost { get; set; }

        IElement Previous { get; set; }
        Types Type { get; set; }

        List<IElement> PossibleNext();
        bool IsPossible();
        void Show();

        string ToString();
    }
}
