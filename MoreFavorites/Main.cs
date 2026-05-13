using System;
using System.Reflection;
using Sims3.UI.CAS;

namespace Destrospean.MoreFavorites
{
    public class Main
    {
        [Sims3.SimIFace.Tunable]
        protected static bool kInstantiator;

        static Main()
        {
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteFood"), typeof(Replacements).GetMethod("GetFavoriteFood"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteFoodName", BindingFlags.NonPublic | BindingFlags.Static), typeof(Replacements).GetMethod("GetFavoriteFood"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteFoodPngName"), typeof(Replacements).GetMethod("GetFavoriteFoodPngName"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteFoodSmallIcon"), typeof(Replacements).GetMethod("GetFavoriteFoodSmallIcon"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteMusic"), typeof(Replacements).GetMethod("GetFavoriteMusic"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteMusicName", BindingFlags.NonPublic | BindingFlags.Static), typeof(Replacements).GetMethod("GetFavoriteMusic"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteMusicPngName"), typeof(Replacements).GetMethod("GetFavoriteMusicPngName"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteMusicSmallIcon"), typeof(Replacements).GetMethod("GetFavoriteMusicSmallIcon"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetInstalledFavoriteFoodList"), typeof(Replacements).GetMethod("GetInstalledFavoriteFoodList"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetInstalledFavoriteMusicList"), typeof(Replacements).GetMethod("GetInstalledFavoriteMusicList"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetMusicIcon", BindingFlags.NonPublic | BindingFlags.Static), typeof(Replacements).GetMethod("GetMusicIcon"));
            ReplaceMethod(typeof(Sims3.Gameplay.CAS.CASLogic).GetMethod("GetRecipe"), typeof(Replacements).GetMethod("GetRecipe"));
            ReplaceMethod(typeof(Sims3.Gameplay.Objects.CookingObjects.EatHeldFood).GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Instance), typeof(Replacements.EatHeldFood).GetMethod("Run"));
            ReplaceMethod(typeof(Sims3.Gameplay.Objects.Appliances.FutureBar.OrderDrinks).GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Instance), typeof(Replacements.OrderDrinks).GetMethod("Run"));
            ReplaceMethod(typeof(Sims3.Gameplay.Objects.Electronics.StereoStationData).GetMethod("GetStationName"), typeof(Replacements).GetMethod("GetStationName"));
            Sims3.SimIFace.World.sOnWorldLoadFinishedEventHandler += (sender, e) => FavoritesUtils.InitFavorites();
        }

        /// <summary>This method was borrowed from Lazy Duchess' Mono Patcher</summary>
        static void ReplaceMethod(MethodInfo oldMethod, MethodInfo newMethod)
        {
            unsafe
            {
                byte[] replacementByteArray = new byte[40];
                System.Runtime.InteropServices.Marshal.Copy(newMethod.MethodHandle.Value, replacementByteArray, 0, 40);
                System.Runtime.InteropServices.Marshal.Copy(replacementByteArray, 0, oldMethod.MethodHandle.Value, 24);
                System.Runtime.InteropServices.Marshal.Copy(replacementByteArray, 28, new IntPtr(oldMethod.MethodHandle.Value.ToInt32() + 28), 12);
            }
        }
    }
}
