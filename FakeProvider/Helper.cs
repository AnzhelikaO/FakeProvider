using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeProvider
{
    public static class Helper
    {
        #region Inside

        public static bool Inside(int PointX, int PointY, int X, int Y, int Width, int Height) =>
            PointX >= X && PointY >= Y && PointX < X + Width && PointY < Y + Height;

        #endregion
        #region Clamp

        public static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);

        #endregion
    }
}
