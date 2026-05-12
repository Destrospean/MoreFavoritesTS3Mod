using System.Collections.Generic;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI.CAS;

namespace Destrospean.MoreFavorites
{
    public static class FavoritesUtils
    {
        static Dictionary<FavoriteFoodType, FavoriteFood> sFavoriteFoodDictionary;

        static Dictionary<FavoriteMusicType, FavoriteMusic> sFavoriteMusicDictionary;

        public static Dictionary<FavoriteFoodType, FavoriteFood> FavoriteFoodDictionary
        {
            get
            {
                if (sFavoriteFoodDictionary == null)
                {
                    InitFavorites();
                }
                return sFavoriteFoodDictionary;
            }
        }

        public static Dictionary<FavoriteMusicType, FavoriteMusic> FavoriteMusicDictionary
        {
            get
            {
                if (sFavoriteMusicDictionary == null)
                {
                    InitFavorites();
                }
                return sFavoriteMusicDictionary;
            }
        }

        public class FavoriteBase
        {
            public readonly string IconKey, SmallIconKey;

            public FavoriteBase(string iconKey, string smallIconKey)
            {
                IconKey = iconKey;
                SmallIconKey = smallIconKey;
            }
        }

        public class FavoriteFood : FavoriteBase
        {
            public readonly Recipe Recipe;

            public FavoriteFood(Recipe recipe, string iconKey, string smallIconKey) : base(iconKey, smallIconKey)
            {
                Recipe = recipe;
            }
        }

        public class FavoriteMusic : FavoriteBase
        {
            public readonly StereoStationData StereoStationData;

            public FavoriteMusic(StereoStationData stereoStationData, string iconKey, string smallIconKey) : base(iconKey, smallIconKey)
            {
                StereoStationData = stereoStationData;
            }
        }

        public static void InitFavorites()
        {
            List<CASCharacter.NameColorPair> favoriteColorList = new List<CASCharacter.NameColorPair>(CASCharacter.kColors);
            Dictionary<FavoriteFoodType, FavoriteFood> favoriteFoodDictionary = new Dictionary<FavoriteFoodType, FavoriteFood>();
            Dictionary<FavoriteMusicType, FavoriteMusic> favoriteMusicDictionary = new Dictionary<FavoriteMusicType, FavoriteMusic>();
            foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType("Destrospean.MoreFavorites.Data") == null)
                {
                    continue;
                }
                System.Xml.XmlReader reader = Simulator.ReadXml(new ResourceKey(ResourceUtils.HashString64(assembly.GetName().Name), 0x333406C, 0));
                while (reader.Read())
                {
                    if (reader.NodeType == System.Xml.XmlNodeType.Element)
                    {
                        if (reader.Name == "Favorites" || reader.Name == "Favourites")
                        {
                            reader.MoveToContent();
                        }
                        else if (reader.Name == "FavoriteColor" || reader.Name == "FavouriteColour")
                        {
                            string hex = reader.GetAttribute("Hex"),
                            name = reader.GetAttribute("Name");
                            uint argb;
                            int startIndex;
                            if (hex != null && name != null && uint.TryParse(("FFFFFFFF".Remove(0, hex.Length - (startIndex = hex.StartsWith("#") ? 1 : hex.StartsWith("0x") ? 2 : 0))) + hex.Substring(startIndex), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out argb))
                            {
                                favoriteColorList.Add(new CASCharacter.NameColorPair(name, new Color(argb)));
                            }
                        }
                        else if (reader.Name == "FavoriteFood" || reader.Name == "FavouriteFood")
                        {
                            Recipe recipe; 
                            if (Recipe.NameToRecipeHash.TryGetValue(reader.GetAttribute("Recipe_Key"), out recipe))
                            {
                                favoriteFoodDictionary.Add((FavoriteFoodType)ResourceUtils.HashString32(recipe.ToString()), new FavoriteFood(recipe, reader.GetAttribute("Icon_Key"), reader.GetAttribute("Small_Icon_Key")));
                            }
                        }
                        else if (reader.Name == "FavoriteMusic" || reader.Name == "FavouriteMusic")
                        {
                            StereoStationData stereoStationData;
                            if (StereoStationData.sStereoStationDictionary.TryGetValue("Gameplay/Excel/Stereo/Stations:" + reader.GetAttribute("Station_Name"), out stereoStationData))
                            {
                                favoriteMusicDictionary.Add((FavoriteMusicType)ResourceUtils.HashString32(stereoStationData.mStationName), new FavoriteMusic(stereoStationData, reader.GetAttribute("Icon_Key"), reader.GetAttribute("Small_Icon_Key")));
                            }
                        }
                    }
                }
                reader.Close();
            }
            CASCharacter.kColors = favoriteColorList.ToArray();
            sFavoriteFoodDictionary = favoriteFoodDictionary;
            sFavoriteMusicDictionary = favoriteMusicDictionary;
        }
    }
}
