using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using Mono.Cecil;
using s3pi.Interfaces;

namespace Destrospean.MoreFavorites_Generator
{
    class Program
    {
        public enum Locales
        {
            ENG_US,
            CHS_CN,
            CHT_CN,
            CZE_CZ,
            DAN_DK,
            DUT_NL,
            FIN_FI,
            FRE_FR,
            GER_DE,
            GRE_GR,
            HUN_HU,
            ITA_IT,
            JPN_JP,
            KOR_KR,
            NOR_NO,
            POL_PL,
            POR_PT,
            POR_BR,
            RUS_RU,
            SPA_ES,
            SPA_MX,
            SWE_SE,
            THA_TH
        }

        public static void Main(string[] args)
        {
            Console.Write("Specify a unique suffix for the batch of favorites entries (leave blank for a random number suffix): ");

            // Get a unique name for the assembly and _XML resource
            string identifier = Console.ReadLine(),
            assemblyName = "MoreFavorites_" + (string.IsNullOrEmpty(identifier) ? FNV32.GetHash(Guid.NewGuid().ToString()).ToString() : identifier);

            // Load the base package and create a new package to clone to
            IPackage basePackage = s3pi.Package.Package.OpenPackage(0, "_MoreFavorites_Base.package"),
            newPackage = s3pi.Package.Package.NewPackage(0);

            // Get the assembly and XML
            AssemblyDefinition assembly = null;
            var xmlDocument = new System.Xml.XmlDocument();
            xmlDocument.Load(args.Length == 0 ? "_MoreFavorites_Base.xml" : args[0]);
            foreach (var resourceIndexEntry in basePackage.FindAll(x => x.Instance == FNV64.GetHash("MoreFavorites_Base")))
            {
                switch (resourceIndexEntry.ResourceType)
                {
                    case 0x73FAA07:
                        assembly = AssemblyDefinition.ReadAssembly(((ScriptResource.ScriptResource)s3pi.WrapperDealer.WrapperDealer.GetResource(0, basePackage, resourceIndexEntry)).Assembly.BaseStream);
                        break;
                }
            }

            Bitmap largeColorIMAG = new Bitmap(((APackage)basePackage).GetResource(basePackage.Find(x => x.Instance == 0x8613BD10D6A2A88F))),
            smallColorIMAG = new Bitmap(((APackage)basePackage).GetResource(basePackage.Find(x => x.Instance == 0x35B84649E916A075)));

            // Return early if no assembly is found
            if (assembly == null)
            {
                return;
            }

            // Copy the elements from the XML to put into the new package
            System.Xml.XmlNode rootNode = xmlDocument.SelectSingleNode("Favorites") ?? xmlDocument.SelectSingleNode("Favourites");
            List<System.Xml.XmlElement> favoriteColorElements = new List<System.Xml.XmlElement>(),
            favoriteFoodElements = new List<System.Xml.XmlElement>(),
            favoriteMusicElements = new List<System.Xml.XmlElement>();
            foreach (System.Xml.XmlNode node in rootNode.ChildNodes)
            {
                if ((node.Name == "FavoriteColor" || node.Name == "FavouriteColour") && !favoriteColorElements.Exists(x => node.Attributes["Name"].Value == x.GetAttribute("Name")))
                {
                    favoriteColorElements.Add((System.Xml.XmlElement)node);
                }
                if ((node.Name == "FavoriteFood" || node.Name == "FavouriteFood") && !favoriteFoodElements.Exists(x => node.Attributes["Recipe_Key"].Value == x.GetAttribute("Recipe_Key")))
                {
                    favoriteFoodElements.Add((System.Xml.XmlElement)node);
                }
                if ((node.Name == "FavoriteMusic" || node.Name == "FavouriteMusic") && !favoriteMusicElements.Exists(x => node.Attributes["Station_Name"].Value == x.GetAttribute("Station_Name")))
                {
                    favoriteMusicElements.Add((System.Xml.XmlElement)node);
                }
            }
            favoriteColorElements.RemoveAt(0);
            favoriteFoodElements.RemoveAt(0);
            favoriteMusicElements.RemoveAt(0);
            rootNode.RemoveAll();
            foreach (var favoriteColorElement in favoriteColorElements)
            {
                rootNode.AppendChild(favoriteColorElement);
            }
            foreach (var favoriteFoodElement in favoriteFoodElements)
            {
                rootNode.AppendChild(favoriteFoodElement);
            }
            foreach (var favoriteMusicElement in favoriteMusicElements)
            {
                rootNode.AppendChild(favoriteMusicElement);
            }

            // Rename the assembly for the new package
            assembly.Name.Name = assemblyName;
            assembly.MainModule.Name = assemblyName + ".dll";

            Stream assemblyStream = new MemoryStream(),
            xmlStream = new MemoryStream();

            // Save the assembly with the new name
            assembly.Write(assemblyStream);

            // Save the new XML to a stream
            xmlDocument.Save(xmlStream);

            // Add the resources
            var s3saKeyInstance = FNV64.GetHash(assemblyName);
            var nameMapResource = new NameMapResource.NameMapResource(0, null);
            var stblResources = new StblResource.StblResource[Enum.GetValues(typeof(Locales)).Length];
            for (var i = 0; i < stblResources.Length && favoriteColorElements.Count > 0; i++)
            {
                var stblKeyInstance = ulong.Parse(i.ToString("X2") + s3saKeyInstance.ToString("X16").Substring(2), System.Globalization.NumberStyles.HexNumber);
                var stblName = "Strings_" + ((Locales)i).ToString() + "_0x" + stblKeyInstance.ToString("X16");
                stblResources[i] = new StblResource.StblResource(0, null);
                foreach (var favoriteColorElement in favoriteColorElements)
                {
                    var colorName = favoriteColorElement.GetAttribute("Name");
                    stblResources[i].Add(FNV64.GetHash("Gameplay/Objects/Appliances/FutureBar:" + colorName), favoriteColorElement.GetAttribute("Drink_Display_Name") ?? "Gameplay/Objects/Appliances/FutureBar:" + colorName);
                    stblResources[i].Add(FNV64.GetHash("Gameplay/Objects/Plumbing/SonicShower:" + colorName), favoriteColorElement.GetAttribute("Display_Name") ?? "Gameplay/Objects/Plumbing/SonicShower:" + colorName);
                    stblResources[i].Add(FNV64.GetHash("Ui/Caption/CAS/Favorites/Color:" + colorName), favoriteColorElement.GetAttribute("Display_Name") ?? "Ui/Caption/CAS/Favorites/Color:" + colorName);
                }
                newPackage.AddResource(new ResourceKey(0x220557DA, 0, stblKeyInstance), stblResources[i].Stream, true);
                nameMapResource.Add(stblKeyInstance, stblName);
            }
            foreach (var favoriteColorElement in favoriteColorElements)
            {
                string hex = favoriteColorElement.GetAttribute("Hex"),
                imageKeyInstanceBase = "cas_favorites_color_i_" + favoriteColorElement.GetAttribute("Name");
                ulong largeIMAGKeyInstance = FNV64.GetHash(imageKeyInstanceBase + "_r2"),
                smallIMAGKeyInstance = FNV64.GetHash(imageKeyInstanceBase + "_s_r2");
                uint argb;
                int startIndex;
                if (hex != null && uint.TryParse(("FFFFFFFF".Remove(0, hex.Length - (startIndex = hex.StartsWith("#") ? 1 : hex.StartsWith("0x") ? 2 : 0))) + hex.Substring(startIndex), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out argb))
                {
                    Stream largeIMAGStream = new MemoryStream(),
                    smallIMAGStream = new MemoryStream();
                    TintBitmap(largeColorIMAG, Color.FromArgb((int)(argb | 0xFF000000)), 1).Save(largeIMAGStream, ImageFormat.Png);
                    TintBitmap(smallColorIMAG, Color.FromArgb((int)(argb | 0xFF000000)), 1).Save(smallIMAGStream, ImageFormat.Png);
                    newPackage.AddResource(new ResourceKey(0x2F7D0004, 0, largeIMAGKeyInstance), largeIMAGStream, true);
                    newPackage.AddResource(new ResourceKey(0x2F7D0004, 0, smallIMAGKeyInstance), smallIMAGStream, true);
                    nameMapResource.Add(largeIMAGKeyInstance, imageKeyInstanceBase + "_r2");
                    nameMapResource.Add(smallIMAGKeyInstance, imageKeyInstanceBase + "_s_r2");
                }
            }
            nameMapResource.Add(s3saKeyInstance, assemblyName);
            newPackage.AddResource(new ResourceKey(0x166038C, 0, 0), nameMapResource.Stream, true);
            newPackage.AddResource(new ResourceKey(0x333406C, 0, s3saKeyInstance), xmlStream, true);
            newPackage.AddResource(new ResourceKey(0x73FAA07, 0, s3saKeyInstance), new ScriptResource.ScriptResource(0, null)
                {
                    Assembly = new BinaryReader(assemblyStream)
                }.Stream, true);

            // Save the new package with the new name
            newPackage.SaveAs(assemblyName + ".package");
        }

        public static Bitmap TintBitmap(Bitmap source, Color tintColor, float intensity)
        {
            var result = new Bitmap(source.Width, source.Height);
            using (var graphics = Graphics.FromImage(result))
            {
                float r = tintColor.R / 255f * intensity,
                g = tintColor.G / 255f * intensity,
                b = tintColor.B / 255f * intensity;
                var colorMatrix = new ColorMatrix(new float[][]
                    {
                        new float[]
                        {
                            r,
                            0,
                            0,
                            0,
                            0
                        },
                        new float[]
                        {
                            0,
                            g,
                            0,
                            0,
                            0
                        },
                        new float[]
                        {
                            0,
                            0,
                            b,
                            0,
                            0
                        },
                        new float[]
                        {
                            0,
                            0,
                            0,
                            1,
                            0
                        },
                        new float[]
                        {
                            0,
                            0,
                            0,
                            0,
                            1
                        }
                    });
                using (var attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(colorMatrix);
                    graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            return result;
        }
    }
}
