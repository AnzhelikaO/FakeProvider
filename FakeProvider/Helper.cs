using System;
using System.Reflection;
using System.Reflection.Emit;

namespace FakeProvider
{
    static class Helper
    {
        #region Data


        private static int TypeCounter = 0;
        internal static ModuleBuilder ModuleBuilder { get; set; }

        #endregion

        #region Inside

        public static bool Inside(int PointX, int PointY, int X, int Y, int Width, int Height) =>
            PointX >= X && PointY >= Y && PointX < X + Width && PointY < Y + Height;

        #endregion
        #region Clamp

        public static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);

        #endregion
        #region CreateType

        public static Type CreateType() =>
            ModuleBuilder.DefineType($"FakeType{TypeCounter++}",
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout).CreateType();

        #endregion
    }
}
