﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

namespace UAssetAPI.Tests
{
    [TestClass]
    public class AssetUnitTests
    {
        /// <summary>
        /// Checks if two files have the same binary data.
        /// </summary>
        public void VerifyBinaryEquality(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            if (file1 == file2) return;

            fs1 = new FileStream(file1, FileMode.Open);
            fs2 = new FileStream(file2, FileMode.Open);

            if (fs1.Length != fs2.Length)
            {
                fs1.Close();
                fs2.Close();
                Assert.IsTrue(false);
            }

            do
            {
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            fs1.Close();
            fs2.Close();

            Assert.IsTrue((file1byte - file2byte) == 0);
        }

        /// <summary>
        /// Determines whether or not all exports in an asset have parsed correctly.
        /// </summary>
        /// <param name="tester">The asset to test.</param>
        /// <returns>true if all the exports in the asset have parsed correctly, otherwise false.</returns>
        public bool CheckAllExportsParsedCorrectly(UAsset tester)
        {
            foreach (Export testExport in tester.Exports)
            {
                if (testExport is RawExport) return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieves all the test assets in a particular folder.
        /// </summary>
        /// <param name="folder">The folder to check for test assets.</param>
        /// <returns>An array of paths to assets that should be tested.</returns>
        public string[] GetAllTestAssets(string folder)
        {
            List<string> allFilesToTest = Directory.GetFiles(folder, "*.uasset").ToList();
            allFilesToTest.AddRange(Directory.GetFiles(folder, "*.umap"));
            return allFilesToTest.ToArray();
        }

        /// <summary>
        /// Tests <see cref="FName.ToString"/> and <see cref="FName.FromString"/>.
        /// </summary>
        [TestMethod]
        public void TestNameConstruction()
        {
            var dummyAsset = new UAsset(Path.Combine("TestAssets", "TestManyAssets", "Astroneer", "Augment_BroadBrush.uasset"), EngineVersion.VER_UE4_23);

            FName test = FName.FromString(dummyAsset, "HelloWorld_0");
            Assert.IsTrue(test.Value.Value == "HelloWorld" && test.Number == 1);
            Assert.IsTrue(test.ToString() == "HelloWorld_0");

            test = FName.FromString(dummyAsset, "5_72");
            Assert.IsTrue(test.Value.Value == "5" && test.Number == 73);
            Assert.IsTrue(test.ToString() == "5_72");

            test = FName.FromString(dummyAsset, "_3");
            Assert.IsTrue(test.Value.Value == "_3" && test.Number == 0);
            Assert.IsTrue(test.ToString() == "_3");

            test = FName.FromString(dummyAsset, "hi_");
            Assert.IsTrue(test.Value.Value == "hi_" && test.Number == 0);
            Assert.IsTrue(test.ToString() == "hi_");

            test = FName.FromString(dummyAsset, "hi_01");
            Assert.IsTrue(test.Value.Value == "hi_01" && test.Number == 0);
            Assert.IsTrue(test.ToString() == "hi_01");

            test = FName.FromString(dummyAsset, "hi_10");
            Assert.IsTrue(test.Value.Value == "hi" && test.Number == 11);
            Assert.IsTrue(test.ToString() == "hi_10");

            test = FName.FromString(dummyAsset, "blah");
            Assert.IsTrue(test.Value.Value == "blah" && test.Number == 0);
            Assert.IsTrue(test.ToString() == "blah");

            test = new FName(dummyAsset, "HelloWorld", 2);
            Assert.IsTrue(test.ToString() == "HelloWorld_1");

            test = new FName(dummyAsset, "HelloWorld", 0);
            Assert.IsTrue(test.ToString() == "HelloWorld");
        }

        /// <summary>
        /// Tests modifying values within the class default object of an asset.
        /// Binary equality is expected.
        /// </summary>
        [TestMethod]
        public void TestCDOModification()
        {
            var tester = new UAsset(Path.Combine("TestAssets", "TestManyAssets", "Astroneer", "Augment_BroadBrush.uasset"), EngineVersion.VER_UE4_23);
            Assert.IsTrue(tester.VerifyBinaryEquality());

            NormalExport cdoExport = null;
            foreach (Export testExport in tester.Exports)
            {
                if (testExport.ObjectFlags.HasFlag(EObjectFlags.RF_ClassDefaultObject))
                {
                    cdoExport = (NormalExport)testExport;
                    break;
                }
            }
            Assert.IsNotNull(cdoExport);

            cdoExport["PickupActor"] = new ObjectPropertyData() { Value = FPackageIndex.FromRawIndex(0) };

            Assert.IsTrue(cdoExport["PickupActor"] is ObjectPropertyData);
            Assert.IsTrue(((ObjectPropertyData)cdoExport["PickupActor"]).Value.Index == 0);
        }

        /// <summary>
        /// MapProperties contain no easy way to determine the type of structs within them.
        /// For C++ classes, it is impossible without access to the headers, but for blueprint classes, the correct serialization is contained within the UClass.
        /// In this test, we take an asset with custom struct serialization in a map and extract data from the ClassExport in order to determine the correct serialization for the structs.
        /// Binary equality is expected.
        /// </summary>
        [TestMethod]
        public void TestCustomSerializationStructsInMap()
        {
            var tester = new UAsset(Path.Combine("TestAssets", "TestCustomSerializationStructsInMap", "wtf.uasset"), EngineVersion.VER_UE4_25);
            Assert.IsTrue(tester.VerifyBinaryEquality());

            // Get the map property in export 2
            Export exportTwo = FPackageIndex.FromRawIndex(2).ToExport(tester);
            Assert.IsTrue(exportTwo is NormalExport);

            NormalExport exportTwoNormal = (NormalExport)exportTwo;

            var mapPropertyName = FName.FromString(tester, "KekWait");
            MapPropertyData testMap = exportTwoNormal[mapPropertyName] as MapPropertyData;
            Assert.IsNotNull(testMap);
            Assert.IsTrue(testMap == exportTwoNormal[mapPropertyName.Value.Value]);

            // Get the first entry of the map
            StructPropertyData entryKey = testMap?.Value?.Keys?.ElementAt(0) as StructPropertyData;
            StructPropertyData entryValue = testMap?.Value?[0] as StructPropertyData;
            Assert.IsNotNull(entryKey?.Value?[0]);
            Assert.IsNotNull(entryValue?.Value?[0]);

            // Check that the properties are correct
            Assert.IsTrue(entryKey.Value[0] is VectorPropertyData);
            Assert.IsTrue(entryValue.Value[0] is LinearColorPropertyData);
        }

        /// <summary>
        /// In this test, we examine a cooked asset that has been modified by an external tool.
        /// As a result of external modification, the asset now has new name map entries whose hashes were left empty.
        /// Binary equality is expected. Expected behavior is for UAssetAPI to detect this and override its normal hash algorithm.
        /// </summary>
        [TestMethod]
        public void TestImproperNameMapHashes()
        {
            var tester = new UAsset(Path.Combine("TestAssets", "TestImproperNameMapHashes", "OC_Gatling_DamageB_B.uasset"), EngineVersion.VER_UE4_25);
            Assert.IsTrue(tester.VerifyBinaryEquality());

            Dictionary<string, bool> testingEntries = new Dictionary<string, bool>();
            testingEntries["/Game/WeaponsNTools/GatlingGun/Overclocks/OC_BonusesAndPenalties/OC_Bonus_MovmentBonus_150p"] = false;
            testingEntries["/Game/WeaponsNTools/GatlingGun/Overclocks/OC_BonusesAndPenalties/OC_Bonus_MovmentBonus_150p.OC_Bonus_MovmentBonus_150p"] = false;

            foreach (KeyValuePair<FString, uint> overrideHashes in tester.OverrideNameMapHashes)
            {
                if (testingEntries.ContainsKey(overrideHashes.Key.Value))
                {
                    Assert.IsTrue(overrideHashes.Value == 0);
                    testingEntries[overrideHashes.Key.Value] = true;
                }
            }

            foreach (KeyValuePair<string, bool> testingEntry in testingEntries)
            {
                Assert.IsTrue(testingEntry.Value);
            }
        }

        /// <summary>
        /// In this test, we examine a cooked asset that has been modified by an external tool.
        /// As a result of external modification, two identical entries now exist in the name map, which never occurs in assets cooked by the Unreal Engine.
        /// Binary equality is not expected, but the asset must successfully parse anyways.
        /// </summary>
        [TestMethod]
        public void TestDuplicateNameMapEntries()
        {
            var tester = new UAsset(Path.Combine("TestAssets", "TestDuplicateNameMapEntries", "BIOME_AzureWeald.uasset"), EngineVersion.VER_UE4_25);

            // Make sure a duplicate entry actually exists
            bool duplicatesExist = false;
            Dictionary<string, bool> enumeratedEntries = new Dictionary<string, bool>();
            foreach (FString entry in tester.GetNameMapIndexList())
            {
                if (enumeratedEntries.ContainsKey(entry.Value) && enumeratedEntries[entry.Value])
                {
                    duplicatesExist = true;
                    break;
                }
                enumeratedEntries[entry.Value] = true;
            }
            Assert.IsTrue(duplicatesExist);

            // Make sure all exports parsed correctly
            Assert.IsTrue(CheckAllExportsParsedCorrectly(tester));
        }

        /// <summary>
        /// In this test, we have an asset with a few properties that UAssetAPI has no serialization for. (The properties do not actually exist in the engine itself, so this is expected behavior.)
        /// UAssetAPI must fallback to UnknownPropertyType to parse the asset correctly and maintain binary equality.
        /// </summary>
        [TestMethod]
        public void TestUnknownProperties()
        {
            var tester = new UAsset(Path.Combine("TestAssets", "TestUnknownProperties", "BP_DetPack_Charge.uasset"), EngineVersion.VER_UE4_25);
            Assert.IsTrue(tester.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(tester));

            // Check that only the expected unknown properties are present
            Dictionary<string, bool> newUnknownProperties = new Dictionary<string, bool>();
            newUnknownProperties.Add("GarbagePropty", false);
            newUnknownProperties.Add("EvenMoreGarbageTestingPropertyy", false);

            foreach (Export testExport in tester.Exports)
            {
                if (testExport is NormalExport normalTestExport)
                {
                    foreach (PropertyData prop in normalTestExport.Data)
                    {
                        if (prop is UnknownPropertyData unknownProp)
                        {
                            string serializingType = unknownProp?.SerializingPropertyType?.Value;
                            Assert.AreNotEqual(serializingType, null);
                            Assert.IsTrue(newUnknownProperties.ContainsKey(serializingType));
                            newUnknownProperties[serializingType] = true;
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, bool> entry in newUnknownProperties)
            {
                Assert.IsTrue(entry.Value);
            }
        }

        private void TestManyAssetsSubsection(string game, EngineVersion version, Usmap mappings = null)
        {
            string[] allTestingAssets = GetAllTestAssets(Path.Combine("TestAssets", "TestManyAssets", game));
            foreach (string assetPath in allTestingAssets)
            {
                Console.WriteLine(assetPath);
                var tester = new UAsset(assetPath, version, mappings);
                Assert.IsTrue(tester.VerifyBinaryEquality());
                Assert.IsTrue(CheckAllExportsParsedCorrectly(tester));
                Console.WriteLine(tester.GetEngineVersion());
            }
        }

        private void TestUE5_3Subsection(string game, EngineVersion version, Usmap mappings = null)
        {
            string[] allTestingAssets = GetAllTestAssets(Path.Combine("TestAssets", "TestUE5_3", game));
            foreach (string assetPath in allTestingAssets)
            {
                Console.WriteLine(assetPath);
                var tester = new UAsset(assetPath, version, mappings);
                Assert.IsTrue(tester.VerifyBinaryEquality());
                Assert.IsTrue(CheckAllExportsParsedCorrectly(tester));
                Console.WriteLine(tester.GetEngineVersion());
            }
        }

        /// <summary>
        /// Tests the GUID/string conversion operations to ensure that they match the Unreal implementation.
        /// </summary>
        [TestMethod]
        public void TestGUIDs()
        {
            string input = "{CF873D05-4977-597A-F120-7F9F90B1ED09}";
            Guid test = input.ConvertToGUID();
            Assert.IsTrue(test.ConvertToString() == input);
            Assert.IsTrue(test.ToByteArray().SequenceEqual(UAPUtils.ConvertHexStringToByteArray("05 3D 87 CF 7A 59 77 49 9F 7F 20 F1 09 ED B1 90")));
        }

        /// <summary>
        /// In this test, we examine a variety of assets from different games and ensure that they parse correctly and maintain binary equality.
        /// </summary>
        [TestMethod]
        public void TestManyAssets()
        {
            TestManyAssetsSubsection("Astroneer", EngineVersion.VER_UE4_23);
            TestManyAssetsSubsection("Bloodstained", EngineVersion.VER_UE4_18);
            TestManyAssetsSubsection("MISC_426", EngineVersion.VER_UE4_26);
            TestManyAssetsSubsection("CodeVein", EngineVersion.VER_UE4_18);
            TestManyAssetsSubsection("StarlitSeason", EngineVersion.VER_UE4_24);
            TestManyAssetsSubsection("Tekken", EngineVersion.VER_UE4_14);
            TestManyAssetsSubsection("VERSIONED", EngineVersion.UNKNOWN);

            // traditional, NOT zen/io store. includes unversioned properties
            TestManyAssetsSubsection("LiesOfP", EngineVersion.VER_UE4_27, new Usmap(Path.Combine("TestAssets", "TestManyAssets", "LiesOfP", "LiesOfP.usmap")));
            TestManyAssetsSubsection("Palia", EngineVersion.VER_UE5_1, new Usmap(Path.Combine("TestAssets", "TestManyAssets", "Palia", "Palia.usmap")));
            TestManyAssetsSubsection("F1Manager2023", EngineVersion.VER_UE5_1, new Usmap(Path.Combine("TestAssets", "TestManyAssets", "F1Manager2023", "F1Manager2023.usmap")));
            TestManyAssetsSubsection("Palworld", EngineVersion.VER_UE5_1, new Usmap(Path.Combine("TestAssets", "TestManyAssets", "Palworld", "Palworld.usmap")));
        }

        /// <summary>
        /// In this test, we examine and modify a DataTable to ensure that it parses correctly and maintains binary equality.
        /// </summary>
        [TestMethod]
        public void TestDataTables()
        {
            var assetPath = Path.Combine("TestAssets", "TestManyAssets", "Bloodstained", "PB_DT_RandomizerRoomCheck.uasset");
            var tester = new UAsset(assetPath, EngineVersion.VER_UE4_18);
            Assert.IsTrue(tester.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(tester));
            Assert.IsTrue(tester.Exports.Count == 1);

            var ourDataTableExport = tester.Exports[0] as DataTableExport;
            var ourTable = ourDataTableExport?.Table;
            Assert.IsNotNull(ourTable);

            // Check out the first entry to make sure it's parsing alright, and flip all the flags for later testing
            StructPropertyData firstEntry = ourTable.Data[0];

            bool didFindTestName = false;
            for (int i = 0; i < firstEntry.Value.Count; i++)
            {
                var propData = firstEntry.Value[i];
                Console.WriteLine(i + ": " + propData.Name + ", " + propData.PropertyType);
                if (propData.Name == new FName(tester, "AcceleratorANDDoubleJump")) didFindTestName = true;
                if (propData is BoolPropertyData boolProp) boolProp.Value = !boolProp.Value;
            }
            Assert.IsTrue(didFindTestName);

            // Save the modified table
            tester.Write(Path.Combine("TestAssets", "MODIFIED.uasset"));

            // Load the modified table back in and make sure we're good
            var tester2 = new UAsset(Path.Combine("TestAssets", "MODIFIED.uasset"), EngineVersion.VER_UE4_18);
            Assert.IsTrue(tester2.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(tester2));
            Assert.IsTrue(tester2.Exports.Count == 1);

            // Flip the flags back to what they originally were
            firstEntry = (tester2.Exports[0] as DataTableExport)?.Table?.Data?[0];
            Assert.IsNotNull(firstEntry);
            for (int i = 0; i < firstEntry.Value.Count; i++)
            {
                if (firstEntry.Value[i] is BoolPropertyData boolProp) boolProp.Value = !boolProp.Value;
            }

            // Save and check that it's binary equal to what we originally had
            tester2.Write(tester2.FilePath);
            Assert.IsTrue(File.ReadAllBytes(assetPath).SequenceEqual(File.ReadAllBytes(Path.Combine("TestAssets", "MODIFIED.uasset"))));
        }

        private void TestJsonOnFile(string file, EngineVersion version, string subFolder = "TestJson", string mappingsFile = null)
        {
            Usmap mappings = string.IsNullOrEmpty(mappingsFile) ? null : new Usmap(Path.Combine("TestAssets", subFolder, mappingsFile));

            Console.WriteLine(file);
            var tester = new UAsset(Path.Combine("TestAssets", subFolder, file), version, mappings);
            Assert.IsTrue(tester.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(tester));

            string jsonSerializedAsset = tester.SerializeJson();
            File.WriteAllText(Path.Combine("TestAssets", subFolder, "raw.json"), jsonSerializedAsset);

            var tester2 = UAsset.DeserializeJson(File.ReadAllText(Path.Combine("TestAssets", subFolder, "raw.json")));
            tester2.Mappings = mappings;
            tester2.Write(Path.Combine("TestAssets", subFolder, "MODIFIED.uasset"));

            // For the assets we're testing binary equality is maintained and can be used as a metric of success, but binary equality is not guaranteed for all assets
            Assert.IsTrue(File.ReadAllBytes(Path.Combine("TestAssets", subFolder, file)).SequenceEqual(File.ReadAllBytes(Path.Combine("TestAssets", subFolder, "MODIFIED.uasset"))));
        }

        /// <summary>
        /// In this test, we serialize some assets to JSON and back to test if the JSON serialization system is functional.
        /// </summary>
        [TestMethod]
        public void TestJson()
        {
            TestJsonOnFile("PB_DT_RandomizerRoomCheck.uasset", EngineVersion.VER_UE4_18, Path.Combine("TestManyAssets", "Bloodstained"));
            TestJsonOnFile("m02VIL_004_Gimmick.umap", EngineVersion.VER_UE4_18, Path.Combine("TestManyAssets", "Bloodstained"));
            TestJsonOnFile("Staging_T2.umap", EngineVersion.VER_UE4_23, Path.Combine("TestManyAssets", "Astroneer"));
            TestJsonOnFile("Items.uasset", EngineVersion.VER_UE4_23); // string table
            //TestJsonOnFile("ABP_SMG_A.uasset", UE4Version.VER_UE4_25);
            TestJsonOnFile("WPN_LockOnRifle.uasset", EngineVersion.VER_UE4_25);
            TestJsonOnFile("Map_FrontEnd_Hotel_LS_Night.umap", EngineVersion.VER_UE4_27);
            TestJsonOnFile("AssetDatabase_AutoGenerated.uasset", EngineVersion.VER_UE4_27);
            TestJsonOnFile("RaceSimDataAsset.uasset", EngineVersion.VER_UE4_27);
            TestJsonOnFile("TurboAcres_Environment.uasset", EngineVersion.VER_UE4_27);
            TestJsonOnFile("MGA_HeavyWeapon_Parent.uasset", EngineVersion.VER_UE4_25, "TestJson", "Outriders.usmap");
        }

        /// <summary>
        /// In this test, we add a new property called "CoolProperty" in the tests assembly to test whether or not PropertyData-inheriting classes in dependent assemblies are registered by UAssetAPI.
        /// </summary>
        /// <see cref="CoolPropertyData"/>
        [TestMethod]
        public void TestCustomProperty()
        {
            var tester = new UAsset(Path.Combine("TestAssets", "TestCustomProperty", "AlternateStartActor.uasset"), EngineVersion.VER_UE4_23);
            Assert.IsTrue(tester.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(tester));

            // Make sure that there are no unknown properties, and that there is at least one CoolProperty with a value of 72
            bool hasCoolProperty = false;
            foreach (Export testExport in tester.Exports)
            {
                if (testExport is NormalExport normalTestExport)
                {
                    foreach (PropertyData prop in normalTestExport.Data)
                    {
                        Assert.IsFalse(prop is UnknownPropertyData);
                        if (prop is CoolPropertyData coolProp)
                        {
                            hasCoolProperty = true;
                            Assert.IsTrue(coolProp.Value == 72);
                        }
                    }
                }
            }
            Assert.IsTrue(hasCoolProperty);
        }

        /// <summary>
        /// In this test, we verify that Ace Combat 7 decryption works.
        /// Binary equality is expected.
        /// </summary>
        [TestMethod]
        public void TestACE7()
        {
            // Create copies of original files
            foreach (var path in Directory.GetFiles(Path.Combine("TestAssets", "TestACE7"), "*.*"))
            {
                File.Copy(path, path + ".bak", true);
            }

            // Decrypt them
            var decrypter = new AC7Decrypt();
            decrypter.Decrypt(Path.Combine("TestAssets", "TestACE7", "plwp_6aam_a0.uasset"), Path.Combine("TestAssets", "TestACE7", "plwp_6aam_a0.uasset"));
            decrypter.Decrypt(Path.Combine("TestAssets", "TestACE7", "ex02_IGC_03_Subtitle.uasset"), Path.Combine("TestAssets", "TestACE7", "ex02_IGC_03_Subtitle.uasset"));

            // Verify the files can be parsed
            var tester = new UAsset(Path.Combine("TestAssets", "TestACE7", "plwp_6aam_a0.uasset"), EngineVersion.VER_UE4_18);
            Assert.IsTrue(tester.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(tester));

            tester = new UAsset(Path.Combine("TestAssets", "TestACE7", "ex02_IGC_03_Subtitle.uasset"), EngineVersion.VER_UE4_18);
            Assert.IsTrue(tester.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(tester));

            // Encrypt them
            decrypter.Encrypt(Path.Combine("TestAssets", "TestACE7", "plwp_6aam_a0.uasset"), Path.Combine("TestAssets", "TestACE7", "plwp_6aam_a0.uasset"));
            decrypter.Encrypt(Path.Combine("TestAssets", "TestACE7", "ex02_IGC_03_Subtitle.uasset"), Path.Combine("TestAssets", "TestACE7", "ex02_IGC_03_Subtitle.uasset"));

            // Verify binary equality
            foreach (var path in Directory.GetFiles(Path.Combine("TestAssets", "TestACE7"), "*.bak"))
            {
                VerifyBinaryEquality(path, path.Substring(0, path.Length - 4));
            }
        }

        /// <summary>
        /// In this test, we verify that material assets parses correctly and maintains binary equality.
        /// Binary equality is expected.
        /// </summary>
        [TestMethod]
        public void TestMaterials()
        {
            // Create copies of original files
            foreach (var path in Directory.GetFiles(Path.Combine("TestAssets", "TestMaterials"), "*.*"))
            {
                File.Copy(path, path + ".bak", true);
            }

            // Verify the files can be parsed
            var tester = new UAsset(Path.Combine("TestAssets", "TestMaterials", "M_COM_DetailMaster_B.uasset"), EngineVersion.VER_UE4_18);
            Assert.IsTrue(tester.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(tester));

            tester = new UAsset(Path.Combine("TestAssets", "TestMaterials", "as_mt_base.uasset"), EngineVersion.VER_UE4_20);
            Assert.IsTrue(tester.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(tester));
        }

        /// <summary>
        /// In this test, we are trying to read a source asset.
        /// Binary equality is expected.
        /// </summary>
        [TestMethod]
        public void TestEditorAssets()
        {
            var soundClass = new UAsset(Path.Combine("TestAssets", "TestEditorAssets", "TestSoundClass.uasset"), EngineVersion.VER_UE4_27);
            Assert.IsTrue(soundClass.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(soundClass));

            var material = new UAsset(Path.Combine("TestAssets", "TestEditorAssets", "TestMaterial.uasset"), EngineVersion.VER_UE4_27);
            Assert.IsTrue(material.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(material));

            var blueprint = new UAsset(Path.Combine("TestAssets", "TestEditorAssets", "TestActorBP.uasset"), EngineVersion.VER_UE4_27);
            Assert.IsTrue(blueprint.VerifyBinaryEquality());
            Assert.IsTrue(CheckAllExportsParsedCorrectly(blueprint));
        }
      
        /// <summary>
        /// In this test, we test several traditional assets specifically from Unreal Engine 5.3 games.
        /// Binary equality is expected.
        /// </summary>
        [TestMethod]
        public void TestTraditionalUE5_3()
        {
            TestUE5_3Subsection("Engine", EngineVersion.VER_UE5_3, new Usmap(Path.Combine("TestAssets", "TestUE5_3", "Engine", "Engine.usmap")));
        }

        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            foreach (var path in Directory.GetDirectories("."))
            {
                if (Path.GetFileName(path).Length < 4 || Path.GetFileName(path).Substring(0, 4).ToLowerInvariant() != "test") continue;
                try
                {
                    Directory.Delete(path, true);
                }
                catch { }
            }
        }
    }
}
