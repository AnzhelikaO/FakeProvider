using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace test
{
    public class Provider
    {
        public string Name;
        public Provider(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }

    [StructLayout(LayoutKind.Sequential, Size = 13, Pack = 1)]
    public class Tile
    {
        public byte wall;
        public byte liquid;
        public byte bTileHeader;
        public byte bTileHeader2;
        public byte bTileHeader3;
        public ushort type;
        public short sTileHeader;
        public short frameX;
        public short frameY;

        public virtual Provider Provider() => null;
    }

    public class CustomTile : Tile
    {
        private static Provider _Provider = new Provider("test");
        public override Provider Provider() => _Provider;
    }

    public class MyClassBuilder
    {
        AssemblyName asemblyName;
        public MyClassBuilder(string ClassName)
        {
            this.asemblyName = new AssemblyName(ClassName);
        }
        public object CreateObject(string[] PropertyNames, Type[] Types)
        {
            if (PropertyNames.Length != Types.Length)
            {
                Console.WriteLine("The number of property names should match their corresopnding types number");
            }

            TypeBuilder DynamicClass = this.CreateClass();
            this.CreateConstructor(DynamicClass);
            for (int ind = 0; ind < PropertyNames.Count(); ind++)
                CreateProperty(DynamicClass, PropertyNames[ind], Types[ind]);
            Type type = DynamicClass.CreateType();

            return Activator.CreateInstance(type);
        }
        private TypeBuilder CreateClass()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(this.asemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(this.asemblyName.FullName
                                , TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout
                                , null);
            return typeBuilder;
        }
        private void CreateConstructor(TypeBuilder typeBuilder)
        {
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        }
        private void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName,
                    MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }

    public interface ITile
    {
        Provider Provider { get; }
        byte wall { get; set; }
    }

    public struct StructTile<T> : ITile
    {
        private static Provider _Provider;
        public Provider Provider => _Provider;

        public byte wall { get; set; }

        public override string ToString() => $"tile[{typeof(T)}] {wall}: {Provider}";
    }

    class Program
    {
        static void Main(string[] args)
        {
            Provider[] a = new Provider[100000000];
            Console.ReadKey();
            Console.WriteLine(a.Length);
            return;
            List<ITile> tiles = new List<ITile>();
            for (int i = 0; i < 10; i++)
            {
                Provider provider = new Provider($"Custom Provider {i}");
                AssemblyName assemblyName = new AssemblyName($"kek{i}");
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
                TypeBuilder typeBuilder = moduleBuilder.DefineType(assemblyName.FullName
                                    , TypeAttributes.Public |
                                    TypeAttributes.Class |
                                    TypeAttributes.AutoClass |
                                    TypeAttributes.AnsiClass |
                                    TypeAttributes.BeforeFieldInit |
                                    TypeAttributes.AutoLayout);
                Type type = typeBuilder.CreateType();
                Type tileType = typeof(StructTile<>).MakeGenericType(type);
                tileType.GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, provider);
                ITile tile = (ITile)Activator.CreateInstance(tileType);
                tiles.Add(tile);
                ITile tile2 = (ITile)Activator.CreateInstance(tileType);
                tile2.wall = 10;
                tiles.Add(tile2);
            }

            for (int i = 0; i < tiles.Count; i++)
                Console.WriteLine(tiles[i]);

            /*
            Provider provider = new Provider("Custom Provider");
            Type tileType = typeof(Tile);
            AssemblyName assemblyName = new AssemblyName("kek");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(assemblyName.FullName
                                , TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout
                                , typeof(Tile));
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_Provider", typeof(Provider), FieldAttributes.Private | FieldAttributes.Static);
            fieldBuilder.SetValue(null, provider);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod($"{tileType.FullName}.Provider",
                MethodAttributes.Public
                | MethodAttributes.HideBySig
                | MethodAttributes.NewSlot
                | MethodAttributes.Virtual
                | MethodAttributes.Final, CallingConventions.HasThis, typeof(Provider), Type.EmptyTypes);
            ILGenerator methodIl = methodBuilder.GetILGenerator();

            methodIl.Emit(OpCodes.Ldarg_0);
            methodIl.Emit(OpCodes.Ldfld, fieldBuilder);
            methodIl.Emit(OpCodes.Ret);

            MethodInfo methodToOverride = tileType.GetMethod("Provider");
            typeBuilder.DefineMethodOverride(methodBuilder, methodToOverride);

            Type customTileType = typeBuilder.CreateType();
            Tile customTile = (Tile)Activator.CreateInstance(customTileType);
            Console.WriteLine(customTile);
            */
        }
    }
}
