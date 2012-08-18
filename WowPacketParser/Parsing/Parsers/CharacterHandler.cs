using System;
using PacketParser.Enums;
using PacketParser.Enums.Version;
using PacketParser.Misc;
using PacketParser.Processing;
using PacketParser.DataStructures;
using Guid = PacketParser.DataStructures.Guid;

namespace PacketParser.Parsing.Parsers
{
    public static class CharacterHandler
    {
        [Parser(Opcode.CMSG_STANDSTATECHANGE)]
        public static void HandleStandStateChange(Packet packet)
        {
            packet.ReadInt32("Standstate");
        }

        [Parser(Opcode.SMSG_STANDSTATE_UPDATE)]
        public static void HandleStandStateUpdate(Packet packet)
        {
            packet.ReadByte("Standstate");
        }

        [Parser(Opcode.CMSG_CHAR_CREATE)]
        public static void HandleClientCharCreate(Packet packet)
        {
            packet.ReadCString("Name");
            packet.ReadEnum<Race>("Race", TypeCode.Byte);
            packet.ReadEnum<Class>("Class", TypeCode.Byte);
            packet.ReadEnum<Gender>("Gender", TypeCode.Byte);
            packet.ReadByte("Skin");
            packet.ReadByte("Face");
            packet.ReadByte("Hair Style");
            packet.ReadByte("Hair Color");
            packet.ReadByte("Facial Hair");
            packet.ReadByte("Outfit Id");
        }

        [Parser(Opcode.CMSG_CHAR_DELETE)]
        public static void HandleClientCharDelete(Packet packet)
        {
            packet.ReadGuid("GUID");
        }

        [Parser(Opcode.CMSG_CHAR_RENAME)]
        public static void HandleClientCharRename(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadCString("New Name");
        }

        [Parser(Opcode.SMSG_CHAR_RENAME)]
        public static void HandleServerCharRename(Packet packet)
        {
            if (packet.ReadEnum<ResponseCode>("Race", TypeCode.Byte) != ResponseCode.RESPONSE_SUCCESS)
                return;

            var guid = packet.ReadGuid("GUID");
            var name = packet.ReadCString("Name");
            PacketFileProcessor.Current.GetProcessor<NameStore>().AddPlayerName(guid, name);
        }

        [Parser(Opcode.SMSG_CHAR_CREATE)]
        [Parser(Opcode.SMSG_CHAR_DELETE)]
        public static void HandleCharResponse(Packet packet)
        {
            packet.ReadEnum<ResponseCode>("Response", TypeCode.Byte);
        }

        [Parser(Opcode.CMSG_ALTER_APPEARANCE)]
        public static void HandleAlterAppearance(Packet packet)
        {
            // In some ancient version, this could be ReadByte
            packet.ReadInt32("Hair Style");
            packet.ReadInt32("Hair Color");
            packet.ReadInt32("Facial Hair");
            packet.ReadInt32("Skin Color");
        }

        [Parser(Opcode.SMSG_BARBER_SHOP_RESULT)]
        public static void HandleBarberShopResult(Packet packet)
        {
            packet.ReadEnum<BarberShopResult>("Result", TypeCode.Int32);
        }

        [Parser(Opcode.CMSG_CHAR_CUSTOMIZE)]
        public static void HandleClientCharCustomize(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadCString("New Name");
            packet.ReadEnum<Gender>("Gender", TypeCode.Byte);
            packet.ReadByte("Skin");
            packet.ReadByte("Face");
            packet.ReadByte("Hair Style");
            packet.ReadByte("Hair Color");
            packet.ReadByte("Facial Hair");
        }

        [Parser(Opcode.SMSG_CHAR_CUSTOMIZE)]
        public static void HandleServerCharCustomize(Packet packet)
        {
            if (packet.ReadEnum<ResponseCode>("Response", TypeCode.Byte) != ResponseCode.RESPONSE_SUCCESS)
                return;

            var guid = packet.ReadGuid("GUID");
            var name = packet.ReadCString("Name");

            PacketFileProcessor.Current.GetProcessor<NameStore>().AddPlayerName(guid, name);

            packet.ReadEnum<Gender>("Gender", TypeCode.Byte);
            packet.ReadByte("Skin");
            packet.ReadByte("Face");
            packet.ReadByte("Hair Style");
            packet.ReadByte("Hair Color");
            packet.ReadByte("Facial Hair");
        }

        [Parser(Opcode.SMSG_CHAR_ENUM, ClientVersionBuild.Zero, ClientVersionBuild.V4_2_2_14545)]
        public static void HandleCharEnum(Packet packet)
        {
            var count = packet.ReadByte("Count");
            packet.StoreBeginList("Characters");
            for (var i = 0; i < count; i++)
            {
                var guid = packet.ReadGuid("GUID", i);
                var name = packet.ReadCString("Name", i);
                var race = packet.ReadEnum<Race>("Race", TypeCode.Byte, i);
                PacketFileProcessor.Current.GetProcessor<NameStore>().AddPlayerName(guid, name);
                var clss = packet.ReadEnum<Class>("Class", TypeCode.Byte, i);
                packet.ReadEnum<Gender>("Gender", TypeCode.Byte, i);

                packet.ReadByte("Skin", i);
                packet.ReadByte("Face", i);
                packet.ReadByte("Hair Style", i);
                packet.ReadByte("Hair Color", i);
                packet.ReadByte("Facial Hair", i);

                var level = packet.ReadByte("Level", i);
                var zone = packet.ReadEntryWithName<UInt32>(StoreNameType.Zone, "Zone Id", i);
                var mapId = packet.ReadEntryWithName<Int32>(StoreNameType.Map, "Map Id", i);

                var pos = packet.ReadVector3("Position", i);
                if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_3_13329))
                    packet.ReadGuid("Guild GUID", i);
                else
                    packet.ReadInt32("Guild Id", i);
                packet.ReadEnum<CharacterFlag>("Character Flags", TypeCode.Int32, i);

                if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_0_2_9056))
                    packet.ReadEnum<CustomizationFlag>("Customization Flags", TypeCode.Int32, i);

                var firstLogin = packet.ReadBoolean("First Login", i);
                packet.ReadInt32("Pet Display Id", i);
                packet.ReadInt32("Pet Level", i);
                packet.ReadEnum<CreatureFamily>("Pet Family", TypeCode.Int32, i);

                packet.StoreBeginList("Equipment", i);
                for (var j = 0; j < 19; j++)
                {
                    packet.ReadInt32("Equip Display Id", i, j);
                    packet.ReadEnum<InventoryType>("Equip Inventory Type", TypeCode.Byte, i, j);
                    packet.ReadInt32("Equip Aura Id", i, j);
                }
                packet.StoreEndList();

                int bagCount = ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_3_11685) ? 4 : 1;
                packet.StoreBeginList("Bags", i);
                for (var j = 0; j < bagCount; j++)
                {
                    packet.ReadInt32("Bag Display Id", i, j);
                    packet.ReadEnum<InventoryType>("Bag Inventory Type", TypeCode.Byte, i, j);
                    packet.ReadInt32("Bag Aura Id", i, j);
                }
                packet.StoreEndList();

                var playerInfo = new Player {Race = race, Class = clss, Name = name, FirstLogin = firstLogin, Level = level};

                PacketFileProcessor.Current.GetProcessor<ObjectStore>().Objects[guid] = new Tuple<WoWObject, TimeSpan?>(playerInfo, packet.TimeSpan);

                PacketFileProcessor.Current.GetProcessor<NameStore>().AddPlayerName(guid, name);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_CHAR_ENUM, ClientVersionBuild.V4_2_2_14545, ClientVersionBuild.V4_3_0_15005)]
        public static void HandleCharEnum422(Packet packet)
        {
            packet.ReadByte("Unk Flag");
            int count = packet.ReadInt32("Char Count");
            packet.ReadInt32("Unk Count");
            var firstLogin = new bool[count];
            var playerGuid = new byte[count][];
            var guildGuid = new byte[count][];
            
            for (int c = 0; c < count; c++)
            {
                playerGuid[c] = new byte[8];
                guildGuid[c] = new byte[8];

                guildGuid[c][5] = packet.ReadBit();//0
                playerGuid[c][4] = packet.ReadBit();//1
                guildGuid[c][3] = packet.ReadBit();//2
                guildGuid[c][7] = packet.ReadBit();//3
                guildGuid[c][1] = packet.ReadBit();//4
                guildGuid[c][6] = packet.ReadBit();//5
                playerGuid[c][5] = packet.ReadBit();//6
                playerGuid[c][6] = packet.ReadBit();//7
                playerGuid[c][3] = packet.ReadBit();//8
                playerGuid[c][2] = packet.ReadBit();//9
                guildGuid[c][4] = packet.ReadBit();//10
                playerGuid[c][0] = packet.ReadBit();//11
                playerGuid[c][1] = packet.ReadBit();//12
                guildGuid[c][2] = packet.ReadBit();//13
                playerGuid[c][7] = packet.ReadBit();//14
                guildGuid[c][0] = packet.ReadBit();//15
                firstLogin[c] = packet.ReadBit();//16
            }

            packet.StoreBeginList("Characters");
            for (int c = 0; c < count; c++)
            {
                var name = packet.ReadCString("Name", c);

                packet.ReadXORByte(guildGuid[c], 5);

                packet.ReadByte("Face", c);
                var mapId = packet.ReadInt32("Map Id", c);

                packet.ReadXORByte(playerGuid[c], 1);
                packet.ReadXORByte(playerGuid[c], 4);
                packet.ReadXORByte(guildGuid[c], 4);
                packet.ReadXORByte(guildGuid[c], 0);

                var pos = packet.ReadVector3("Position", c);

                packet.ReadXORByte(playerGuid[c], 0);

                var zone = packet.ReadEntryWithName<Int32>(StoreNameType.Zone, "Zone Id", c);
                packet.ReadInt32("Pet Level", c);

                packet.ReadXORByte(playerGuid[c], 3);

                packet.ReadXORByte(playerGuid[c], 7);

                packet.ReadByte("Facial Hair", c);
                packet.ReadByte("Skin", c);
                var clss = packet.ReadEnum<Class>("Class", TypeCode.Byte, c);
                packet.ReadInt32("Pet Family", c);
                packet.ReadEnum<CharacterFlag>("CharacterFlag", TypeCode.Int32, c);

                packet.ReadXORByte(playerGuid[c], 2);

                packet.ReadInt32("Pet Display ID", c);

                packet.ReadXORByte(guildGuid[c], 7);

                var level = packet.ReadByte("Level", c);

                packet.ReadXORByte(playerGuid[c], 6);

                packet.ReadByte("Hair Style", c);

                packet.ReadXORByte(guildGuid[c], 2);

                var race = packet.ReadEnum<Race>("Race", TypeCode.Byte, c);
                packet.ReadByte("Hair Color", c);

                packet.ReadXORByte(guildGuid[c], 6);

                packet.ReadEnum<Gender>("Gender", TypeCode.Byte, c);

                packet.ReadXORByte(playerGuid[c], 5);

                packet.ReadXORByte(guildGuid[c], 3);

                packet.ReadByte("List Order", c);

                packet.StoreBeginList("Equipment", c);
                for (int itm = 0; itm < 19; itm++)
                {
                    packet.ReadInt32("Item EnchantID", c, itm);
                    packet.ReadEnum<InventoryType>("Item InventoryType", TypeCode.Byte, c, itm);
                    packet.ReadInt32("Item DisplayID", c, itm);
                }
                packet.StoreEndList();

                packet.StoreBeginList("Bags", c);
                for (int itm = 0; itm < 4; itm++)
                {
                    packet.ReadInt32("Bag EnchantID", c, itm);
                    packet.ReadEnum<InventoryType>("Bag InventoryType", TypeCode.Byte, c, itm);
                    packet.ReadInt32("Bag DisplayID", c, itm);
                }
                packet.StoreEndList();

                packet.ReadEnum<CustomizationFlag>("CustomizationFlag", TypeCode.UInt32, c);

                packet.ReadXORByte(guildGuid[c], 1);

                var guidPlayer = packet.StoreBitstreamGuid("Character GUID", playerGuid[c], c);
                packet.StoreBitstreamGuid("Guild GUID", guildGuid[c], c);


                packet.Store("First Login", firstLogin, c);

                var playerInfo = new Player { Race = race, Class = clss, Name = name, FirstLogin = firstLogin[c], Level = level };
                PacketFileProcessor.Current.GetProcessor<ObjectStore>().Objects[guidPlayer] = new Tuple<WoWObject, TimeSpan?>(playerInfo, packet.TimeSpan);

                PacketFileProcessor.Current.GetProcessor<NameStore>().AddPlayerName(guidPlayer, name);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_CHAR_ENUM, ClientVersionBuild.V4_3_0_15005, ClientVersionBuild.V4_3_3_15354)]
        public static void HandleCharEnum430(Packet packet)
        {
            var count = packet.ReadBits("Char count", 17);

            var charGuids = new byte[count][];
            var guildGuids = new byte[count][];
            var firstLogins = new bool[count];
            var nameLenghts = new uint[count];

            for (var c = 0; c < count; ++c)
            {
                charGuids[c] = new byte[8];
                guildGuids[c] = new byte[8];

                guildGuids[c][2] = packet.ReadBit();
                charGuids[c][2] = packet.ReadBit();
                charGuids[c][6] = packet.ReadBit();
                charGuids[c][5] = packet.ReadBit();
                charGuids[c][4] = packet.ReadBit();
                guildGuids[c][4] = packet.ReadBit();
                guildGuids[c][3] = packet.ReadBit();
                guildGuids[c][7] = packet.ReadBit();
                nameLenghts[c] = packet.ReadBits(7);
                guildGuids[c][0] = packet.ReadBit();
                charGuids[c][0] = packet.ReadBit();
                charGuids[c][3] = packet.ReadBit();
                charGuids[c][1] = packet.ReadBit();
                firstLogins[c] = packet.ReadBit();
                guildGuids[c][5] = packet.ReadBit();
                charGuids[c][7] = packet.ReadBit();
                guildGuids[c][6] = packet.ReadBit();
                guildGuids[c][1] = packet.ReadBit();
            }

            var unkCounter = packet.ReadBits("Unk Counter", 23);
            packet.ReadBit(); // no idea, not used in client

            packet.StoreBeginList("Characters");
            for (int c = 0; c < count; ++c)
            {
                packet.StoreBeginList("Equipment", c);
                for (var itm = 0; itm < 19; ++itm)
                {
                    packet.ReadEnum<InventoryType>("Item InventoryType", TypeCode.Byte, c, itm);
                    packet.ReadInt32("Item DisplayID", c, itm);
                    packet.ReadInt32("Item EnchantID", c, itm);
                }
                packet.StoreEndList();

                packet.StoreBeginList("Bags", c);
                for (var itm = 0; itm < 4; ++itm)
                {
                    packet.ReadEnum<InventoryType>("Bag InventoryType", TypeCode.Byte, c, itm);
                    packet.ReadInt32("Bag DisplayID", c, itm);
                    packet.ReadInt32("Bag EnchantID", c, itm);
                }
                packet.StoreEndList();

                packet.ReadXORByte(guildGuids[c], 0);
                packet.ReadXORByte(guildGuids[c], 1);

                packet.ReadByte("Face", c);
                packet.ReadInt32("Pet Display ID", c);
                packet.ReadXORByte(guildGuids[c], 7);

                packet.ReadEnum<Gender>("Gender", TypeCode.Byte, c);
                var level = packet.ReadByte("Level", c);
                packet.ReadInt32("Pet Level", c);
                var zone = packet.ReadEntryWithName<UInt32>(StoreNameType.Zone, "Zone Id", c);
                var y = packet.ReadSingle("Position Y", c);
                packet.ReadInt32("Pet Family", c);
                packet.ReadByte("Hair Style", c);
                packet.ReadXORByte(charGuids[c], 1);

                var name = packet.ReadWoWString("Name", (int)nameLenghts[c], c);
                packet.ReadXORByte(charGuids[c], 0);

                var race = packet.ReadEnum<Race>("Race", TypeCode.Byte, c);
                packet.ReadByte("List Order", c);
                packet.ReadXORByte(charGuids[c], 7);

                var z = packet.ReadSingle("Position Z", c);
                var mapId = packet.ReadInt32("Map Id", c);
                packet.ReadXORByte(guildGuids[c], 4);

                packet.ReadByte("Hair Color", c);
                packet.ReadXORByte(charGuids[c], 3);

                packet.ReadEnum<CharacterFlag>("CharacterFlag", TypeCode.Int32, c);
                packet.ReadByte("Skin", c);
                packet.ReadXORByte(charGuids[c], 4);
                packet.ReadXORByte(charGuids[c], 5);
                packet.ReadXORByte(guildGuids[c], 5);

                packet.ReadEnum<CustomizationFlag>("CustomizationFlag", TypeCode.UInt32, c);
                var x = packet.ReadSingle("Position X", c);
                packet.ReadByte("Facial Hair", c);
                packet.ReadXORByte(charGuids[c], 6);
                packet.ReadXORByte(guildGuids[c], 3);
                packet.ReadXORByte(charGuids[c], 2);

                var clss = packet.ReadEnum<Class>("Class", TypeCode.Byte, c);
                packet.ReadXORByte(guildGuids[c], 6);
                packet.ReadXORByte(guildGuids[c], 2);

                var playerGuid = new Guid(BitConverter.ToUInt64(charGuids[c], 0));

                packet.StoreBitstreamGuid("Character GUID", charGuids[c], c);
                packet.StoreBitstreamGuid("Guild GUID", guildGuids[c], c);

                packet.Store("First Login", firstLogins[c], c);
                packet.Store("Position", new Vector3(x, y, z), c);

                var playerInfo = new Player{Race = race, Class = clss, Name = name, FirstLogin = firstLogins[c], Level = level};
                PacketFileProcessor.Current.GetProcessor<ObjectStore>().Objects[playerGuid] = new Tuple<WoWObject, TimeSpan?>(playerInfo, packet.TimeSpan);
                PacketFileProcessor.Current.GetProcessor<NameStore>().AddPlayerName(playerGuid, name);
            }

            packet.StoreBeginList("Unk datas");
            for (var c = 0; c < unkCounter; c++)
            {
                packet.ReadUInt32("Unk UInt32", c);
                packet.ReadByte("Unk Byte", c);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_CHAR_ENUM, ClientVersionBuild.V4_3_3_15354, ClientVersionBuild.V4_3_4_15595)]
        public static void HandleCharEnum433(Packet packet)
        {
            var unkCounter = packet.ReadBits("Unk Counter", 23);
            var count = packet.ReadBits("Char count", 17);

            var charGuids = new byte[count][];
            var guildGuids = new byte[count][];
            var firstLogins = new bool[count];
            var nameLenghts = new uint[count];

            for (var c = 0; c < count; ++c)
            {
                charGuids[c] = new byte[8];
                guildGuids[c] = new byte[8];
                //100%  pozition, and flag
                //%50   flag
                //20    nothing

                charGuids[c][0] = packet.ReadBit(); //100%
                guildGuids[c][0] = packet.ReadBit();//50%
                charGuids[c][2] = packet.ReadBit(); //100%
                guildGuids[c][2] = packet.ReadBit();//50%
                firstLogins[c] = packet.ReadBit();                  //100%
                charGuids[c][3] = packet.ReadBit(); //100%
                charGuids[c][6] = packet.ReadBit(); //100%
                guildGuids[c][2] = packet.ReadBit();//20%

                charGuids[c][4] = packet.ReadBit(); //20%
                charGuids[c][5] = packet.ReadBit(); //20%
                nameLenghts[c] = packet.ReadBits(4);                //100%
                guildGuids[c][3] = packet.ReadBit();//20%
                guildGuids[c][4] = packet.ReadBit();//50%

                guildGuids[c][5] = packet.ReadBit();//20%
                charGuids[c][1] = packet.ReadBit(); //100%
                packet.ReadBit();                                   //20%
                guildGuids[c][6] = packet.ReadBit();//20%
                charGuids[c][7] = packet.ReadBit(); //100%
                guildGuids[c][7] = packet.ReadBit();//50%
                packet.ReadBit();                                   //20%
                packet.ReadBit();                                   //20%
            }

            // no idea, not used in client
            packet.ReadByte();

            packet.StoreBeginList("Characters");
            for (int c = 0; c < count; ++c)
            {
                packet.StoreBeginList("Items", c);
                for (var itm = 0; itm < 19; ++itm)
                {
                    packet.ReadInt32("Item EnchantID", c, itm);
                    packet.ReadInt32("Item DisplayID", c, itm);
                    packet.ReadEnum<InventoryType>("Item InventoryType", TypeCode.Byte, c, itm);
                }
                packet.StoreEndList();

                packet.StoreBeginList("Bags", c);
                for (var itm = 0; itm < 4; ++itm)
                {
                    packet.ReadInt32("Bag EnchantID", c, itm);
                    packet.ReadInt32("Bag DisplayID", c, itm);
                    packet.ReadEnum<InventoryType>("Bag InventoryType", TypeCode.Byte, c, itm);
                }
                packet.StoreEndList();

                var zone = packet.ReadEntryWithName<UInt32>(StoreNameType.Zone, "Zone Id", c);
                packet.ReadInt32("Pet Level", c);
                packet.ReadEnum<CharacterFlag>("CharacterFlag", TypeCode.Int32, c);

                packet.ReadByte("Facial Hair", c);

                packet.ReadXORByte(guildGuids[c], 0);
                    packet.ReadXORByte(charGuids[c], 0);

                packet.ReadXORByte(charGuids[c], 2);
                if (guildGuids[c][2] != 0)
                    //  guildGuids[c][2] ^= packet.ReadByte();

                    if (charGuids[c][7] != 0)
                        charGuids[c][7] ^= packet.ReadByte();
                if (guildGuids[c][7] != 0)
                    // guildGuids[c][7] ^= packet.ReadByte();

                    packet.ReadByte("List Order", c);
                packet.ReadInt32("Pet Display ID", c);

                // no ideal //////////////////////////////
                if (charGuids[c][4] != 0)
                    charGuids[c][4] ^= packet.ReadByte();

                if (guildGuids[c][4] != 0)
                    // guildGuids[c][4] ^= packet.ReadByte();

                if (charGuids[c][5] != 0)
                        // charGuids[c][5] ^= packet.ReadByte();

                if (guildGuids[c][5] != 0)
                            // guildGuids[c][5] ^= packet.ReadByte();

                if (guildGuids[c][1] != 0)
                                // guildGuids[c][1] ^= packet.ReadByte();

                                if (guildGuids[c][3] != 0)
                                    // guildGuids[c][3] ^= packet.ReadByte();

                                    if (guildGuids[c][6] != 0)
                                        // guildGuids[c][6] ^= packet.ReadByte();

                                        //////////////////////////////////////////

                                        if (charGuids[c][3] != 0)
                                            charGuids[c][3] ^= packet.ReadByte();

                var clss = packet.ReadEnum<Class>("Class", TypeCode.Byte, c);

                if (charGuids[c][6] != 0)
                    charGuids[c][6] ^= packet.ReadByte();

                var x = packet.ReadSingle("Position X", c);

                if (charGuids[c][1] != 0)
                    charGuids[c][1] ^= packet.ReadByte();

                var race = packet.ReadEnum<Race>("Race", TypeCode.Byte, c);
                packet.ReadInt32("Pet Family", c);
                var y = packet.ReadSingle("Position Y", c);
                packet.ReadEnum<Gender>("Gender", TypeCode.Byte, c);
                packet.ReadByte("Hair Style", c);
                var level = packet.ReadByte("Level", c);
                var z = packet.ReadSingle("Position Z", c);
                packet.ReadEnum<CustomizationFlag>("CustomizationFlag", TypeCode.UInt32, c);
                packet.ReadByte("Skin", c);
                packet.ReadByte("Hair Color", c);
                packet.ReadByte("Face", c);
                var mapId = packet.ReadInt32("Map Id", c);
                var name = packet.ReadWoWString("Name", (int)nameLenghts[c], c);

                var playerGuid = packet.StoreBitstreamGuid("Character GUID", charGuids[c], c);
                packet.StoreBitstreamGuid("Guild GUID", guildGuids[c], c);

                packet.Store("First Login", firstLogins[c], c);
                packet.Store("Position", new Vector3(x, y, z), c);

                var playerInfo = new Player { Race = race, Class = clss, Name = name, FirstLogin = firstLogins[c], Level = level };
                PacketFileProcessor.Current.GetProcessor<ObjectStore>().Objects[playerGuid] = new Tuple<WoWObject, TimeSpan?>(playerInfo, packet.TimeSpan);
                PacketFileProcessor.Current.GetProcessor<NameStore>().AddPlayerName(playerGuid, name);
            }
            packet.StoreEndList();

            packet.StoreBeginList("Unk Datas");
            for (var c = 0; c < unkCounter; c++)
            {
                packet.ReadUInt32("Unk UInt32", c);
                packet.ReadByte("Unk Byte", c);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_CHAR_ENUM, ClientVersionBuild.V4_3_4_15595)]
        public static void HandleCharEnum434(Packet packet)
        {
            var unkCounter = packet.ReadBits("Unk Counter", 23);
            packet.ReadBit("Unk bit");
            var count = packet.ReadBits("Char count", 17);

            var charGuids = new byte[count][];
            var guildGuids = new byte[count][];
            var firstLogins = new bool[count];
            var nameLenghts = new uint[count];

            for (var c = 0; c < count; ++c)
            {
                charGuids[c] = new byte[8];
                guildGuids[c] = new byte[8];

                charGuids[c][3] = packet.ReadBit();
                guildGuids[c][1] = packet.ReadBit();
                guildGuids[c][7] = packet.ReadBit();
                guildGuids[c][2] = packet.ReadBit();
                nameLenghts[c] = packet.ReadBits(7);
                charGuids[c][4] = packet.ReadBit();
                charGuids[c][7] = packet.ReadBit();
                guildGuids[c][3] = packet.ReadBit();
                charGuids[c][5] = packet.ReadBit();
                guildGuids[c][6] = packet.ReadBit();
                charGuids[c][1] = packet.ReadBit();
                guildGuids[c][5] = packet.ReadBit();
                guildGuids[c][4] = packet.ReadBit();
                firstLogins[c] = packet.ReadBit();
                charGuids[c][0] = packet.ReadBit();
                charGuids[c][2] = packet.ReadBit();
                charGuids[c][6] = packet.ReadBit();
                guildGuids[c][0] = packet.ReadBit();
            }

            packet.StoreBeginList("Characters");
            for (int c = 0; c < count; ++c)
            {
                var clss = packet.ReadEnum<Class>("Class", TypeCode.Byte, c);

                packet.StoreBeginList("Items", c);
                for (var itm = 0; itm < 19; ++itm)
                {
                    packet.ReadEnum<InventoryType>("Item InventoryType", TypeCode.Byte, c, itm);
                    packet.ReadInt32("Item DisplayID", c, itm);
                    packet.ReadInt32("Item EnchantID", c, itm);
                }
                packet.StoreEndList();

                packet.StoreBeginList("Bags", c);
                for (var itm = 0; itm < 4; ++itm)
                {
                    packet.ReadEnum<InventoryType>("Bag InventoryType", TypeCode.Byte, c, itm);
                    packet.ReadInt32("Bag DisplayID", c, itm);
                    packet.ReadInt32("Bag EnchantID", c, itm);
                }
                packet.StoreEndList();

                packet.ReadInt32("Pet Family", c);

                packet.ReadXORByte(guildGuids[c], 2);

                packet.ReadByte("List Order", c);
                packet.ReadByte("Hair Style", c);
                packet.ReadXORByte(guildGuids[c], 3);

                packet.ReadInt32("Pet Display ID", c);
                packet.ReadEnum<CharacterFlag>("CharacterFlag", TypeCode.Int32, c);
                packet.ReadByte("Hair Color", c);
                
                packet.ReadXORByte(charGuids[c], 4);
                var mapId = packet.ReadInt32("Map Id", c);
                packet.ReadXORByte(guildGuids[c], 5);

                var z = packet.ReadSingle("Position Z", c);
                packet.ReadXORByte(guildGuids[c], 6);

                packet.ReadInt32("Pet Level", c);

                packet.ReadXORByte(charGuids[c], 3);

                var y = packet.ReadSingle("Position Y", c);

                packet.ReadEnum<CustomizationFlag>("CustomizationFlag", TypeCode.UInt32, c);
                packet.ReadByte("Facial Hair", c);

                packet.ReadXORByte(charGuids[c], 7);

                packet.ReadEnum<Gender>("Gender", TypeCode.Byte, c);
                var name = packet.ReadWoWString("Name", (int)nameLenghts[c], c);
                packet.ReadByte("Face", c);

                packet.ReadXORByte(charGuids[c], 0);

                packet.ReadXORByte(charGuids[c], 2);

                packet.ReadXORByte(guildGuids[c], 1);

                packet.ReadXORByte(guildGuids[c], 7);

                var x = packet.ReadSingle("Position X", c);
                packet.ReadByte("Skin", c);
                var race = packet.ReadEnum<Race>("Race", TypeCode.Byte, c);
                var level = packet.ReadByte("Level", c);
                packet.ReadXORByte(charGuids[c], 6);

                packet.ReadXORByte(guildGuids[c], 4);

                packet.ReadXORByte(guildGuids[c], 0);

                packet.ReadXORByte(charGuids[c], 5);

                packet.ReadXORByte(charGuids[c], 1);

                var zone = packet.ReadEntryWithName<UInt32>(StoreNameType.Zone, "Zone Id", c);

                var playerGuid = packet.StoreBitstreamGuid("Character GUID", charGuids[c], c);

                packet.StoreBitstreamGuid("Character GUID", charGuids[c], c);
                packet.StoreBitstreamGuid("Guild GUID", guildGuids[c], c);

                packet.Store("First Login", firstLogins[c], c);
                packet.Store("Position", new Vector3(x, y, z), c);

                var playerInfo = new Player { Race = race, Class = clss, Name = name, FirstLogin = firstLogins[c], Level = level };
                PacketFileProcessor.Current.GetProcessor<ObjectStore>().Objects[playerGuid] = new Tuple<WoWObject, TimeSpan?>(playerInfo, packet.TimeSpan);
                PacketFileProcessor.Current.GetProcessor<NameStore>().AddPlayerName(playerGuid, name);
            }
            packet.StoreEndList();
            packet.StoreBeginList("UnkList");
            for (var i = 0; i < unkCounter; ++i)
            {
                packet.ReadByte("Unk byte", i);
                packet.ReadUInt32("Unk int", i);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_COMPRESSED_CHAR_ENUM)]
        public static void HandleCompressedCharEnum(Packet packet)
        {
            packet.Inflate(packet.ReadInt32());
            switch (ClientVersion.Build)
            {
                case ClientVersionBuild.V4_3_4_15595:
                    HandleCharEnum434(packet);
                    break;
                case ClientVersionBuild.V4_3_3_15354:
                    HandleCharEnum433(packet);
                    break;
                case ClientVersionBuild.V4_3_0_15005:
                    HandleCharEnum430(packet);
                    break;
                case ClientVersionBuild.V4_2_2_14545:
                    HandleCharEnum422(packet);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        [Parser(Opcode.SMSG_PLAYER_VEHICLE_DATA)]
        public static void HandlePlayerVehicleData(Packet packet)
        {
            packet.ReadPackedGuid("GUID");
            packet.ReadInt32("Vehicle Id");
        }

        [Parser(Opcode.CMSG_PLAYED_TIME)]
        [Parser(Opcode.SMSG_PLAYED_TIME)]
        public static void HandlePlayedTime(Packet packet)
        {
            if (packet.Opcode == Opcodes.GetOpcode(Opcode.SMSG_PLAYED_TIME))
            {
                packet.ReadInt32("Time Played");
                packet.ReadInt32("Total");
            }
            packet.ReadBoolean("Print in chat");
        }

        [Parser(Opcode.SMSG_LOG_XPGAIN)]
        public static void HandleLogXPGain(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadUInt32("Total XP");
            var type = packet.ReadByte("XP type"); // Need enum

            if (type == 0) // kill
            {
                packet.ReadUInt32("Base XP");
                packet.ReadSingle("Group rate (unk)");
            }

            packet.ReadBoolean("RAF Bonus");
        }

        [Parser(Opcode.SMSG_TITLE_EARNED)]
        public static void HandleTitleEarned(Packet packet)
        {
            packet.ReadUInt32("Title Id");
            packet.ReadUInt32("Earned?"); // vs lost
        }

        [Parser(Opcode.CMSG_SET_TITLE)]
        public static void HandleSetTitle(Packet packet)
        {
            packet.ReadUInt32("Title Id");
        }

        [Parser(Opcode.SMSG_INIT_CURRENCY, ClientVersionBuild.V4_3_4_15595)]
        public static void HandleInitCurrency434(Packet packet)
        {
            var count = packet.ReadBits("Count", 23);
            if (count == 0)
                return;

            var hasWeekCount = new bool[count];
            var hasWeekCap = new bool[count];
            var hasSeasonTotal = new bool[count];
            var flags = new uint[count];
            for (var i = 0; i < count; ++i)
            {
                hasWeekCount[i] = packet.ReadBit();
                flags[i] = packet.ReadBits(4);
                hasWeekCap[i] = packet.ReadBit();
                hasSeasonTotal[i] = packet.ReadBit();
            }

            packet.StoreBeginList("Currencies");
            for (var i = 0; i < count; ++i)
            {
                packet.Store("Flags", flags[i], i);
                packet.ReadUInt32("Currency count", i);
                if (hasWeekCap[i])
                    packet.ReadUInt32("Weekly cap", i);

                if (hasSeasonTotal[i])
                    packet.ReadUInt32("Season total earned", i);

                packet.ReadUInt32("Currency id", i);
                if (hasWeekCount[i])
                    packet.ReadUInt32("Weekly count", i);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_INIT_CURRENCY, ClientVersionBuild.V4_2_2_14545, ClientVersionBuild.V4_3_4_15595)]
        public static void HandleInitCurrency422(Packet packet)
        {
            var count = packet.ReadUInt32("Count");
            var bits = new bool[count, 3];

            for (var i = 0; i < count; ++i)
                for (var j = 0; j < 3; ++j)
                    bits[i, j] = packet.ReadBit();

            packet.StoreBeginList("CurrencyDatas");
            for (var i = 0; i < count; ++i)
            {
                packet.ReadInt32("Currency Id", i);
                if (bits[i, 0])
                    packet.ReadInt32("Weekly Cap", i);

                packet.ReadInt32("Total Count", i);
                packet.ReadByte("Unk Byte1", i);

                if (bits[i, 1])
                    packet.ReadInt32("Season Total Earned?", i);

                if (bits[i, 2])
                    packet.ReadUInt32("Week Count", i);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_INIT_CURRENCY, ClientVersionBuild.Zero, ClientVersionBuild.V4_2_2_14545)]
        public static void HandleInitCurrency(Packet packet)
        {
            var count = packet.ReadUInt32("Count");
            packet.StoreBeginList("CurrencyDatas");
            for (var i = 0; i < count; ++i)
            {
                packet.ReadUInt32("Week Count", i);
                packet.ReadByte("Unk Byte", i);
                packet.ReadUInt32("Currency ID", i);
                packet.ReadTime("Reset Time", i);
                packet.ReadUInt32("Week Cap", i);
                packet.ReadInt32("Total Count", i);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_UPDATE_CURRENCY, ClientVersionBuild.Zero, ClientVersionBuild.V4_3_4_15595)]
        public static void HandleUpdateCurrency(Packet packet)
        {
            packet.ReadUInt32("Currency ID");
            packet.ReadUInt32("Week Count");
            packet.ReadUInt32("Total Count");
        }

        [Parser(Opcode.SMSG_UPDATE_CURRENCY, ClientVersionBuild.V4_3_4_15595)]
        public static void HandleUpdateCurrency434(Packet packet)
        {
            var hasWeekCap = packet.ReadBit();
            var hasSeasonCount = packet.ReadBit();
            packet.ReadBit("Print in log");

            if (hasSeasonCount)
                packet.ReadInt32("Season Total Earned");

            packet.ReadInt32("Total Count");
            packet.ReadInt32("Currency ID");

            if (hasWeekCap)
                packet.ReadInt32("Week Count");
        }

        [Parser(Opcode.SMSG_UPDATE_CURRENCY_WEEK_LIMIT)]
        public static void HandleUpdateCurrencyWeekLimit(Packet packet)
        {
            packet.ReadUInt32("Week Cap");
            packet.ReadUInt32("Currency ID");
        }

        [Parser(Opcode.CMSG_SET_CURRENCY_FLAGS)]
        public static void HandleSetCurrencyFlags(Packet packet)
        {
            packet.ReadUInt32("Flags");
            packet.ReadUInt32("Currency ID");
        }

        [Parser(Opcode.SMSG_XP_GAIN_ABORTED)] // 4.3.4, related to EVENT_TRIAL_CAP_REACHED_LEVEL
        public static void HandleXPGainAborted(Packet packet)
        {
            var guid = packet.StartBitStream(4, 0, 1, 2, 6, 7, 5, 3);

            packet.ReadXORByte(guid, 7);
            packet.ReadXORByte(guid, 1);
            packet.ReadXORByte(guid, 4);
            packet.ReadXORByte(guid, 0);
            packet.ReadXORByte(guid, 2);

            packet.ReadInt32("Unk Int32 1");

            packet.ReadXORByte(guid, 6);

            packet.ReadInt32("Unk Int32 2");

            packet.ReadXORByte(guid, 3);
            packet.ReadXORByte(guid, 5);

            packet.ReadInt32("Unk Int32 3");

            packet.StoreBitstreamGuid("Guid", guid);
        }

        [Parser(Opcode.SMSG_FAILED_PLAYER_CONDITION)]
        public static void HandleFailedPlayerCondition(Packet packet)
        {
            packet.ReadInt32("Id"); // quest id? PlayerCondition.dbc id?
        }

        [Parser(Opcode.CMSG_SHOWING_CLOAK)]
        [Parser(Opcode.CMSG_SHOWING_HELM)]
        public static void HandleShowingCloakAndHelm434(Packet packet)
        {
            packet.ReadBoolean("Showing");
        }

        [Parser(Opcode.CMSG_AUTO_DECLINE_GUILD_INVITES)]
        public static void HandleAutoDeclineGuildInvites434(Packet packet)
        {
            packet.ReadBoolean("Auto decline");
        }

        [Parser(Opcode.CMSG_REORDER_CHARACTERS)] // 4.3.4
        public static void HandleReorderCharacters(Packet packet)
        {
            var count = packet.ReadBits("Count", 10);

            var guids = new byte[count][];

            for (int i = 0; i < count; ++i)
                guids[i] = packet.StartBitStream(1, 4, 5, 3, 0, 7, 6, 2);

            packet.StoreBeginList("Characters");
            for (int i = 0; i < count; ++i)
            {
                packet.ReadXORByte(guids[i], 6);
                packet.ReadXORByte(guids[i], 5);
                packet.ReadXORByte(guids[i], 1);
                packet.ReadXORByte(guids[i], 4);
                packet.ReadXORByte(guids[i], 0);
                packet.ReadXORByte(guids[i], 3);
                packet.ReadByte("Slot", i);
                packet.ReadXORByte(guids[i], 2);
                packet.ReadXORByte(guids[i], 7);

                packet.StoreBitstreamGuid("Character Guid", guids[i], i);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_LEVELUP_INFO)]
        public static void HandleLevelUp(Packet packet)
        {
            var level = packet.ReadInt32("Level");
            packet.ReadInt32("Health");

            var powerCount = 5;
            if (ClientVersion.AddedInVersion(ClientType.WrathOfTheLichKing))
                powerCount = 7;
            if (ClientVersion.AddedInVersion(ClientType.Cataclysm))
                powerCount = 5;

            // TODO: Exclude happiness on Cata
            packet.StoreBeginList("Powers");
            for (var i = 0; i < powerCount; i++)
            {
                packet.Store("Power type", (PowerType)i, i);
                packet.ReadInt32("Value", i);
            }
            packet.StoreEndList();

            packet.StoreBeginList("Stats");
            for (var i = 0; i < 5; i++)
            {
                packet.Store("Stat type", (StatType)i, i);
                packet.ReadInt32("Value", i);
            }
            packet.StoreEndList();

            Player character = PacketFileProcessor.Current.GetProcessor<SessionStore>().LoggedInCharacter;
            if (character != null)
                character.Level = level;
        }

        [Parser(Opcode.SMSG_HEALTH_UPDATE)]
        public static void HandleHealthUpdate(Packet packet)
        {
            packet.ReadPackedGuid("GUID");
            packet.ReadUInt32("Value");
        }

        [Parser(Opcode.SMSG_POWER_UPDATE)]
        public static void HandlePowerUpdate(Packet packet)
        {
            packet.ReadPackedGuid("GUID");

            var count = 1;

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_1_13164))
                count = packet.ReadInt32("Count");

            packet.StoreBeginList("Powers");
            for (var i = 0; i < count; i++)
            {
                packet.ReadEnum<PowerType>("Power type", TypeCode.Byte); // Actually powertype for class
                packet.ReadInt32("Value");
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.CMSG_CHAR_ENUM)]
        [Parser(Opcode.CMSG_HEARTH_AND_RESURRECT)]
        [Parser(Opcode.CMSG_SELF_RES)]
        public static void HandleCharacterNull(Packet packet)
        {
        }
    }
}
