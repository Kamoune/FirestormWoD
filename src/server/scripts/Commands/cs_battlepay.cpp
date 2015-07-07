////////////////////////////////////////////////////////////////////////////////
///
///  MILLENIUM-STUDIO
///  Copyright 2015 Millenium-studio SARL
///  All Rights Reserved.
///
////////////////////////////////////////////////////////////////////////////////

#include "ScriptMgr.h"
#include "Chat.h"
#include "AccountMgr.h"
#include "ObjectAccessor.h"
#include "ObjectMgr.h"
#include "BattlepayMgr.h"
#include "BattlepayPacketFactory.h"

class battlepay_commandscript: public CommandScript
{
    public:
        battlepay_commandscript() : CommandScript("battlepay_commandscript") { }

        ChatCommand* GetCommands() const
        {
            static ChatCommand g_BattlepayCommandTable[] =
            {
                { "dumpwebshopcategory",    SEC_ADMINISTRATOR, false, &HandleDumpWebShopCategory,   "", nullptr },
                { "reload",                 SEC_ADMINISTRATOR, false, &HandleReloadBattlePay,       "", nullptr },
                { nullptr,                  0,                 false, nullptr,                       "", nullptr }
            };

            static ChatCommand g_CommandTable[] =
            {
                 { "battlepay", SEC_ADMINISTRATOR, true, nullptr, "", g_BattlepayCommandTable }
            };

            return g_CommandTable;
        }

        static bool HandleDumpWebShopCategory(ChatHandler* p_ChatHandler, char const* p_Args)
        {
            uint32 l_Category = atoi(strtok((char*)p_Args, " "));
            if (!l_Category)
                return false;

            uint32 l_Group = atoi(strtok(nullptr, " "));
            if (!l_Group)
                return false;

            uint32 l_FlagsFilter = atoi(strtok(nullptr, " "));

            QueryResult l_Result = WebDatabase.PQuery("SELECT itemID, price, fake_price FROM shop_items WHERE category = %u", l_Category);
            if (!l_Result)
                return false;

            FILE* l_Output = fopen("./battlepay_webshop.sql", "w+");
            if (!l_Output)
                return false;

            std::ostringstream l_StrBuilder;

            l_StrBuilder << "SET @PRODUCTID := COALESCE((SELECT MAX(ProductID) FROM battlepay_product), 0) + 1;" << std::endl;
            l_StrBuilder << "SET @ORDER := COALESCE((SELECT MAX(Ordering) FROM battlepay_shop_entry WHERE GroupID = " << l_Group << ") + 1;" << std::endl;
            l_StrBuilder << "SET @DISPLAYINFOID := COALESCE((SELECT MAX(DisplayInfoId) FROM battlepay_display_info), 0) + 1;" << std::endl;

            do
            {
                Field* l_Fields    = l_Result->Fetch();
                uint32 l_ItemID    = l_Fields[0].GetUInt32();
                uint32 l_Price     = l_Fields[1].GetUInt32();
                uint32 l_FakePrice = l_Fields[2].GetUInt32();

                if (l_FakePrice == 0)
                    l_FakePrice = l_Price;

                ItemTemplate const* l_Item = sObjectMgr->GetItemTemplate(l_ItemID);
                if (l_Item == nullptr)
                    continue;

                uint32 l_CreatureDisplayInfoID = 0;
                uint32 l_FileDataID = 0;

                std::string l_Description = "";

                /// Mount
                if (l_FlagsFilter & 0x01)
                {
                    for (uint32 l_I = 0; l_I < sMountStore.GetNumRows(); ++l_I)
                    {
                        auto l_MountEntry = sMountStore.LookupEntry(l_I);
                        if (!l_MountEntry || l_MountEntry->SpellID != l_Item->Spells[1].SpellId)
                            continue;

                        l_CreatureDisplayInfoID = l_MountEntry->CreatureDisplayID;
                        l_Description = l_MountEntry->Description->Get(LocaleConstant::LOCALE_enUS);
                        break;
                    }
                }

                /// BattlePet
                if (l_FlagsFilter & 0x02)
                {
                    // Species
                    for (size_t l_I = 0; l_I < sBattlePetSpeciesStore.GetNumRows(); ++l_I)
                    {
                        BattlePetSpeciesEntry const* l_BattlePet = sBattlePetSpeciesStore.LookupEntry(l_I);
                        if (!l_BattlePet || l_BattlePet->spellId != l_Item->Spells[1].SpellId)
                            continue;

                        CreatureTemplate const* l_CreatureTemplate = sObjectMgr->GetCreatureTemplate(l_BattlePet->entry);
                        if (l_CreatureTemplate == nullptr)
                            continue;

                        l_CreatureDisplayInfoID = l_CreatureTemplate->Modelid1;
                        break;
                    }
                }

                /// Item icon
                if (l_FlagsFilter & 0x04)
                {
                    if (g_ItemFileDataId.find(l_ItemID) != g_ItemFileDataId.end())
                        l_FileDataID = g_ItemFileDataId[l_ItemID];
                }

                l_StrBuilder << "INSERT INTO `battlepay_shop_entry` (GroupID, ProductID, Ordering, Flags, BannerType, DisplayInfoID) VALUES (" << l_Group << ",@PRODUCTID, @ORDER, 0, 0, 0);" << std::endl;
                l_StrBuilder << "INSERT INTO `battlepay_product` (ProductID, NormalPriceFixedPoint, CurrentPriceFixedPoint, Type, ChoiceType, Flags, DisplayInfoID) VALUES (" << "@PRODUCTID" << "," << l_Price << "," << l_FakePrice << ",0,2,47," << "@DISPLAYINFOID" << ");" << std::endl;
                l_StrBuilder << "INSERT INTO `battlepay_product_item` (ProductID, ItemID, Quantity, DisplayID, PetResult) VALUES (" << "@PRODUCTID" << "," << l_ItemID << ",1,0,0);" << std::endl;
                l_StrBuilder << "INSERT INTO `battlepay_display_info` (DisplayInfoId, CreatureDisplayInfoID, FileDataID, Name1, Name2, Name3, Flags) VALUES (" << "@DISPLAYINFOID" << "," << l_CreatureDisplayInfoID << "," << l_FileDataID << ",\"" << l_Item->Name1->Get(LocaleConstant::LOCALE_enUS) << "\", '',\"" << "" << "\", 0);" << std::endl;
                l_StrBuilder << "SET @PRODUCTID := @PRODUCTID + 1;" << std::endl;
                l_StrBuilder << "SET @ORDER := @ORDER + 1;" << std::endl;
                l_StrBuilder << "SET @DISPLAYINFOID := @DISPLAYINFOID + 1;" << std::endl;
            }
            while (l_Result->NextRow());

            fwrite(l_StrBuilder.str().c_str(), l_StrBuilder.str().length(), 1, l_Output);
            fflush(l_Output);
            fclose(l_Output);

            return true;
        }

        static bool HandleReloadBattlePay(ChatHandler* p_ChatHandler, char const* p_Args)
        {
            sBattlepayMgr->LoadFromDatabase();
            return true;
        }
};

void AddSC_battlepay_commandscript()
{
    new battlepay_commandscript();
}
