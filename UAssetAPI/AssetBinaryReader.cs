using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UAssetAPI.IO;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

namespace UAssetAPI
{
    /// <summary>
    /// Any binary reader used in the parsing of Unreal file types.
    /// </summary>
    public class UnrealBinaryReader : BinaryReader
    {
        public UnrealBinaryReader(Stream stream) : base(stream)
        {

        }

        protected byte[] ReverseIfBigEndian(byte[] data)
        {
            if (!BitConverter.IsLittleEndian) Array.Reverse(data);
            return data;
        }

        public override short ReadInt16()
        {
            return BitConverter.ToInt16(ReverseIfBigEndian(base.ReadBytes(2)), 0);
        }

        public override ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(ReverseIfBigEndian(base.ReadBytes(2)), 0);
        }

        public override int ReadInt32()
        {
            if (BaseStream.Length - BaseStream.Position < 4)
            {
                Console.WriteLine($"Stream length: {BaseStream.Length}, Current position: {BaseStream.Position}, Remaining bytes: {BaseStream.Length - BaseStream.Position}");
                throw new EndOfStreamException("Not enough data to read 4 bytes for Int32 conversion.");
            }
            byte[] bytes = base.ReadBytes(4);
            Console.WriteLine($"ReadInt32 bytes: {BitConverter.ToString(bytes)}, Stream position: {BaseStream.Position}");
            int result = BitConverter.ToInt32(ReverseIfBigEndian(bytes), 0);
            Console.WriteLine($"ReadInt32 result: {result}, Stream position after read: {BaseStream.Position}");
            return result;
        }

        public override uint ReadUInt32()
        {
            return BitConverter.ToUInt32(ReverseIfBigEndian(base.ReadBytes(4)), 0);
        }

        public override long ReadInt64()
        {
            return BitConverter.ToInt64(ReverseIfBigEndian(base.ReadBytes(8)), 0);
        }

        public override ulong ReadUInt64()
        {
            return BitConverter.ToUInt64(ReverseIfBigEndian(base.ReadBytes(8)), 0);
        }

        public override float ReadSingle()
        {
            return BitConverter.ToSingle(ReverseIfBigEndian(base.ReadBytes(4)), 0);
        }

        public override double ReadDouble()
        {
            return BitConverter.ToDouble(ReverseIfBigEndian(base.ReadBytes(8)), 0);
        }

        public override string ReadString()
        {
            return ReadFString()?.Value;
        }

        public virtual FString ReadFString(FSerializedNameHeader nameHeader = null)
        {
            Console.WriteLine($"ReadFString called. Stream position: {BaseStream.Position}");

            if (nameHeader == null)
            {
                if (BaseStream.Length - BaseStream.Position < 4)
                {
                    Console.WriteLine($"Stream length: {BaseStream.Length}, Current position: {BaseStream.Position}, Remaining bytes: {BaseStream.Length - BaseStream.Position}");
                    throw new EndOfStreamException("Not enough data to read 4 bytes for Int32 conversion.");
                }

                byte[] lengthBytes = base.ReadBytes(4);
                Console.WriteLine($"Raw bytes for length: {BitConverter.ToString(lengthBytes)}, Stream position: {BaseStream.Position}");
                int length = BitConverter.ToInt32(ReverseIfBigEndian(lengthBytes), 0);
                Console.WriteLine($"ReadInt32 for length: {length}, Stream position: {BaseStream.Position}");

                if (length == 0)
                {
                    return null;
                }

                if (length < 0)
                {
                    if (BaseStream.Length - BaseStream.Position < -length * 2)
                    {
                        Console.WriteLine($"Stream length: {BaseStream.Length}, Current position: {BaseStream.Position}, Remaining bytes: {BaseStream.Length - BaseStream.Position}");
                        throw new EndOfStreamException("Not enough data to read Unicode string.");
                    }

                    byte[] data = this.ReadBytes(-length * 2);
                    Console.WriteLine($"ReadBytes for Unicode string: {data.Length} bytes, Stream position: {BaseStream.Position}");
                    return new FString(Encoding.Unicode.GetString(data, 0, data.Length - 2), Encoding.Unicode);
                }
                else
                {
                    if (BaseStream.Length - BaseStream.Position < length)
                    {
                        Console.WriteLine($"Stream length: {BaseStream.Length}, Current position: {BaseStream.Position}, Remaining bytes: {BaseStream.Length - BaseStream.Position}");
                        throw new EndOfStreamException("Not enough data to read ASCII string.");
                    }

                    byte[] data = this.ReadBytes(length);
                    Console.WriteLine($"ReadBytes for ASCII string: {data.Length} bytes, Stream position: {BaseStream.Position}");
                    return new FString(Encoding.ASCII.GetString(data, 0, data.Length - 1), Encoding.ASCII);
                }
            }
            else
            {
                if (nameHeader.bIsWide)
                {
                    if (BaseStream.Length - BaseStream.Position < nameHeader.Len * 2)
                    {
                        Console.WriteLine($"Stream length: {BaseStream.Length}, Current position: {BaseStream.Position}, Remaining bytes: {BaseStream.Length - BaseStream.Position}");
                        throw new EndOfStreamException("Not enough data to read wide string.");
                    }

                    byte[] data = this.ReadBytes(nameHeader.Len * 2);
                    Console.WriteLine($"ReadBytes for wide string: {data.Length} bytes, Stream position: {BaseStream.Position}");
                    return new FString(Encoding.Unicode.GetString(data, 0, data.Length), Encoding.Unicode);
                }
                else
                {
                    if (BaseStream.Length - BaseStream.Position < nameHeader.Len)
                    {
                        Console.WriteLine($"Stream length: {BaseStream.Length}, Current position: {BaseStream.Position}, Remaining bytes: {BaseStream.Length - BaseStream.Position}");
                        throw new EndOfStreamException("Not enough data to read narrow string.");
                    }

                    byte[] data = this.ReadBytes(nameHeader.Len);
                    Console.WriteLine($"ReadBytes for narrow string: {data.Length} bytes, Stream position: {BaseStream.Position}");
                    return new FString(Encoding.ASCII.GetString(data, 0, data.Length), Encoding.ASCII);
                }
            }
        }

        public virtual FString ReadNameMapString(FSerializedNameHeader nameHeader, out uint hashes)
        {
            hashes = 0;
            FString str = this.ReadFString(nameHeader);
            if (this is AssetBinaryReader abr)
            {
                if (abr.Asset is UAsset abrUa && abrUa.WillSerializeNameHashes != false && !string.IsNullOrEmpty(str.Value))
                {
                    hashes = this.ReadUInt32();
                    if (hashes < (1 << 10) && abrUa.ObjectVersion < ObjectVersion.VER_UE4_NAME_HASHES_SERIALIZED) // "i lied, there's actually no hashes"
                    {
                        abrUa.WillSerializeNameHashes = false;
                        hashes = 0;
                        this.BaseStream.Position -= sizeof(uint);
                    }
                    else
                    {
                        abrUa.WillSerializeNameHashes = true;
                    }
                }
            }
            return str;
        }

        internal const ulong CityHash64 = 0x00000000C1640000;
        public void ReadNameBatch(bool VerifyHashes, out ulong HashVersion, out List<FString> nameMap)
        {
            // TODO: implement pre-ue5 serialization

            HashVersion = 0;
            nameMap = new List<FString>();

            int numStrings = ReadInt32();
            if (numStrings == 0) return;
            ReadInt32(); // length of strings in bytes

            // read hashes
            HashVersion = ReadUInt64();
            ulong[] hashes = new ulong[numStrings];
            switch (HashVersion)
            {
                case UnrealBinaryReader.CityHash64:
                    for (int i = 0; i < numStrings; i++) hashes[i] = ReadUInt64(); // CityHash64 of str.ToLowerCase();
                    break;
                default:
                    throw new InvalidOperationException("Unknown algorithm ID " + HashVersion);
            }

            // read headers
            FSerializedNameHeader[] nameHeaders = new FSerializedNameHeader[numStrings];
            for (int i = 0; i < numStrings; i++) nameHeaders[i] = FSerializedNameHeader.Read(this);

            // read strings
            for (int i = 0; i < numStrings; i++)
            {
                FString newStr = ReadNameMapString(nameHeaders[i], out _);
                nameMap.Add(newStr);
            }

            // verify hashes if requested
            if (VerifyHashes)
            {
                for (int i = 0; i < nameMap.Count; i++)
                {
                    switch (HashVersion)
                    {
                        case UnrealBinaryReader.CityHash64:
                            ulong expectedHash = CRCGenerator.CityHash64WithLower(nameMap[i]);
                            if (expectedHash != hashes[i]) throw new IOException("Expected hash \"" + expectedHash + "\", received \"" + hashes[i] + "\" for string " + nameMap[i].Value + " in name map; corrupt data?");
                            break;
                        default:
                            throw new InvalidOperationException("Unknown algorithm ID " + HashVersion);
                    }
                }
            }
        }

        public List<CustomVersion> ReadCustomVersionContainer(ECustomVersionSerializationFormat format, List<CustomVersion> oldCustomVersionContainer = null, Usmap Mappings = null)
        {
            var newCustomVersionContainer = new List<CustomVersion>();
            var existingCustomVersions = new HashSet<Guid>();
            switch (format)
            {
                case ECustomVersionSerializationFormat.Enums:
                    throw new NotImplementedException("Custom version serialization format Enums is currently unimplemented");
                case ECustomVersionSerializationFormat.Guids:
                    int numCustomVersions = ReadInt32();
                    for (int i = 0; i < numCustomVersions; i++)
                    {
                        var customVersionID = new Guid(ReadBytes(16));
                        var customVersionNumber = ReadInt32();
                        newCustomVersionContainer.Add(new CustomVersion(customVersionID, customVersionNumber) { Name = ReadFString() });
                        existingCustomVersions.Add(customVersionID);
                    }
                    break;
                case ECustomVersionSerializationFormat.Optimized:
                    numCustomVersions = ReadInt32();
                    for (int i = 0; i < numCustomVersions; i++)
                    {
                        var customVersionID = new Guid(ReadBytes(16));
                        var customVersionNumber = ReadInt32();
                        newCustomVersionContainer.Add(new CustomVersion(customVersionID, customVersionNumber));
                        existingCustomVersions.Add(customVersionID);
                    }
                    break;

            }

            if (Mappings != null && Mappings.CustomVersionContainer != null && Mappings.CustomVersionContainer.Count > 0)
            {
                foreach (CustomVersion entry in Mappings.CustomVersionContainer)
                {
                    if (!existingCustomVersions.Contains(entry.Key)) newCustomVersionContainer.Add(entry.SetIsSerialized(false));
                }
            }

            if (oldCustomVersionContainer != null)
            {
                foreach (CustomVersion entry in oldCustomVersionContainer)
                {
                    if (!existingCustomVersions.Contains(entry.Key)) newCustomVersionContainer.Add(entry.SetIsSerialized(false));
                }
            }

            return newCustomVersionContainer;
        }
    }

    /// <summary>
    /// Reads primitive data types from Unreal Engine assets.
    /// </summary>
    public class AssetBinaryReader : UnrealBinaryReader
    {
        public UnrealPackage Asset;

        public AssetBinaryReader(Stream stream, UnrealPackage asset = null) : base(stream)
        {
            Asset = asset;
        }

        public virtual Guid? ReadPropertyGuid()
        {
            if (Asset.HasUnversionedProperties) return null;
            if (Asset.ObjectVersion >= ObjectVersion.VER_UE4_PROPERTY_GUID_IN_PROPERTY_TAG)
            {
                bool hasPropertyGuid = ReadBoolean();
                if (hasPropertyGuid) return new Guid(ReadBytes(16));
            }
            return null;
        }

        public virtual FName ReadFName()
        {
            if (Asset is ZenAsset)
            {
                uint Index = this.ReadUInt32();
                uint Number = this.ReadUInt32();

                var res = new FName(Asset, (int)(Index & FName.IndexMask), (int)Number);
                res.Type = (EMappedNameType)((Index & FName.TypeMask) >> FName.TypeShift);
                return res;
            }
            else
            {
                int nameMapPointer = this.ReadInt32();
                int number = this.ReadInt32();
                return new FName(Asset, nameMapPointer, number);
            }
        }

        public FObjectThumbnail ReadObjectThumbnail()
        {
            var thumb = new FObjectThumbnail();

            thumb.Width = ReadInt32();
            thumb.Height = ReadInt32();
            var imageBytesCount = ReadInt32();
            thumb.CompressedImageData = imageBytesCount > 0 ? ReadBytes(imageBytesCount) : Array.Empty<byte>();

            return thumb;
        }

        public FLocMetadataObject ReadLocMetadataObject()
        {
            var locMetadataObject = new FLocMetadataObject();

            var valueCount = ReadInt32();
            if (valueCount > 0)
                throw new NotImplementedException("TODO: implement ReadLocMetadataObject");

            return locMetadataObject;
        }

        public string XFERSTRING()
        {
            List<byte> readData = new List<byte>();
            while (true)
            {
                byte newVal = this.ReadByte();
                if (newVal == 0) break;
                readData.Add(newVal);
            }
            return Encoding.ASCII.GetString(readData.ToArray());
        }

        public string XFERUNICODESTRING()
        {
            List<byte> readData = new List<byte>();
            while (true)
            {
                byte newVal1 = this.ReadByte();
                byte newVal2 = this.ReadByte();
                if (newVal1 == 0 && newVal2 == 0) break;
                readData.Add(newVal1);
                readData.Add(newVal2);
            }
            return Encoding.Unicode.GetString(readData.ToArray());
        }

        public void XFERTEXT()
        {

        }

        public FName XFERNAME()
        {
            return this.ReadFName();
        }

        public FName XFER_FUNC_NAME()
        {
            return this.XFERNAME();
        }

        public FPackageIndex XFERPTR()
        {
            return new FPackageIndex(this.ReadInt32());
        }

        public FPackageIndex XFER_FUNC_POINTER()
        {
            return this.XFERPTR();
        }

        public KismetPropertyPointer XFER_PROP_POINTER()
        {
            if (Asset.ObjectVersion >= KismetPropertyPointer.XFER_PROP_POINTER_SWITCH_TO_SERIALIZING_AS_FIELD_PATH_VERSION)
            {
                int numEntries = this.ReadInt32();
                FName[] allNames = new FName[numEntries];
                for (int i = 0; i < numEntries; i++)
                {
                    allNames[i] = this.ReadFName();
                }
                FPackageIndex owner = this.XFER_OBJECT_POINTER();
                return new KismetPropertyPointer(new FFieldPath(allNames, owner));
            }
            else
            {
                return new KismetPropertyPointer(this.XFERPTR());
            }
        }

        public FPackageIndex XFER_OBJECT_POINTER()
        {
            return this.XFERPTR();
        }

        public KismetExpression[] ReadExpressionArray(EExprToken endToken)
        {
            List<KismetExpression> newData = new List<KismetExpression>();
            KismetExpression currExpression = null;
            while (currExpression == null || currExpression.Token != endToken)
            {
                if (currExpression != null) newData.Add(currExpression);
                currExpression = ExpressionSerializer.ReadExpression(this);
            }
            return newData.ToArray();
        }
    }
}
