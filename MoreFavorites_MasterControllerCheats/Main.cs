using System.Collections.Generic;
using System.Reflection;
using NRaas.MasterControllerSpace.SelectionCriteria;
using NRaas.MasterControllerSpace.Sims.Intermediate.Favorites;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.UI;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI.CAS;
using Tuning = Sims3.Gameplay.Destrospean.MoreFavorites.MasterControllerCheats;

namespace Destrospean.MoreFavorites.MasterControllerCheats
{
    public class Main
    {
        [Tunable]
        protected static bool kInstantiator;

        public class ChangeFavoriteColorPatch : ChangeFavoriteColor
        {
            protected override bool Run(SimDescription me, bool singleSelection)
            {
                FieldInfo favoriteColorField = typeof(ChangeFavoriteColor).GetField("mFavoriteColor", BindingFlags.NonPublic | BindingFlags.Instance);
                if (!ApplyAll)
                {
                    List<PreferenceColor.Item> options = new List<PreferenceColor.Item>();
                    foreach (CASCharacter.NameColorPair nameColorPair in CASCharacter.kColors)
                    {
                        if (!Tuning.kAllowBlacklistedFavoritesInMasterControllerDialogs && nameColorPair.IsBlacklisted() || !Tuning.kAllowHiddenFavoritesInMasterControllerDialogs && nameColorPair.IsHidden())
                        {
                            continue;
                        }
                        options.Add(new PreferenceColor.Item(nameColorPair.mColor, me.FavoriteColor == nameColorPair.mColor ? 1 : 0));
                    }
                    PreferenceColor.Item choice = new NRaas.CommonSpace.Selection.CommonSelection<PreferenceColor.Item>(Name, me.FullName, options).SelectSingle();
                    if (choice == null)
                    {
                        return false;
                    }
                    favoriteColorField.SetValue(this, choice.Value);
                }
                me.FavoriteColor = (Color)favoriteColorField.GetValue(this);
                if (Sims3.Gameplay.Core.PlumbBob.SelectedActor == me.CreatedSim)
                {
                    ((HudModel)Responder.Instance.HudModel).OnSimFavoritesChanged(me.CreatedSim.ObjectId);
                }
                return true;
            }
        }

        public class ChangeFavoriteFoodPatch : ChangeFavoriteFood
        {
            protected override bool Run(SimDescription me, bool singleSelection)
            {
                FieldInfo favoriteFoodField = typeof(ChangeFavoriteFood).GetField("mFavoriteFood", BindingFlags.NonPublic | BindingFlags.Instance);
                if (!ApplyAll)
                {
                    List<PreferenceFood.Item> options = new List<PreferenceFood.Item>();
                    foreach (FavoriteFoodType foodType in System.Enum.GetValues(typeof(FavoriteFoodType)))
                    {
                        if (0 < foodType && foodType < FavoriteFoodType.Count)
                        {
                            if (!Tuning.kAllowBlacklistedFavoritesInMasterControllerDialogs && foodType.IsBlacklisted() || !Tuning.kAllowHiddenFavoritesInMasterControllerDialogs && foodType.IsHidden())
                            {
                                continue;
                            }
                            options.Add(new PreferenceFood.Item(foodType, me.FavoriteFood == foodType ? 1 : 0));
                        }
                    }
                    foreach (FavoriteFoodType foodType in FavoritesUtils.FavoriteFoodDictionary.Keys)
                    {
                        if (!Tuning.kAllowBlacklistedFavoritesInMasterControllerDialogs && foodType.IsBlacklisted() || !Tuning.kAllowHiddenFavoritesInMasterControllerDialogs && foodType.IsHidden())
                        {
                            continue;
                        }
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
            protected override bool Run(SimDescription me, bool singleSelection)
            {
                FieldInfo favoriteMusicField = typeof(ChangeFavoriteMusic).GetField("mFavoriteMusic", BindingFlags.NonPublic | BindingFlags.Instance);
                if (!ApplyAll)
                {
                    List<PreferenceMusic.Item> options = new List<PreferenceMusic.Item>();
                    foreach (FavoriteMusicType musicType in System.Enum.GetValues(typeof(FavoriteMusicType)))
                    {
                        if (0 < musicType && musicType < FavoriteMusicType.Count)
                        {
                            if (!Tuning.kAllowBlacklistedFavoritesInMasterControllerDialogs && musicType.IsBlacklisted() || !Tuning.kAllowHiddenFavoritesInMasterControllerDialogs && musicType.IsHidden())
                            {
                                continue;
                            }
                            options.Add(new PreferenceMusic.Item(musicType, me.FavoriteMusic == musicType ? 1 : 0));
                        }
                    }
                    foreach (FavoriteMusicType musicType in FavoritesUtils.FavoriteMusicDictionary.Keys)
                    {
                        if (!Tuning.kAllowBlacklistedFavoritesInMasterControllerDialogs && musicType.IsBlacklisted() || !Tuning.kAllowHiddenFavoritesInMasterControllerDialogs && musicType.IsHidden())
                        {
                            continue;
                        }
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

        public class PreferenceMusicItemPatch : PreferenceMusic.Item
        {
            public override void SetValue(FavoriteMusicType value, FavoriteMusicType storeType)
            {
                mValue = value;
                mName = CASCharacter.GetFavoriteMusic(value);
                SetThumbnail(ResourceKey.CreatePNGKey(CASCharacter.GetFavoriteMusicPngName(value), FavoritesUtils.FavoriteMusicDictionary.ContainsKey(storeType) ? 0 : ResourceUtils.ProductVersionToGroupId(Responder.Instance.GetProductVersionForStereoStation(storeType))));
            }
        }

        static Main()
        {
            BindingFlags nonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
            MoreFavorites.Main.ReplaceMethod(typeof(ChangeFavoriteColor).GetMethod("Run", nonPublicInstance), typeof(ChangeFavoriteColorPatch).GetMethod("Run", nonPublicInstance));
            MoreFavorites.Main.ReplaceMethod(typeof(ChangeFavoriteFood).GetMethod("Run", nonPublicInstance), typeof(ChangeFavoriteFoodPatch).GetMethod("Run", nonPublicInstance));
            MoreFavorites.Main.ReplaceMethod(typeof(ChangeFavoriteMusic).GetMethod("Run", nonPublicInstance), typeof(ChangeFavoriteMusicPatch).GetMethod("Run", nonPublicInstance));
            MoreFavorites.Main.ReplaceMethod(typeof(PreferenceMusic.Item).GetMethod("SetValue"), typeof(PreferenceMusicItemPatch).GetMethod("SetValue"));
        }
    }
}
