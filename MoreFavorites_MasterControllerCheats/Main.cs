using System.Collections.Generic;
using System.Reflection;
using NRaas.MasterControllerSpace.SelectionCriteria;
using NRaas.MasterControllerSpace.Sims.Intermediate.Favorites;
using Sims3.Gameplay.UI;
using Sims3.SimIFace.CAS;
using Sims3.UI.CAS;

namespace Destrospean.MoreFavorites.MasterControllerCheats
{
    public class Main
    {
        [Sims3.SimIFace.Tunable]
        protected static bool kInstantiator;

        public class ChangeFavoriteFoodPatch : ChangeFavoriteFood
        {
            protected override bool Run(Sims3.Gameplay.CAS.SimDescription me, bool singleSelection)
            {
                FieldInfo favoriteFoodField = typeof(ChangeFavoriteFood).GetField("mFavoriteFood", BindingFlags.NonPublic | BindingFlags.Instance);
                if (!ApplyAll)
                {
                    List<PreferenceFood.Item> options = new List<PreferenceFood.Item>();
                    foreach (FavoriteFoodType foodType in System.Enum.GetValues(typeof(FavoriteFoodType)))
                    {
                        if (0 < foodType && foodType < FavoriteFoodType.Count)
                        {
                            options.Add(new PreferenceFood.Item(foodType, me.FavoriteFood == foodType ? 1 : 0));
                        }
                    }
                    foreach (FavoriteFoodType foodType in FavoritesUtils.FavoriteFoodDictionary.Keys)
                    {
                        options.Add(new PreferenceFood.Item(foodType, me.FavoriteFood == foodType ? 1 : 0));
                    }
                    PreferenceFood.Item choice = new NRaas.CommonSpace.Selection.CommonSelection<PreferenceFood.Item>(Name, me.FullName, options).SelectSingle();
                    if (choice == null)
                    {
                        return false;
                    }
                    favoriteFoodField.SetValue(this, choice.Value);
                }
                me.mFavouriteFoodType = (FavoriteFoodType)favoriteFoodField.GetValue(this);
                if (Sims3.Gameplay.Core.PlumbBob.SelectedActor == me.CreatedSim)
                {
                    ((HudModel)Responder.Instance.HudModel).OnSimFavoritesChanged(me.CreatedSim.ObjectId);
                }
                return true;
            }
        }

        public class ChangeFavoriteMusicPatch : ChangeFavoriteMusic
        {
            protected override bool Run(Sims3.Gameplay.CAS.SimDescription me, bool singleSelection)
            {
                FieldInfo favoriteMusicField = typeof(ChangeFavoriteMusic).GetField("mFavoriteMusic", BindingFlags.NonPublic | BindingFlags.Instance);
                if (!ApplyAll)
                {
                    List<PreferenceMusic.Item> options = new List<PreferenceMusic.Item>();
                    foreach (FavoriteMusicType musicType in System.Enum.GetValues(typeof(FavoriteMusicType)))
                    {
                        if (0 < musicType && musicType < FavoriteMusicType.Count)
                        {
                            options.Add(new PreferenceMusic.Item(musicType, me.FavoriteMusic == musicType ? 1 : 0));
                        }
                    }
                    foreach (FavoriteMusicType musicType in FavoritesUtils.FavoriteMusicDictionary.Keys)
                    {
                        options.Add(new PreferenceMusic.Item(musicType, me.FavoriteMusic == musicType ? 1 : 0));
                    }
                    PreferenceMusic.Item choice = new NRaas.CommonSpace.Selection.CommonSelection<PreferenceMusic.Item>(Name, me.FullName, options).SelectSingle();
                    if (choice == null)
                    {
                        return false;
                    }
                    favoriteMusicField.SetValue(this, choice.Value);
                }
                me.mFavouriteMusicType = (FavoriteMusicType)favoriteMusicField.GetValue(this);
                if (Sims3.Gameplay.Core.PlumbBob.SelectedActor == me.CreatedSim)
                {
                    ((HudModel)Responder.Instance.HudModel).OnSimFavoritesChanged(me.CreatedSim.ObjectId);
                }
                return true;
            }
        }

        static Main()
        {
            BindingFlags nonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
            MoreFavorites.Main.ReplaceMethod(typeof(ChangeFavoriteFood).GetMethod("Run", nonPublicInstance), typeof(ChangeFavoriteFoodPatch).GetMethod("Run", nonPublicInstance));
            MoreFavorites.Main.ReplaceMethod(typeof(ChangeFavoriteMusic).GetMethod("Run", nonPublicInstance), typeof(ChangeFavoriteMusicPatch).GetMethod("Run", nonPublicInstance));
        }
    }
}
