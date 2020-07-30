using MoyskleyTech.ImageProcessing.Image;
using System;
using System.Collections.Generic;
using System.Text;

namespace AttentionAndRetag.Retag
{
    public class Distortion
    {
        /*input:
    strength as floating point >= 0.  0 = no change, high numbers equal stronger correction.
    zoom as floating point >= 1.  (1 = no change in zoom)

algorithm:

    set halfWidth = imageWidth / 2
    set halfHeight = imageHeight / 2
    
    if strength = 0 then strength = 0.00001
    set correctionRadius = squareroot(imageWidth ^ 2 + imageHeight ^ 2) / strength

    for each pixel (x,y) in destinationImage
        set newX = x - halfWidth
        set newY = y - halfHeight

        set distance = squareroot(newX ^ 2 + newY ^ 2)
        set r = distance / correctionRadius
        
        if r = 0 then
            set theta = 1
        else
            set theta = arctangent(r) / r

        set sourceX = halfWidth + theta * newX * zoom
        set sourceY = halfHeight + theta * newY * zoom

        set color of pixel (x, y) to color of source image pixel at (sourceX, sourceY)

*/
        public static Image<T> CorrectImage<T>(ImageProxy<T> src,
                                               float strength,
                                               float zoom)
            where T:unmanaged
        {
            var halfwidth = src.Width/2;
            var halfHeight = src.Height / 2;
            if (strength == 0) strength = 0.000001f;
            var correctionRadius = Math.Sqrt(Math.Pow(src.Width, 2) + Math.Pow(src.Height, 2)) / strength;

            var scaleFactor = strength;
            Image<T> dst = Image<T>.Create((int)(src.Width* scaleFactor), (int)(src.Height* scaleFactor));
            dst.ApplyFilter((px, pt) => {
                var newX = pt.X - halfwidth* scaleFactor;
                var newY = pt.Y - halfHeight* scaleFactor;

                var distance = Math.Sqrt(newX * newX + newY * newY);
                var r = distance / correctionRadius;
                double theta;
                if (r == 0)
                    theta = 1;
                else
                    theta = Math.Atan(r) / r;

                var sourceX = halfwidth + theta * newX * zoom;
                var sourceY = halfwidth + theta * newY * zoom;
                return src[(int)sourceX, (int)sourceY];
            });
            return dst;
        }
    }
}
