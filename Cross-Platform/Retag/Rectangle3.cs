using System.Runtime.InteropServices;

namespace AttentionAndRetag.Retag
{
    [StructLayout(LayoutKind.Explicit, Size =sizeof(double)*6)]
    public struct Rectangle3
    {
        [FieldOffset(0)]
        public Point3 Location;
        [FieldOffset(sizeof(double) * 3)]
        public Point3 Size;
        [FieldOffset(0)]
        public double X;
        [FieldOffset(sizeof(double) * 1)]
        public double Y;
        [FieldOffset(sizeof(double) * 2)]
        public double Z;
        [FieldOffset(sizeof(double) * 3)]
        public double Width;
        [FieldOffset(sizeof(double) * 4)]
        public double Height;
        [FieldOffset(sizeof(double) * 5)]
        public double Depth;

        public Rectangle3(Point3 location,
                          Point3 size) :this(location.X,
                              location.Y,
                              location.Z,
                              size.X,
                              size.Y,
                              size.Z)
        {
        }
        public Rectangle3(double x,
                          double y,
                          double z,
                          double xs,
                          double ys,
                          double zs)
        {
            X = x;
            Y = y;
            Z = z;
            Width = xs;
            Height = ys;
            Depth = zs;
            Location = new Point3(x, y, z);
            Size = new Point3(xs, ys, zs);
        }
        public Point3 End => new Point3(Location.X + Size.X - 1, Location.Y + Size.Y - 1, Location.Z + Size.Z - 1);
        public bool IsValid => Location.IsValid && Size.IsValid;
    }
}