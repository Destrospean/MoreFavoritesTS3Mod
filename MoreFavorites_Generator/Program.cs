using System;
using System.Collections.Generic;
using System.IO;
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
            assemblyName = "MoreFavorites_" + (string.IsNullOrEmpty(identifier) ? System.Security.Cryptography.FNV32.GetHash(Guid.NewGuid().ToString()).ToString() : identifier);

            // Load the base package and create a new package to clone to
            IPackage basePackage = s3pi.Package.Package.OpenPackage(0, "_MoreFavorites_Base.package"),
            newPackage = s3pi.Package.Package.NewPackage(0);

            // Get the assembly and XML
            AssemblyDefinition assembly = null;
            var xmlDocument = new System.Xml.XmlDocument();
            xmlDocument.Load(args.Length == 0 ? "_MoreFavorites_Base.xml" : args[0]);
            foreach (var resourceIndexEntry in basePackage.FindAll(x => x.Instance == System.Security.Cryptography.FNV64.GetHash("MoreFavorites_Base")))
            {
                switch (resourceIndexEntry.ResourceType)
                {
                    case 0x73FAA07:
                        assembly = AssemblyDefinition.ReadAssembly(((ScriptResource.ScriptResource)s3pi.WrapperDealer.WrapperDealer.GetResource(0, basePackage, resourceIndexEntry)).Assembly.BaseStream);
                        break;
                }
            }

            // Return early if no assembly is found
            if (assembly == null)
            {
                return;
            }

            // Copy the elements from the XML to put into the new package
            System.Xml.XmlNode rootNode = xmlDocument.SelectSingleNode("Favorites");
            List<System.Xml.XmlElement> favoriteFoodElements = new List<System.Xml.XmlElement>(),
            favoriteMusicElements = new List<System.Xml.XmlElement>();
            foreach (System.Xml.XmlNode node in rootNode.ChildNodes)
            {
                if (node.Name == "FavoriteFood" && !favoriteFoodElements.Exists(x => node.Attributes["Recipe_Key"].Value == x.GetAttribute("Recipe_Key")))
                {
                    favoriteFoodElements.Add((System.Xml.XmlElement)node);
                }
                if (node.Name == "FavoriteMusic" && !favoriteMusicElements.Exists(x => node.Attributes["Station_Name"].Value == x.GetAttribute("Station_Name")))
                {
                    favoriteMusicElements.Add((System.Xml.XmlElement)node);
                }
            }
            favoriteFoodElements.RemoveAt(0);
            favoriteMusicElements.RemoveAt(0);
            rootNode.RemoveAll();
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
            var s3saKeyInstance = System.Security.Cryptography.FNV64.GetHash(assemblyName);
            var nameMapResource = new NameMapResource.NameMapResource(0, null);
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
    }
}
