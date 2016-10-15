namespace Assets.Scripts.Map.Utils
{
    public class Point2<T>
    {
        public T X { get; set; }
        public T Y { get; set; }
    }

    public class Point3<T1, T2>
    {
        public T1 X { get; set; }
        public T1 Y { get; set; }
        public T2 Z { get; set; }
    }
}
