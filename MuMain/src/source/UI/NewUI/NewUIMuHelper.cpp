#include "stdafx.h"

#include <algorithm>
#include <vector>
#include <array>

#include "UI/Legacy/UIControls.h"
#include "UI/NewUI/HUD/Skills/SkillTooltip.h"
#include "UI/NewUI/NewUISystem.h"
#include "UI/NewUI/NewUIMuHelper.h"
#include "Character/CharacterManager.h"
#include "MUHelper/MuHelper.h"
#include "Engine/Object/ZzzInventory.h"

using namespace MUHelper;

// defining constants naming since the original code hard coded these ids

enum ECheckBoxId: uint16_t
{
    CHECKBOX_ID_POTION = 0,
    CHECKBOX_ID_SKILL2_DELAY,
    CHECKBOX_ID_SKILL2_CONDITION,
    CHECKBOX_ID_SKILL3_DELAY,
    CHECKBOX_ID_SKILL3_CONDITION,
    CHECKBOX_ID_COMBO,
    CHECKBOX_ID_BUFF_DURATION,
    CHECKBOX_ID_USE_PET,
    CHECKBOX_ID_PARTY,
    CHECKBOX_ID_AUTO_HEAL,
    CHECKBOX_ID_DRAIN_LIFE,
    CHECKBOX_ID_REPAIR_ITEM,
    CHECKBOX_ID_PICK_ALL,
    CHECKBOX_ID_PICK_SELECTED,
    CHECKBOX_ID_PICK_JEWEL,
    CHECKBOX_ID_PICK_ANCIENT,
    CHECKBOX_ID_PICK_ZEN,
    CHECKBOX_ID_PICK_EXCELLENT,
    CHECKBOX_ID_ADD_OTHER_ITEM,
    CHECKBOX_ID_AUTO_ACCEPT_FRIEND,
    CHECKBOX_ID_AUTO_DEFEND,
    CHECKBOX_ID_AUTO_ACCEPT_GUILD,
    CHECKBOX_ID_DR_ATTACK_CEASE,
    CHECKBOX_ID_DR_ATTACK_AUTO,
    CHECKBOX_ID_DR_ATTACK_TOGETHER,

    // Other Settings tab — party request mode (radio group)
    CHECKBOX_ID_PARTY_REQUEST_NORMAL,
    CHECKBOX_ID_PARTY_REQUEST_AUTO,
    CHECKBOX_ID_PARTY_REQUEST_OFF,
    CHECKBOX_ID_OFFLEVEL,
};

enum EButtonId : uint16_t
{
    BUTTON_ID_SKILL2_CONFIG = 2,
    BUTTON_ID_SKILL3_CONFIG,
    BUTTON_ID_POTION_CONFIG_ELF,
    BUTTON_ID_POTION_CONFIG_SUMMY,
    BUTTON_ID_POTION_CONFIG,
    BUTTON_ID_PARTY_CONFIG,
    BUTTON_ID_PARTY_CONFIG_ELF,
    BUTTON_ID_ADD_OTHER_ITEM,
    BUTTON_ID_DELETE_OTHER_ITEM,
    BUTTON_ID_SAVE_CONFIG,
    BUTTON_ID_INIT_CONFIG,
    BUTTON_ID_EXIT_CONFIG,
    BUTTON_ID_JEWELBANK,
    BUTTON_ID_AUCTIONHOUSE
};

enum ESkillSlotImg : uint16_t
{
    SKILL_SLOT_SKILL1 = 0,
    SKILL_SLOT_SKILL2 = 1,
    SKILL_SLOT_SKILL3 = 2,
    SKILL_SLOT_BUFF1 = 3,
    SKILL_SLOT_BUFF2 = 4,
    SKILL_SLOT_BUFF3 = 5
};

enum ETextBoxImg : uint16_t
{
    TEXTBOX_IMG_SKILL1_TIME = 7,
    TEXTBOX_IMG_SKILL2_TIME = 8,
    TEXTBOX_IMG_ADD_EXTRA_ITEM = 9
};

constexpr int MAX_NUMBER_DIGITS = 3;

// Cast the sentinel so it can be used as a map key / skill-list entry
constexpr int BASIC_ATTACK_SKILL_ENTRY = static_cast<int>(MUHelper::MUHELPER_BASIC_ATTACK_ID);

enum ESkillSlot
{
    SUB_PAGE_SKILL2_CONFIG = 2, // aligns with BUTTON_ID_SKILL2_CONFIG
    SUB_PAGE_SKILL3_CONFIG,
    SUB_PAGE_POTION_CONFIG_ELF,
    SUB_PAGE_POTION_CONFIG_SUMMY,
    SUB_PAGE_POTION_CONFIG,
    SUB_PAGE_PARTY_CONFIG,
    SUB_PAGE_PARTY_CONFIG_ELF
};

using namespace SEASON3B;

ConfigData _TempConfig;

static bool IsOfflevelVipVisible()
{
    if (Hero == NULL)
    {
        return false;
    }

    return g_isCharacterBuff((&Hero->Object), eBuff_PcRoomSeal1)
        || g_isCharacterBuff((&Hero->Object), eBuff_PcRoomSeal2)
        || g_isCharacterBuff((&Hero->Object), eBuff_PcRoomSeal3)
        || g_isCharacterBuff((&Hero->Object), eBuff_Seal1)
        || g_isCharacterBuff((&Hero->Object), eBuff_Seal2)
        || g_isCharacterBuff((&Hero->Object), eBuff_Seal3)
        || g_isCharacterBuff((&Hero->Object), eBuff_Seal4)
        || g_isCharacterBuff((&Hero->Object), eBuff_AscensionSealMaster)
        || g_isCharacterBuff((&Hero->Object), eBuff_WealthSealMaster)
        || g_isCharacterBuff((&Hero->Object), eBuff_NewWealthSeal);
}

static bool IsVipOnlyCheckBox(int iCheckboxId)
{
    return iCheckboxId == CHECKBOX_ID_OFFLEVEL;
}

static void SendOfflevelCommand()
{
    wchar_t wsCommand[] = L"/offlevel";
    SocketClient->ToGameServer()->SendPublicChatMessage(Hero->ID, wsCommand);
}

CNewUIMuHelper::CNewUIMuHelper()
{
    m_pNewUIMng = NULL;
    m_Pos.x = m_Pos.y = 0;
    m_ButtonList.clear();

    m_iCurrentOpenTab = 0;

    m_iSelectedSkillSlot = 0;
    m_aiSelectedSkills.fill(-1);
}

CNewUIMuHelper::~CNewUIMuHelper()
{
    Release();
}

bool CNewUIMuHelper::Create(CNewUIManager* pNewUIMng, int x, int y)
{
    if (NULL == pNewUIMng)
        return false;

    m_pNewUIMng = pNewUIMng;
    m_pNewUIMng->AddUIObj(INTERFACE_MUHELPER, this);

    SetPos(x, y);

    LoadImages();

    InitButtons();

    InitCheckBox();

    InitImage();

    InitText();

    InitTextboxInput();

    Show(false);

    return true;
}

void CNewUIMuHelper::Release()
{
    UnloadImages();

    if (m_pNewUIMng)
    {
        m_pNewUIMng->RemoveUIObj(this);
        m_pNewUIMng = NULL;
    }
}

void CNewUIMuHelper::SetPos(int x, int y)
{
    m_Pos.x = x;
    m_Pos.y = y;
}

void CNewUIMuHelper::InitButtons()
{
    std::list<std::wstring> ltext;
    ltext.push_back(GlobalText[3500]);
    ltext.push_back(GlobalText[3501]);
    ltext.push_back(GlobalText[3590]);

    m_TabBtn.CreateRadioGroup(3, IMAGE_WINDOW_TAB_BTN, TRUE);
    m_TabBtn.ChangeRadioText(ltext);
    m_TabBtn.ChangeRadioButtonInfo(true, m_Pos.x + 10.f, m_Pos.y + 48.f, 56, 22);
    m_TabBtn.ChangeFrame(m_iCurrentOpenTab);

    InsertButton(IMAGE_CLEARNESS_BTN, m_Pos.x + 132, m_Pos.y + 191, 38, 24, 1, 0, 1, 1, GlobalText[3502], L"", BUTTON_ID_SKILL2_CONFIG, 0); //-- skill 2
    InsertButton(IMAGE_CLEARNESS_BTN, m_Pos.x + 132, m_Pos.y + 243, 38, 24, 1, 0, 1, 1, GlobalText[3502], L"", BUTTON_ID_SKILL3_CONFIG, 0); //-- skill 3
    InsertButton(IMAGE_CLEARNESS_BTN, m_Pos.x + 132, m_Pos.y + 84, 38, 24, 1, 0, 1, 1, GlobalText[3502], L"", BUTTON_ID_POTION_CONFIG_ELF, 0); //-- Buff
    InsertButton(IMAGE_CLEARNESS_BTN, m_Pos.x + 132, m_Pos.y + 79, 38, 24, 1, 0, 1, 1, GlobalText[3502], L"", BUTTON_ID_POTION_CONFIG_SUMMY, 0); //-- potion
    InsertButton(IMAGE_CLEARNESS_BTN, m_Pos.x + 132, m_Pos.y + 84, 38, 24, 1, 0, 1, 1, GlobalText[3502], L"", BUTTON_ID_POTION_CONFIG, 0); //-- potion
    InsertButton(IMAGE_CLEARNESS_BTN, m_Pos.x + 17, m_Pos.y + 234, 38, 24, 1, 0, 1, 1, GlobalText[3502], L"", BUTTON_ID_PARTY_CONFIG, 0); //-- potion
    InsertButton(IMAGE_CLEARNESS_BTN, m_Pos.x + 17, m_Pos.y + 234, 38, 24, 1, 0, 1, 1, GlobalText[3502], L"", BUTTON_ID_PARTY_CONFIG_ELF, 0); //-- potion

    InsertButton(IMAGE_CLEARNESS_BTN, m_Pos.x + 132, m_Pos.y + 208, 38, 24, 1, 0, 1, 1, GlobalText[3505], L"", BUTTON_ID_ADD_OTHER_ITEM, 1); //-- Buff
    InsertButton(IMAGE_CLEARNESS_BTN, m_Pos.x + 132, m_Pos.y + 309, 38, 24, 1, 0, 1, 1, GlobalText[3506], L"", BUTTON_ID_DELETE_OTHER_ITEM, 1); //-- Buff
    //--
    InsertButton(IMAGE_IGS_BUTTON, m_Pos.x + 120, m_Pos.y + 388, 52, 26, 1, 0, 1, 1, GlobalText[3503], L"", BUTTON_ID_SAVE_CONFIG, -1);
    InsertButton(IMAGE_IGS_BUTTON, m_Pos.x + 65, m_Pos.y + 388, 52, 26, 1, 0, 1, 1, GlobalText[3504], L"", BUTTON_ID_INIT_CONFIG, -1);
    InsertButton(IMAGE_BASE_WINDOW_BTN_EXIT, m_Pos.x + 20, m_Pos.y + 388, 36, 29, 0, 0, 0, 0, L"", GlobalText[388], BUTTON_ID_EXIT_CONFIG, -1);
    //-- BarnaMu: Jewel Bank entry button (Other Settings tab)
    InsertButton(IMAGE_IGS_BUTTON, m_Pos.x + 30, m_Pos.y + 318, 72, 26, 1, 0, 1, 1, L"Jewel Bank", L"", BUTTON_ID_JEWELBANK, 2);
    InsertButton(IMAGE_IGS_BUTTON, m_Pos.x + 106, m_Pos.y + 318, 72, 26, 1, 0, 1, 1, L"Auction", L"", BUTTON_ID_AUCTIONHOUSE, 2);

    RegisterBtnCharacter(0xFF, BUTTON_ID_SKILL2_CONFIG);
    RegisterBtnCharacter(0xFF, BUTTON_ID_ADD_OTHER_ITEM);
    RegisterBtnCharacter(0xFF, BUTTON_ID_DELETE_OTHER_ITEM);
    RegisterBtnCharacter(0xFF, BUTTON_ID_SAVE_CONFIG);
    RegisterBtnCharacter(0xFF, BUTTON_ID_INIT_CONFIG);
    RegisterBtnCharacter(0xFF, BUTTON_ID_EXIT_CONFIG);
    RegisterBtnCharacter(0xFF, BUTTON_ID_JEWELBANK);
    RegisterBtnCharacter(0xFF, BUTTON_ID_AUCTIONHOUSE);

    RegisterBtnCharacter(Dark_Knight, BUTTON_ID_SKILL3_CONFIG);
    RegisterBtnCharacter(Dark_Knight, BUTTON_ID_POTION_CONFIG);

    RegisterBtnCharacter(Dark_Wizard, BUTTON_ID_SKILL3_CONFIG);
    RegisterBtnCharacter(Dark_Wizard, BUTTON_ID_POTION_CONFIG);
    RegisterBtnCharacter(Dark_Wizard, BUTTON_ID_PARTY_CONFIG);

    RegisterBtnCharacter(Magic_Gladiator, BUTTON_ID_SKILL3_CONFIG);
    RegisterBtnCharacter(Magic_Gladiator, BUTTON_ID_POTION_CONFIG);
    RegisterBtnCharacter(Dark_Lord, BUTTON_ID_POTION_CONFIG);

    RegisterBtnCharacter(Rage_Fighter, BUTTON_ID_SKILL3_CONFIG);
    RegisterBtnCharacter(Rage_Fighter, BUTTON_ID_POTION_CONFIG);

    RegisterBtnCharacter(Fairy_Elf, BUTTON_ID_SKILL3_CONFIG);
    RegisterBtnCharacter(Fairy_Elf, BUTTON_ID_POTION_CONFIG_ELF);
    RegisterBtnCharacter(Fairy_Elf, BUTTON_ID_PARTY_CONFIG_ELF);

    RegisterBtnCharacter(Summoner, BUTTON_ID_SKILL3_CONFIG);
    RegisterBtnCharacter(Summoner, BUTTON_ID_POTION_CONFIG_SUMMY);
}

void CNewUIMuHelper::InitCheckBox()
{
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 79, m_Pos.y + 80, 15, 15, 0, GlobalText[3507], CHECKBOX_ID_POTION, 0);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 94, m_Pos.y + 174, 15, 15, 0, GlobalText[3510], CHECKBOX_ID_SKILL2_DELAY, 0);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 94, m_Pos.y + 191, 15, 15, 0, GlobalText[3511], CHECKBOX_ID_SKILL2_CONDITION, 0);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 94, m_Pos.y + 226, 15, 15, 0, GlobalText[3510], CHECKBOX_ID_SKILL3_DELAY, 0);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 94, m_Pos.y + 243, 15, 15, 0, GlobalText[3511], CHECKBOX_ID_SKILL3_CONDITION, 0);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 18, m_Pos.y + 226, 15, 15, 0, GlobalText[3512], CHECKBOX_ID_COMBO, 0);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 18, m_Pos.y + 276, 15, 15, 0, GlobalText[3513], CHECKBOX_ID_BUFF_DURATION, 0);

    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 18, m_Pos.y + 218, 15, 15, 0, GlobalText[3514], CHECKBOX_ID_USE_PET, 0);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 18, m_Pos.y + 218, 15, 15, 0, GlobalText[3515], CHECKBOX_ID_PARTY, 0);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 79, m_Pos.y + 97, 15, 15, 0, GlobalText[3516], CHECKBOX_ID_AUTO_HEAL, 0);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 79, m_Pos.y + 97, 15, 15, 0, GlobalText[3517], CHECKBOX_ID_DRAIN_LIFE, 0);

    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 79, m_Pos.y + 80, 15, 15, 0, GlobalText[3518], CHECKBOX_ID_REPAIR_ITEM, 1);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 17, m_Pos.y + 125, 15, 15, 0, GlobalText[3519], CHECKBOX_ID_PICK_ALL, 1);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 17, m_Pos.y + 152, 15, 15, 0, GlobalText[3520], CHECKBOX_ID_PICK_SELECTED, 1);

    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 22, m_Pos.y + 170, 15, 15, 0, GlobalText[3521], CHECKBOX_ID_PICK_JEWEL, 1);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 85, m_Pos.y + 170, 15, 15, 0, GlobalText[3522], CHECKBOX_ID_PICK_ANCIENT, 1);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 22, m_Pos.y + 185, 15, 15, 0, GlobalText[3523], CHECKBOX_ID_PICK_ZEN, 1);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 85, m_Pos.y + 185, 15, 15, 0, GlobalText[3524], CHECKBOX_ID_PICK_EXCELLENT, 1);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 22, m_Pos.y + 200, 15, 15, 0, GlobalText[3525], CHECKBOX_ID_ADD_OTHER_ITEM, 1);
    //--

    InsertCheckBox(IMAGE_MACROUI_HELPER_OPTIONBUTTON, m_Pos.x + 94, m_Pos.y + 235, 15, 15, 0, GlobalText[3533], CHECKBOX_ID_DR_ATTACK_CEASE, 0);
    InsertCheckBox(IMAGE_MACROUI_HELPER_OPTIONBUTTON, m_Pos.x + 30, m_Pos.y + 235, 15, 15, 0, GlobalText[3534], CHECKBOX_ID_DR_ATTACK_AUTO, 0);
    InsertCheckBox(IMAGE_MACROUI_HELPER_OPTIONBUTTON, m_Pos.x + 30, m_Pos.y + 250, 15, 15, 0, GlobalText[3535], CHECKBOX_ID_DR_ATTACK_TOGETHER, 0);

    //--
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 18, m_Pos.y + 80, 15, 15, 0, GlobalText[3591], CHECKBOX_ID_AUTO_ACCEPT_FRIEND, 2);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 18, m_Pos.y + 125, 15, 15, 0, GlobalText[3593], CHECKBOX_ID_AUTO_DEFEND, 2);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 18, m_Pos.y + 97, 15, 15, 0, GlobalText[3592], CHECKBOX_ID_AUTO_ACCEPT_GUILD, 2);

    // Party Request mode (other settings tab) — radio-style option buttons
    InsertCheckBox(IMAGE_MACROUI_HELPER_OPTIONBUTTON, m_Pos.x + 18, m_Pos.y + 155, 15, 15, 0, L"Party Req: On",   CHECKBOX_ID_PARTY_REQUEST_NORMAL, 2);
    InsertCheckBox(IMAGE_MACROUI_HELPER_OPTIONBUTTON, m_Pos.x + 18, m_Pos.y + 170, 15, 15, 0, L"Party Req: Auto", CHECKBOX_ID_PARTY_REQUEST_AUTO,   2);
    InsertCheckBox(IMAGE_MACROUI_HELPER_OPTIONBUTTON, m_Pos.x + 18, m_Pos.y + 185, 15, 15, 0, L"Party Req: Off",  CHECKBOX_ID_PARTY_REQUEST_OFF,    2);
    InsertCheckBox(IMAGE_CHECKBOX_BTN, m_Pos.x + 18, m_Pos.y + 215, 15, 15, 0, L"Offlevel", CHECKBOX_ID_OFFLEVEL, 2);

    RegisterBoxCharacter(0xFF, CHECKBOX_ID_POTION);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_SKILL2_DELAY);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_SKILL2_CONDITION);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_BUFF_DURATION);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_REPAIR_ITEM);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_PICK_ALL);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_PICK_SELECTED);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_PICK_JEWEL);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_PICK_ANCIENT);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_PICK_ZEN);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_PICK_EXCELLENT);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_ADD_OTHER_ITEM);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_AUTO_ACCEPT_FRIEND);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_AUTO_DEFEND);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_AUTO_ACCEPT_GUILD);

    RegisterBoxCharacter(0xFF, CHECKBOX_ID_PARTY_REQUEST_NORMAL);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_PARTY_REQUEST_AUTO);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_PARTY_REQUEST_OFF);
    RegisterBoxCharacter(0xFF, CHECKBOX_ID_OFFLEVEL);

    RegisterBoxCharacter(Dark_Knight, CHECKBOX_ID_SKILL3_DELAY);
    RegisterBoxCharacter(Dark_Knight, CHECKBOX_ID_SKILL3_CONDITION);
    RegisterBoxCharacter(Dark_Knight, CHECKBOX_ID_COMBO);

    RegisterBoxCharacter(Dark_Wizard, CHECKBOX_ID_SKILL3_DELAY);
    RegisterBoxCharacter(Dark_Wizard, CHECKBOX_ID_SKILL3_CONDITION);
    RegisterBoxCharacter(Dark_Wizard, CHECKBOX_ID_PARTY);

    RegisterBoxCharacter(Magic_Gladiator, CHECKBOX_ID_SKILL3_DELAY);
    RegisterBoxCharacter(Magic_Gladiator, CHECKBOX_ID_SKILL3_CONDITION);

    RegisterBoxCharacter(Dark_Lord, CHECKBOX_ID_USE_PET);
    RegisterBoxCharacter(Dark_Lord, CHECKBOX_ID_DR_ATTACK_CEASE);
    RegisterBoxCharacter(Dark_Lord, CHECKBOX_ID_DR_ATTACK_AUTO);
    RegisterBoxCharacter(Dark_Lord, CHECKBOX_ID_DR_ATTACK_TOGETHER);

    RegisterBoxCharacter(Fairy_Elf, CHECKBOX_ID_AUTO_HEAL);
    RegisterBoxCharacter(Fairy_Elf, CHECKBOX_ID_SKILL3_DELAY);
    RegisterBoxCharacter(Fairy_Elf, CHECKBOX_ID_SKILL3_CONDITION);
    RegisterBoxCharacter(Fairy_Elf, CHECKBOX_ID_PARTY);

    RegisterBoxCharacter(Summoner, CHECKBOX_ID_SKILL3_DELAY);
    RegisterBoxCharacter(Summoner, CHECKBOX_ID_SKILL3_CONDITION);
    RegisterBoxCharacter(Summoner, CHECKBOX_ID_DRAIN_LIFE);

    RegisterBoxCharacter(Rage_Fighter, CHECKBOX_ID_SKILL3_DELAY);
    RegisterBoxCharacter(Rage_Fighter, CHECKBOX_ID_SKILL3_CONDITION);
}

void CNewUIMuHelper::InitImage()
{
    InsertIcon(BITMAP_INTERFACE_NEW_SKILLICON_BEGIN + 4, m_Pos.x + 17, m_Pos.y + 171, 32, 38, SKILL_SLOT_SKILL1, 0);
    InsertIcon(BITMAP_INTERFACE_NEW_SKILLICON_BEGIN + 4, m_Pos.x + 61, m_Pos.y + 171, 32, 38, SKILL_SLOT_SKILL2, 0);
    InsertIcon(BITMAP_INTERFACE_NEW_SKILLICON_BEGIN + 4, m_Pos.x + 61, m_Pos.y + 222, 32, 38, SKILL_SLOT_SKILL3, 0);
    InsertIcon(BITMAP_INTERFACE_NEW_SKILLICON_BEGIN + 4, m_Pos.x + 21, m_Pos.y + 293, 32, 38, SKILL_SLOT_BUFF1, 0);
    InsertIcon(BITMAP_INTERFACE_NEW_SKILLICON_BEGIN + 4, m_Pos.x + 55, m_Pos.y + 293, 32, 38, SKILL_SLOT_BUFF2, 0);
    InsertIcon(BITMAP_INTERFACE_NEW_SKILLICON_BEGIN + 4, m_Pos.x + 89, m_Pos.y + 293, 32, 38, SKILL_SLOT_BUFF3, 0);

    InsertIcon(IMAGE_MACROUI_HELPER_INPUTNUMBER, m_Pos.x + 140, m_Pos.y + 174, 20, 15, TEXTBOX_IMG_SKILL1_TIME, 0);
    InsertIcon(IMAGE_MACROUI_HELPER_INPUTNUMBER, m_Pos.x + 140, m_Pos.y + 226, 20, 15, TEXTBOX_IMG_SKILL2_TIME, 0);
    InsertIcon(IMAGE_MACROUI_HELPER_INPUTSTRING, m_Pos.x + 34, m_Pos.y + 216, 94, 15, TEXTBOX_IMG_ADD_EXTRA_ITEM, 1);

    RegisterIconCharacter(0xFF, SKILL_SLOT_SKILL1);
    RegisterIconCharacter(0xFF, SKILL_SLOT_SKILL2);
    RegisterIconCharacter(0xFF, SKILL_SLOT_BUFF1);
    RegisterIconCharacter(0xFF, SKILL_SLOT_BUFF2);
    RegisterIconCharacter(0xFF, SKILL_SLOT_BUFF3);
    RegisterIconCharacter(0xFF, TEXTBOX_IMG_SKILL1_TIME);
    RegisterIconCharacter(0xFF, TEXTBOX_IMG_ADD_EXTRA_ITEM);

    RegisterIconCharacter(Dark_Knight, SKILL_SLOT_SKILL3);
    RegisterIconCharacter(Dark_Knight, TEXTBOX_IMG_SKILL2_TIME);
    RegisterIconCharacter(Dark_Wizard, SKILL_SLOT_SKILL3);
    RegisterIconCharacter(Dark_Wizard, TEXTBOX_IMG_SKILL2_TIME);
    RegisterIconCharacter(Fairy_Elf, SKILL_SLOT_SKILL3);
    RegisterIconCharacter(Fairy_Elf, TEXTBOX_IMG_SKILL2_TIME);
    RegisterIconCharacter(Magic_Gladiator, SKILL_SLOT_SKILL3);
    RegisterIconCharacter(Magic_Gladiator, TEXTBOX_IMG_SKILL2_TIME);
    RegisterIconCharacter(Summoner, SKILL_SLOT_SKILL3);
    RegisterIconCharacter(Summoner, TEXTBOX_IMG_SKILL2_TIME);
    RegisterIconCharacter(Rage_Fighter, SKILL_SLOT_SKILL3);
    RegisterIconCharacter(Rage_Fighter, TEXTBOX_IMG_SKILL2_TIME);
}

void CNewUIMuHelper::InitText()
{
    InsertText(m_Pos.x + 18, m_Pos.y + 160, GlobalText[3529], 5, 0); // Basic Skill
    InsertText(m_Pos.x + 59, m_Pos.y + 160, GlobalText[3530], 7, 0); // Activation Skill 1
    //InsertText(m_Pos.x + 162, m_Pos.y + 178, GlobalText[3528], 8, 0);
    InsertText(m_Pos.x + 162, m_Pos.y + 178, L"s", 8, 0);
    InsertText(m_Pos.x + 59, m_Pos.y + 212, GlobalText[3531], 9, 0); // Activation Skill 2

    //InsertText(m_Pos.x + 162, m_Pos.y + 230, GlobalText[3528], 10, 0);
    InsertText(m_Pos.x + 162, m_Pos.y + 230, L"s", 10, 0);
    RegisterTextCharacter(0xFF, 5);
    RegisterTextCharacter(0xFF, 7);
    RegisterTextCharacter(0xFF, 8);

    RegisterTextCharacter(Dark_Knight, 9);
    RegisterTextCharacter(Dark_Knight, 10);
    RegisterTextCharacter(Dark_Wizard, 9);
    RegisterTextCharacter(Dark_Wizard, 10);
    RegisterTextCharacter(Fairy_Elf, 9);
    RegisterTextCharacter(Fairy_Elf, 10);
    RegisterTextCharacter(Magic_Gladiator, 9);
    RegisterTextCharacter(Magic_Gladiator, 10);
    RegisterTextCharacter(Summoner, 9);
    RegisterTextCharacter(Summoner, 10);
    RegisterTextCharacter(Rage_Fighter, 9);
    RegisterTextCharacter(Rage_Fighter, 10);
}

void CNewUIMuHelper::InitTextboxInput()
{
    wchar_t wsInitText[MAX_NUMBER_DIGITS + 1];

    m_Skill2DelayInput.Init(g_hWnd, 17, 15, MAX_NUMBER_DIGITS, false);
    m_Skill2DelayInput.SetPosition(m_Pos.x + 142, m_Pos.y + 177);
    m_Skill2DelayInput.SetTextColor(255, 0, 0, 0);
    m_Skill2DelayInput.SetBackColor(255, 255, 255, 255);
    m_Skill2DelayInput.SetFont(g_hFont);
    m_Skill2DelayInput.SetState(UISTATE_NORMAL);
    m_Skill2DelayInput.SetOption(UIOPTION_NUMBERONLY);
    std::swprintf(wsInitText, MAX_NUMBER_DIGITS + 1, L"%d", _TempConfig.aiSkillInterval[1]);
    m_Skill2DelayInput.SetText(wsInitText);

    m_Skill3DelayInput.Init(g_hWnd, 17, 15, MAX_NUMBER_DIGITS, false);
    m_Skill3DelayInput.SetPosition(m_Pos.x + 142, m_Pos.y + 229);
    m_Skill3DelayInput.SetTextColor(255, 0, 0, 0);
    m_Skill3DelayInput.SetBackColor(255, 255, 255, 255);
    m_Skill3DelayInput.SetFont(g_hFont);
    m_Skill3DelayInput.SetState(UISTATE_NORMAL);
    m_Skill3DelayInput.SetOption(UIOPTION_NUMBERONLY);
    std::swprintf(wsInitText, MAX_NUMBER_DIGITS + 1, L"%d", _TempConfig.aiSkillInterval[2]);
    m_Skill3DelayInput.SetText(wsInitText);

    m_ItemInput.Init(g_hWnd, 88, 15, MAX_ITEM_NAME, false);
    m_ItemInput.SetPosition(m_Pos.x + 36, m_Pos.y + 219);
    m_ItemInput.SetTextColor(255, 0, 0, 0);
    m_ItemInput.SetBackColor(255, 255, 255, 255);
    m_ItemInput.SetFont(g_hFont);
    m_ItemInput.SetState(UISTATE_HIDE);

    m_ItemFilter.SetSize(160, 70);
    m_ItemFilter.SetPosition(m_Pos.x + 20, m_Pos.y + 238 + m_ItemFilter.GetHeight());
}

bool CNewUIMuHelper::Update()
{
    if (IsVisible())
    {
        int iNumCurOpenTab = m_TabBtn.UpdateMouseEvent();

        if (iNumCurOpenTab == RADIOGROUPEVENT_NONE)
            return true;

        m_iCurrentOpenTab = iNumCurOpenTab;

        if (m_iCurrentOpenTab == 0)
        {
            m_Skill2DelayInput.SetState(UISTATE_NORMAL);
            m_Skill3DelayInput.SetState(UISTATE_NORMAL);
            m_ItemInput.SetState(UISTATE_HIDE);
        }
        else if (m_iCurrentOpenTab == 1)
        {
            m_Skill2DelayInput.SetState(UISTATE_HIDE);
            m_Skill3DelayInput.SetState(UISTATE_HIDE);
            m_ItemInput.SetState(UISTATE_NORMAL);
        }
    }
    return true;
}

bool CNewUIMuHelper::UpdateMouseEvent()
{
    // Ignore events outside MU Helper window
    if (!CheckMouseIn(m_Pos.x, m_Pos.y, WINDOW_WIDTH, WINDOW_HEIGHT))
    {
        return true;
    }

    int iButtonId = UpdateMouseBtnList();
    if (iButtonId != -1)
    {
        g_ConsoleDebug->Write(MCD_NORMAL, L"[MU Helper] Clicked button [%d]", iButtonId);

        if (iButtonId == BUTTON_ID_ADD_OTHER_ITEM)
        {
            SaveExtraItem();
        }
        else if (iButtonId == BUTTON_ID_DELETE_OTHER_ITEM)
        {
            RemoveExtraItem();
        }
        else if (iButtonId == BUTTON_ID_SKILL2_CONFIG)
        {
            m_CheckBoxList[CHECKBOX_ID_SKILL2_DELAY].box->RegisterBoxState(false);
            m_CheckBoxList[CHECKBOX_ID_SKILL2_CONDITION].box->RegisterBoxState(true);
            ApplyConfigFromCheckbox(CHECKBOX_ID_SKILL2_CONDITION, true);
            g_pNewUIMuHelperExt->Toggle(SUB_PAGE_SKILL2_CONFIG);
        }
        else if (iButtonId == BUTTON_ID_SKILL3_CONFIG)
        {
            m_CheckBoxList[CHECKBOX_ID_SKILL3_DELAY].box->RegisterBoxState(false);
            m_CheckBoxList[CHECKBOX_ID_SKILL3_CONDITION].box->RegisterBoxState(true);
            ApplyConfigFromCheckbox(CHECKBOX_ID_SKILL3_CONDITION, true);
            g_pNewUIMuHelperExt->Toggle(SUB_PAGE_SKILL3_CONFIG);
        }
        else if (iButtonId == BUTTON_ID_POTION_CONFIG_ELF)
        {
            g_pNewUIMuHelperExt->Toggle(SUB_PAGE_POTION_CONFIG_ELF);
        }
        else if (iButtonId == BUTTON_ID_POTION_CONFIG_SUMMY)
        {
            g_pNewUIMuHelperExt->Toggle(SUB_PAGE_POTION_CONFIG_SUMMY);
        }
        else if (iButtonId == BUTTON_ID_POTION_CONFIG)
        {
            g_pNewUIMuHelperExt->Toggle(SUB_PAGE_POTION_CONFIG);
        }
        else if (iButtonId == BUTTON_ID_PARTY_CONFIG)
        {
            g_pNewUIMuHelperExt->Toggle(SUB_PAGE_PARTY_CONFIG);
        }
        else if (iButtonId == BUTTON_ID_PARTY_CONFIG_ELF)
        {
            g_pNewUIMuHelperExt->Toggle(SUB_PAGE_PARTY_CONFIG_ELF);
        }
        else if (iButtonId == BUTTON_ID_EXIT_CONFIG)
        {
            g_pNewUISystem->Hide(INTERFACE_MUHELPER);
            SetFocus(g_hWnd);
        }
        else if (iButtonId == BUTTON_ID_INIT_CONFIG)
        {
            InitConfig();
        }
        else if (iButtonId == BUTTON_ID_SAVE_CONFIG)
        {
            SaveConfig();
            g_pNewUISystem->Hide(INTERFACE_MUHELPER);
            SetFocus(g_hWnd);
        }
        else if (iButtonId == BUTTON_ID_JEWELBANK)
        {
            g_pNewUIJewelBank->Toggle();
        }
        else if (iButtonId == BUTTON_ID_AUCTIONHOUSE)
        {
            g_pNewUIAuctionHouse->Toggle();
        }

        return false;
    }

    int iCheckboxId = UpdateMouseBoxList();
    if (iCheckboxId != -1)
    {
        auto element = m_CheckBoxList[iCheckboxId];
        auto state = element.box->GetBoxState();
        g_ConsoleDebug->Write(MCD_NORMAL, L"[MU Helper] Clicked checkbox [%d] state[%d]", iCheckboxId, state);

        if (iCheckboxId == CHECKBOX_ID_SKILL2_DELAY)
        {
            bool bState = m_CheckBoxList[CHECKBOX_ID_SKILL2_DELAY].box->GetBoxState();
            if (bState == true)
            {
                m_CheckBoxList[CHECKBOX_ID_SKILL2_CONDITION].box->RegisterBoxState(false);
                g_pNewUISystem->Hide(INTERFACE_MUHELPER_EXT);
            }
        }
        else if (iCheckboxId == CHECKBOX_ID_SKILL2_CONDITION)
        {
            bool bState = m_CheckBoxList[CHECKBOX_ID_SKILL2_CONDITION].box->GetBoxState();
            if (bState == true)
            {
                m_CheckBoxList[CHECKBOX_ID_SKILL2_DELAY].box->RegisterBoxState(false);
            }
            else
            {
                g_pNewUISystem->Hide(INTERFACE_MUHELPER_EXT);
            }
        }
        else if (iCheckboxId == CHECKBOX_ID_SKILL3_DELAY)
        {
            bool bState = m_CheckBoxList[CHECKBOX_ID_SKILL3_DELAY].box->GetBoxState();
            if (bState == true)
            {
                m_CheckBoxList[CHECKBOX_ID_SKILL3_CONDITION].box->RegisterBoxState(false);
                g_pNewUISystem->Hide(INTERFACE_MUHELPER_EXT);
            }
        }
        else if (iCheckboxId == CHECKBOX_ID_SKILL3_CONDITION)
        {
            bool bState = m_CheckBoxList[CHECKBOX_ID_SKILL3_CONDITION].box->GetBoxState();
            if (bState == true)
            {
                m_CheckBoxList[CHECKBOX_ID_SKILL3_DELAY].box->RegisterBoxState(false);
            }
            else
            {
                g_pNewUISystem->Hide(INTERFACE_MUHELPER_EXT);
            }
        }
        else if (iCheckboxId == CHECKBOX_ID_DR_ATTACK_CEASE)
        {
            m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_CEASE].box->RegisterBoxState(true);
            m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_AUTO].box->RegisterBoxState(false);
            m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_TOGETHER].box->RegisterBoxState(false);
        }
        else if (iCheckboxId == CHECKBOX_ID_DR_ATTACK_AUTO)
        {
            m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_AUTO].box->RegisterBoxState(true);
            m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_CEASE].box->RegisterBoxState(false);
            m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_TOGETHER].box->RegisterBoxState(false);
        }
        else if (iCheckboxId == CHECKBOX_ID_DR_ATTACK_TOGETHER)
        {
            m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_TOGETHER].box->RegisterBoxState(true);
            m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_CEASE].box->RegisterBoxState(false);
            m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_AUTO].box->RegisterBoxState(false);
        }
        else if (iCheckboxId == CHECKBOX_ID_PARTY_REQUEST_NORMAL)
        {
            m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_NORMAL].box->RegisterBoxState(true);
            m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_AUTO].box->RegisterBoxState(false);
            m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_OFF].box->RegisterBoxState(false);
        }
        else if (iCheckboxId == CHECKBOX_ID_PARTY_REQUEST_AUTO)
        {
            m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_AUTO].box->RegisterBoxState(true);
            m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_NORMAL].box->RegisterBoxState(false);
            m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_OFF].box->RegisterBoxState(false);
        }
        else if (iCheckboxId == CHECKBOX_ID_PARTY_REQUEST_OFF)
        {
            m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_OFF].box->RegisterBoxState(true);
            m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_NORMAL].box->RegisterBoxState(false);
            m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_AUTO].box->RegisterBoxState(false);
        }

        ApplyConfigFromCheckbox(iCheckboxId, state);

        return false;
    }

    if (IsRelease(VK_LBUTTON))
    {
        int iPrevIndex = m_iSelectedSkillSlot;
        int iIconIndex = UpdateMouseIconList();

        if (iIconIndex != -1 && iIconIndex < MAX_SKILLS_SLOT)
        {
            g_ConsoleDebug->Write(MCD_NORMAL, L"[MU Helper] Clicked skill slot [%d]", iIconIndex);
            m_iSelectedSkillSlot = iIconIndex;

            bool bPrevVisible = g_pNewUISystem->IsVisible(INTERFACE_MUHELPER_SKILL_LIST);

            if (iIconIndex == SKILL_SLOT_SKILL1
                || iIconIndex == SKILL_SLOT_SKILL2
                || iIconIndex == SKILL_SLOT_SKILL3)
            {
                g_pNewUIMuHelperSkillList->FilterByAttackSkills();
            }
            else
            {
                g_pNewUIMuHelperSkillList->FilterByBuffSkills();
            }

            if (iIconIndex == iPrevIndex && bPrevVisible)
            {
                g_pNewUISystem->Hide(INTERFACE_MUHELPER_SKILL_LIST);
            }
            else
            {
                g_pNewUISystem->Show(INTERFACE_MUHELPER_SKILL_LIST);
            }

            return false;
        }
        else if (iIconIndex == TEXTBOX_IMG_SKILL1_TIME)
        {
            m_Skill2DelayInput.GiveFocus();
        }
        else if (iIconIndex == TEXTBOX_IMG_SKILL2_TIME)
        {
            m_Skill3DelayInput.GiveFocus();
        }
        else if (iIconIndex == TEXTBOX_IMG_ADD_EXTRA_ITEM)
        {
            m_ItemInput.GiveFocus();
        }
        else
        {
            SetFocus(g_hWnd);
        }

        POINT ptExitBtn = { m_Pos.x + 169, m_Pos.y + 7 };
        if (CheckMouseIn(ptExitBtn.x, ptExitBtn.y, 13, 12))
        {
            g_pNewUISystem->Hide(SEASON3B::INTERFACE_MUHELPER);
        }
    }
    if (IsRelease(VK_RBUTTON))
    {
        int iSlotIndex = UpdateMouseIconList();
        if (iSlotIndex != -1)
        {
            g_ConsoleDebug->Write(MCD_NORMAL, L"[MU Helper] Clicked slot slot [%d]", iSlotIndex);
            m_aiSelectedSkills[iSlotIndex] = -1;

            auto cboxCombo = m_CheckBoxList[CHECKBOX_ID_COMBO];
            if (cboxCombo.box->GetBoxState() == true)
            {
                cboxCombo.box->RegisterBoxState(false);
                _TempConfig.bUseCombo = false;
            }

            return false;
        }
    }

    if (m_iCurrentOpenTab == 1)
    {
        m_ItemFilter.DoAction();
    }

    return false;
}

bool CNewUIMuHelper::UpdateKeyEvent()
{
    if (IsVisible())
    {
        if (IsPress(VK_ESCAPE) == true)
        {
            g_pNewUISystem->Hide(INTERFACE_MUHELPER);
            g_pNewUISystem->Hide(INTERFACE_MUHELPER_SKILL_LIST);
            //PlayBuffer(SOUND_CLICK01);
            SetFocus(g_hWnd);

            return false;
        }
    }
    return true;
}

void CNewUIMuHelper::ApplyConfigFromCheckbox(int iCheckboxId, bool bState)
{
    switch (iCheckboxId) {
    case CHECKBOX_ID_POTION:
        _TempConfig.bUseHealPotion = bState;
        break;

    case CHECKBOX_ID_SKILL2_DELAY:
        _TempConfig.aiSkillCondition[1] &= ~ON_CONDITION;
        _TempConfig.aiSkillCondition[1] = bState
            ? (_TempConfig.aiSkillCondition[1] | ON_TIMER)
            : (_TempConfig.aiSkillCondition[1] & ~ON_TIMER);
        break;

    case CHECKBOX_ID_SKILL2_CONDITION:
        _TempConfig.aiSkillCondition[1] &= ~ON_TIMER;
        _TempConfig.aiSkillCondition[1] = bState
            ? (_TempConfig.aiSkillCondition[1] | ON_CONDITION)
            : (_TempConfig.aiSkillCondition[1] & ~ON_CONDITION);
        break;

    case CHECKBOX_ID_SKILL3_DELAY:
        _TempConfig.aiSkillCondition[2] &= ~ON_CONDITION;
        _TempConfig.aiSkillCondition[2] = bState
            ? (_TempConfig.aiSkillCondition[2] | ON_TIMER)
            : (_TempConfig.aiSkillCondition[2] & ~ON_TIMER);
        break;

    case CHECKBOX_ID_SKILL3_CONDITION:
        _TempConfig.aiSkillCondition[2] &= ~ON_TIMER;
        _TempConfig.aiSkillCondition[2] = bState
            ? (_TempConfig.aiSkillCondition[2] | ON_CONDITION)
            : (_TempConfig.aiSkillCondition[2] & ~ON_CONDITION);
        break;

    case CHECKBOX_ID_COMBO:
	{
		auto cboxCombo = m_CheckBoxList[CHECKBOX_ID_COMBO];

		if (bState == true)
		{
			if (m_aiSelectedSkills[0] <= 0 || m_aiSelectedSkills[1] <= 0 || m_aiSelectedSkills[2] <= 0)
			{
				g_pSystemLogBox->AddText(GlobalText[3565], SEASON3B::TYPE_ERROR_MESSAGE);
				cboxCombo.box->RegisterBoxState(false);
			}
		}
		
		_TempConfig.bUseCombo = cboxCombo.box->GetBoxState();
		break;
	}

    case CHECKBOX_ID_BUFF_DURATION:
        _TempConfig.bBuffDuration = bState;
        break;

    case CHECKBOX_ID_USE_PET:
        _TempConfig.bUseDarkRaven = bState;
        break;

    case CHECKBOX_ID_DR_ATTACK_CEASE:
        _TempConfig.iDarkRavenMode = PET_ATTACK_CEASE;
        break;

    case CHECKBOX_ID_DR_ATTACK_AUTO:
        _TempConfig.iDarkRavenMode = PET_ATTACK_AUTO;
        break;

    case CHECKBOX_ID_DR_ATTACK_TOGETHER:
        _TempConfig.iDarkRavenMode = PET_ATTACK_TOGETHER;
        break;

    case CHECKBOX_ID_PARTY:
        _TempConfig.bSupportParty = bState;
        break;

    case CHECKBOX_ID_AUTO_HEAL:
        _TempConfig.bAutoHeal = bState;
        break;

    case CHECKBOX_ID_DRAIN_LIFE:
        _TempConfig.bUseDrainLife = bState;
        break;

    case CHECKBOX_ID_REPAIR_ITEM:
        _TempConfig.bRepairItem = bState;
        break;

    case CHECKBOX_ID_PICK_ALL:
	{
		auto cboxPickSelected = m_CheckBoxList[CHECKBOX_ID_PICK_SELECTED];
		if (cboxPickSelected.box->GetBoxState())
		{
			cboxPickSelected.box->RegisterBoxState(false);
		}
		_TempConfig.bPickAllItems = bState;
		break;
	}

    case CHECKBOX_ID_PICK_SELECTED:
	{
		auto cboxPickAll = m_CheckBoxList[CHECKBOX_ID_PICK_ALL];
		if (cboxPickAll.box->GetBoxState())
		{
			cboxPickAll.box->RegisterBoxState(false);
		}
		_TempConfig.bPickSelectItems = bState;
		break;
	}

    case CHECKBOX_ID_PICK_JEWEL:
        _TempConfig.bPickJewel = bState;
        break;

    case CHECKBOX_ID_PICK_ANCIENT:
        _TempConfig.bPickAncient = bState;
        break;

    case CHECKBOX_ID_PICK_ZEN:
        _TempConfig.bPickZen = bState;
        break;

    case CHECKBOX_ID_PICK_EXCELLENT:
        _TempConfig.bPickExcellent = bState;
        break;

    case CHECKBOX_ID_ADD_OTHER_ITEM:
        _TempConfig.bPickExtraItems = bState;
        break;

    case CHECKBOX_ID_AUTO_DEFEND:
        _TempConfig.bUseSelfDefense = bState;
        break;

    case CHECKBOX_ID_AUTO_ACCEPT_FRIEND:
        _TempConfig.bAutoAcceptFriend = bState;
        break;

    case CHECKBOX_ID_AUTO_ACCEPT_GUILD:
        _TempConfig.bAutoAcceptGuild = bState;
        break;

    case CHECKBOX_ID_PARTY_REQUEST_NORMAL:
        _TempConfig.iPartyRequestMode = MUHelper::PARTY_REQUEST_NORMAL;
        break;

    case CHECKBOX_ID_PARTY_REQUEST_AUTO:
        _TempConfig.iPartyRequestMode = MUHelper::PARTY_REQUEST_AUTO;
        break;

    case CHECKBOX_ID_PARTY_REQUEST_OFF:
        _TempConfig.iPartyRequestMode = MUHelper::PARTY_REQUEST_OFF;
        break;

    case CHECKBOX_ID_OFFLEVEL:
        if (!IsOfflevelVipVisible())
        {
            _TempConfig.bOfflevel = false;
            m_CheckBoxList[CHECKBOX_ID_OFFLEVEL].box->RegisterBoxState(false);
            break;
        }

        _TempConfig.bOfflevel = bState;
        if (bState)
        {
            SendOfflevelCommand();
        }
        break;

    default:
        break;
    }
}

void CNewUIMuHelper::ApplyConfigFromSkillSlot(int iSlot, int iSkill)
{
    if (iSlot < 3)
    {
        _TempConfig.aiSkill[iSlot] = iSkill;
    }
    else
    {
        _TempConfig.aiBuff[iSlot - SKILL_SLOT_BUFF1] = iSkill;
    }
}

void CNewUIMuHelper::SaveExtraItem()
{
    wchar_t wsExtraItem[MAX_ITEM_NAME + 1] = { 0 };

    m_ItemInput.GetText(wsExtraItem, sizeof(wsExtraItem));

    if (wcscmp(wsExtraItem, L"") != 0)
    {
        m_ItemFilter.AddText(wsExtraItem);
        m_ItemFilter.Scrolling(-m_ItemFilter.GetBoxSize());

        _TempConfig.aExtraItems.insert(std::wstring(wsExtraItem));
    }

    int iItemIndex = 0;
    for (const auto& item : _TempConfig.aExtraItems)
    {
        g_ConsoleDebug->Write(MCD_NORMAL, L"%ls", item.c_str());
    }

    m_ItemInput.SetText(L"");
    SetFocus(g_hWnd);
}

void CNewUIMuHelper::RemoveExtraItem()
{
    FILTERLIST_TEXT* pText = m_ItemFilter.GetSelectedText();
    if (pText)
    {
        _TempConfig.aExtraItems.erase(std::wstring(pText->m_szPattern));
        m_ItemFilter.DeleteText(pText->m_szPattern);
    }
}

int CNewUIMuHelper::GetIntFromTextInput(wchar_t* pwsInput)
{
    wchar_t* end;

    int value = static_cast<int>(wcstol(pwsInput, &end, 10));  // Base 10

    if (*end != L'\0')
    {
        return 0;
    }

    return value;
}

void CNewUIMuHelper::Reset()
{
    _TempConfig.aiSkill.fill(0);
    _TempConfig.bUseCombo = false;

    _TempConfig.aiSkillInterval.fill(0);

    _TempConfig.aiSkillCondition.fill(0);

    _TempConfig.aiBuff.fill(0);

    _TempConfig.bBuffDuration = true;
    _TempConfig.bBuffDurationParty = true;
    _TempConfig.iBuffCastInterval = 0;

    _TempConfig.bAutoHeal = false;
    _TempConfig.iHealThreshold = 60;
    _TempConfig.bUseDrainLife = false;
    _TempConfig.bUseHealPotion = false;
    _TempConfig.iPotionThreshold = 40;
    _TempConfig.bSupportParty = false;
    _TempConfig.bAutoHealParty = false;
    _TempConfig.iHealPartyThreshold = 60;

    _TempConfig.bUseDarkRaven = false;
    _TempConfig.iDarkRavenMode = PET_ATTACK_CEASE;
    _TempConfig.bRepairItem = false;

    _TempConfig.bPickAllItems = false;
    _TempConfig.bPickSelectItems = false;
    _TempConfig.bPickZen = false;
    _TempConfig.bPickJewel = false;
    _TempConfig.bPickExcellent = false;
    _TempConfig.bPickAncient = false;
    _TempConfig.bPickExtraItems = false;
    _TempConfig.aExtraItems.clear();

    _TempConfig.bOfflevel = false;
    _TempConfig.iPartyRequestMode = MUHelper::PARTY_REQUEST_NORMAL;

    ApplyConfig();
}

void CNewUIMuHelper::LoadSavedConfig(const ConfigData& config)
{
    _TempConfig = config;
    ApplyConfig();
}

void CNewUIMuHelper::ApplyConfig()
{
    g_MuHelper.Load(_TempConfig);

    m_aiSelectedSkills[0] = _TempConfig.aiSkill[0] ? _TempConfig.aiSkill[0] : -1;
    m_aiSelectedSkills[1] = _TempConfig.aiSkill[1] ? _TempConfig.aiSkill[1] : -1;
    m_aiSelectedSkills[2] = _TempConfig.aiSkill[2] ? _TempConfig.aiSkill[2] : -1;
    m_aiSelectedSkills[3] = _TempConfig.aiBuff[0] ? _TempConfig.aiBuff[0] : -1;
    m_aiSelectedSkills[4] = _TempConfig.aiBuff[1] ? _TempConfig.aiBuff[1] : -1;
    m_aiSelectedSkills[5] = _TempConfig.aiBuff[2] ? _TempConfig.aiBuff[2] : -1;

    m_CheckBoxList[CHECKBOX_ID_POTION].box->RegisterBoxState(_TempConfig.bUseHealPotion);
    m_CheckBoxList[CHECKBOX_ID_AUTO_HEAL].box->RegisterBoxState(_TempConfig.bAutoHeal);
    m_CheckBoxList[CHECKBOX_ID_DRAIN_LIFE].box->RegisterBoxState(_TempConfig.bUseDrainLife);

    m_CheckBoxList[CHECKBOX_ID_SKILL2_DELAY].box->RegisterBoxState(_TempConfig.aiSkillCondition[1] & ON_TIMER);
    m_CheckBoxList[CHECKBOX_ID_SKILL2_CONDITION].box->RegisterBoxState(_TempConfig.aiSkillCondition[1] & ON_CONDITION);
    m_CheckBoxList[CHECKBOX_ID_SKILL3_DELAY].box->RegisterBoxState(_TempConfig.aiSkillCondition[2] & ON_TIMER);
    m_CheckBoxList[CHECKBOX_ID_SKILL3_CONDITION].box->RegisterBoxState(_TempConfig.aiSkillCondition[2] & ON_CONDITION);
    m_CheckBoxList[CHECKBOX_ID_COMBO].box->RegisterBoxState(_TempConfig.bUseCombo);

    wchar_t wsTempNum[MAX_NUMBER_DIGITS + 1];
    memset(wsTempNum, 0, sizeof(wsTempNum));
    std::swprintf(wsTempNum, MAX_NUMBER_DIGITS + 1, L"%d", _TempConfig.aiSkillInterval[1]);
    m_Skill2DelayInput.SetText(wsTempNum);

    memset(wsTempNum, 0, sizeof(wsTempNum));
    std::swprintf(wsTempNum, MAX_NUMBER_DIGITS + 1, L"%d", _TempConfig.aiSkillInterval[2]);
    m_Skill3DelayInput.SetText(wsTempNum);

    m_CheckBoxList[CHECKBOX_ID_BUFF_DURATION].box->RegisterBoxState(_TempConfig.bBuffDuration);
    m_CheckBoxList[CHECKBOX_ID_PARTY].box->RegisterBoxState(_TempConfig.bSupportParty);

    m_CheckBoxList[CHECKBOX_ID_USE_PET].box->RegisterBoxState(_TempConfig.bUseDarkRaven);
    m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_CEASE].box->RegisterBoxState(_TempConfig.iDarkRavenMode == PET_ATTACK_CEASE);
    m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_AUTO].box->RegisterBoxState(_TempConfig.iDarkRavenMode == PET_ATTACK_AUTO);
    m_CheckBoxList[CHECKBOX_ID_DR_ATTACK_TOGETHER].box->RegisterBoxState(_TempConfig.iDarkRavenMode == PET_ATTACK_TOGETHER);

    m_CheckBoxList[CHECKBOX_ID_REPAIR_ITEM].box->RegisterBoxState(_TempConfig.bRepairItem);
    m_CheckBoxList[CHECKBOX_ID_PICK_ALL].box->RegisterBoxState(_TempConfig.bPickAllItems);
    m_CheckBoxList[CHECKBOX_ID_PICK_SELECTED].box->RegisterBoxState(_TempConfig.bPickSelectItems);
    m_CheckBoxList[CHECKBOX_ID_PICK_JEWEL].box->RegisterBoxState(_TempConfig.bPickJewel);
    m_CheckBoxList[CHECKBOX_ID_PICK_ZEN].box->RegisterBoxState(_TempConfig.bPickZen);
    m_CheckBoxList[CHECKBOX_ID_PICK_EXCELLENT].box->RegisterBoxState(_TempConfig.bPickExcellent);
    m_CheckBoxList[CHECKBOX_ID_PICK_ANCIENT].box->RegisterBoxState(_TempConfig.bPickAncient);
    m_CheckBoxList[CHECKBOX_ID_ADD_OTHER_ITEM].box->RegisterBoxState(_TempConfig.bPickExtraItems);

    m_CheckBoxList[CHECKBOX_ID_AUTO_ACCEPT_FRIEND].box->RegisterBoxState(_TempConfig.bAutoAcceptFriend);
    m_CheckBoxList[CHECKBOX_ID_AUTO_ACCEPT_GUILD].box->RegisterBoxState(_TempConfig.bAutoAcceptGuild);
    m_CheckBoxList[CHECKBOX_ID_AUTO_DEFEND].box->RegisterBoxState(_TempConfig.bUseSelfDefense);

    m_CheckBoxList[CHECKBOX_ID_OFFLEVEL].box->RegisterBoxState(IsOfflevelVipVisible() && _TempConfig.bOfflevel);

    m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_NORMAL].box->RegisterBoxState(
        _TempConfig.iPartyRequestMode == MUHelper::PARTY_REQUEST_NORMAL);
    m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_AUTO].box->RegisterBoxState(
        _TempConfig.iPartyRequestMode == MUHelper::PARTY_REQUEST_AUTO);
    m_CheckBoxList[CHECKBOX_ID_PARTY_REQUEST_OFF].box->RegisterBoxState(
        _TempConfig.iPartyRequestMode == MUHelper::PARTY_REQUEST_OFF);

    m_ItemFilter.Clear();
    for (const auto& item : _TempConfig.aExtraItems)
    {
        m_ItemFilter.AddText(item.c_str());
    }
}

void CNewUIMuHelper::InitConfig()
{
    Reset();

    g_pNewUIMuHelperExt->InitConfig();
}

void CNewUIMuHelper::SaveConfig()
{
    wchar_t wsNumberInput[MAX_NUMBER_DIGITS + 1]{};

    m_Skill2DelayInput.GetText(wsNumberInput, sizeof(wsNumberInput));
    _TempConfig.aiSkillInterval[1] = GetIntFromTextInput(wsNumberInput);

    m_Skill3DelayInput.GetText(wsNumberInput, sizeof(wsNumberInput));
    _TempConfig.aiSkillInterval[2] = GetIntFromTextInput(wsNumberInput);

    _TempConfig.aiSkill[0] = m_aiSelectedSkills[0] > 0 ? m_aiSelectedSkills[0] : 0;
    _TempConfig.aiSkill[1] = m_aiSelectedSkills[1] > 0 ? m_aiSelectedSkills[1] : 0;
    _TempConfig.aiSkill[2] = m_aiSelectedSkills[2] > 0 ? m_aiSelectedSkills[2] : 0;
    _TempConfig.aiBuff[0] = m_aiSelectedSkills[3] > 0 ? m_aiSelectedSkills[3] : 0;
    _TempConfig.aiBuff[1] = m_aiSelectedSkills[4] > 0 ? m_aiSelectedSkills[4] : 0;
    _TempConfig.aiBuff[2] = m_aiSelectedSkills[5] > 0 ? m_aiSelectedSkills[5] : 0;

    g_MuHelper.Save(_TempConfig);
}

float CNewUIMuHelper::GetLayerDepth()
{
    return 3.4;
}

float CNewUIMuHelper::GetKeyEventOrder()
{
    return 3.4;
}

void CNewUIMuHelper::Show(bool bShow)
{
    CNewUIObj::Show(bShow);

    if (bShow == false)
    {
        if (g_pNewUIMuHelperExt)
            g_pNewUIMuHelperExt->Show(false);

        if (g_pNewUIMuHelperSkillList)
            g_pNewUIMuHelperSkillList->Show(false);
    }

    SetFocus(g_hWnd);
}

bool CNewUIMuHelper::Render()
{
    EnableAlphaTest();
    glColor4f(1.f, 1.f, 1.f, 1.f);

    DWORD TextColor = g_pRenderText->GetTextColor();

    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->SetTextColor(0xFFFFFFFF);
    g_pRenderText->SetBgColor(0);

    RenderImage(IMAGE_BASE_WINDOW_BACK, m_Pos.x, m_Pos.y, float(WINDOW_WIDTH), float(WINDOW_HEIGHT));
    RenderImage(IMAGE_BASE_WINDOW_TOP, m_Pos.x, m_Pos.y, float(WINDOW_WIDTH), 64.f);
    RenderImage(IMAGE_BASE_WINDOW_LEFT, m_Pos.x, m_Pos.y + 64.f, 21.f, float(WINDOW_HEIGHT) - 64.f - 45.f);
    RenderImage(IMAGE_BASE_WINDOW_RIGHT, m_Pos.x + float(WINDOW_WIDTH) - 21.f, m_Pos.y + 64.f, 21.f, float(WINDOW_HEIGHT) - 64.f - 45.f);
    RenderImage(IMAGE_BASE_WINDOW_BOTTOM, m_Pos.x, m_Pos.y + float(WINDOW_HEIGHT) - 45.f, float(WINDOW_WIDTH), 45.f);

    g_pRenderText->SetFont(g_hFontBold);

    g_pRenderText->RenderText(m_Pos.x, m_Pos.y + 13, GlobalText[3536], 190, 0, RT3_SORT_CENTER);

    RenderBack(m_Pos.x + 12, m_Pos.y + 340, 165, 46);

    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->RenderText(m_Pos.x + 20, m_Pos.y + 347, GlobalText[3537], 0, 0, RT3_SORT_CENTER);

    g_pRenderText->SetTextColor(0xFF00B4FF);
    g_pRenderText->RenderText(m_Pos.x + 20, m_Pos.y + 365, GlobalText[3538], 0, 0, RT3_SORT_CENTER);

    g_pRenderText->SetTextColor(TextColor);

    m_TabBtn.Render();

    if (m_iCurrentOpenTab == 1)
    {
        RenderBack(m_Pos.x + 75, m_Pos.y + 73, 102, 50);
        RenderBack(m_Pos.x + 12, m_Pos.y + 120, 165, 30);
        RenderBack(m_Pos.x + 12, m_Pos.y + 147, 165, 195);
        RenderBack(m_Pos.x + 16, m_Pos.y + 235, 158, 75);

        m_ItemFilter.Render();
    }
    else if (m_iCurrentOpenTab == 2)
    {
        RenderBack(m_Pos.x + 12, m_Pos.y + 73, 165, 50);
        RenderBack(m_Pos.x + 12, m_Pos.y + 120, 165, 222);
    }
    else
    {
        RenderBack(m_Pos.x + 75, m_Pos.y + 73, 102, 50);
        RenderBack(m_Pos.x + 12, m_Pos.y + 120, 165, 39);
        RenderBack(m_Pos.x + 12, m_Pos.y + 156, 165, 120);
        RenderBack(m_Pos.x + 12, m_Pos.y + 273, 165, 69);
    }

    RenderBoxList();
    RenderIconList();
    RenderTextList();
    RenderBtnList();

    if (m_iCurrentOpenTab == 0)
    {
        m_Skill2DelayInput.Render();

        if (gCharacterManager.GetBaseClass(Hero->Class) != CLASS_DARK_LORD)
        {
            m_Skill3DelayInput.Render();
        }
    }
    else if (m_iCurrentOpenTab == 1)
    {
        m_ItemInput.Render();
    }

    DisableAlphaBlend();

    return true;
}

void CNewUIMuHelper::RenderBack(int x, int y, int width, int height)
{
    EnableAlphaTest();
    glColor4f(0.0, 0.0, 0.0, 0.4f);
    RenderColor(x + 3.f, y + 2.f, width - 7.f, height - 7, 0.0, 0);
    EndRenderColor();

    RenderImage(IMAGE_TABLE_TOP_LEFT, x, y, 14.0, 14.0);
    RenderImage(IMAGE_TABLE_TOP_RIGHT, (x + width) - 14.f, y, 14.0, 14.0);
    RenderImage(IMAGE_TABLE_BOTTOM_LEFT, x, (y + height) - 14.f, 14.0, 14.0);
    RenderImage(IMAGE_TABLE_BOTTOM_RIGHT, (x + width) - 14.f, (y + height) - 14.f, 14.0, 14.0);
    RenderImage(IMAGE_TABLE_TOP_PIXEL, x + 6.f, y, (width - 12.f), 14.0);
    RenderImage(IMAGE_TABLE_RIGHT_PIXEL, (x + width) - 14.f, y + 6.f, 14.0, (height - 14.f));
    RenderImage(IMAGE_TABLE_BOTTOM_PIXEL, x + 6.f, (y + height) - 14.f, (width - 12.f), 14.0);
    RenderImage(IMAGE_TABLE_LEFT_PIXEL, x, (y + 6.f), 14.0, (height - 14.f));
}

void CNewUIMuHelper::LoadImages()
{
    LoadBitmap(L"Interface\\MacroUI\\MacroUI_OptionButton.tga", IMAGE_MACROUI_HELPER_OPTIONBUTTON, GL_LINEAR, GL_CLAMP, 1, 0);
    LoadBitmap(L"Interface\\MacroUI\\MacroUI_InputNumber.tga", IMAGE_MACROUI_HELPER_INPUTNUMBER, GL_LINEAR, GL_CLAMP, 1, 0);
    LoadBitmap(L"Interface\\MacroUI\\MacroUI_InputString.tga", IMAGE_MACROUI_HELPER_INPUTSTRING, GL_LINEAR, GL_CLAMP, 1, 0);
    //--
    LoadBitmap(L"Interface\\InGameShop\\Ingame_Bt03.tga", IMAGE_IGS_BUTTON, GL_LINEAR, GL_CLAMP, 1, 0);
}

void CNewUIMuHelper::UnloadImages()
{
    DeleteBitmap(IMAGE_MACROUI_HELPER_OPTIONBUTTON);
    DeleteBitmap(IMAGE_MACROUI_HELPER_INPUTNUMBER);
    DeleteBitmap(IMAGE_MACROUI_HELPER_INPUTSTRING);
    //--
    DeleteBitmap(IMAGE_IGS_BUTTON);
}

//===============================================================================================================
//===============================================================================================================

void CNewUIMuHelper::RegisterButton(int Identifier, CButtonTap button)
{
    m_ButtonList.insert(std::pair<int, CButtonTap>(Identifier, button));
}

void CNewUIMuHelper::RegisterBtnCharacter(BYTE class_character, int Identifier)
{
    auto li = m_ButtonList.find(Identifier);

    if (li != m_ButtonList.end())
    {
        CButtonTap* cBTN = &li->second;
        if (class_character >= 0 && class_character < MAX_CLASS)
        {
            cBTN->class_character[class_character] = TRUE;
        }
        else
        {
            memset(cBTN->class_character, 1, sizeof(cBTN->class_character));
        }
    }
}

void CNewUIMuHelper::InsertButton(int imgindex, int x, int y, int sx, int sy, bool overflg, bool isimgwidth, bool bClickEffect, bool MoveTxt, std::wstring btname, std::wstring tooltiptext, int Identifier, int iNumTab)
{
    CButtonTap cBTN;
    auto* button = new CNewUIButton();

    button->ChangeButtonImgState(1, imgindex, overflg, isimgwidth, bClickEffect);
    button->ChangeButtonInfo(x, y, sx, sy);

    button->ChangeText(btname);
    button->ChangeToolTipText(tooltiptext, TRUE);

    if (MoveTxt)
    {
        button->MoveTextPos(0, -1);
    }

    cBTN.btn = button;
    cBTN.iNumTab = iNumTab;
    memset(cBTN.class_character, 0, sizeof(cBTN.class_character));

    RegisterButton(Identifier, cBTN);
}

void CNewUIMuHelper::RenderBtnList()
{
    auto li = m_ButtonList.begin();

    for (; li != m_ButtonList.end(); li++)
    {
        CButtonTap* cBTN = &li->second;

        if ((cBTN->class_character[gCharacterManager.GetBaseClass(Hero->Class)]) && (cBTN->iNumTab == m_iCurrentOpenTab || cBTN->iNumTab == -1))
        {
            cBTN->btn->Render();
        }
    }
}

int CNewUIMuHelper::UpdateMouseBtnList()
{
    auto li = m_ButtonList.begin();

    for (; li != m_ButtonList.end(); li++)
    {
        CButtonTap* cBTN = &li->second;

        if ((cBTN->class_character[gCharacterManager.GetBaseClass(Hero->Class)]) && (cBTN->iNumTab == m_iCurrentOpenTab || cBTN->iNumTab == -1))
        {
            if (cBTN->btn->UpdateMouseEvent())
            {
                return li->first;
            }
        }
    }
    return -1;
}

//===============================================================================================================
//===============================================================================================================

void CNewUIMuHelper::RegisterBoxCharacter(BYTE class_character, int Identifier)
{
    auto li = m_CheckBoxList.find(Identifier);

    if (li != m_CheckBoxList.end())
    {
        CheckBoxTap* cBOX = &li->second;

        if (class_character >= 0 && class_character < MAX_CLASS)
        {
            cBOX->class_character[class_character] = TRUE;
        }
        else
        {
            memset(cBOX->class_character, 1, sizeof(cBOX->class_character));
        }
    }
}

void CNewUIMuHelper::RegisterCheckBox(int Identifier, CheckBoxTap button)
{
    m_CheckBoxList.insert(std::pair<int, CheckBoxTap>(Identifier, button));
}

void CNewUIMuHelper::InsertCheckBox(int imgindex, int x, int y, int sx, int sy, bool overflg, std::wstring btname, int Identifier, int iNumTab)
{
    CheckBoxTap cBOX;

    auto* cbox = new CNewUICheckBox;

    cbox->CheckBoxImgState(imgindex);
    cbox->CheckBoxInfo(x, y, sx, sy);

    cbox->ChangeText(btname);
    cbox->RegisterBoxState(overflg);

    cBOX.box = cbox;
    cBOX.iNumTab = iNumTab;
    memset(cBOX.class_character, 0, sizeof(cBOX.class_character));

    RegisterCheckBox(Identifier, cBOX);
}

void CNewUIMuHelper::RenderBoxList()
{
    auto li = m_CheckBoxList.begin();

    for (; li != m_CheckBoxList.end(); li++)
    {
        CheckBoxTap* cBOX = &li->second;

        if ((cBOX->class_character[gCharacterManager.GetBaseClass(Hero->Class)]) && (cBOX->iNumTab == m_iCurrentOpenTab || cBOX->iNumTab == -1))
        {
            if (IsVipOnlyCheckBox(li->first) && !IsOfflevelVipVisible())
            {
                continue;
            }

            cBOX->box->Render();
        }
    }
}

int CNewUIMuHelper::UpdateMouseBoxList()
{
    auto li = m_CheckBoxList.begin();

    for (; li != m_CheckBoxList.end(); li++)
    {
        CheckBoxTap* cBOX = &li->second;

        if ((cBOX->class_character[gCharacterManager.GetBaseClass(Hero->Class)]) && (cBOX->iNumTab == m_iCurrentOpenTab || cBOX->iNumTab == -1))
        {
            if (IsVipOnlyCheckBox(li->first) && !IsOfflevelVipVisible())
            {
                continue;
            }

            if (cBOX->box->UpdateMouseEvent())
            {
                return li->first;
            }
        }
    }
    return -1;
}

//===============================================================================================================
//===============================================================================================================

void CNewUIMuHelper::RenderIconList()
{
    auto li = m_IconList.begin();

    for (; li != m_IconList.end(); li++)
    {
        cTexture* cImage = &li->second;

        if ((cImage->class_character[gCharacterManager.GetBaseClass(Hero->Class)]) && (cImage->iNumTab == m_iCurrentOpenTab || cImage->iNumTab == -1))
        {
            RenderImage(cImage->s_ImgIndex, cImage->m_Pos.x, cImage->m_Pos.y, cImage->m_Size.x, cImage->m_Size.y);

            if (li->first < MAX_SKILLS_SLOT)
            {
                int iAssigned = m_aiSelectedSkills[li->first];
                if (iAssigned == BASIC_ATTACK_SKILL_ENTRY)
                {
                    RenderImage(IMAGE_NON_SKILL1, cImage->m_Pos.x + 6, cImage->m_Pos.y + 6, 20, 28);
                }
                else if (iAssigned >= 0 && iAssigned < MAX_SKILLS)
                {
                    RenderSkillIcon(iAssigned, cImage->m_Pos.x + 6, cImage->m_Pos.y + 6, 20, 28);
                }
            }
        }
    }
}

int CNewUIMuHelper::UpdateMouseIconList()
{
    auto li = m_IconList.begin();

    for (; li != m_IconList.end(); li++)
    {
        cTexture* cImage = &li->second;

        if ((cImage->class_character[gCharacterManager.GetBaseClass(Hero->Class)]) && (cImage->iNumTab == m_iCurrentOpenTab || cImage->iNumTab == -1))
        {
            if (CheckMouseIn(cImage->m_Pos.x, cImage->m_Pos.y, cImage->m_Size.x, cImage->m_Size.y))
            {
                return li->first;
            }
        }
    }

    return -1;
}

void CNewUIMuHelper::RegisterIconCharacter(BYTE class_character, int Identifier)
{
    auto li = m_IconList.find(Identifier);

    if (li != m_IconList.end())
    {
        cTexture* cImage = &li->second;

        if (class_character >= 0 && class_character < MAX_CLASS)
        {
            cImage->class_character[class_character] = TRUE;
        }
        else
        {
            memset(cImage->class_character, 1, sizeof(cImage->class_character));
        }
    }
}

void CNewUIMuHelper::RegisterIcon(int Identifier, cTexture button)
{
    m_IconList.insert(std::pair<int, cTexture>(Identifier, button));
}

void CNewUIMuHelper::InsertIcon(int imgindex, int x, int y, int sx, int sy, int Identifier, int iNumTab)
{
    cTexture cImage;

    cImage.s_ImgIndex = imgindex;
    cImage.m_Pos.x = x;
    cImage.m_Pos.y = y;
    cImage.m_Size.x = sx;
    cImage.m_Size.y = sy;
    cImage.iNumTab = iNumTab;

    memset(cImage.class_character, 0, sizeof(cImage.class_character));

    RegisterIcon(Identifier, cImage);
}

//===============================================================================================================
//===============================================================================================================

void CNewUIMuHelper::RenderTextList()
{
    auto li = m_TextNameList.begin();

    for (; li != m_TextNameList.end(); li++)
    {
        cTextName* cImage = &li->second;

        if ((cImage->class_character[gCharacterManager.GetBaseClass(Hero->Class)]) && (cImage->iNumTab == m_iCurrentOpenTab || cImage->iNumTab == -1))
        {
            g_pRenderText->RenderText(cImage->m_Pos.x, cImage->m_Pos.y, cImage->m_Name.c_str());
        }
    }
}

void CNewUIMuHelper::RegisterTextCharacter(BYTE class_character, int Identifier)
{
    auto li = m_TextNameList.find(Identifier);

    if (li != m_TextNameList.end())
    {
        cTextName* cImage = &li->second;

        if (class_character >= 0 && class_character < MAX_CLASS)
        {
            cImage->class_character[class_character] = TRUE;
        }
        else
        {
            memset(cImage->class_character, 1, sizeof(cImage->class_character));
        }
    }
}

void CNewUIMuHelper::RegisterText(int Identifier, cTextName button)
{
    m_TextNameList.insert(std::pair<int, cTextName>(Identifier, button));
}

void CNewUIMuHelper::InsertText(int x, int y, std::wstring Name, int Identifier, int iNumTab)
{
    cTextName cText;

    cText.m_Pos.x = x;
    cText.m_Pos.y = y;
    cText.m_Name = Name;
    cText.iNumTab = iNumTab;

    memset(cText.class_character, 0, sizeof(cText.class_character));
    RegisterText(Identifier, cText);
}

void CNewUIMuHelper::AssignSkill(int iSkill)
{
    if (m_iSelectedSkillSlot != -1 && m_iSelectedSkillSlot < MAX_SKILLS_SLOT)
    {
        if (!IsSkillAssigned(iSkill))
        {
            m_aiSelectedSkills[m_iSelectedSkillSlot] = iSkill;
            ApplyConfigFromSkillSlot(m_iSelectedSkillSlot, iSkill);

            g_ConsoleDebug->Write(MCD_NORMAL, L"[MU Helper] Assign m_aiSelectedSkills[%d] = %d", m_iSelectedSkillSlot, iSkill);
        }
        else
        {
            int iPrevIndex = GetSkillIndex(iSkill);
            m_aiSelectedSkills[iPrevIndex] = -1;
            m_aiSelectedSkills[m_iSelectedSkillSlot] = iSkill;

            auto cboxCombo = m_CheckBoxList[CHECKBOX_ID_COMBO];
            if (cboxCombo.box->GetBoxState() == true)
            {
                cboxCombo.box->RegisterBoxState(false);
                _TempConfig.bUseCombo = false;
            }
        }
    }
}

bool CNewUIMuHelper::IsSkillAssigned(int iSkill)
{
    return std::find(m_aiSelectedSkills.begin(), m_aiSelectedSkills.end(), iSkill) != m_aiSelectedSkills.end();
}

int CNewUIMuHelper::GetSkillIndex(int iSkill)
{
    auto it = std::find(m_aiSelectedSkills.begin(), m_aiSelectedSkills.end(), iSkill);

    if (it != m_aiSelectedSkills.end()) {
        return std::distance(m_aiSelectedSkills.begin(), it);
    }

    return -1;
}

void CNewUIMuHelper::RenderSkillIcon(int skill, float x, float y, float width, float height)
{
    float fU, fV;
    int iKindofSkill = 0;

    BYTE bySkillUseType = SkillAttribute[skill].SkillUseType;
    int Skill_Icon = SkillAttribute[skill].Magic_Icon;

    if (skill >= AT_PET_COMMAND_DEFAULT && skill <= AT_PET_COMMAND_END)
    {
        fU = ((skill - AT_PET_COMMAND_DEFAULT) % 8) * width / 256.f;
        fV = ((skill - AT_PET_COMMAND_DEFAULT) / 8) * height / 256.f;
        iKindofSkill = KOS_COMMAND;
    }
    else if (skill == AT_SKILL_PLASMA_STORM_FENRIR)
    {
        fU = 4 * width / 256.f;
        fV = 0.f;
        iKindofSkill = KOS_COMMAND;
    }
    else if ((skill >= AT_SKILL_ALICE_DRAINLIFE && skill <= AT_SKILL_ALICE_THORNS))
    {
        fU = ((skill - AT_SKILL_ALICE_DRAINLIFE) % 8) * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill >= AT_SKILL_ALICE_SLEEP && skill <= AT_SKILL_ALICE_BLIND)
    {
        fU = ((skill - AT_SKILL_ALICE_SLEEP + 4) % 8) * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill == AT_SKILL_ALICE_BERSERKER
        || skill == AT_SKILL_ALICE_BERSERKER_STR
        || skill == AT_SKILL_BerserkerProficiency)
    {
        fU = 10 * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill >= AT_SKILL_ALICE_WEAKNESS && skill <= AT_SKILL_ALICE_ENERVATION)
    {
        fU = (skill - AT_SKILL_ALICE_WEAKNESS + 8) * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill >= AT_SKILL_SUMMON_EXPLOSION && skill <= AT_SKILL_SUMMON_REQUIEM)
    {
        fU = ((skill - AT_SKILL_SUMMON_EXPLOSION + 6) % 8) * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill == AT_SKILL_SUMMON_POLLUTION)
    {
        fU = 11 * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill == AT_SKILL_STRIKE_OF_DESTRUCTION)
    {
        fU = 7 * width / 256.f;
        fV = 2 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill == AT_SKILL_CHAOTIC_DISEIER)
    {
        fU = 3 * width / 256.f;
        fV = 8 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill == AT_SKILL_RECOVER)
    {
        fU = 9 * width / 256.f;
        fV = 2 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill == AT_SKILL_MULTI_SHOT)
    {
        fU = 0 * width / 256.f;
        fV = 8 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill == AT_SKILL_FLAME_STRIKE)
    {
        int iTypeL = CharacterMachine->Equipment[EQUIPMENT_WEAPON_LEFT].Type;
        int iTypeR = CharacterMachine->Equipment[EQUIPMENT_WEAPON_RIGHT].Type;

        fU = 1 * width / 256.f;
        fV = 8 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill == AT_SKILL_GIGANTIC_STORM)
    {
        fU = 2 * width / 256.f;
        fV = 8 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill == AT_SKILL_LIGHTNING_SHOCK)
    {
        fU = 2 * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill == AT_SKILL_LIGHTNING_SHOCK_STR)
    {
        fU = 6 * width / 256.f;
        fV = 8 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill == AT_SKILL_EXPANSION_OF_WIZARDRY)
    {
        fU = 8 * width / 256.f;
        fV = 2 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (bySkillUseType == 4)
    {
        fU = (width / 256.f) * (Skill_Icon % 12);
        fV = (height / 256.f) * ((Skill_Icon / 12) + 4);
        iKindofSkill = KOS_SKILL2;
    }
    else if (skill >= AT_SKILL_KILLING_BLOW)
    {
        fU = ((skill - AT_SKILL_KILLING_BLOW) % 12) * width / 256.f;
        fV = ((skill - AT_SKILL_KILLING_BLOW) / 12) * height / 256.f;
        iKindofSkill = KOS_SKILL3;
    }
    else if (skill >= AT_SKILL_SPIRAL_SLASH)
    {
        fU = ((skill - AT_SKILL_SPIRAL_SLASH) % 8) * width / 256.f;
        fV = ((skill - AT_SKILL_SPIRAL_SLASH) / 8) * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else
    {
        fU = ((skill - 1) % 8) * width / 256.f;
        fV = ((skill - 1) / 8) * height / 256.f;
        iKindofSkill = KOS_SKILL1;
    }
    int iTextureIndex = 0;
    switch (iKindofSkill)
    {
    case KOS_COMMAND:
    {
        iTextureIndex = IMAGE_COMMAND;
    }break;
    case KOS_SKILL1:
    {
        iTextureIndex = IMAGE_SKILL1;
    }break;
    case KOS_SKILL2:
    {
        iTextureIndex = IMAGE_SKILL2;
    }break;
    case KOS_SKILL3:
    {
        iTextureIndex = IMAGE_SKILL3;
    }break;
    }

    if (skill >= AT_SKILL_MASTER_BEGIN)
    {
        RenderImage(BITMAP_INTERFACE_MASTER_BEGIN + 2, x, y, width, height, (20.f / 512.f) * (Skill_Icon % 25), ((28.f / 512.f) * ((Skill_Icon / 25))), 20.f / 512.f, 28.f / 512.f);
    }
    else if (iTextureIndex != 0)
    {
        RenderBitmap(iTextureIndex, x, y, width, height, fU, fV, width / 256.f, height / 256.f);
    }
}

CNewUIMuHelperSkillList::CNewUIMuHelperSkillList()
{
    m_pNewUIMng = NULL;
    Reset();
}

CNewUIMuHelperSkillList::~CNewUIMuHelperSkillList()
{
    Release();
}


bool CNewUIMuHelperSkillList::Create(CNewUIManager* pNewUIMng, CNewUI3DRenderMng* pNewUI3DRenderMng)
{
    if (NULL == pNewUIMng)
        return false;

    m_pNewUIMng = pNewUIMng;
    m_pNewUIMng->AddUIObj(INTERFACE_MUHELPER_SKILL_LIST, this);

    m_pNewUI3DRenderMng = pNewUI3DRenderMng;

    LoadImages();

    Show(false);

    return true;
}

void CNewUIMuHelperSkillList::Release()
{
    if (m_pNewUI3DRenderMng)
    {
        m_pNewUI3DRenderMng->DeleteUI2DEffectObject(UI2DEffectCallback);
    }

    UnloadImages();

    if (m_pNewUIMng)
    {
        m_pNewUIMng->RemoveUIObj(this);
        m_pNewUIMng = NULL;
    }
}

void CNewUIMuHelperSkillList::Reset()
{
    m_bRenderSkillInfo = false;
    m_iRenderSkillInfoType = 0;
    m_iRenderSkillInfoPosX = 0;
    m_iRenderSkillInfoPosY = 0;

    m_EventState = EVENT_NONE;
}

void CNewUIMuHelperSkillList::LoadImages()
{
    LoadBitmap(L"Interface\\newui_skill.jpg", IMAGE_SKILL1, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_skill2.jpg", IMAGE_SKILL2, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_command.jpg", IMAGE_COMMAND, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_skillbox.jpg", IMAGE_SKILLBOX, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_skillbox2.jpg", IMAGE_SKILLBOX_USE, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_non_skill.jpg", IMAGE_NON_SKILL1, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_non_skill2.jpg", IMAGE_NON_SKILL2, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_non_command.jpg", IMAGE_NON_COMMAND, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_skill3.jpg", IMAGE_SKILL3, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_non_skill3.jpg", IMAGE_NON_SKILL3, GL_LINEAR);
}

void CNewUIMuHelperSkillList::UnloadImages()
{
    DeleteBitmap(IMAGE_SKILL1);
    DeleteBitmap(IMAGE_SKILL2);
    DeleteBitmap(IMAGE_COMMAND);
    DeleteBitmap(IMAGE_SKILLBOX);
    DeleteBitmap(IMAGE_SKILLBOX_USE);
    DeleteBitmap(IMAGE_NON_SKILL1);
    DeleteBitmap(IMAGE_NON_SKILL2);
    DeleteBitmap(IMAGE_NON_COMMAND);
    DeleteBitmap(IMAGE_SKILL3);
    DeleteBitmap(IMAGE_NON_SKILL3);
}

bool CNewUIMuHelperSkillList::UpdateMouseEvent()
{
    if (IsRelease(VK_LBUTTON))
    {
        int skillId = UpdateMouseSkillList();
        if (skillId != -1)
        {
            g_ConsoleDebug->Write(MCD_NORMAL, L"[MU Helper] Clicked skill [%d]", skillId);
            g_pNewUIMuHelper->AssignSkill(skillId);
            Show(false);
            return false;
        }

        return true;
    }

    return false;
}

bool CNewUIMuHelperSkillList::UpdateKeyEvent()
{
    if (IsVisible())
    {
        if (IsPress(VK_ESCAPE) == true)
        {
            g_pNewUISystem->Hide(INTERFACE_MUHELPER_SKILL_LIST);
            SetFocus(g_hWnd);
            //PlayBuffer(SOUND_CLICK01);

            return false;
        }
    }
    return true;
}

void CNewUIMuHelperSkillList::PrepareSkillsToRender()
{
    m_aiSkillsToRender.clear();
    m_aiSkillSlots.clear();
    m_skillIconMap.clear();

    BYTE skillNumber = CharacterAttribute->SkillNumber;
    if (skillNumber > 0)
    {
        for (int i = 0; i < MAX_MAGIC; ++i)
        {
            int iSkillType = CharacterAttribute->Skill[i];

            if (iSkillType != 0 && (iSkillType < AT_SKILL_STUN || iSkillType > AT_SKILL_REMOVAL_BUFF))
            {
                if ((m_bFilterByAttackSkills && IsAttackSkill(iSkillType))
                    || (m_bFilterByBuffSkills && IsBuffSkill(iSkillType)))
                {
                    m_aiSkillsToRender.push_back(iSkillType);
                    m_aiSkillSlots.push_back(i);
                }
            }
        }
    }
}

bool CNewUIMuHelperSkillList::Update()
{
    m_bRenderSkillInfo = false;

    for (const auto& [id, icon] : m_skillIconMap)
    {
        if (!icon.isVisible)
        {
            continue;
        }

        if (CheckMouseIn(icon.location.x, icon.location.y, icon.area.cx, icon.area.cy))
        {
            m_bRenderSkillInfo = true;
            m_iRenderSkillInfoType = icon.slotIndex; // -1 for basic attack, slot index otherwise
            m_iRenderSkillInfoPosX = icon.location.x;
            m_iRenderSkillInfoPosY = icon.location.y;
            break;
        }
    }

    return true;
}

bool CNewUIMuHelperSkillList::Render()
{
    float scale = 1.0f; // 
    float boxWidth = 32.f * scale;
    float boxHeight = 38.f * scale;
    float iconWidth = 20.f * scale;
    float iconHeight = 28.f * scale;
    float iconOffsetX = (boxWidth - iconWidth) / 2.f;
    float iconOffsetY = (boxHeight - iconHeight) / 2.f;

    // x position relative to the position of mu helper window
    float startX = (float)REFERENCE_WIDTH - 190.f - 32.f;
    // y position relative to the skill/buff selection in mu helper window
    float startY = m_bFilterByAttackSkills ? 171.f : 293.f;

    int itemsPerColumn = m_bFilterByAttackSkills ? 10 : 5;

    for (int i = 0; i < m_aiSkillsToRender.size(); i++)
    {
        int iSkillType = m_aiSkillsToRender[i];

        int col = i / itemsPerColumn;
        int rowInColumn = i % itemsPerColumn;

        int offset = (rowInColumn + 1) / 2;
        bool skillCountEven = (rowInColumn % 2 == 0);

        float x = startX - col * boxWidth; // left to right
        float y = skillCountEven           // bounce up and down from center
            ? startY - offset * boxHeight
            : startY + offset * boxHeight;

        RenderImage(IMAGE_SKILLBOX, x, y, boxWidth, boxHeight);

        RenderSkillIcon(iSkillType, x + iconOffsetX, y + iconOffsetY, iconWidth, iconHeight);

        const int iSlotIndex = (i < static_cast<int>(m_aiSkillSlots.size())) ? m_aiSkillSlots[i] : -1;
        m_skillIconMap.insert_or_assign(iSkillType, cSkillIcon{
            iSkillType,
            iSlotIndex,
            { static_cast<LONG>(x), static_cast<LONG>(y) },
            { static_cast<LONG>(boxWidth), static_cast<LONG>(boxHeight) },
            true
            });
    }

    if (m_bRenderSkillInfo && m_pNewUI3DRenderMng)
    {
        m_pNewUI3DRenderMng->RenderUI2DEffect(INVENTORY_CAMERA_Z_ORDER, UI2DEffectCallback, this, 0, 0);
        m_bRenderSkillInfo = false;
    }

    return true;
}

void CNewUIMuHelperSkillList::RenderSkillInfo()
{
    if (m_iRenderSkillInfoType < 0)
    {
        // Basic Attack has no SkillAttribute slot — render a simple name tooltip
        int tx = m_iRenderSkillInfoPosX + 15;
        int ty = m_iRenderSkillInfoPosY - 10;
        g_pRenderText->SetFont(g_hFont);
        g_pRenderText->SetBgColor(0, 0, 0, 180);
        g_pRenderText->SetTextColor(255, 220, 100, 255);
        g_pRenderText->RenderText(tx, ty, L"Basic Attack", 100, 14, RT3_SORT_LEFT);
        return;
    }
    UI::Skills::Tooltip::Render(m_iRenderSkillInfoPosX + 15, m_iRenderSkillInfoPosY - 10, m_iRenderSkillInfoType);
}

float CNewUIMuHelperSkillList::GetLayerDepth()
{
    return 5.2f;
}

void CNewUIMuHelperSkillList::RenderSkillIcon(int iSkillType, float x, float y, float width, float height)
{
    float fU, fV;
    int iKindofSkill = 0;

    BYTE bySkillUseType = SkillAttribute[iSkillType].SkillUseType;
    int Skill_Icon = SkillAttribute[iSkillType].Magic_Icon;

    if (iSkillType >= AT_PET_COMMAND_DEFAULT && iSkillType <= AT_PET_COMMAND_END)
    {
        fU = ((iSkillType - AT_PET_COMMAND_DEFAULT) % 8) * width / 256.f;
        fV = ((iSkillType - AT_PET_COMMAND_DEFAULT) / 8) * height / 256.f;
        iKindofSkill = KOS_COMMAND;
    }
    else if (iSkillType == AT_SKILL_PLASMA_STORM_FENRIR)
    {
        fU = 4 * width / 256.f;
        fV = 0.f;
        iKindofSkill = KOS_COMMAND;
    }
    else if ((iSkillType >= AT_SKILL_ALICE_DRAINLIFE && iSkillType <= AT_SKILL_ALICE_THORNS))
    {
        fU = ((iSkillType - AT_SKILL_ALICE_DRAINLIFE) % 8) * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType >= AT_SKILL_ALICE_SLEEP && iSkillType <= AT_SKILL_ALICE_BLIND)
    {
        fU = ((iSkillType - AT_SKILL_ALICE_SLEEP + 4) % 8) * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType == AT_SKILL_ALICE_SLEEP_STR)
    {
        fU = (4 % 8) * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType == AT_SKILL_ALICE_BERSERKER
        || iSkillType == AT_SKILL_ALICE_BERSERKER_STR
        || iSkillType == AT_SKILL_BerserkerProficiency)
    {
        fU = 10 * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType >= AT_SKILL_ALICE_WEAKNESS && iSkillType <= AT_SKILL_ALICE_ENERVATION)
    {
        fU = (iSkillType - AT_SKILL_ALICE_WEAKNESS + 8) * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType >= AT_SKILL_SUMMON_EXPLOSION && iSkillType <= AT_SKILL_SUMMON_REQUIEM)
    {
        fU = ((iSkillType - AT_SKILL_SUMMON_EXPLOSION + 6) % 8) * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType == AT_SKILL_SUMMON_POLLUTION)
    {
        fU = 11 * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType == AT_SKILL_STRIKE_OF_DESTRUCTION)
    {
        fU = 7 * width / 256.f;
        fV = 2 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType == AT_SKILL_CHAOTIC_DISEIER)
    {
        fU = 3 * width / 256.f;
        fV = 8 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType == AT_SKILL_RECOVER)
    {
        fU = 9 * width / 256.f;
        fV = 2 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType == AT_SKILL_MULTI_SHOT)
    {
        fU = 0 * width / 256.f;
        fV = 8 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType == AT_SKILL_FLAME_STRIKE)
    {
        int iTypeL = CharacterMachine->Equipment[EQUIPMENT_WEAPON_LEFT].Type;
        int iTypeR = CharacterMachine->Equipment[EQUIPMENT_WEAPON_RIGHT].Type;

        fU = 1 * width / 256.f;
        fV = 8 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType == AT_SKILL_GIGANTIC_STORM)
    {
        fU = 2 * width / 256.f;
        fV = 8 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType == AT_SKILL_LIGHTNING_SHOCK)
    {
        fU = 2 * width / 256.f;
        fV = 3 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType == AT_SKILL_EXPANSION_OF_WIZARDRY)
    {
        fU = 8 * width / 256.f;
        fV = 2 * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else if (bySkillUseType == 4)
    {
        fU = (width / 256.f) * (Skill_Icon % 12);
        fV = (height / 256.f) * ((Skill_Icon / 12) + 4);
        iKindofSkill = KOS_SKILL2;
    }
    else if (iSkillType >= AT_SKILL_KILLING_BLOW)
    {
        fU = ((iSkillType - AT_SKILL_KILLING_BLOW) % 12) * width / 256.f;
        fV = ((iSkillType - AT_SKILL_KILLING_BLOW) / 12) * height / 256.f;
        iKindofSkill = KOS_SKILL3;
    }
    else if (iSkillType >= AT_SKILL_SPIRAL_SLASH)
    {
        fU = ((iSkillType - AT_SKILL_SPIRAL_SLASH) % 8) * width / 256.f;
        fV = ((iSkillType - AT_SKILL_SPIRAL_SLASH) / 8) * height / 256.f;
        iKindofSkill = KOS_SKILL2;
    }
    else
    {
        fU = ((iSkillType - 1) % 8) * width / 256.f;
        fV = ((iSkillType - 1) / 8) * height / 256.f;
        iKindofSkill = KOS_SKILL1;
    }
    int iTextureId = 0;
    switch (iKindofSkill)
    {
    case KOS_COMMAND:
    {
        iTextureId = IMAGE_COMMAND;
    }break;
    case KOS_SKILL1:
    {
        iTextureId = IMAGE_SKILL1;
    }break;
    case KOS_SKILL2:
    {
        iTextureId = IMAGE_SKILL2;
    }break;
    case KOS_SKILL3:
    {
        iTextureId = IMAGE_SKILL3;
    }break;
    }

    if (iSkillType >= AT_SKILL_MASTER_BEGIN)
    {
        RenderImage(BITMAP_INTERFACE_MASTER_BEGIN + 2, x, y, width, height, (20.f / 512.f) * (Skill_Icon % 25), ((28.f / 512.f) * ((Skill_Icon / 25))), 20.f / 512.f, 28.f / 512.f);
    }
    else if (iTextureId != 0)
    {
        RenderBitmap(iTextureId, x, y, width, height, fU, fV, width / 256.f, height / 256.f);
    }
}

bool CNewUIMuHelperSkillList::IsAttackSkill(int iSkillType)
{
    if (IsBuffSkill(iSkillType))
    {
        return false;
    }

    if (IsDefenseSkill(iSkillType))
    {
        return false;
    }

    if (IsHealingSkill(iSkillType))
    {
        return false;
    }

    return true;
}

bool CNewUIMuHelperSkillList::IsBuffSkill(int iSkillType)
{
    // To-do: Complete list of buffs

    switch (iSkillType)
    {
    // BK buffs
    case AT_SKILL_SWELL_LIFE:
    case AT_SKILL_SWELL_LIFE_STR:
    case AT_SKILL_SWELL_LIFE_PROFICIENCY:
        return true;
    // Elf buffs
    case AT_SKILL_INFINITY_ARROW:
    case AT_SKILL_INFINITY_ARROW_STR:
    case AT_SKILL_DEFENSE:
    case AT_SKILL_DEFENSE_STR:
    case AT_SKILL_DEFENSE_MASTERY:
    case AT_SKILL_ATTACK:
    case AT_SKILL_ATTACK_STR:
    case AT_SKILL_ATTACK_MASTERY:
        return true;
    // Wiz buffs
    case AT_SKILL_SOUL_BARRIER:
    case AT_SKILL_SOUL_BARRIER_STR:
    case AT_SKILL_SOUL_BARRIER_PROFICIENCY:
    case AT_SKILL_EXPANSION_OF_WIZARDRY:
    case AT_SKILL_EXPANSION_OF_WIZARDRY_STR:
    case AT_SKILL_EXPANSION_OF_WIZARDRY_MASTERY:
        return true;
    // DL buffs
    case AT_SKILL_ADD_CRITICAL:
    case AT_SKILL_ADD_CRITICAL_STR1:
    case AT_SKILL_ADD_CRITICAL_STR2:
    case AT_SKILL_ADD_CRITICAL_STR3:
        return true;
    // Summoner buffs
    case AT_SKILL_ALICE_BERSERKER:
    case AT_SKILL_ALICE_BERSERKER_STR:
    case AT_SKILL_BerserkerProficiency:
    case AT_SKILL_ALICE_THORNS:
        return true;
        // RF Buffs
    case AT_SKILL_ATT_UP_OURFORCES:
    case AT_SKILL_HP_UP_OURFORCES:
    case AT_SKILL_DEF_UP_OURFORCES:
    case AT_SKILL_HP_UP_OURFORCES_STR:
    case AT_SKILL_DEF_UP_OURFORCES_MASTERY:
    case AT_SKILL_DEF_UP_OURFORCES_STR:
        return true;
    }

    return false;
}

bool CNewUIMuHelperSkillList::IsHealingSkill(int iSkillType)
{
    // To-do: Complete list of healing skills

    switch (iSkillType)
    {
    case AT_SKILL_HEALING:
    case AT_SKILL_HEALING_STR:
        return true;
    }

    return false;
}

bool CNewUIMuHelperSkillList::IsDefenseSkill(int iSkillType)
{
    switch (iSkillType)
    {
    case AT_SKILL_DEFENSE:
    case AT_SKILL_DEFENSE_STR:
        return true;
    }

    return false;
}

void CNewUIMuHelperSkillList::FilterByAttackSkills()
{
    m_bFilterByAttackSkills = true;
    m_bFilterByBuffSkills = false;

    PrepareSkillsToRender();
}

void CNewUIMuHelperSkillList::FilterByBuffSkills()
{
    m_bFilterByBuffSkills = true;
    m_bFilterByAttackSkills = false;

    PrepareSkillsToRender();
}

void CNewUIMuHelperSkillList::UI2DEffectCallback(LPVOID pClass, DWORD dwParamA, DWORD dwParamB)
{
    if (pClass)
    {
        auto* pSkillList = (CNewUIMuHelperSkillList*)(pClass);
        pSkillList->RenderSkillInfo();
    }
}

int CNewUIMuHelperSkillList::UpdateMouseSkillList()
{
    auto li = m_skillIconMap.begin();

    for (; li != m_skillIconMap.end(); li++)
    {
        cSkillIcon* pIcon = &li->second;

        if (CheckMouseIn(pIcon->location.x, pIcon->location.y, pIcon->area.cx, pIcon->area.cy))
        {
            return li->first;
        }
    }

    return -1;
}

CNewUIMuHelperExt::CNewUIMuHelperExt()
{
    m_pNewUIMng = NULL;
    m_Pos.x = 0;
    m_Pos.y = 0;
    m_iCurrentPage = -1;
}

CNewUIMuHelperExt::~CNewUIMuHelperExt()
{
    Release();
}

bool CNewUIMuHelperExt::Create(CNewUIManager* pNewUIMng, int x, int y)
{
    if (NULL == pNewUIMng)
        return false;

    m_pNewUIMng = pNewUIMng;
    m_pNewUIMng->AddUIObj(INTERFACE_MUHELPER_EXT, this);

    SetPos(x, y);

    LoadImages();

    InitButtons();

    InitCheckBox();

    InitImage();

    InitText();

    Show(false);

    return true;
}

void CNewUIMuHelperExt::Release()
{
    UnloadImages();

    if (m_pNewUIMng)
    {
        m_pNewUIMng->RemoveUIObj(this);
        m_pNewUIMng = NULL;
    }
}

void CNewUIMuHelperExt::SetPos(int x, int y)
{
    m_Pos.x = x;
    m_Pos.y = y;
}

void CNewUIMuHelperExt::InitText()
{
    m_BuffTimeInput.Init(g_hWnd, 17, 15, MAX_NUMBER_DIGITS, false);
    m_BuffTimeInput.SetTextColor(255, 0, 0, 0);
    m_BuffTimeInput.SetBackColor(255, 255, 255, 255);
    m_BuffTimeInput.SetFont(g_hFont);
    m_BuffTimeInput.SetState(UISTATE_NORMAL);
    m_BuffTimeInput.SetOption(UIOPTION_NUMBERONLY);
}

void CNewUIMuHelperExt::InitImage()
{

}

void CNewUIMuHelperExt::InitButtons()
{
    m_BtnPreConHuntRange.CheckBoxImgState(IMAGE_MACROUI_HELPER_OPTIONBUTTON);
    m_BtnPreConHuntRange.CheckBoxInfo(m_Pos.x + 17, m_Pos.y + 78, 15, 15);
    m_BtnPreConHuntRange.ChangeText(GlobalText[3555]); // "Monster Within Hunting range"

    m_BtnPreConAttacking.CheckBoxImgState(IMAGE_MACROUI_HELPER_OPTIONBUTTON);
    m_BtnPreConAttacking.CheckBoxInfo(m_Pos.x + 17, m_Pos.y + 93, 15, 15);
    m_BtnPreConAttacking.ChangeText(GlobalText[3556]); // "Monster Attacking Me"

    m_BtnSubConMoreThanTwo.CheckBoxImgState(IMAGE_MACROUI_HELPER_OPTIONBUTTON);
    m_BtnSubConMoreThanTwo.CheckBoxInfo(m_Pos.x + 17, m_Pos.y + 143, 15, 15);
    m_BtnSubConMoreThanTwo.ChangeText(GlobalText[3557]); // "More Than 2 Mobs"

    m_BtnSubConMoreThanThree.CheckBoxImgState(IMAGE_MACROUI_HELPER_OPTIONBUTTON);
    m_BtnSubConMoreThanThree.CheckBoxInfo(m_Pos.x + 17, m_Pos.y + 158, 15, 15);
    m_BtnSubConMoreThanThree.ChangeText(GlobalText[3558]); // "More Than 3 Mobs"

    m_BtnSubConMoreThanFour.CheckBoxImgState(IMAGE_MACROUI_HELPER_OPTIONBUTTON);
    m_BtnSubConMoreThanFour.CheckBoxInfo(m_Pos.x + 17 + 78, m_Pos.y + 143, 15, 15);
    m_BtnSubConMoreThanFour.ChangeText(GlobalText[3559]); // "More Than 4 Mobs"

    m_BtnSubConMoreThanFive.CheckBoxImgState(IMAGE_MACROUI_HELPER_OPTIONBUTTON);
    m_BtnSubConMoreThanFive.CheckBoxInfo(m_Pos.x + 17 + 78, m_Pos.y + 158, 15, 15);
    m_BtnSubConMoreThanFive.ChangeText(GlobalText[3560]); // "More Than 5 Mobs"

    m_BtnPartyHeal.CheckBoxImgState(IMAGE_OPTION_BTN_CHECK);
    m_BtnPartyHeal.CheckBoxInfo(m_Pos.x + 17, m_Pos.y + 78, 15, 15);
    m_BtnPartyHeal.ChangeText(GlobalText[3539]); // "Preference of Party Heal"

    m_BtnPartyDuration.CheckBoxImgState(IMAGE_OPTION_BTN_CHECK);
    m_BtnPartyDuration.CheckBoxInfo(m_Pos.x + 17, m_Pos.y + 168, 15, 15);
    m_BtnPartyDuration.ChangeText(GlobalText[3540]); // "Buff Duration for All Party Members"

    m_BtnSave.ChangeButtonImgState(1, IMAGE_IGS_BUTTON, 1, 0, 1);
    m_BtnSave.ChangeButtonInfo(m_Pos.x + 120, m_Pos.y + 388, 52, 26);
    m_BtnSave.ChangeText(GlobalText[3503]); // "Save Setting"
    m_BtnSave.MoveTextPos(0, -1);
    m_BtnSave.ChangeToolTipText(L"", TRUE);

    m_BtnReset.ChangeButtonImgState(1, IMAGE_IGS_BUTTON, 1, 0, 1);
    m_BtnReset.ChangeButtonInfo(m_Pos.x + 65, m_Pos.y + 388, 52, 26);
    m_BtnReset.ChangeText(GlobalText[3504]); // "Initialization"
    m_BtnReset.MoveTextPos(0, -1);
    m_BtnReset.ChangeToolTipText(L"", TRUE);

    m_BtnClose.ChangeButtonImgState(1, IMAGE_BASE_WINDOW_BTN_EXIT, 0, 0, 0);
    m_BtnClose.ChangeButtonInfo(m_Pos.x + 20, m_Pos.y + 388, 36, 29);
    m_BtnClose.ChangeText(L"");
    m_BtnClose.ChangeToolTipText(GlobalText[388], TRUE); // "Close"
}

void CNewUIMuHelperExt::InitCheckBox()
{

}

bool CNewUIMuHelperExt::Render()
{
    EnableAlphaTest();
    glColor4f(1.f, 1.f, 1.f, 1.f);

    DWORD TextColor = g_pRenderText->GetTextColor();

    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->SetTextColor(0xFFFFFFFF);
    g_pRenderText->SetBgColor(0);

    RenderImage(IMAGE_BASE_WINDOW_BACK, m_Pos.x, m_Pos.y, float(WINDOW_WIDTH), float(WINDOW_HEIGHT));
    RenderImage(IMAGE_BASE_WINDOW_TOP, m_Pos.x, m_Pos.y, float(WINDOW_WIDTH), 64.f);
    RenderImage(IMAGE_BASE_WINDOW_LEFT, m_Pos.x, m_Pos.y + 64.f, 21.f, float(WINDOW_HEIGHT) - 64.f - 45.f);
    RenderImage(IMAGE_BASE_WINDOW_RIGHT, m_Pos.x + float(WINDOW_WIDTH) - 21.f, m_Pos.y + 64.f, 21.f, float(WINDOW_HEIGHT) - 64.f - 45.f);
    RenderImage(IMAGE_BASE_WINDOW_BOTTOM, m_Pos.x, m_Pos.y + float(WINDOW_HEIGHT) - 45.f, float(WINDOW_WIDTH), 45.f);

    g_pRenderText->SetFont(g_hFontBold);

    if (m_iCurrentPage == SUB_PAGE_POTION_CONFIG_ELF)
    {
        g_pRenderText->RenderText(m_Pos.x, m_Pos.y + 13, GlobalText[3553], 190, 0, RT3_SORT_CENTER); // "Auto Recovery"
        RenderBackPane(m_Pos.x + 12, m_Pos.y + 55, 165, 45, GlobalText[3545]); // "Auto Potion"
        RenderHpLevel(m_Pos.x + 32, m_Pos.y + 80, 124.f, 16.f, m_iCurrentPotionThreshold, GlobalText[3547]); // "HP Status"

        RenderBackPane(m_Pos.x + 12, m_Pos.y + 120, 165, 45, GlobalText[3546]); // "Auto Heal"
        RenderHpLevel(m_Pos.x + 32, m_Pos.y + 145, 124.f, 16.f, m_iCurrentHealThreshold, GlobalText[3547]); // "HP Status"
    }
    else if (m_iCurrentPage == SUB_PAGE_POTION_CONFIG_SUMMY)
    {
        g_pRenderText->RenderText(m_Pos.x, m_Pos.y + 13, GlobalText[3553], 190, 0, RT3_SORT_CENTER); // "Auto Recovery"
        RenderBackPane(m_Pos.x + 12, m_Pos.y + 55, 165, 45, GlobalText[3545]); // "Auto Potion"
        RenderHpLevel(m_Pos.x + 32, m_Pos.y + 80, 124.f, 16.f, m_iCurrentPotionThreshold, GlobalText[3547]); // "HP Status"

        RenderBackPane(m_Pos.x + 12, m_Pos.y + 120, 165, 45, GlobalText[3517]); // "Drain Life"
        RenderHpLevel(m_Pos.x + 32, m_Pos.y + 145, 124.f, 16.f, m_iCurrentHealThreshold, GlobalText[3547]); // "HP Status"
    }
    else if (m_iCurrentPage == SUB_PAGE_POTION_CONFIG)
    {
        g_pRenderText->RenderText(m_Pos.x, m_Pos.y + 13, GlobalText[3553], 190, 0, RT3_SORT_CENTER); // "Auto Recovery"
        RenderBackPane(m_Pos.x + 12, m_Pos.y + 55, 165, 45, GlobalText[3545]); // "Auto Potion"

        RenderHpLevel(m_Pos.x + 32, m_Pos.y + 80, 124.f, 16.f, m_iCurrentPotionThreshold, GlobalText[3547]);
    }
    else if (m_iCurrentPage == SUB_PAGE_SKILL2_CONFIG
        || m_iCurrentPage == SUB_PAGE_SKILL3_CONFIG)
    {
        g_pRenderText->RenderText(m_Pos.x, m_Pos.y + 13, GlobalText[3552], 190, 0, RT3_SORT_CENTER); // "Activation Skill"
        RenderBackPane(m_Pos.x + 12, m_Pos.y + 55, 165, 45, GlobalText[3543]);  // "Pre-con"
        m_BtnPreConHuntRange.Render();
        m_BtnPreConAttacking.Render();

        RenderBackPane(m_Pos.x + 12, m_Pos.y + 120, 165, 45, GlobalText[3544]); // "Sub-con"
        m_BtnSubConMoreThanTwo.Render();
        m_BtnSubConMoreThanThree.Render();
        m_BtnSubConMoreThanFour.Render();
        m_BtnSubConMoreThanFive.Render();
    }

    else if (m_iCurrentPage == SUB_PAGE_PARTY_CONFIG)
    {
        g_pRenderText->RenderText(m_Pos.x, m_Pos.y + 13, GlobalText[3554], 190, 0, RT3_SORT_CENTER); // "Party"
        g_pRenderText->SetTextColor(TextColor);
        RenderBackPane(m_Pos.x + 12, m_Pos.y + 55, 165, 45, GlobalText[3549]); // Buff Support
        m_BtnPartyDuration.Render();
        g_pRenderText->RenderText(m_Pos.x + 40, m_Pos.y + 97, GlobalText[3551], 124, 0, RT3_SORT_LEFT); // "Time Space of Casting Buff"
        RenderImage(IMAGE_MACROUI_HELPER_INPUTNUMBER, m_Pos.x + 125, m_Pos.y + 93, 20, 15);
        m_BuffTimeInput.Render();
        g_pRenderText->RenderText(m_Pos.x + 146, m_Pos.y + 97, L"s", 124, 0, RT3_SORT_LEFT); // "s"
    }
    else if (m_iCurrentPage == SUB_PAGE_PARTY_CONFIG_ELF)
    {
        g_pRenderText->RenderText(m_Pos.x, m_Pos.y + 13, GlobalText[3554], 190, 0, RT3_SORT_CENTER); // "Party"
        g_pRenderText->SetTextColor(TextColor);
        RenderBackPane(m_Pos.x + 12, m_Pos.y + 55, 165, 70, GlobalText[3548]); // Heal Support
        m_BtnPartyHeal.Render();
        RenderHpLevel(m_Pos.x + 32, m_Pos.y + 100, 124.f, 16.f, m_iCurrentPartyHealThreshold, GlobalText[3550]); // "HP Status of Party Members"

        RenderBackPane(m_Pos.x + 12, m_Pos.y + 145, 165, 45, GlobalText[3549]); // Buff Support
        m_BtnPartyDuration.Render();
        g_pRenderText->RenderText(m_Pos.x + 40, m_Pos.y + 187, GlobalText[3551], 124, 0, RT3_SORT_LEFT); // "Time Space of Casting Buff"
        RenderImage(IMAGE_MACROUI_HELPER_INPUTNUMBER, m_Pos.x + 125, m_Pos.y + 183, 20, 15);
        m_BuffTimeInput.Render();
        g_pRenderText->RenderText(m_Pos.x + 146, m_Pos.y + 187, L"s", 124, 0, RT3_SORT_LEFT); // "s"
    }

    m_BtnSave.Render();
    m_BtnReset.Render();
    m_BtnClose.Render();

    DisableAlphaBlend();

    return true;
}

void CNewUIMuHelperExt::RenderHpLevel(int x, int y, int width, int height, int level, const wchar_t* pszLabel)
{
    RenderImage(IMAGE_OPTION_VOLUME_BACK, x, y, 124.f, 16.f);
    if (level > 0)
    {
        RenderImage(IMAGE_OPTION_VOLUME_COLOR, x, y, 124.f * 0.1f * (level), 16.f);
    }
    g_pRenderText->RenderText(x, y + 18, pszLabel, width, 0, RT3_SORT_CENTER);
}

void CNewUIMuHelperExt::RenderBackPane(int x, int y, int width, int height, const wchar_t* pszHeader)
{
    DWORD TextColor = g_pRenderText->GetTextColor();
    int headerWidth = 65;

    EnableAlphaTest();
    glColor4f(0.0, 0.0, 0.0, 0.4f);
    RenderColor(x + 3.f, y + 2.f, headerWidth - 7.f, 18.f, 0.0, 0);  // shade for top box
    RenderColor(x + 3.f, y + 2.f + 18.f, width - 7.f, height - 7.f, 0.0, 0);  // shade for bottom box
    EndRenderColor();

    // Top box (tab) without bottom line
    RenderImage(IMAGE_TABLE_TOP_LEFT, x, y, 14.0, 14.0);                                // Top-left corner of the tab
    RenderImage(IMAGE_TABLE_TOP_RIGHT, (x + headerWidth) - 14.f, y, 14.0, 14.0);        // Top-right corner of the tab
    RenderImage(IMAGE_TABLE_TOP_PIXEL, x + 6.f, y, (headerWidth - 12.f), 14.0);         // Top edge of the tab
    RenderImage(IMAGE_TABLE_RIGHT_PIXEL, (x + headerWidth) - 14.f, y + 6.f, 14.0, 14.0); // Right edge of the tab

    // Bottom box without top line
    RenderImage(IMAGE_TABLE_TOP_RIGHT, (x + width) - 14.f, y + 18.f, 14.0, 14.0);       // Main box top-right corner
    RenderImage(IMAGE_TABLE_BOTTOM_LEFT, x, (y + height + 18.f) - 14.f, 14.0, 14.0);    // Main box bottom-left corner
    RenderImage(IMAGE_TABLE_BOTTOM_RIGHT, (x + width) - 14.f, (y + height + 18.f) - 14.f, 14.0, 14.0); // Main box bottom-right corner
    RenderImage(IMAGE_TABLE_TOP_PIXEL, x + 2.f, y + 18.f, (width - 12.f), 14.0);        // Top edge of main box
    RenderImage(IMAGE_TABLE_RIGHT_PIXEL, (x + width) - 14.f, y + 24.f, 14.0, (height - 14.f)); // Right edge of main box
    RenderImage(IMAGE_TABLE_BOTTOM_PIXEL, x + 6.f, (y + height + 18.f) - 14.f, (width - 12.f), 14.0); // Bottom edge of main box

    // Left line to connect top box and bottom box
    RenderImage(IMAGE_TABLE_LEFT_PIXEL, x, y + 6.f, 14.0, (height));             // Connecting left edge

    // Header inside top box
    g_pRenderText->SetTextColor(TextColor);
    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->RenderText(x + 10.f, y + 6.f, pszHeader, headerWidth, 0, RT3_SORT_LEFT);
}

void CNewUIMuHelperExt::LoadImages()
{
    LoadBitmap(L"Interface\\MacroUI\\MacroUI_OptionButton.tga", IMAGE_MACROUI_HELPER_OPTIONBUTTON, GL_LINEAR, GL_CLAMP, 1, 0);
    LoadBitmap(L"Interface\\MacroUI\\MacroUI_InputNumber.tga", IMAGE_MACROUI_HELPER_INPUTNUMBER, GL_LINEAR, GL_CLAMP, 1, 0);
    LoadBitmap(L"Interface\\MacroUI\\MacroUI_InputString.tga", IMAGE_MACROUI_HELPER_INPUTSTRING, GL_LINEAR, GL_CLAMP, 1, 0);
    //--
    LoadBitmap(L"Interface\\InGameShop\\Ingame_Bt03.tga", IMAGE_IGS_BUTTON, GL_LINEAR, GL_CLAMP, 1, 0);
}

void CNewUIMuHelperExt::UnloadImages()
{
    DeleteBitmap(IMAGE_MACROUI_HELPER_OPTIONBUTTON);
    DeleteBitmap(IMAGE_MACROUI_HELPER_INPUTNUMBER);
    DeleteBitmap(IMAGE_MACROUI_HELPER_INPUTSTRING);
    //--
    DeleteBitmap(IMAGE_IGS_BUTTON);
}

bool CNewUIMuHelperExt::Update()
{
    if (IsVisible())
    {
        if (m_iCurrentPage == SUB_PAGE_SKILL2_CONFIG || m_iCurrentPage == SUB_PAGE_SKILL3_CONFIG)
        {
            int iSkillIndex = m_iCurrentPage == SUB_PAGE_SKILL2_CONFIG ? 1 : 2; // SKill 2 : Skill 3

            if (m_BtnPreConHuntRange.UpdateMouseEvent())
            {
                m_BtnPreConHuntRange.RegisterBoxState(true);
                m_BtnPreConAttacking.RegisterBoxState(!m_BtnPreConHuntRange.GetBoxState());

                // Clear other precondition bits and set the bit for "Hunt Range"
                _TempConfig.aiSkillCondition[iSkillIndex] =
                    (_TempConfig.aiSkillCondition[iSkillIndex] & MUHELPER_SKILL_PRECON_CLEAR) |
                    ON_MOBS_NEARBY;
            }
            else if (m_BtnPreConAttacking.UpdateMouseEvent())
            {
                m_BtnPreConAttacking.RegisterBoxState(true);
                m_BtnPreConHuntRange.RegisterBoxState(!m_BtnPreConAttacking.GetBoxState());

                // Clear other precondition bits and set the bit for "Attacking"
                _TempConfig.aiSkillCondition[iSkillIndex] =
                    (_TempConfig.aiSkillCondition[iSkillIndex] & MUHELPER_SKILL_PRECON_CLEAR) |
                    ON_MOBS_ATTACKING;
            }
            else if (m_BtnSubConMoreThanTwo.UpdateMouseEvent())
            {
                m_BtnSubConMoreThanTwo.RegisterBoxState(true);
                m_BtnSubConMoreThanThree.RegisterBoxState(false);
                m_BtnSubConMoreThanFour.RegisterBoxState(false);
                m_BtnSubConMoreThanFive.RegisterBoxState(false);

                // Clear other bits and set the bit for "More Than Two Mobs"
                _TempConfig.aiSkillCondition[iSkillIndex] =
                    (_TempConfig.aiSkillCondition[iSkillIndex] & MUHELPER_SKILL_SUBCON_CLEAR) |
                    ON_MORE_THAN_TWO_MOBS;
            }
            else if (m_BtnSubConMoreThanThree.UpdateMouseEvent())
            {
                m_BtnSubConMoreThanTwo.RegisterBoxState(false);
                m_BtnSubConMoreThanThree.RegisterBoxState(true);
                m_BtnSubConMoreThanFour.RegisterBoxState(false);
                m_BtnSubConMoreThanFive.RegisterBoxState(false);

                // Clear other bits and set the bit for "More Than Three Mobs"
                _TempConfig.aiSkillCondition[iSkillIndex] =
                    (_TempConfig.aiSkillCondition[iSkillIndex] & MUHELPER_SKILL_SUBCON_CLEAR) |
                    ON_MORE_THAN_THREE_MOBS;
            }
            else if (m_BtnSubConMoreThanFour.UpdateMouseEvent())
            {
                m_BtnSubConMoreThanTwo.RegisterBoxState(false);
                m_BtnSubConMoreThanThree.RegisterBoxState(false);
                m_BtnSubConMoreThanFour.RegisterBoxState(true);
                m_BtnSubConMoreThanFive.RegisterBoxState(false);

                // Clear other bits and set the bit for "More Than Four Mobs"
                _TempConfig.aiSkillCondition[iSkillIndex] =
                    (_TempConfig.aiSkillCondition[iSkillIndex] & MUHELPER_SKILL_SUBCON_CLEAR) |
                    ON_MORE_THAN_FOUR_MOBS;
            }
            else if (m_BtnSubConMoreThanFive.UpdateMouseEvent())
            {
                m_BtnSubConMoreThanTwo.RegisterBoxState(false);
                m_BtnSubConMoreThanThree.RegisterBoxState(false);
                m_BtnSubConMoreThanFour.RegisterBoxState(false);
                m_BtnSubConMoreThanFive.RegisterBoxState(true);

                // Clear other bits and set the bit for "More Than Five Mobs"
                _TempConfig.aiSkillCondition[iSkillIndex] =
                    (_TempConfig.aiSkillCondition[iSkillIndex] & MUHELPER_SKILL_SUBCON_CLEAR) |
                    ON_MORE_THAN_FIVE_MOBS;
            }
        }

        if (m_iCurrentPage == SUB_PAGE_PARTY_CONFIG_ELF)
        {
            if (m_BtnPartyHeal.UpdateMouseEvent())
            {
                _TempConfig.bAutoHealParty = m_BtnPartyHeal.GetBoxState();
            }
        }

        if (m_iCurrentPage == SUB_PAGE_PARTY_CONFIG || m_iCurrentPage == SUB_PAGE_PARTY_CONFIG_ELF)
        {
            if (m_BtnPartyDuration.UpdateMouseEvent())
            {
                _TempConfig.bBuffDurationParty = m_BtnPartyDuration.GetBoxState();
            }
        }

        if (m_BtnClose.UpdateMouseEvent())
        {
            g_pNewUISystem->Hide(INTERFACE_MUHELPER_EXT);
        }
        else if (m_BtnSave.UpdateMouseEvent())
        {
            Save();
            g_pNewUISystem->Hide(INTERFACE_MUHELPER_EXT);
        }
        else if (m_BtnReset.UpdateMouseEvent())
        {
            Reset();
        }
    }
    return true;
}

bool CNewUIMuHelperExt::UpdateMouseEvent()
{
    // Ignore events outside MU Helper window
    if (!CheckMouseIn(m_Pos.x, m_Pos.y, WINDOW_WIDTH, WINDOW_HEIGHT))
    {
        return true;
    }

    if (CheckMouseIn(m_Pos.x + 33 - 8, m_Pos.y + 80, 124 + 8, 16))
    {
        int iOldValue = m_iCurrentPotionThreshold;
        if (MouseWheel > 0)
        {
            MouseWheel = 0;
            m_iCurrentPotionThreshold++;
            if (m_iCurrentPotionThreshold > 10)
            {
                m_iCurrentPotionThreshold = 10;
            }
        }
        else if (MouseWheel < 0)
        {
            MouseWheel = 0;
            m_iCurrentPotionThreshold--;
            if (m_iCurrentPotionThreshold < 0)
            {
                m_iCurrentPotionThreshold = 0;
            }
        }
        if (IsRepeat(VK_LBUTTON))
        {
            int x = MouseX - (m_Pos.x + 33);
            if (x < 0)
            {
                m_iCurrentPotionThreshold = 0;
            }
            else
            {
                float fValue = (10.f * x) / 124.f;
                m_iCurrentPotionThreshold = (int)fValue + 1;
            }
        }
    }

    if (m_iCurrentPage == SUB_PAGE_POTION_CONFIG_ELF || m_iCurrentPage == SUB_PAGE_POTION_CONFIG_SUMMY)
    {
        if (CheckMouseIn(m_Pos.x + 33 - 8, m_Pos.y + 145, 124 + 8, 16))
        {
            int iOldValue = m_iCurrentHealThreshold;
            if (MouseWheel > 0)
            {
                MouseWheel = 0;
                m_iCurrentHealThreshold++;
                if (m_iCurrentHealThreshold > 10)
                {
                    m_iCurrentHealThreshold = 10;
                }
            }
            else if (MouseWheel < 0)
            {
                MouseWheel = 0;
                m_iCurrentHealThreshold--;
                if (m_iCurrentHealThreshold < 0)
                {
                    m_iCurrentHealThreshold = 0;
                }
            }
            if (IsRepeat(VK_LBUTTON))
            {
                int x = MouseX - (m_Pos.x + 33);
                if (x < 0)
                {
                    m_iCurrentHealThreshold = 0;
                }
                else
                {
                    float fValue = (10.f * x) / 124.f;
                    m_iCurrentHealThreshold = (int)fValue + 1;
                }
            }
        }
    }
    else if (m_iCurrentPage == SUB_PAGE_PARTY_CONFIG_ELF)
    {
        if (CheckMouseIn(m_Pos.x + 32 - 8, m_Pos.y + 100, 124 + 8, 16))
        {
            int iOldValue = m_iCurrentPartyHealThreshold;
            if (MouseWheel > 0)
            {
                MouseWheel = 0;
                m_iCurrentPartyHealThreshold++;
                if (m_iCurrentPartyHealThreshold > 10)
                {
                    m_iCurrentPartyHealThreshold = 10;
                }
            }
            else if (MouseWheel < 0)
            {
                MouseWheel = 0;
                m_iCurrentPartyHealThreshold--;
                if (m_iCurrentPartyHealThreshold < 0)
                {
                    m_iCurrentPartyHealThreshold = 0;
                }
            }
            if (IsRepeat(VK_LBUTTON))
            {
                int x = MouseX - (m_Pos.x + 33);
                if (x < 0)
                {
                    m_iCurrentPartyHealThreshold = 0;
                }
                else
                {
                    float fValue = (10.f * x) / 124.f;
                    m_iCurrentPartyHealThreshold = (int)fValue + 1;
                }
            }
        }
        else if (CheckMouseIn(m_BuffTimeInput.GetPosition_x(), m_BuffTimeInput.GetPosition_y(), 20, 15))
        {
            m_BuffTimeInput.GiveFocus();
        }
        else
        {
            SetFocus(g_hWnd);
        }
    }
    else if (m_iCurrentPage == SUB_PAGE_PARTY_CONFIG)
    {
        if (CheckMouseIn(m_BuffTimeInput.GetPosition_x(), m_BuffTimeInput.GetPosition_y(), 20, 15))
        {
            m_BuffTimeInput.GiveFocus();
        }
        else
        {
            SetFocus(g_hWnd);
        }
    }

    return false;
}

bool CNewUIMuHelperExt::UpdateKeyEvent()
{
    if (IsVisible())
    {
        if (IsPress(VK_ESCAPE) == true)
        {
            g_pNewUISystem->Hide(INTERFACE_MUHELPER_EXT);
            //PlayBuffer(SOUND_CLICK01);

            return false;
        }
    }
    return true;
}

float CNewUIMuHelperExt::GetLayerDepth()
{
    return 3.4;
}

float CNewUIMuHelperExt::GetKeyEventOrder()
{
    return 3.4;
}

void CNewUIMuHelperExt::Toggle(int iPageId)
{
    int iPrevPage = m_iCurrentPage;
    m_iCurrentPage = iPageId;

    if (IsVisible() && m_iCurrentPage == iPrevPage)
    {
        m_iCurrentPage = -1;
        this->Show(false);
        return;
    }

    if (m_iCurrentPage == SUB_PAGE_SKILL2_CONFIG || m_iCurrentPage == SUB_PAGE_SKILL3_CONFIG)
    {
        int iSkillIndex = m_iCurrentPage == SUB_PAGE_SKILL2_CONFIG ? 1 : 2;
        m_BtnPreConHuntRange.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MOBS_NEARBY);
        m_BtnPreConAttacking.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MOBS_ATTACKING);
        m_BtnSubConMoreThanTwo.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MORE_THAN_TWO_MOBS);
        m_BtnSubConMoreThanThree.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MORE_THAN_THREE_MOBS);
        m_BtnSubConMoreThanFour.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MORE_THAN_FOUR_MOBS);
        m_BtnSubConMoreThanFive.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MORE_THAN_FIVE_MOBS);
    }
    else if (m_iCurrentPage == SUB_PAGE_POTION_CONFIG || m_iCurrentPage == SUB_PAGE_POTION_CONFIG_ELF || m_iCurrentPage == SUB_PAGE_POTION_CONFIG_SUMMY)
    {
        m_iCurrentPotionThreshold = _TempConfig.iPotionThreshold / 10;
        m_iCurrentHealThreshold = _TempConfig.iHealThreshold / 10;
    }
    else if (m_iCurrentPage == SUB_PAGE_PARTY_CONFIG)
    {
        m_BtnPartyDuration.CheckBoxInfo(m_Pos.x + 17, m_Pos.y + 78, 15, 15);
        m_BtnPartyDuration.RegisterBoxState(_TempConfig.bBuffDurationParty);
        m_iCurrentPartyHealThreshold = _TempConfig.iHealPartyThreshold / 10;

        wchar_t wsBuffTime[MAX_NUMBER_DIGITS + 1] = { 0 };
        std::swprintf(wsBuffTime, MAX_NUMBER_DIGITS + 1, L"%d", _TempConfig.iBuffCastInterval);
        m_BuffTimeInput.SetText(wsBuffTime);
        m_BuffTimeInput.SetPosition(m_Pos.x + 127, m_Pos.y + 97);
    }
    else if (m_iCurrentPage == SUB_PAGE_PARTY_CONFIG_ELF)
    {
        m_BtnPartyHeal.RegisterBoxState(_TempConfig.bAutoHealParty);

        m_BtnPartyDuration.CheckBoxInfo(m_Pos.x + 17, m_Pos.y + 168, 15, 15);
        m_BtnPartyDuration.RegisterBoxState(_TempConfig.bBuffDurationParty);
        m_iCurrentPartyHealThreshold = _TempConfig.iHealPartyThreshold / 10;

        wchar_t wsBuffTime[MAX_NUMBER_DIGITS + 1] = { 0 };
        std::swprintf(wsBuffTime, MAX_NUMBER_DIGITS + 1, L"%d", _TempConfig.iBuffCastInterval);
        m_BuffTimeInput.SetText(wsBuffTime);
        m_BuffTimeInput.SetPosition(m_Pos.x + 127, m_Pos.y + 187);
    }

    this->Show(true);
}

void CNewUIMuHelperExt::Save()
{
    wchar_t wsNumberInput[MAX_NUMBER_DIGITS + 1]{};
    m_BuffTimeInput.GetText(wsNumberInput, sizeof(wsNumberInput));
    _TempConfig.iBuffCastInterval = CNewUIMuHelper::GetIntFromTextInput(wsNumberInput);
    _TempConfig.iPotionThreshold = m_iCurrentPotionThreshold * 10;
    _TempConfig.iHealThreshold = m_iCurrentHealThreshold * 10;
    _TempConfig.iHealPartyThreshold = m_iCurrentPartyHealThreshold * 10;
    MUHelper::g_MuHelper.Save(_TempConfig);
}

void CNewUIMuHelperExt::ApplySavedConfig()
{
    m_iCurrentPotionThreshold = _TempConfig.iPotionThreshold / 10;
    m_iCurrentHealThreshold = _TempConfig.iHealThreshold / 10;
    m_iCurrentPartyHealThreshold = _TempConfig.iHealPartyThreshold / 10;
}

// Called by the "Initialization" button from the main page
void CNewUIMuHelperExt::InitConfig()
{
    _TempConfig.iPotionThreshold = 40;
    _TempConfig.iHealThreshold = 60;
    _TempConfig.iBuffCastInterval = 0;
    _TempConfig.iHealPartyThreshold = 60;
    _TempConfig.bAutoHealParty = false;
    _TempConfig.bBuffDurationParty = false;
}

// Called by the "Initialization" button from the sub page
void CNewUIMuHelperExt::Reset()
{
    if (m_iCurrentPage == SUB_PAGE_SKILL2_CONFIG 
        || m_iCurrentPage == SUB_PAGE_SKILL3_CONFIG)
    {
        int iSkillIndex = m_iCurrentPage == SUB_PAGE_SKILL2_CONFIG ? 1 : 2;

        _TempConfig.aiSkillCondition[iSkillIndex] = static_cast<int>(ON_MOBS_NEARBY) | ON_MORE_THAN_TWO_MOBS;

        m_BtnPreConHuntRange.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MOBS_NEARBY);
        m_BtnPreConAttacking.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MOBS_ATTACKING);
        m_BtnSubConMoreThanTwo.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MORE_THAN_TWO_MOBS);
        m_BtnSubConMoreThanThree.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MORE_THAN_THREE_MOBS);
        m_BtnSubConMoreThanFour.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MORE_THAN_FOUR_MOBS);
        m_BtnSubConMoreThanFive.RegisterBoxState(_TempConfig.aiSkillCondition[iSkillIndex] & ON_MORE_THAN_FIVE_MOBS);
    }
    else if (m_iCurrentPage == SUB_PAGE_POTION_CONFIG 
        || m_iCurrentPage == SUB_PAGE_POTION_CONFIG_ELF 
        || m_iCurrentPage == SUB_PAGE_POTION_CONFIG_SUMMY)
    {
        _TempConfig.iPotionThreshold = 0;
        _TempConfig.iHealThreshold = 0;

        m_iCurrentPotionThreshold = _TempConfig.iPotionThreshold / 10;
        m_iCurrentHealThreshold = _TempConfig.iHealThreshold / 10;
    }
}

//////////////////////////////////////////////////////////////////////////
// BarnaMu: CNewUIJewelBank - per-account jewel bank window (MU Helper menu).
//////////////////////////////////////////////////////////////////////////

namespace
{
    struct JewelBankItemInfo
    {
        const wchar_t* Name;
        int Type;
        int Level;
    };

    constexpr int JEWEL_BANK_TABLE_X = 13;
    constexpr int JEWEL_BANK_TABLE_Y = 44;
    constexpr int JEWEL_BANK_HEADER_HEIGHT = 20;
    constexpr int JEWEL_BANK_TABLE_WIDTH = 594;
    constexpr int JEWEL_BANK_BUTTON_WIDTH = 18;
    constexpr int JEWEL_BANK_BUTTON_HEIGHT = 18;
    constexpr int JEWEL_BANK_ICON_SIZE = 18;

    constexpr int JEWEL_BANK_COLUMN_COUNT = 8;
    constexpr int s_JewelBankColumns[JEWEL_BANK_COLUMN_COUNT] =
    {
        0, 172, 242, 332, 399, 466, 532, 594,
    };

    const wchar_t* const s_JewelBankHeaders[JEWEL_BANK_COLUMN_COUNT - 1] =
    {
        L"Item",
        L"Amount",
        L"10 Pack Amount",
        L"Deposit x1",
        L"Deposit Pack",
        L"Withdraw x1",
        L"Withdraw Pack",
    };

    const JewelBankItemInfo s_JewelBankItems[CNewUIJewelBank::ITEM_COUNT] =
    {
        { L"Jewel of Bless", ITEM_PACKED_JEWEL_OF_BLESS, 0 },
        { L"Jewel of Soul", ITEM_PACKED_JEWEL_OF_SOUL, 0 },
        { L"Jewel of Life", ITEM_PACKED_JEWEL_OF_LIFE, 0 },
        { L"Jewel of Creation", ITEM_PACKED_JEWEL_OF_CREATION, 0 },
        { L"Jewel of Guardian", ITEM_PACKED_JEWEL_OF_GUARDIAN, 0 },
        { L"Gemstone", ITEM_PACKED_GEMSTONE, 0 },
        { L"Jewel of Harmony", ITEM_PACKED_JEWEL_OF_HARMONY, 0 },
        { L"Jewel of Chaos", ITEM_PACKED_JEWEL_OF_CHAOS, 0 },
        { L"Lower refine stone", ITEM_PACKED_LOWER_REFINE_STONE, 0 },
        { L"Higher refine stone", ITEM_PACKED_HIGHER_REFINE_STONE, 0 },
        { L"Box of Kundun +1", ITEM_BOX_OF_LUCK, 8 },
        { L"Box of Kundun +2", ITEM_BOX_OF_LUCK, 9 },
        { L"Box of Kundun +3", ITEM_BOX_OF_LUCK, 10 },
        { L"Box of Kundun +4", ITEM_BOX_OF_LUCK, 11 },
        { L"Box of Kundun +5", ITEM_BOX_OF_LUCK, 12 },
        { L"Blue Chocolate Box", ITEM_BLUE_CHOCOLATE_BOX, 0 },
        { L"Pink Chocolate Box", ITEM_PINK_CHOCOLATE_BOX, 0 },
    };

    void RenderJewelBankRect(int x, int y, int width, int height, float red, float green, float blue, float alpha)
    {
        DisableTexture();

        float sx = float(x) * float(WindowWidth) / float(REFERENCE_WIDTH);
        float sy = float(y) * float(WindowHeight) / float(REFERENCE_HEIGHT);
        float sw = float(width) * float(WindowWidth) / float(REFERENCE_WIDTH);
        float sh = float(height) * float(WindowHeight) / float(REFERENCE_HEIGHT);
        sy = float(WindowHeight) - sy;

        glColor4f(red, green, blue, alpha);
        glBegin(GL_TRIANGLE_FAN);
        glVertex2f(sx, sy);
        glVertex2f(sx, sy - sh);
        glVertex2f(sx + sw, sy - sh);
        glVertex2f(sx + sw, sy);
        glEnd();

        EndRenderColor();
    }

    void RenderJewelBankFrame(int x, int y, int width, int height)
    {
        RenderImage(CNewUIJewelBank::IMAGE_TABLE_TOP_LEFT, x, y, 14.f, 14.f);
        RenderImage(CNewUIJewelBank::IMAGE_TABLE_TOP_RIGHT, x + width - 14.f, y, 14.f, 14.f);
        RenderImage(CNewUIJewelBank::IMAGE_TABLE_BOTTOM_LEFT, x, y + height - 14.f, 14.f, 14.f);
        RenderImage(CNewUIJewelBank::IMAGE_TABLE_BOTTOM_RIGHT, x + width - 14.f, y + height - 14.f, 14.f, 14.f);
        RenderImage(CNewUIJewelBank::IMAGE_TABLE_TOP_PIXEL, x + 6.f, y, width - 12.f, 14.f);
        RenderImage(CNewUIJewelBank::IMAGE_TABLE_BOTTOM_PIXEL, x + 6.f, y + height - 14.f, width - 12.f, 14.f);
        RenderImage(CNewUIJewelBank::IMAGE_TABLE_LEFT_PIXEL, x, y + 6.f, 14.f, height - 12.f);
        RenderImage(CNewUIJewelBank::IMAGE_TABLE_RIGHT_PIXEL, x + width - 14.f, y + 6.f, 14.f, height - 12.f);
    }

    void RenderJewelBankLine(int x, int y, int width, int height, float red, float green, float blue, float alpha)
    {
        RenderJewelBankRect(x, y, width, height, red, green, blue, alpha);
    }

    void RenderJewelBankTexture(int x, int y, int width, int height)
    {
        for (int offsetY = 4; offsetY < height - 4; offsetY += 7)
        {
            const float alpha = ((offsetY / 7) % 2 == 0) ? 0.12f : 0.06f;
            RenderJewelBankRect(x + 3, y + offsetY, width - 6, 1, 0.22f, 0.23f, 0.24f, alpha);
        }

        for (int offsetX = 17; offsetX < width - 6; offsetX += 31)
        {
            RenderJewelBankRect(x + offsetX, y + 3, 1, height - 6, 0.00f, 0.00f, 0.00f, 0.10f);
        }
    }
}

CNewUIJewelBank::CNewUIJewelBank()
{
    m_pNewUIMng = NULL;
    m_pNewUI3DRenderMng = NULL;
    m_Pos.x = 0;
    m_Pos.y = 0;
    for (int i = 0; i < ITEM_COUNT; i++)
        m_Balances[i] = 0;
}

CNewUIJewelBank::~CNewUIJewelBank()
{
    Release();
}

bool CNewUIJewelBank::Create(CNewUIManager* pNewUIMng, CNewUI3DRenderMng* pNewUI3DRenderMng, int x, int y)
{
    if (NULL == pNewUIMng || NULL == pNewUI3DRenderMng)
        return false;

    m_pNewUIMng = pNewUIMng;
    m_pNewUIMng->AddUIObj(INTERFACE_JEWELBANK, this);

    m_pNewUI3DRenderMng = pNewUI3DRenderMng;
    m_pNewUI3DRenderMng->Add3DRenderObj(this, INFORMATION_CAMERA_Z_ORDER);

    SetPos((640 - WINDOW_WIDTH) / 2, 20);
    LoadImages();
    InitButtons();
    Show(false);

    return true;
}

void CNewUIJewelBank::Release()
{
    UnloadImages();

    if (m_pNewUI3DRenderMng)
    {
        m_pNewUI3DRenderMng->Remove3DRenderObj(this);
        m_pNewUI3DRenderMng = NULL;
    }

    if (m_pNewUIMng)
    {
        m_pNewUIMng->RemoveUIObj(this);
        m_pNewUIMng = NULL;
    }
}

void CNewUIJewelBank::SetPos(int x, int y)
{
    m_Pos.x = x;
    m_Pos.y = y;
}

void CNewUIJewelBank::LoadImages()
{
    LoadBitmap(L"Interface\\barna_jewelbank_back.jpg", IMAGE_JEWEL_BANK_BACK, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_box.tga", IMAGE_ITEM_BOX, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table01(L).tga", IMAGE_TABLE_TOP_LEFT, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table01(R).tga", IMAGE_TABLE_TOP_RIGHT, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table02(L).tga", IMAGE_TABLE_BOTTOM_LEFT, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table02(R).tga", IMAGE_TABLE_BOTTOM_RIGHT, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table03(Up).tga", IMAGE_TABLE_TOP_PIXEL, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table03(Dw).tga", IMAGE_TABLE_BOTTOM_PIXEL, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table03(L).tga", IMAGE_TABLE_LEFT_PIXEL, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table03(R).tga", IMAGE_TABLE_RIGHT_PIXEL, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_Btn_round.tga", IMAGE_ROUND_BUTTON, GL_LINEAR);
    LoadBitmap(L"Interface\\InGameShop\\Ingame_Bt03.tga", IMAGE_IGS_BUTTON, GL_LINEAR, GL_CLAMP, 1, 0);
}

void CNewUIJewelBank::UnloadImages()
{
    DeleteBitmap(IMAGE_JEWEL_BANK_BACK);
    DeleteBitmap(IMAGE_ROUND_BUTTON);
    DeleteBitmap(IMAGE_IGS_BUTTON);
    DeleteBitmap(IMAGE_TABLE_RIGHT_PIXEL);
    DeleteBitmap(IMAGE_TABLE_LEFT_PIXEL);
    DeleteBitmap(IMAGE_TABLE_BOTTOM_PIXEL);
    DeleteBitmap(IMAGE_TABLE_TOP_PIXEL);
    DeleteBitmap(IMAGE_TABLE_BOTTOM_RIGHT);
    DeleteBitmap(IMAGE_TABLE_BOTTOM_LEFT);
    DeleteBitmap(IMAGE_TABLE_TOP_RIGHT);
    DeleteBitmap(IMAGE_TABLE_TOP_LEFT);
    DeleteBitmap(IMAGE_ITEM_BOX);
}

void CNewUIJewelBank::InitButtons()
{
    const int tableX = m_Pos.x + JEWEL_BANK_TABLE_X;

    for (int i = 0; i < ITEM_COUNT; i++)
    {
        int rowY = m_Pos.y + ROW_START_Y + i * ROW_HEIGHT;
        int buttonY = rowY + 1;

        auto setupButton = [&](CNewUIButton& button, int column, const wchar_t* text, const wchar_t* tooltip, int textX)
        {
            const int columnX = tableX + s_JewelBankColumns[column];
            const int columnWidth = s_JewelBankColumns[column + 1] - s_JewelBankColumns[column];
            const int buttonX = columnX + ((columnWidth - JEWEL_BANK_BUTTON_WIDTH) / 2);

            button.ChangeButtonImgState(1, IMAGE_ROUND_BUTTON, 1, 0, 1);
            button.ChangeButtonInfo(buttonX, buttonY, JEWEL_BANK_BUTTON_WIDTH, JEWEL_BANK_BUTTON_HEIGHT);
            button.ChangeText(L"");
            button.MoveTextPos(0, 0);
            button.ChangeToolTipText(tooltip, TRUE);
        };

        setupButton(m_BtnDepSingle[i], 3, L"+", L"Deposit one", 6);
        setupButton(m_BtnDepPack[i], 4, L"+", L"Deposit pack", 6);
        setupButton(m_BtnWdrSingle[i], 5, L"-", L"Withdraw one", 7);
        setupButton(m_BtnWdrPack[i], 6, L"-", L"Withdraw pack", 7);
    }

    m_BtnClose.ChangeButtonImgState(1, IMAGE_BASE_WINDOW_BTN_EXIT, 0, 0, 0);
    m_BtnClose.ChangeButtonInfo(m_Pos.x + WINDOW_WIDTH - 44, m_Pos.y + 6, 36, 29);
    m_BtnClose.ChangeText(L"");
    m_BtnClose.ChangeToolTipText(GlobalText[388], TRUE);
}

float CNewUIJewelBank::GetLayerDepth()
{
    return 3.5f;
}

float CNewUIJewelBank::GetKeyEventOrder()
{
    return 3.5f;
}

void CNewUIJewelBank::SetBalances(const unsigned int* pBalances)
{
    if (pBalances == NULL)
        return;

    for (int i = 0; i < ITEM_COUNT; i++)
        m_Balances[i] = pBalances[i];
}

void CNewUIJewelBank::SendRequest(BYTE op, BYTE arg1, WORD arg2, WORD arg3)
{
    if (SocketClient == NULL)
        return;

    SocketClient->ToGameServer()->SendJewelBankRequest(op, arg1, arg2, arg3);
}

void CNewUIJewelBank::Toggle()
{
    if (IsVisible())
    {
        Show(false);
        return;
    }

    Show(true);
    SendRequest(0, 0, 0, 0); // query current balances
}

bool CNewUIJewelBank::Update()
{
    if (IsVisible())
    {
        for (int i = 0; i < ITEM_COUNT; i++)
        {
            if (m_BtnDepSingle[i].UpdateMouseEvent())
                SendRequest(1, (BYTE)i, 0, 0);

            if (m_BtnDepPack[i].UpdateMouseEvent())
                SendRequest(1, (BYTE)i, 1, 0);

            if (m_BtnWdrSingle[i].UpdateMouseEvent())
                SendRequest(2, (BYTE)i, 0, 0);

            if (m_BtnWdrPack[i].UpdateMouseEvent())
                SendRequest(2, (BYTE)i, 1, 0);
        }

        if (m_BtnClose.UpdateMouseEvent())
            g_pNewUISystem->Hide(INTERFACE_JEWELBANK);
    }
    return true;
}

bool CNewUIJewelBank::UpdateMouseEvent()
{
    if (!CheckMouseIn(m_Pos.x, m_Pos.y, WINDOW_WIDTH, WINDOW_HEIGHT))
        return true;

    return false;
}

bool CNewUIJewelBank::UpdateKeyEvent()
{
    if (IsVisible())
    {
        if (IsPress(VK_ESCAPE) == true)
        {
            g_pNewUISystem->Hide(INTERFACE_JEWELBANK);
            return false;
        }
    }
    return true;
}

bool CNewUIJewelBank::IsVisible() const
{
    return CNewUIObj::IsVisible();
}

void CNewUIJewelBank::RenderBack()
{
    RenderImage(IMAGE_JEWEL_BANK_BACK, m_Pos.x, m_Pos.y, float(WINDOW_WIDTH), float(WINDOW_HEIGHT));
}

void CNewUIJewelBank::RenderTable()
{
    const int tableX = m_Pos.x + JEWEL_BANK_TABLE_X;
    const int tableY = m_Pos.y + JEWEL_BANK_TABLE_Y;

    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->SetBgColor(0);
    g_pRenderText->SetTextColor(226, 226, 218, 255);

    for (int i = 0; i < JEWEL_BANK_COLUMN_COUNT - 1; i++)
    {
        const int columnX = tableX + s_JewelBankColumns[i];
        const int columnWidth = s_JewelBankColumns[i + 1] - s_JewelBankColumns[i];
        if (i == 0)
            g_pRenderText->RenderText(columnX + 33, tableY + 5, s_JewelBankHeaders[i], columnWidth - 35, 0, RT3_SORT_LEFT);
        else
            g_pRenderText->RenderText(columnX, tableY + 5, s_JewelBankHeaders[i], columnWidth, 0, RT3_SORT_CENTER);
    }

    g_pRenderText->SetTextColor(232, 190, 84, 255);
    for (int i = 0; i < ITEM_COUNT; i++)
    {
        const int rowY = m_Pos.y + ROW_START_Y + i * ROW_HEIGHT;
        const JewelBankItemInfo& item = s_JewelBankItems[i];

        RenderImage(IMAGE_ITEM_BOX, tableX + 8, rowY + 1, 20.f, 18.f);
        g_pRenderText->SetFont(g_hFont);
        g_pRenderText->SetTextColor(238, 199, 86, 255);
        g_pRenderText->SetBgColor(0);
        g_pRenderText->RenderText(tableX + 33, rowY + 5, item.Name, s_JewelBankColumns[1] - 35, 0, RT3_SORT_LEFT);

        unsigned int bal = m_Balances[i];
        wchar_t amount[32];
        wchar_t packAmount[32];
        std::swprintf(amount, 32, L"x %u", bal % 10);
        std::swprintf(packAmount, 32, L"x %u", bal / 10);
        g_pRenderText->SetTextColor(238, 196, 105, 255);
        g_pRenderText->RenderText(tableX + s_JewelBankColumns[1], rowY + 5, amount, s_JewelBankColumns[2] - s_JewelBankColumns[1], 0, RT3_SORT_CENTER);
        g_pRenderText->RenderText(tableX + s_JewelBankColumns[2], rowY + 5, packAmount, s_JewelBankColumns[3] - s_JewelBankColumns[2], 0, RT3_SORT_CENTER);
    }
}

void CNewUIJewelBank::RenderBankButton(CNewUIButton& button, const wchar_t* glyph)
{
    const POINT& pos = button.GetPos();
    const POINT& size = button.GetSize();
    const int centerX = pos.x + (size.x / 2);
    const BUTTON_STATE state = button.GetBTState();
    const float light = state == BUTTON_STATE_DOWN ? 0.74f : (state == BUTTON_STATE_OVER ? 1.08f : 0.94f);

    RenderJewelBankRect(centerX - 5, pos.y + 0, 10, 1, 0.72f * light, 0.60f * light, 0.48f * light, 0.92f);
    RenderJewelBankRect(centerX - 8, pos.y + 1, 16, 2, 0.19f * light, 0.16f * light, 0.13f * light, 0.98f);
    RenderJewelBankRect(centerX - 9, pos.y + 3, 18, 12, 0.040f, 0.037f, 0.034f, 0.98f);
    RenderJewelBankRect(centerX - 7, pos.y + 4, 14, 10, 0.25f * light, 0.21f * light, 0.17f * light, 0.94f);
    RenderJewelBankRect(centerX - 5, pos.y + 5, 10, 8, 0.42f * light, 0.34f * light, 0.25f * light, 0.50f);
    RenderJewelBankRect(centerX - 8, pos.y + 15, 16, 2, 0.55f * light, 0.45f * light, 0.34f * light, 0.82f);
    RenderJewelBankRect(centerX - 4, pos.y + 8, 8, 1, 0.86f, 0.78f, 0.66f, 0.96f);

    if (glyph[0] == L'+')
    {
        RenderJewelBankRect(centerX, pos.y + 5, 1, 8, 0.32f, 0.92f, 0.74f, 0.98f);
    }
    else
    {
        RenderJewelBankRect(centerX - 4, pos.y + 8, 8, 1, 1.00f, 0.42f, 0.18f, 0.98f);
    }

    g_pRenderText->SetFont(g_hFontBold);
    g_pRenderText->SetTextColor(glyph[0] == L'+' ? 98 : 255, glyph[0] == L'+' ? 235 : 116, glyph[0] == L'+' ? 194 : 58, 255);
    g_pRenderText->SetBgColor(0);
    g_pRenderText->RenderText(pos.x, pos.y + 4, glyph, size.x, 0, RT3_SORT_CENTER);
}

void CNewUIJewelBank::Render3D()
{
    if (!IsVisible())
        return;

    const int tableX = m_Pos.x + JEWEL_BANK_TABLE_X;

    for (int i = 0; i < ITEM_COUNT; i++)
    {
        const int rowY = m_Pos.y + ROW_START_Y + i * ROW_HEIGHT;
        const JewelBankItemInfo& item = s_JewelBankItems[i];

        glColor4f(1.f, 1.f, 1.f, 1.f);
        RenderItem3D(float(tableX + 6), float(rowY - 1), 24.f, 22.f, item.Type, item.Level, 0, 0, false);
    }
}

bool CNewUIJewelBank::Render()
{
    EnableAlphaTest();
    glColor4f(1.f, 1.f, 1.f, 1.f);

    RenderBack();
    RenderTable();

    for (int i = 0; i < ITEM_COUNT; i++)
    {
        RenderBankButton(m_BtnDepSingle[i], L"+");
        RenderBankButton(m_BtnDepPack[i], L"+");
        RenderBankButton(m_BtnWdrSingle[i], L"-");
        RenderBankButton(m_BtnWdrPack[i], L"-");
    }
    m_BtnClose.Render();

    DisableAlphaBlend();

    return true;
}

//////////////////////////////////////////////////////////////////////////
// BarnaMu: CNewUIAuctionHouse - DB escrow auction house client window.
//////////////////////////////////////////////////////////////////////////

namespace
{
    constexpr BYTE AUCTION_VIEW_BROWSE = 0;
    constexpr BYTE AUCTION_VIEW_MINE = 1;
    constexpr BYTE AUCTION_VIEW_DELIVERIES = 2;
    constexpr BYTE AUCTION_VIEW_PAYOUTS = 3;
    constexpr BYTE AUCTION_VIEW_CREATE = 4;

    constexpr int AUCTION_TAB_Y = 34;
    constexpr int AUCTION_FILTER_X = 8;
    constexpr int AUCTION_FILTER_Y = 58;
    constexpr int AUCTION_FILTER_WIDTH = 72;
    constexpr int AUCTION_FILTER_HEIGHT = 188;
    constexpr int AUCTION_TABLE_PANEL_X = 86;
    constexpr int AUCTION_TABLE_PANEL_Y = 58;
    constexpr int AUCTION_TABLE_PANEL_WIDTH = 208;
    constexpr int AUCTION_TABLE_PANEL_HEIGHT = 188;
    constexpr int AUCTION_TABLE_X = 92;
    constexpr int AUCTION_TABLE_Y = 94;
    constexpr int AUCTION_HEADER_HEIGHT = 15;
    constexpr int AUCTION_ROW_HEIGHT = 13;
    constexpr int AUCTION_TABLE_WIDTH = 196;
    constexpr int AUCTION_DETAILS_X = 300;
    constexpr int AUCTION_DETAILS_Y = 58;
    constexpr int AUCTION_DETAILS_WIDTH = 122;
    constexpr int AUCTION_DETAILS_HEIGHT = 188;
    constexpr int AUCTION_CREATE_X = 86;
    constexpr int AUCTION_CREATE_Y = 58;
    constexpr int AUCTION_CREATE_WIDTH = 336;
    constexpr int AUCTION_CREATE_HEIGHT = 188;
    constexpr int AUCTION_BOTTOM_X = 8;
    constexpr int AUCTION_BOTTOM_Y = 250;
    constexpr int AUCTION_BOTTOM_WIDTH = 414;
    constexpr int AUCTION_BOTTOM_HEIGHT = 28;
    constexpr int AUCTION_COLUMN_COUNT = 6;
    constexpr int s_AuctionColumns[AUCTION_COLUMN_COUNT] =
    {
        0, 76, 100, 140, 164, 196,
    };

    const wchar_t* const s_AuctionHeaders[AUCTION_COLUMN_COUNT - 1] =
    {
        L"Item",
        L"Lvl",
        L"Seller",
        L"Cur",
        L"Price",
    };

    const wchar_t* const s_AuctionJewelNames[17] =
    {
        L"Bless",
        L"Soul",
        L"Life",
        L"Creation",
        L"Guardian",
        L"Gemstone",
        L"Harmony",
        L"Chaos",
        L"Low Ref",
        L"High Ref",
        L"BoK +1",
        L"BoK +2",
        L"BoK +3",
        L"BoK +4",
        L"BoK +5",
        L"Blue Choco",
        L"Pink Choco",
    };

    void SetupAuctionButton(CNewUIButton& button, int x, int y, int width, int height, const wchar_t* text, const wchar_t* tooltip)
    {
        button.ChangeButtonImgState(1, CNewUIAuctionHouse::IMAGE_IGS_BUTTON, 1, 0, 1);
        button.ChangeButtonInfo(x, y, width, height);
        button.ChangeText(text);
        button.ChangeToolTipText(tooltip, TRUE);
    }

    enum AuctionButtonTone
    {
        AUCTION_TONE_NEUTRAL,
        AUCTION_TONE_CONFIRM,
        AUCTION_TONE_BUY,
        AUCTION_TONE_DISABLED,
    };

    void RenderAuctionPanel(int x, int y, int width, int height, const wchar_t* title)
    {
        RenderJewelBankRect(x, y, width, height, 0.015f, 0.017f, 0.020f, 0.92f);
        RenderJewelBankRect(x + 1, y + 1, width - 2, height - 2, 0.055f, 0.065f, 0.078f, 0.88f);
        RenderJewelBankRect(x + 3, y + 3, width - 6, 18, 0.095f, 0.100f, 0.110f, 0.92f);
        RenderJewelBankLine(x, y, width, 1, 0.46f, 0.39f, 0.24f, 0.70f);
        RenderJewelBankLine(x, y + height - 1, width, 1, 0.07f, 0.08f, 0.09f, 0.92f);
        RenderJewelBankLine(x, y, 1, height, 0.35f, 0.34f, 0.31f, 0.68f);
        RenderJewelBankLine(x + width - 1, y, 1, height, 0.06f, 0.07f, 0.08f, 0.90f);

        if (title != NULL)
        {
            g_pRenderText->SetFont(g_hFontBold);
            g_pRenderText->SetBgColor(0);
            g_pRenderText->SetTextColor(226, 190, 112, 255);
            g_pRenderText->RenderText(x + 6, y + 6, title, width - 12, 0, RT3_SORT_CENTER);
        }
    }

    void RenderAuctionButton(CNewUIButton& button, const wchar_t* text, bool enabled, int tone)
    {
        const POINT& pos = button.GetPos();
        const POINT& size = button.GetSize();
        const BUTTON_STATE state = button.GetBTState();
        const bool hot = enabled && state == BUTTON_STATE_OVER;
        const bool down = enabled && state == BUTTON_STATE_DOWN;

        float red = 0.12f, green = 0.13f, blue = 0.15f;
        if (tone == AUCTION_TONE_CONFIRM)
        {
            red = 0.05f; green = 0.23f; blue = 0.11f;
        }
        else if (tone == AUCTION_TONE_BUY)
        {
            red = 0.34f; green = 0.22f; blue = 0.04f;
        }
        else if (tone == AUCTION_TONE_DISABLED)
        {
            red = 0.08f; green = 0.08f; blue = 0.09f;
        }

        const float light = !enabled ? 0.58f : (down ? 0.78f : (hot ? 1.18f : 1.0f));
        RenderJewelBankRect(pos.x, pos.y, size.x, size.y, 0.015f, 0.016f, 0.018f, 0.94f);
        RenderJewelBankRect(pos.x + 1, pos.y + 1, size.x - 2, size.y - 2, red * light, green * light, blue * light, enabled ? 0.92f : 0.62f);
        RenderJewelBankLine(pos.x + 2, pos.y + 2, size.x - 4, 1, 0.58f * light, 0.50f * light, 0.34f * light, enabled ? 0.70f : 0.26f);
        RenderJewelBankLine(pos.x + 2, pos.y + size.y - 2, size.x - 4, 1, 0.02f, 0.02f, 0.025f, 0.82f);

        g_pRenderText->SetFont(g_hFont);
        g_pRenderText->SetBgColor(0);
        if (!enabled)
            g_pRenderText->SetTextColor(120, 124, 128, 255);
        else if (tone == AUCTION_TONE_CONFIRM)
            g_pRenderText->SetTextColor(180, 245, 194, 255);
        else if (tone == AUCTION_TONE_BUY)
            g_pRenderText->SetTextColor(255, 211, 112, 255);
        else
            g_pRenderText->SetTextColor(218, 222, 224, 255);

        const int textY = pos.y + (size.y > 20 ? 8 : 5);
        g_pRenderText->RenderText(pos.x, textY, text, size.x, 0, RT3_SORT_CENTER);
    }

    void RenderAuctionLargeButton(CNewUIButton& button, const wchar_t* line1, const wchar_t* line2, bool enabled, int tone)
    {
        RenderAuctionButton(button, L"", enabled, tone);

        const POINT& pos = button.GetPos();
        const POINT& size = button.GetSize();
        g_pRenderText->SetFont(g_hFontBold);
        g_pRenderText->SetBgColor(0);
        if (!enabled)
            g_pRenderText->SetTextColor(128, 132, 136, 255);
        else if (tone == AUCTION_TONE_BUY)
            g_pRenderText->SetTextColor(255, 216, 122, 255);
        else
            g_pRenderText->SetTextColor(190, 246, 204, 255);

        if (line2 == NULL || line2[0] == L'\0')
        {
            g_pRenderText->RenderText(pos.x + 4, pos.y + 8, line1, size.x - 8, 0, RT3_SORT_CENTER);
        }
        else
        {
            g_pRenderText->RenderText(pos.x + 4, pos.y + 4, line1, size.x - 8, 0, RT3_SORT_CENTER);
            g_pRenderText->RenderText(pos.x + 4, pos.y + 14, line2, size.x - 8, 0, RT3_SORT_CENTER);
        }
    }

    void RenderAuctionTab(CNewUIButton& button, const wchar_t* text, bool selected, bool enabled)
    {
        const POINT& pos = button.GetPos();
        const POINT& size = button.GetSize();
        const BUTTON_STATE state = button.GetBTState();
        const bool hot = enabled && state == BUTTON_STATE_OVER;

        RenderJewelBankRect(pos.x, pos.y, size.x, size.y, 0.012f, 0.014f, 0.018f, 0.94f);
        if (!enabled)
            RenderJewelBankRect(pos.x + 1, pos.y + 1, size.x - 2, size.y - 2, 0.055f, 0.058f, 0.064f, 0.72f);
        else if (selected)
            RenderJewelBankRect(pos.x + 1, pos.y + 1, size.x - 2, size.y - 2, 0.05f, 0.17f, 0.30f, 0.92f);
        else
            RenderJewelBankRect(pos.x + 1, pos.y + 1, size.x - 2, size.y - 2, 0.070f, 0.080f, 0.095f, hot ? 0.94f : 0.80f);

        if (selected || hot)
            RenderJewelBankLine(pos.x + 2, pos.y + size.y - 2, size.x - 4, 1, 0.16f, 0.48f, 0.88f, 0.88f);

        g_pRenderText->SetFont(g_hFont);
        g_pRenderText->SetBgColor(0);
        if (!enabled)
            g_pRenderText->SetTextColor(112, 116, 122, 255);
        else if (selected)
            g_pRenderText->SetTextColor(235, 207, 138, 255);
        else
            g_pRenderText->SetTextColor(214, 218, 222, 255);

        g_pRenderText->RenderText(pos.x, pos.y + 7, text, size.x, 0, RT3_SORT_CENTER);
    }

    void RenderAuctionSeparator(int x, int y, int width)
    {
        RenderJewelBankLine(x, y, width, 1, 0.42f, 0.35f, 0.22f, 0.42f);
        RenderJewelBankLine(x, y + 1, width, 1, 0.00f, 0.00f, 0.00f, 0.34f);
    }

    bool IsAuctionListingCancellable(const CNewUIAuctionHouse::ListingView& listing)
    {
        return listing.Status == 0 || listing.Status == 3;
    }
}

CNewUIAuctionHouse::CNewUIAuctionHouse()
{
    m_pNewUIMng = NULL;
    m_pNewUI3DRenderMng = NULL;
    m_Pos.x = 0;
    m_Pos.y = 0;
    m_CurrentView = 0;
    m_CurrentPage = 1;
    m_Filter = 0;
    m_SellCurrency = 1;
    m_SellJewelSlot = 0;
    m_CreateSlot = -1;
    m_CreateItemType = -1;
    m_CreateItemLevel = 0;
    m_SelectedRow = -1;
    m_HoveredRow = -1;
    m_RowCount = 0;
    m_StatusMessage[0] = L'\0';
    m_StatusMessage2[0] = L'\0';
    ZeroMemory(m_Listings, sizeof(m_Listings));
}

CNewUIAuctionHouse::~CNewUIAuctionHouse()
{
    Release();
}

bool CNewUIAuctionHouse::Create(CNewUIManager* pNewUIMng, CNewUI3DRenderMng* pNewUI3DRenderMng, int x, int y)
{
    if (NULL == pNewUIMng || NULL == pNewUI3DRenderMng)
        return false;

    m_pNewUIMng = pNewUIMng;
    m_pNewUIMng->AddUIObj(INTERFACE_AUCTIONHOUSE, this);

    m_pNewUI3DRenderMng = pNewUI3DRenderMng;
    m_pNewUI3DRenderMng->Add3DRenderObj(this, INFORMATION_CAMERA_Z_ORDER);

    SetPos(10, 70);
    LoadImages();
    InitButtons();
    Show(false);

    return true;
}

void CNewUIAuctionHouse::Release()
{
    UnloadImages();

    if (m_pNewUI3DRenderMng)
    {
        m_pNewUI3DRenderMng->Remove3DRenderObj(this);
        m_pNewUI3DRenderMng = NULL;
    }

    if (m_pNewUIMng)
    {
        m_pNewUIMng->RemoveUIObj(this);
        m_pNewUIMng = NULL;
    }
}

void CNewUIAuctionHouse::SetPos(int x, int y)
{
    m_Pos.x = x;
    m_Pos.y = y;
}

void CNewUIAuctionHouse::LoadImages()
{
    LoadBitmap(L"Interface\\barna_auctionhouse_back.jpg", IMAGE_AUCTION_HOUSE_BACK, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table01(L).tga", IMAGE_TABLE_TOP_LEFT, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table01(R).tga", IMAGE_TABLE_TOP_RIGHT, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table02(L).tga", IMAGE_TABLE_BOTTOM_LEFT, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table02(R).tga", IMAGE_TABLE_BOTTOM_RIGHT, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table03(Up).tga", IMAGE_TABLE_TOP_PIXEL, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table03(Dw).tga", IMAGE_TABLE_BOTTOM_PIXEL, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table03(L).tga", IMAGE_TABLE_LEFT_PIXEL, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_item_table03(R).tga", IMAGE_TABLE_RIGHT_PIXEL, GL_LINEAR);
    LoadBitmap(L"Interface\\newui_Btn_round.tga", IMAGE_ROUND_BUTTON, GL_LINEAR);
    LoadBitmap(L"Interface\\InGameShop\\Ingame_Bt03.tga", IMAGE_IGS_BUTTON, GL_LINEAR, GL_CLAMP, 1, 0);
}

void CNewUIAuctionHouse::UnloadImages()
{
    DeleteBitmap(IMAGE_AUCTION_HOUSE_BACK);
    DeleteBitmap(IMAGE_ROUND_BUTTON);
    DeleteBitmap(IMAGE_IGS_BUTTON);
    DeleteBitmap(IMAGE_TABLE_RIGHT_PIXEL);
    DeleteBitmap(IMAGE_TABLE_LEFT_PIXEL);
    DeleteBitmap(IMAGE_TABLE_BOTTOM_PIXEL);
    DeleteBitmap(IMAGE_TABLE_TOP_PIXEL);
    DeleteBitmap(IMAGE_TABLE_BOTTOM_RIGHT);
    DeleteBitmap(IMAGE_TABLE_BOTTOM_LEFT);
    DeleteBitmap(IMAGE_TABLE_TOP_RIGHT);
    DeleteBitmap(IMAGE_TABLE_TOP_LEFT);
}

void CNewUIAuctionHouse::InitButtons()
{
    const int x = m_Pos.x;
    const int y = m_Pos.y;
    SetupAuctionButton(m_BtnHelp, -200, -200, 1, 1, L"?", L"");
    SetupAuctionButton(m_BtnMinimize, -200, -200, 1, 1, L"-", L"");

    int tabX = x + 8;
    SetupAuctionButton(m_BtnBrowse, tabX, y + AUCTION_TAB_Y, 52, 20, L"Browse", L"Browse active auctions");
    tabX += 55;
    SetupAuctionButton(m_BtnMine, tabX, y + AUCTION_TAB_Y, 70, 20, L"My Listings", L"Listings created by you");
    tabX += 73;
    SetupAuctionButton(m_BtnDeliveries, tabX, y + AUCTION_TAB_Y, 52, 20, L"Bought", L"Items you bought");
    tabX += 55;
    SetupAuctionButton(m_BtnPayouts, tabX, y + AUCTION_TAB_Y, 58, 20, L"Payouts", L"Sold listings to claim");
    tabX += 61;
    SetupAuctionButton(m_BtnCreate, tabX, y + AUCTION_TAB_Y, 52, 20, L"Create", L"Create a direct-sale listing");

    SetupAuctionButton(m_BtnMyBidsDisabled, -200, -200, 1, 1, L"", L"");
    SetupAuctionButton(m_BtnWatchlistDisabled, -200, -200, 1, 1, L"", L"");
    SetupAuctionButton(m_BtnHistoryDisabled, -200, -200, 1, 1, L"", L"");

    SetupAuctionButton(m_BtnFilterAll, x + AUCTION_FILTER_X + 8, y + AUCTION_FILTER_Y + 38, 56, 17, L"All", L"Show all listings");
    SetupAuctionButton(m_BtnFilterZen, x + AUCTION_FILTER_X + 8, y + AUCTION_FILTER_Y + 58, 56, 17, L"Zen", L"Show Zen listings");
    SetupAuctionButton(m_BtnFilterWCoin, x + AUCTION_FILTER_X + 8, y + AUCTION_FILTER_Y + 78, 56, 17, L"W Coin", L"Show W Coin listings");
    SetupAuctionButton(m_BtnFilterJewel, x + AUCTION_FILTER_X + 8, y + AUCTION_FILTER_Y + 98, 56, 17, L"Jewels", L"Show jewel listings");
    SetupAuctionButton(m_BtnFilterApply, x + AUCTION_FILTER_X + 8, y + AUCTION_FILTER_Y + 124, 56, 17, L"Apply", L"Apply supported filters");
    SetupAuctionButton(m_BtnFilterReset, x + AUCTION_FILTER_X + 8, y + AUCTION_FILTER_Y + 144, 56, 17, L"Reset", L"Reset supported filters");
    SetupAuctionButton(m_BtnAdvancedFiltersDisabled, -200, -200, 1, 1, L"", L"");

    SetupAuctionButton(m_BtnRefresh, x + AUCTION_BOTTOM_X + 8, y + AUCTION_BOTTOM_Y + 5, 54, 18, L"Refresh", L"Refresh current view");
    SetupAuctionButton(m_BtnPrev, x + AUCTION_BOTTOM_X + 66, y + AUCTION_BOTTOM_Y + 5, 36, 18, L"Prev", L"Previous page");
    SetupAuctionButton(m_BtnNext, x + AUCTION_BOTTOM_X + 146, y + AUCTION_BOTTOM_Y + 5, 36, 18, L"Next", L"Next page");
    SetupAuctionButton(m_BtnBuy, x + AUCTION_DETAILS_X + 12, y + AUCTION_DETAILS_Y + 154, 98, 24, L"Buy", L"Buy selected browse listing");
    SetupAuctionButton(m_BtnCancelListing, x + AUCTION_DETAILS_X + 12, y + AUCTION_DETAILS_Y + 154, 98, 24, L"Cancel", L"Cancel selected own listing");
    SetupAuctionButton(m_BtnReceiveItem, x + AUCTION_DETAILS_X + 12, y + AUCTION_DETAILS_Y + 154, 98, 24, L"Receive", L"Receive selected bought item");
    SetupAuctionButton(m_BtnClaimPayout, x + AUCTION_DETAILS_X + 12, y + AUCTION_DETAILS_Y + 154, 98, 24, L"Claim", L"Claim selected seller payout");
    SetupAuctionButton(m_BtnPlaceBidDisabled, -200, -200, 1, 1, L"", L"");
    SetupAuctionButton(m_BtnCompareDisabled, -200, -200, 1, 1, L"", L"");
    SetupAuctionButton(m_BtnReportDisabled, -200, -200, 1, 1, L"", L"");

    SetupAuctionButton(m_BtnSellZen, x + AUCTION_CREATE_X + 176, y + AUCTION_CREATE_Y + 68, 44, 18, L"Zen", L"Price in Zen");
    SetupAuctionButton(m_BtnSellWCoin, x + AUCTION_CREATE_X + 224, y + AUCTION_CREATE_Y + 68, 52, 18, L"W Coin", L"Price in W Coin");
    SetupAuctionButton(m_BtnSellJewel, x + AUCTION_CREATE_X + 280, y + AUCTION_CREATE_Y + 68, 50, 18, L"Jewel", L"Price in Jewel Bank currency");
    SetupAuctionButton(m_BtnSellJewelType, x + AUCTION_CREATE_X + 176, y + AUCTION_CREATE_Y + 90, 154, 18, L"Bless", L"Cycle jewel currency type");
    SetupAuctionButton(m_BtnAddItem, x + AUCTION_CREATE_X + 16, y + AUCTION_CREATE_Y + 112, 64, 18, L"Add Item", L"Use selected inventory item");
    SetupAuctionButton(m_BtnClearItem, x + AUCTION_CREATE_X + 84, y + AUCTION_CREATE_Y + 112, 56, 18, L"Clear", L"Clear selected item");
    SetupAuctionButton(m_BtnPostListing, x + AUCTION_CREATE_X + 232, y + AUCTION_CREATE_Y + 112, 78, 22, L"Post", L"Create listing");

    m_BtnClose.ChangeButtonImgState(1, IMAGE_BASE_WINDOW_BTN_EXIT, 0, 0, 0);
    m_BtnClose.ChangeButtonInfo(x + WINDOW_WIDTH - 42, y + 4, 36, 29);
    m_BtnClose.ChangeText(L"");
    m_BtnClose.ChangeToolTipText(GlobalText[388], TRUE);

    m_PriceInput.Init(g_hWnd, 80, 16, 10, false);
    m_PriceInput.SetPosition(x + AUCTION_CREATE_X + 190, y + AUCTION_CREATE_Y + 40);
    m_PriceInput.SetTextColor(255, 230, 220, 190);
    m_PriceInput.SetBackColor(170, 6, 7, 7);
    m_PriceInput.SetSelectBackColor(180, 42, 86, 145);
    m_PriceInput.SetOption(UIOPTION_NUMBERONLY | UIOPTION_ENTERIMECHKOFF);
    m_PriceInput.SetFont(g_hFont);
    m_PriceInput.SetState(UISTATE_HIDE);
    m_PriceInput.SetText(L"100");
}

float CNewUIAuctionHouse::GetLayerDepth()
{
    return 3.4f;
}

float CNewUIAuctionHouse::GetKeyEventOrder()
{
    return 3.4f;
}

void CNewUIAuctionHouse::SetListingsHeader(BYTE view, BYTE page, BYTE count)
{
    m_CurrentView = view;
    m_CurrentPage = page == 0 ? 1 : page;
    m_RowCount = 0;
    m_SelectedRow = -1;
    m_HoveredRow = -1;
    ZeroMemory(m_Listings, sizeof(m_Listings));
    std::swprintf(m_StatusMessage, 128, L"%u result(s)", count);
    m_StatusMessage2[0] = L'\0';
}

void CNewUIAuctionHouse::AddListing(const ListingView& listing)
{
    if (m_RowCount >= MAX_ROWS)
        return;

    m_Listings[m_RowCount] = listing;
    m_RowCount++;
}

void CNewUIAuctionHouse::SetStatusMessage(const wchar_t* message)
{
    if (message == NULL)
        return;

    wcsncpy(m_StatusMessage, message, 127);
    m_StatusMessage[127] = L'\0';
    m_StatusMessage2[0] = L'\0';
}

bool CNewUIAuctionHouse::IsCreateListingView() const
{
    return IsVisible() && m_CurrentView == AUCTION_VIEW_CREATE;
}

bool CNewUIAuctionHouse::TrySetCreateListingItemFromInventorySlot(int slot)
{
    if (!IsCreateListingView())
    {
        return false;
    }

    if (slot < MAX_EQUIPMENT_INDEX || slot >= MAX_MY_INVENTORY_EX_INDEX)
    {
        return false;
    }

    ITEM* item = g_pMyInventory != NULL ? g_pMyInventory->FindItem(slot) : NULL;
    m_CreateSlot = slot;
    if (item != NULL)
    {
        m_CreateItemType = item->Type;
        m_CreateItemLevel = item->Level;
    }
    else
    {
        m_CreateItemType = -1;
        m_CreateItemLevel = 0;
    }

    wchar_t message[128] = { 0 };
    std::swprintf(message, 128, L"Item selected for listing: slot %d", slot);
    SetStatusMessage(message);
    return true;
}

void CNewUIAuctionHouse::SetStatusMessages(const wchar_t* line1, const wchar_t* line2)
{
    if (line1 == NULL)
        line1 = L"";
    if (line2 == NULL)
        line2 = L"";

    wcsncpy(m_StatusMessage, line1, 127);
    m_StatusMessage[127] = L'\0';
    wcsncpy(m_StatusMessage2, line2, 127);
    m_StatusMessage2[127] = L'\0';
}

void CNewUIAuctionHouse::SendRequest(BYTE op, BYTE arg1, BYTE currency, BYTE jewelSlot, unsigned int arg2, unsigned int arg3)
{
    if (SocketClient == NULL)
        return;

    SocketClient->ToGameServer()->SendAuctionHouseRequest(op, arg1, currency, jewelSlot, arg2, arg3);
}

void CNewUIAuctionHouse::RequestCurrentView()
{
    switch (m_CurrentView)
    {
    case AUCTION_VIEW_MINE:
        SendRequest(3, 0, 0, 0xFF, 0, 0);
        break;
    case AUCTION_VIEW_DELIVERIES:
        SendRequest(5, 0, 0, 0xFF, 0, 0);
        break;
    case AUCTION_VIEW_PAYOUTS:
        SendRequest(7, 0, 0, 0xFF, 0, 0);
        break;
    case AUCTION_VIEW_CREATE:
        SetStatusMessage(L"Create listing: add a backpack item, set price, then post.");
        break;
    default:
        SendRequest(0, m_CurrentPage, m_Filter, 0xFF, 0, 0);
        break;
    }
}

void CNewUIAuctionHouse::SetView(BYTE view)
{
    m_CurrentView = view;
    m_CurrentPage = 1;
    m_SelectedRow = -1;
    m_HoveredRow = -1;
    if (view == AUCTION_VIEW_CREATE)
    {
        m_RowCount = 0;
        ZeroMemory(m_Listings, sizeof(m_Listings));
        m_PriceInput.SetState(UISTATE_NORMAL);
    }
    else
    {
        m_PriceInput.SetState(UISTATE_HIDE);
        SetRelatedWnd(g_hWnd);
    }
    RequestCurrentView();
}

void CNewUIAuctionHouse::Toggle()
{
    if (IsVisible())
    {
        m_PriceInput.SetState(UISTATE_HIDE);
        SetRelatedWnd(g_hWnd);
        Show(false);
        return;
    }

    Show(true);
    SetRelatedWnd(g_hWnd);
    SetView(0);
}

void CNewUIAuctionHouse::SetFilter(BYTE filter)
{
    m_Filter = filter;
    m_CurrentPage = 1;
    SetView(0);
}

void CNewUIAuctionHouse::ClearCreateSelection()
{
    m_CreateSlot = -1;
    m_CreateItemType = -1;
    m_CreateItemLevel = 0;
}

void CNewUIAuctionHouse::AddSelectedInventoryItemToCreateListing()
{
    const int slot = GetSelectedInventorySlot();
    ITEM* item = slot >= 0 && g_pMyInventory != NULL ? g_pMyInventory->FindItem(slot) : NULL;
    if (slot < 0 || slot > 255 || item == NULL)
    {
        SetStatusMessage(L"Open inventory, select an item, then press Add Item.");
        return;
    }

    m_CreateSlot = slot;
    m_CreateItemType = item->Type;
    m_CreateItemLevel = item->Level;
    SetStatusMessage(L"Item selected for listing.");
}

bool CNewUIAuctionHouse::SendSelectedListingAction(BYTE op, const wchar_t* action)
{
    if (m_SelectedRow < 0 || m_SelectedRow >= m_RowCount)
    {
        SetStatusMessage(L"No listing selected");
        return true;
    }

    const ListingView listing = m_Listings[m_SelectedRow];
    if (listing.ListingNumber == 0)
    {
        SetStatusMessage(L"Invalid listing number");
        return true;
    }

    wchar_t selectedMessage[128] = { 0 };
    std::swprintf(
        selectedMessage,
        128,
        L"Selected idx=%d list=%u st=%u cur=%u price=%u jewel=%u",
        m_SelectedRow,
        listing.ListingNumber,
        static_cast<unsigned int>(listing.Status),
        static_cast<unsigned int>(listing.Currency),
        listing.Price,
        static_cast<unsigned int>(listing.JewelSlot));
    g_ConsoleDebug->Write(MCD_NORMAL, L"[Auction House] %ls", selectedMessage);

    switch (op)
    {
    case 2:
    {
        wchar_t message[128] = { 0 };
        std::swprintf(message, 128, L"Sending op 2 list=%u price=%u cur=%u jewel=%u", listing.ListingNumber, listing.Price, listing.Currency, listing.JewelSlot);
        SetStatusMessages(selectedMessage, message);
        g_ConsoleDebug->Write(MCD_NORMAL, L"[Auction House] %ls", message);
        SendRequest(2, 0, listing.Currency, listing.JewelSlot, listing.ListingNumber, listing.Price);
        return true;
    }
    case 4:
    {
        if (!IsAuctionListingCancellable(listing))
        {
            SetStatusMessage(L"Action unavailable in this view");
            return true;
        }

        wchar_t message[128] = { 0 };
        std::swprintf(message, 128, L"Sending op 4 list=%u", listing.ListingNumber);
        SetStatusMessages(selectedMessage, message);
        g_ConsoleDebug->Write(MCD_NORMAL, L"[Auction House] %ls", message);
        SendRequest(4, 0, 0, 0xFF, listing.ListingNumber, 0);
        return true;
    }
    case 6:
    {
        wchar_t message[128] = { 0 };
        std::swprintf(message, 128, L"Sending op 6 list=%u", listing.ListingNumber);
        SetStatusMessages(selectedMessage, message);
        g_ConsoleDebug->Write(MCD_NORMAL, L"[Auction House] %ls", message);
        SendRequest(6, 0, 0, 0xFF, listing.ListingNumber, 0);
        return true;
    }
    case 8:
    {
        wchar_t message[128] = { 0 };
        std::swprintf(message, 128, L"Sending op 8 list=%u", listing.ListingNumber);
        SetStatusMessages(selectedMessage, message);
        g_ConsoleDebug->Write(MCD_NORMAL, L"[Auction House] %ls", message);
        SendRequest(8, 0, 0, 0xFF, listing.ListingNumber, 0);
        return true;
    }
    default:
        SetStatusMessage(L"Action unavailable in this view");
        return true;
    }
}

bool CNewUIAuctionHouse::SendCreateListingAction()
{
    unsigned int price = 0;
    if (m_CreateSlot < 0 || m_CreateSlot > 255)
    {
        SetStatusMessage(L"Invalid inventory slot");
        return true;
    }

    if (!TryGetSellPrice(price))
    {
        SetStatusMessage(L"Invalid price");
        return true;
    }

    const BYTE jewelSlot = m_SellCurrency == 2 ? m_SellJewelSlot : 0xFF;
    wchar_t postMessage[128] = { 0 };
    wchar_t sendMessage[128] = { 0 };
    std::swprintf(postMessage, 128, L"POST slot=%d price=%u cur=%u jewel=%u", m_CreateSlot, price, m_SellCurrency, jewelSlot);
    std::swprintf(sendMessage, 128, L"Sending op 1 slot=%d price=%u cur=%u jewel=%u", m_CreateSlot, price, m_SellCurrency, jewelSlot);
    SetStatusMessages(postMessage, sendMessage);
    g_ConsoleDebug->Write(MCD_NORMAL, L"[Auction House] %ls", postMessage);
    g_ConsoleDebug->Write(MCD_NORMAL, L"[Auction House] %ls", sendMessage);

    SendRequest(1, static_cast<BYTE>(m_CreateSlot), m_SellCurrency, jewelSlot, price, 0);
    ClearCreateSelection();
    return true;
}

bool CNewUIAuctionHouse::ProcessMouseButtons()
{
    if (m_BtnClose.UpdateMouseEvent())
    {
        m_PriceInput.SetState(UISTATE_HIDE);
        SetRelatedWnd(g_hWnd);
        g_pNewUISystem->Hide(INTERFACE_AUCTIONHOUSE);
        return true;
    }

    if (m_BtnBrowse.UpdateMouseEvent())
    {
        SetView(AUCTION_VIEW_BROWSE);
        return true;
    }
    if (m_BtnMine.UpdateMouseEvent())
    {
        SetView(AUCTION_VIEW_MINE);
        return true;
    }
    if (m_BtnDeliveries.UpdateMouseEvent())
    {
        SetView(AUCTION_VIEW_DELIVERIES);
        return true;
    }
    if (m_BtnPayouts.UpdateMouseEvent())
    {
        SetView(AUCTION_VIEW_PAYOUTS);
        return true;
    }
    if (m_BtnCreate.UpdateMouseEvent())
    {
        SetView(AUCTION_VIEW_CREATE);
        return true;
    }

    if (m_BtnFilterAll.UpdateMouseEvent())
    {
        SetFilter(0);
        return true;
    }
    if (m_BtnFilterZen.UpdateMouseEvent())
    {
        SetFilter(1);
        return true;
    }
    if (m_BtnFilterWCoin.UpdateMouseEvent())
    {
        SetFilter(2);
        return true;
    }
    if (m_BtnFilterJewel.UpdateMouseEvent())
    {
        SetFilter(3);
        return true;
    }
    if (m_BtnFilterApply.UpdateMouseEvent())
    {
        SetView(AUCTION_VIEW_BROWSE);
        return true;
    }
    if (m_BtnFilterReset.UpdateMouseEvent())
    {
        SetFilter(0);
        return true;
    }

    if (m_CurrentView != AUCTION_VIEW_CREATE)
    {
        if (m_BtnRefresh.UpdateMouseEvent())
        {
            RequestCurrentView();
            return true;
        }
        if (m_BtnPrev.UpdateMouseEvent())
        {
            if (m_CurrentView == AUCTION_VIEW_BROWSE && m_CurrentPage > 1)
            {
                m_CurrentPage--;
                RequestCurrentView();
            }
            else
            {
                SetStatusMessage(L"Action unavailable in this view");
            }

            return true;
        }
        if (m_BtnNext.UpdateMouseEvent())
        {
            if (m_CurrentView == AUCTION_VIEW_BROWSE)
            {
                m_CurrentPage++;
                RequestCurrentView();
            }
            else
            {
                SetStatusMessage(L"Action unavailable in this view");
            }

            return true;
        }
    }

    if (m_CurrentView == AUCTION_VIEW_CREATE)
    {
        if (m_BtnSellZen.UpdateMouseEvent())
        {
            m_SellCurrency = 0;
            SetStatusMessage(L"Create listing currency: Zen.");
            return true;
        }
        if (m_BtnSellWCoin.UpdateMouseEvent())
        {
            m_SellCurrency = 1;
            SetStatusMessage(L"Create listing currency: W Coin.");
            return true;
        }
        if (m_BtnSellJewel.UpdateMouseEvent())
        {
            m_SellCurrency = 2;
            if (m_SellJewelSlot > 16)
                m_SellJewelSlot = 0;
            SetStatusMessage(L"Create listing currency: Jewel Bank.");
            return true;
        }
        if (m_BtnSellJewelType.UpdateMouseEvent())
        {
            m_SellCurrency = 2;
            m_SellJewelSlot = (m_SellJewelSlot + 1) % 17;
            SetStatusMessage(L"Jewel currency type changed.");
            return true;
        }
        if (m_BtnAddItem.UpdateMouseEvent())
        {
            AddSelectedInventoryItemToCreateListing();
            return true;
        }
        if (m_BtnClearItem.UpdateMouseEvent())
        {
            ClearCreateSelection();
            SetStatusMessage(L"Create listing cleared.");
            return true;
        }
        if (m_BtnPostListing.UpdateMouseEvent())
        {
            return SendCreateListingAction();
        }

        return false;
    }

    if (m_CurrentView == AUCTION_VIEW_BROWSE && m_BtnBuy.UpdateMouseEvent())
        return SendSelectedListingAction(2, L"BUY");
    if (m_CurrentView == AUCTION_VIEW_MINE && m_BtnCancelListing.UpdateMouseEvent())
        return SendSelectedListingAction(4, L"CANCEL");
    if (m_CurrentView == AUCTION_VIEW_DELIVERIES && m_BtnReceiveItem.UpdateMouseEvent())
        return SendSelectedListingAction(6, L"RECEIVE");
    if (m_CurrentView == AUCTION_VIEW_PAYOUTS && m_BtnClaimPayout.UpdateMouseEvent())
        return SendSelectedListingAction(8, L"CLAIM");

    return false;
}

bool CNewUIAuctionHouse::TryGetSellPrice(unsigned int& price) const
{
    wchar_t text[32] = { 0 };
    const_cast<CUITextInputBox&>(m_PriceInput).GetText(text, 32);
    wchar_t* end = nullptr;
    const unsigned long parsed = std::wcstoul(text, &end, 10);
    if (parsed == 0 || parsed > 2000000000UL)
    {
        return false;
    }

    price = static_cast<unsigned int>(parsed);
    return true;
}

int CNewUIAuctionHouse::GetSelectedInventorySlot() const
{
    if (g_pMyInventory == NULL)
    {
        return -1;
    }

    CNewUIPickedItem* picked = CNewUIInventoryCtrl::GetPickedItem();
    if (picked != NULL && picked->GetOwnerInventory() == g_pMyInventory->GetInventoryCtrl())
    {
        return picked->GetSourceLinealPos();
    }

    return g_pMyInventory->GetPointedItemIndex();
}

bool CNewUIAuctionHouse::Update()
{
    if (!IsVisible())
        return true;

    m_PriceInput.DoAction();
    if (m_PriceInput.HaveFocus())
    {
        SetRelatedWnd(m_PriceInput.GetHandle());
    }
    else
    {
        SetRelatedWnd(g_hWnd);
    }

    return true;
}

bool CNewUIAuctionHouse::UpdateMouseEvent()
{
    if (!CheckMouseIn(m_Pos.x, m_Pos.y, WINDOW_WIDTH, WINDOW_HEIGHT))
        return true;

    m_HoveredRow = -1;
    if (ProcessMouseButtons())
        return false;

    if (m_CurrentView != AUCTION_VIEW_CREATE)
    {
        const int tableX = m_Pos.x + AUCTION_TABLE_X;
        const int tableY = m_Pos.y + AUCTION_TABLE_Y;
        for (int i = 0; i < m_RowCount; ++i)
        {
            const int rowY = tableY + AUCTION_HEADER_HEIGHT + i * AUCTION_ROW_HEIGHT;
            if (CheckMouseIn(tableX, rowY, AUCTION_TABLE_WIDTH, AUCTION_ROW_HEIGHT))
            {
                m_HoveredRow = i;
                if (IsRelease(VK_LBUTTON))
                {
                    m_SelectedRow = i;
                    return false;
                }
            }
        }
    }

    return false;
}

bool CNewUIAuctionHouse::UpdateKeyEvent()
{
    if (IsVisible() && IsPress(VK_ESCAPE) == true)
    {
        m_PriceInput.SetState(UISTATE_HIDE);
        SetRelatedWnd(g_hWnd);
        g_pNewUISystem->Hide(INTERFACE_AUCTIONHOUSE);
        return false;
    }

    return true;
}

bool CNewUIAuctionHouse::IsVisible() const
{
    return CNewUIObj::IsVisible();
}

void CNewUIAuctionHouse::RenderBack()
{
    RenderJewelBankRect(m_Pos.x, m_Pos.y, WINDOW_WIDTH, WINDOW_HEIGHT, 0.010f, 0.012f, 0.015f, 0.96f);
    RenderJewelBankRect(m_Pos.x + 2, m_Pos.y + 2, WINDOW_WIDTH - 4, WINDOW_HEIGHT - 4, 0.035f, 0.040f, 0.048f, 0.94f);
    RenderJewelBankRect(m_Pos.x + 6, m_Pos.y + 6, WINDOW_WIDTH - 12, 24, 0.060f, 0.064f, 0.072f, 0.94f);
    RenderJewelBankLine(m_Pos.x + 6, m_Pos.y + 31, WINDOW_WIDTH - 12, 1, 0.38f, 0.34f, 0.24f, 0.62f);

    g_pRenderText->SetFont(g_hFontBold);
    g_pRenderText->SetBgColor(0);
    g_pRenderText->SetTextColor(238, 210, 142, 255);
    g_pRenderText->RenderText(m_Pos.x, m_Pos.y + 13, L"AUCTION HOUSE", WINDOW_WIDTH, 0, RT3_SORT_CENTER);
}

void CNewUIAuctionHouse::RenderTable()
{
    const int panelX = m_Pos.x + AUCTION_TABLE_PANEL_X;
    const int panelY = m_Pos.y + AUCTION_TABLE_PANEL_Y;
    const int tableX = m_Pos.x + AUCTION_TABLE_X;
    const int tableY = m_Pos.y + AUCTION_TABLE_Y;

    RenderAuctionPanel(panelX, panelY, AUCTION_TABLE_PANEL_WIDTH, AUCTION_TABLE_PANEL_HEIGHT, L"LISTINGS");
    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->SetBgColor(0);
    g_pRenderText->SetTextColor(156, 184, 218, 255);
    g_pRenderText->RenderText(panelX + 8, panelY + 23, GetViewTitle(), AUCTION_TABLE_PANEL_WIDTH - 16, 0, RT3_SORT_LEFT);
    g_pRenderText->SetTextColor(196, 177, 125, 255);
    wchar_t pageText[32] = { 0 };
    std::swprintf(pageText, 32, L"Page %u", m_CurrentPage);
    g_pRenderText->RenderText(panelX + AUCTION_TABLE_PANEL_WIDTH - 72, panelY + 23, pageText, 64, 0, RT3_SORT_RIGHT);

    RenderJewelBankRect(tableX, tableY, AUCTION_TABLE_WIDTH, AUCTION_HEADER_HEIGHT, 0.075f, 0.084f, 0.094f, 0.94f);
    RenderJewelBankLine(tableX, tableY, AUCTION_TABLE_WIDTH, 1, 0.47f, 0.40f, 0.25f, 0.54f);
    RenderJewelBankLine(tableX, tableY + AUCTION_HEADER_HEIGHT, AUCTION_TABLE_WIDTH, 1, 0.23f, 0.25f, 0.27f, 0.70f);

    for (int i = 0; i <= MAX_ROWS; ++i)
    {
        const int rowY = tableY + AUCTION_HEADER_HEIGHT + i * AUCTION_ROW_HEIGHT;
        RenderJewelBankLine(tableX, rowY, AUCTION_TABLE_WIDTH, 1, 0.16f, 0.17f, 0.18f, 0.62f);
    }

    for (int i = 0; i < AUCTION_COLUMN_COUNT; ++i)
    {
        const int columnX = tableX + s_AuctionColumns[i];
        RenderJewelBankLine(columnX, tableY, 1, AUCTION_HEADER_HEIGHT + MAX_ROWS * AUCTION_ROW_HEIGHT, 0.18f, 0.19f, 0.20f, 0.66f);
    }

    for (int i = 0; i < MAX_ROWS; ++i)
    {
        const int rowY = tableY + AUCTION_HEADER_HEIGHT + i * AUCTION_ROW_HEIGHT;
        const bool alt = (i % 2) != 0;
        RenderJewelBankRect(tableX + 1, rowY + 1, AUCTION_TABLE_WIDTH - 2, AUCTION_ROW_HEIGHT - 1,
            alt ? 0.045f : 0.060f, alt ? 0.050f : 0.060f, alt ? 0.058f : 0.070f, 0.72f);
    }

    for (int i = 0; i < m_RowCount; ++i)
    {
        const int rowY = tableY + AUCTION_HEADER_HEIGHT + i * AUCTION_ROW_HEIGHT;
        if (i == m_HoveredRow)
        {
            RenderJewelBankRect(tableX + 1, rowY + 1, AUCTION_TABLE_WIDTH - 2, AUCTION_ROW_HEIGHT - 2, 0.02f, 0.18f, 0.34f, 0.42f);
        }
        if (i == m_SelectedRow)
        {
            RenderJewelBankRect(tableX + 1, rowY + 1, AUCTION_TABLE_WIDTH - 2, AUCTION_ROW_HEIGHT - 2, 0.02f, 0.27f, 0.55f, 0.58f);
            RenderJewelBankLine(tableX + 1, rowY + 1, AUCTION_TABLE_WIDTH - 2, 1, 0.14f, 0.58f, 1.00f, 0.92f);
        }
    }

    for (int i = 0; i <= MAX_ROWS; ++i)
    {
        const int rowY = tableY + AUCTION_HEADER_HEIGHT + i * AUCTION_ROW_HEIGHT;
        RenderJewelBankLine(tableX, rowY, AUCTION_TABLE_WIDTH, 1, 0.18f, 0.19f, 0.20f, 0.72f);
    }

    for (int i = 0; i < AUCTION_COLUMN_COUNT; ++i)
    {
        const int columnX = tableX + s_AuctionColumns[i];
        RenderJewelBankLine(columnX, tableY, 1, AUCTION_HEADER_HEIGHT + MAX_ROWS * AUCTION_ROW_HEIGHT, 0.18f, 0.19f, 0.20f, 0.70f);
    }

    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->SetBgColor(0);
    g_pRenderText->SetTextColor(222, 224, 220, 255);
    for (int i = 0; i < AUCTION_COLUMN_COUNT - 1; ++i)
    {
        const int columnX = tableX + s_AuctionColumns[i];
        const int columnWidth = s_AuctionColumns[i + 1] - s_AuctionColumns[i];
        g_pRenderText->RenderText(columnX, tableY + 4, s_AuctionHeaders[i], columnWidth, 0, RT3_SORT_CENTER);
    }

    for (int i = 0; i < m_RowCount; ++i)
    {
        const ListingView& listing = m_Listings[i];
        const int rowY = tableY + AUCTION_HEADER_HEIGHT + i * AUCTION_ROW_HEIGHT;
        wchar_t price[32] = { 0 };
        wchar_t level[16] = { 0 };
        std::swprintf(price, 32, L"%u", listing.Price);
        std::swprintf(level, 16, L"+%u", listing.ItemLevel);
        const wchar_t* currencyText = L"?";
        if (listing.Currency == 0)
            currencyText = L"Zen";
        else if (listing.Currency == 1)
            currencyText = L"W";
        else if (listing.Currency == 2)
            currencyText = L"Jwl";

        g_pRenderText->SetTextColor(235, 202, 104, 255);
        g_pRenderText->RenderText(tableX + s_AuctionColumns[0] + 18, rowY + 3, listing.ItemName, s_AuctionColumns[1] - s_AuctionColumns[0] - 20, 0, RT3_SORT_LEFT);
        g_pRenderText->SetTextColor(216, 220, 224, 255);
        g_pRenderText->RenderText(tableX + s_AuctionColumns[1], rowY + 3, level, s_AuctionColumns[2] - s_AuctionColumns[1], 0, RT3_SORT_CENTER);
        g_pRenderText->RenderText(tableX + s_AuctionColumns[2], rowY + 3, listing.SellerName, s_AuctionColumns[3] - s_AuctionColumns[2], 0, RT3_SORT_CENTER);
        g_pRenderText->SetTextColor(236, 176, 96, 255);
        g_pRenderText->RenderText(tableX + s_AuctionColumns[3], rowY + 3, currencyText, s_AuctionColumns[4] - s_AuctionColumns[3], 0, RT3_SORT_CENTER);
        g_pRenderText->RenderText(tableX + s_AuctionColumns[4], rowY + 3, price, s_AuctionColumns[5] - s_AuctionColumns[4], 0, RT3_SORT_CENTER);
    }

    if (m_RowCount == 0)
    {
        g_pRenderText->SetFont(g_hFont);
        g_pRenderText->SetTextColor(130, 138, 148, 255);
        g_pRenderText->RenderText(tableX, tableY + 104, L"No listings in this view.", AUCTION_TABLE_WIDTH, 0, RT3_SORT_CENTER);
    }
}

void CNewUIAuctionHouse::RenderFilters()
{
    const int x = m_Pos.x + AUCTION_FILTER_X;
    const int y = m_Pos.y + AUCTION_FILTER_Y;

    RenderAuctionPanel(x, y, AUCTION_FILTER_WIDTH, AUCTION_FILTER_HEIGHT, L"FILTERS");

    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->SetBgColor(0);
    g_pRenderText->SetTextColor(212, 214, 214, 255);
    g_pRenderText->RenderText(x + 8, y + 25, L"Currency", AUCTION_FILTER_WIDTH - 16, 0, RT3_SORT_LEFT);

    RenderAuctionButton(m_BtnFilterAll, L"All", true, m_Filter == 0 ? AUCTION_TONE_BUY : AUCTION_TONE_NEUTRAL);
    RenderAuctionButton(m_BtnFilterZen, L"Zen", true, m_Filter == 1 ? AUCTION_TONE_BUY : AUCTION_TONE_NEUTRAL);
    RenderAuctionButton(m_BtnFilterWCoin, L"W Coin", true, m_Filter == 2 ? AUCTION_TONE_BUY : AUCTION_TONE_NEUTRAL);
    RenderAuctionButton(m_BtnFilterJewel, L"Jewels", true, m_Filter == 3 ? AUCTION_TONE_BUY : AUCTION_TONE_NEUTRAL);

    g_pRenderText->SetTextColor(166, 198, 245, 255);
    const wchar_t* filterText = L"All currencies";
    if (m_Filter == 1)
        filterText = L"Zen only";
    else if (m_Filter == 2)
        filterText = L"W Coin only";
    else if (m_Filter == 3)
        filterText = L"Jewels only";
    g_pRenderText->RenderText(x + 8, y + 166, L"Status", AUCTION_FILTER_WIDTH - 16, 0, RT3_SORT_LEFT);
    g_pRenderText->SetTextColor(232, 190, 84, 255);
    g_pRenderText->RenderText(x + 8, y + 178, filterText, AUCTION_FILTER_WIDTH - 16, 0, RT3_SORT_LEFT);

    RenderAuctionButton(m_BtnFilterApply, L"Apply", true, AUCTION_TONE_CONFIRM);
    RenderAuctionButton(m_BtnFilterReset, L"Reset", true, AUCTION_TONE_NEUTRAL);
}

void CNewUIAuctionHouse::RenderDetails()
{
    const int x = m_Pos.x + AUCTION_DETAILS_X;
    const int y = m_Pos.y + AUCTION_DETAILS_Y;
    const ListingView* listing = (m_SelectedRow >= 0 && m_SelectedRow < m_RowCount) ? &m_Listings[m_SelectedRow] : NULL;

    RenderAuctionPanel(x, y, AUCTION_DETAILS_WIDTH, AUCTION_DETAILS_HEIGHT, L"LISTING DETAILS");

    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->SetBgColor(0);
    if (listing == NULL)
    {
        g_pRenderText->SetTextColor(150, 154, 160, 255);
        g_pRenderText->RenderText(x + 8, y + 70, L"Select a listing", AUCTION_DETAILS_WIDTH - 16, 0, RT3_SORT_CENTER);
        g_pRenderText->RenderText(x + 8, y + 84, L"to inspect it.", AUCTION_DETAILS_WIDTH - 16, 0, RT3_SORT_CENTER);
        if (m_CurrentView == AUCTION_VIEW_MINE)
        {
            g_pRenderText->RenderText(x + 8, y + 104, L"Select one of", AUCTION_DETAILS_WIDTH - 16, 0, RT3_SORT_CENTER);
            g_pRenderText->RenderText(x + 8, y + 118, L"your listings.", AUCTION_DETAILS_WIDTH - 16, 0, RT3_SORT_CENTER);
        }
        return;
    }

    wchar_t idText[32] = { 0 };
    wchar_t priceText[48] = { 0 };
    wchar_t typeText[32] = { 0 };
    wchar_t levelText[16] = { 0 };
    wchar_t jewelText[48] = { 0 };
    std::swprintf(idText, 32, L"#%u", listing->ListingNumber);
    std::swprintf(priceText, 48, L"%u %s", listing->Price, GetCurrencyText(listing->Currency, listing->JewelSlot));
    std::swprintf(typeText, 32, L"%u:%u", listing->ItemType / 512, listing->ItemType % 512);
    std::swprintf(levelText, 16, L"+%u", listing->ItemLevel);
    if (listing->Currency == 2 && listing->JewelSlot < 17)
        std::swprintf(jewelText, 48, L"%u - %s", listing->JewelSlot, GetCurrencyText(listing->Currency, listing->JewelSlot));
    else
        std::swprintf(jewelText, 48, L"-");

    g_pRenderText->SetTextColor(86, 236, 86, 255);
    g_pRenderText->RenderText(x + 42, y + 28, listing->ItemName, 74, 0, RT3_SORT_LEFT);
    g_pRenderText->SetTextColor(216, 218, 218, 255);
    g_pRenderText->RenderText(x + 8, y + 54, L"Listing", 40, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 50, y + 54, idText, 66, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 8, y + 66, L"Type", 40, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 50, y + 66, typeText, 66, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 8, y + 78, L"Level", 40, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 50, y + 78, levelText, 66, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 8, y + 90, L"Seller", 40, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 50, y + 90, listing->SellerName, 66, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 8, y + 102, L"Status", 40, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 50, y + 102, GetStatusText(listing->Status), 66, 0, RT3_SORT_LEFT);
    g_pRenderText->SetTextColor(236, 184, 86, 255);
    g_pRenderText->RenderText(x + 8, y + 114, L"Price", 40, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 50, y + 114, priceText, 66, 0, RT3_SORT_LEFT);
    g_pRenderText->SetTextColor(156, 166, 176, 255);
    g_pRenderText->RenderText(x + 8, y + 126, L"Jewel", 40, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 50, y + 126, jewelText, 66, 0, RT3_SORT_LEFT);
}

void CNewUIAuctionHouse::RenderCreateListing()
{
    const int x = m_Pos.x + AUCTION_CREATE_X;
    const int y = m_Pos.y + AUCTION_CREATE_Y;

    RenderAuctionPanel(x, y, AUCTION_CREATE_WIDTH, AUCTION_CREATE_HEIGHT, L"CREATE LISTING");
    RenderJewelBankRect(x + 16, y + 34, 72, 62, 0.010f, 0.012f, 0.014f, 0.94f);
    RenderJewelBankRect(x + 18, y + 36, 68, 58, 0.055f, 0.060f, 0.068f, 0.86f);
    RenderJewelBankLine(x + 18, y + 36, 68, 1, 0.46f, 0.38f, 0.23f, 0.58f);

    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->SetBgColor(0);
    if (m_CreateSlot >= 0)
    {
        wchar_t slotText[32] = { 0 };
        std::swprintf(slotText, 32, L"Slot %d", m_CreateSlot);

        g_pRenderText->SetTextColor(116, 236, 126, 255);
        g_pRenderText->RenderText(x + 16, y + 98, slotText, 72, 0, RT3_SORT_CENTER);
    }
    else
    {
        g_pRenderText->SetTextColor(154, 160, 168, 255);
        g_pRenderText->RenderText(x + 20, y + 54, L"Item slot", 64, 0, RT3_SORT_CENTER);
        g_pRenderText->RenderText(x + 20, y + 68, L"empty", 64, 0, RT3_SORT_CENTER);
    }

    g_pRenderText->SetTextColor(220, 220, 210, 255);
    g_pRenderText->RenderText(x + 116, y + 44, L"Price", 58, 0, RT3_SORT_LEFT);
    g_pRenderText->RenderText(x + 116, y + 72, L"Currency", 58, 0, RT3_SORT_LEFT);

    RenderAuctionButton(m_BtnSellZen, L"Zen", true, m_SellCurrency == 0 ? AUCTION_TONE_BUY : AUCTION_TONE_NEUTRAL);
    RenderAuctionButton(m_BtnSellWCoin, L"W", true, m_SellCurrency == 1 ? AUCTION_TONE_BUY : AUCTION_TONE_NEUTRAL);
    RenderAuctionButton(m_BtnSellJewel, L"Jewel", true, m_SellCurrency == 2 ? AUCTION_TONE_BUY : AUCTION_TONE_NEUTRAL);

    wchar_t jewelText[64] = { 0 };
    if (m_SellCurrency == 2)
        std::swprintf(jewelText, 64, L"%s", GetCurrencyText(2, m_SellJewelSlot));
    else
        std::swprintf(jewelText, 64, L"Jewel type");
    RenderAuctionButton(m_BtnSellJewelType, jewelText, m_SellCurrency == 2, m_SellCurrency == 2 ? AUCTION_TONE_NEUTRAL : AUCTION_TONE_DISABLED);

    m_PriceInput.Render();
    RenderAuctionButton(m_BtnAddItem, L"Add Item", true, AUCTION_TONE_NEUTRAL);
    RenderAuctionButton(m_BtnClearItem, L"Clear", m_CreateSlot >= 0, AUCTION_TONE_NEUTRAL);
    RenderAuctionButton(m_BtnPostListing, L"Post", m_CreateSlot >= 0, AUCTION_TONE_CONFIRM);

    g_pRenderText->SetTextColor(188, 198, 210, 255);
    g_pRenderText->RenderText(x + 16, y + 144, L"Use Add Item, set price/currency, then Post.", AUCTION_CREATE_WIDTH - 32, 0, RT3_SORT_LEFT);
    g_pRenderText->SetTextColor(150, 156, 164, 255);
    g_pRenderText->RenderText(x + 16, y + 160, L"Right-click inventory item to select it.", AUCTION_CREATE_WIDTH - 32, 0, RT3_SORT_LEFT);
}

void CNewUIAuctionHouse::RenderFooter()
{
    RenderAuctionPanel(m_Pos.x + AUCTION_BOTTOM_X, m_Pos.y + AUCTION_BOTTOM_Y, AUCTION_BOTTOM_WIDTH, AUCTION_BOTTOM_HEIGHT, NULL);
    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->SetBgColor(0);
    wchar_t pageText[32] = { 0 };
    std::swprintf(pageText, 32, L"Page %u", m_CurrentPage);

    g_pRenderText->SetTextColor(196, 177, 125, 255);
    g_pRenderText->RenderText(m_Pos.x + AUCTION_BOTTOM_X + 106, m_Pos.y + AUCTION_BOTTOM_Y + 8, pageText, 38, 0, RT3_SORT_CENTER);
    if (m_StatusMessage2[0] != L'\0')
    {
        g_pRenderText->SetTextColor(230, 190, 90, 255);
        g_pRenderText->RenderText(m_Pos.x + AUCTION_BOTTOM_X + 194, m_Pos.y + AUCTION_BOTTOM_Y + 3, m_StatusMessage, AUCTION_BOTTOM_WIDTH - 200, 0, RT3_SORT_LEFT);
        g_pRenderText->SetTextColor(166, 198, 245, 255);
        g_pRenderText->RenderText(m_Pos.x + AUCTION_BOTTOM_X + 194, m_Pos.y + AUCTION_BOTTOM_Y + 15, m_StatusMessage2, AUCTION_BOTTOM_WIDTH - 200, 0, RT3_SORT_LEFT);
    }
    else
    {
        g_pRenderText->SetTextColor(188, 198, 210, 255);
        g_pRenderText->RenderText(m_Pos.x + AUCTION_BOTTOM_X + 190, m_Pos.y + AUCTION_BOTTOM_Y + 8, L"Status", 42, 0, RT3_SORT_LEFT);
        g_pRenderText->SetTextColor(230, 190, 90, 255);
        g_pRenderText->RenderText(m_Pos.x + AUCTION_BOTTOM_X + 234, m_Pos.y + AUCTION_BOTTOM_Y + 8, m_StatusMessage, AUCTION_BOTTOM_WIDTH - 240, 0, RT3_SORT_LEFT);
    }
}

void CNewUIAuctionHouse::RenderContextAction()
{
    if (m_CurrentView == AUCTION_VIEW_CREATE)
        return;

    const int x = m_Pos.x + AUCTION_DETAILS_X;
    const int y = m_Pos.y + AUCTION_DETAILS_Y;
    const bool hasSelected = m_SelectedRow >= 0 && m_SelectedRow < m_RowCount;
    const ListingView* listing = hasSelected ? &m_Listings[m_SelectedRow] : NULL;

    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->SetBgColor(0);
    g_pRenderText->SetTextColor(156, 166, 176, 255);

    switch (m_CurrentView)
    {
    case AUCTION_VIEW_MINE:
        if (!hasSelected)
        {
            g_pRenderText->RenderText(x + 8, y + 140, L"Select your listing.", AUCTION_DETAILS_WIDTH - 16, 0, RT3_SORT_CENTER);
        }
        else if (!IsAuctionListingCancellable(*listing))
        {
            g_pRenderText->RenderText(x + 8, y + 140, L"Cannot cancel.", AUCTION_DETAILS_WIDTH - 16, 0, RT3_SORT_CENTER);
        }
        RenderAuctionButton(m_BtnCancelListing, L"CANCEL", hasSelected && IsAuctionListingCancellable(*listing), AUCTION_TONE_NEUTRAL);
        break;
    case AUCTION_VIEW_DELIVERIES:
        if (!hasSelected)
            g_pRenderText->RenderText(x + 8, y + 140, L"Select bought item.", AUCTION_DETAILS_WIDTH - 16, 0, RT3_SORT_CENTER);
        RenderAuctionButton(m_BtnReceiveItem, L"RECEIVE", hasSelected, AUCTION_TONE_CONFIRM);
        break;
    case AUCTION_VIEW_PAYOUTS:
        if (!hasSelected)
            g_pRenderText->RenderText(x + 8, y + 140, L"Select payout.", AUCTION_DETAILS_WIDTH - 16, 0, RT3_SORT_CENTER);
        RenderAuctionButton(m_BtnClaimPayout, L"CLAIM", hasSelected, AUCTION_TONE_CONFIRM);
        break;
    default:
        if (!hasSelected)
            g_pRenderText->RenderText(x + 8, y + 140, L"Select listing.", AUCTION_DETAILS_WIDTH - 16, 0, RT3_SORT_CENTER);
        RenderAuctionButton(m_BtnBuy, L"BUY", hasSelected, AUCTION_TONE_BUY);
        break;
    }
}

void CNewUIAuctionHouse::RenderListingActionButton(CNewUIButton& button, const wchar_t* text, bool enabled, int tone)
{
    RenderAuctionButton(button, text, enabled, tone);
}

void CNewUIAuctionHouse::Render3D()
{
    if (!IsVisible())
        return;

    if (m_CurrentView == AUCTION_VIEW_CREATE)
    {
        if (m_CreateItemType >= 0)
        {
            glColor4f(1.f, 1.f, 1.f, 1.f);
            RenderItem3D(float(m_Pos.x + AUCTION_CREATE_X + 22), float(m_Pos.y + AUCTION_CREATE_Y + 40), 58.f, 50.f, m_CreateItemType, m_CreateItemLevel, 0, 0, false);
        }
        return;
    }

    const int tableX = m_Pos.x + AUCTION_TABLE_X;
    const int tableY = m_Pos.y + AUCTION_TABLE_Y;
    for (int i = 0; i < m_RowCount; ++i)
    {
        const int rowY = tableY + AUCTION_HEADER_HEIGHT + i * AUCTION_ROW_HEIGHT;
        glColor4f(1.f, 1.f, 1.f, 1.f);
        RenderItem3D(float(tableX + 2), float(rowY + 1), 14.f, 12.f, m_Listings[i].ItemType, m_Listings[i].ItemLevel, 0, 0, false);
    }

    if (m_SelectedRow >= 0 && m_SelectedRow < m_RowCount)
    {
        const ListingView& listing = m_Listings[m_SelectedRow];
        glColor4f(1.f, 1.f, 1.f, 1.f);
        RenderItem3D(float(m_Pos.x + AUCTION_DETAILS_X + 9), float(m_Pos.y + AUCTION_DETAILS_Y + 29), 28.f, 24.f, listing.ItemType, listing.ItemLevel, 0, 0, false);
    }
}

bool CNewUIAuctionHouse::Render()
{
    EnableAlphaTest();
    glColor4f(1.f, 1.f, 1.f, 1.f);

    RenderBack();
    RenderAuctionTab(m_BtnBrowse, L"Browse", m_CurrentView == AUCTION_VIEW_BROWSE, true);
    RenderAuctionTab(m_BtnMine, L"My Listings", m_CurrentView == AUCTION_VIEW_MINE, true);
    RenderAuctionTab(m_BtnDeliveries, L"Bought", m_CurrentView == AUCTION_VIEW_DELIVERIES, true);
    RenderAuctionTab(m_BtnPayouts, L"Payouts", m_CurrentView == AUCTION_VIEW_PAYOUTS, true);
    RenderAuctionTab(m_BtnCreate, L"Create", m_CurrentView == AUCTION_VIEW_CREATE, true);

    RenderFilters();
    RenderFooter();
    if (m_CurrentView == AUCTION_VIEW_CREATE)
    {
        RenderCreateListing();
    }
    else
    {
        RenderTable();
        RenderDetails();
        RenderContextAction();
        RenderListingActionButton(m_BtnRefresh, L"Refresh", true, AUCTION_TONE_NEUTRAL);
        RenderListingActionButton(m_BtnPrev, L"Prev", m_CurrentView == AUCTION_VIEW_BROWSE && m_CurrentPage > 1, AUCTION_TONE_NEUTRAL);
        RenderListingActionButton(m_BtnNext, L"Next", m_CurrentView == AUCTION_VIEW_BROWSE, AUCTION_TONE_NEUTRAL);
    }
    m_BtnClose.Render();

    DisableAlphaBlend();
    return true;
}

const wchar_t* CNewUIAuctionHouse::GetCurrencyText(BYTE currency, BYTE jewelSlot) const
{
    switch (currency)
    {
    case 0:
        return L"Zen";
    case 1:
        return L"W Coin";
    case 2:
        return jewelSlot < 17 ? s_AuctionJewelNames[jewelSlot] : L"Jewel";
    default:
        return L"?";
    }
}

const wchar_t* CNewUIAuctionHouse::GetStatusText(BYTE status) const
{
    switch (status)
    {
    case 0:
        return L"Active";
    case 1:
        return L"Sold";
    case 2:
        return L"Cancelled";
    case 3:
        return L"Expired";
    case 4:
        return L"Done";
    default:
        return L"?";
    }
}

const wchar_t* CNewUIAuctionHouse::GetViewTitle() const
{
    switch (m_CurrentView)
    {
    case AUCTION_VIEW_MINE:
        return L"MY LISTINGS";
    case AUCTION_VIEW_DELIVERIES:
        return L"BOUGHT ITEMS";
    case AUCTION_VIEW_PAYOUTS:
        return L"SELLER PAYOUTS";
    case AUCTION_VIEW_CREATE:
        return L"CREATE LISTING";
    default:
        return L"MARKET LISTINGS";
    }
}

const wchar_t* CNewUIAuctionHouse::GetActionText() const
{
    switch (m_CurrentView)
    {
    case AUCTION_VIEW_MINE:
        return L"Cancel Listing";
    case AUCTION_VIEW_DELIVERIES:
        return L"Receive Item";
    case AUCTION_VIEW_PAYOUTS:
        return L"Claim W Coin";
    case AUCTION_VIEW_CREATE:
        return L"Post Listing";
    default:
        return L"Buy Selected";
    }
}

//////////////////////////////////////////////////////////////////////////
// BarnaMu: CNewUIDuelLadder - PvP Duel Ladder window opened with L hotkey.
//////////////////////////////////////////////////////////////////////////

static const wchar_t* const s_DuelLadderTierNames[6] =
{
    L"Bronze", L"Silver", L"Gold", L"Platinum", L"Diamond", L"Master",
};

static const wchar_t* GetDuelTierName(BYTE tier)
{
    return tier < 6 ? s_DuelLadderTierNames[tier] : L"?";
}

CNewUIDuelLadder::CNewUIDuelLadder()
{
    m_pNewUIMng = NULL;
    m_Pos.x = 0;
    m_Pos.y = 0;
    m_CurrentTab = 0;
    m_CurrentBracket = 1;
    m_EntryCount = 0;
    m_ProfileBracket = 0;
    m_ProfileTier = 0;
    m_ProfileRating = 0;
    m_ProfileWins = 0;
    m_ProfileLosses = 0;
    m_ProfileRank = 0;
    memset(m_Entries, 0, sizeof(m_Entries));
}

CNewUIDuelLadder::~CNewUIDuelLadder()
{
    Release();
}

bool CNewUIDuelLadder::Create(CNewUIManager* pNewUIMng, int x, int y)
{
    if (NULL == pNewUIMng)
        return false;

    m_pNewUIMng = pNewUIMng;
    m_pNewUIMng->AddUIObj(INTERFACE_DUELLADDER, this);

    SetPos(x, y);
    LoadImages();
    InitButtons();
    Show(false);

    return true;
}

void CNewUIDuelLadder::Release()
{
    UnloadImages();
    if (m_pNewUIMng)
    {
        m_pNewUIMng->RemoveUIObj(this);
        m_pNewUIMng = NULL;
    }
}

void CNewUIDuelLadder::SetPos(int x, int y)
{
    m_Pos.x = x;
    m_Pos.y = y;
}

void CNewUIDuelLadder::LoadImages()
{
    LoadBitmap(L"Interface\\InGameShop\\Ingame_Bt03.tga", IMAGE_IGS_BUTTON, GL_LINEAR, GL_CLAMP, 1, 0);
}

void CNewUIDuelLadder::UnloadImages()
{
    DeleteBitmap(IMAGE_IGS_BUTTON);
}

void CNewUIDuelLadder::InitButtons()
{
    m_BtnTabRankings.ChangeButtonImgState(1, IMAGE_IGS_BUTTON, 1, 0, 1);
    m_BtnTabRankings.ChangeButtonInfo(m_Pos.x + 22, m_Pos.y + 44, 110, 24);
    m_BtnTabRankings.ChangeText(L"Rankings");
    m_BtnTabRankings.MoveTextPos(0, -1);
    m_BtnTabRankings.ChangeToolTipText(L"", TRUE);

    m_BtnTabProfile.ChangeButtonImgState(1, IMAGE_IGS_BUTTON, 1, 0, 1);
    m_BtnTabProfile.ChangeButtonInfo(m_Pos.x + 142, m_Pos.y + 44, 110, 24);
    m_BtnTabProfile.ChangeText(L"My Profile");
    m_BtnTabProfile.MoveTextPos(0, -1);
    m_BtnTabProfile.ChangeToolTipText(L"", TRUE);

    for (int i = 0; i < 5; i++)
    {
        m_BtnBracket[i].ChangeButtonImgState(1, IMAGE_IGS_BUTTON, 1, 0, 1);
        m_BtnBracket[i].ChangeButtonInfo(m_Pos.x + 22 + i * 70, m_Pos.y + 76, 62, 22);
        wchar_t label[8];
        std::swprintf(label, 8, L"T%d", i + 1);
        m_BtnBracket[i].ChangeText(label);
        m_BtnBracket[i].MoveTextPos(0, -1);
        m_BtnBracket[i].ChangeToolTipText(L"", TRUE);
    }

    m_BtnClose.ChangeButtonImgState(1, IMAGE_BASE_WINDOW_BTN_EXIT, 0, 0, 0);
    m_BtnClose.ChangeButtonInfo(m_Pos.x + 22, m_Pos.y + WINDOW_HEIGHT - 41, 36, 29);
    m_BtnClose.ChangeText(L"");
    m_BtnClose.ChangeToolTipText(GlobalText[388], TRUE);
}

float CNewUIDuelLadder::GetLayerDepth()
{
    return 3.6f;
}

float CNewUIDuelLadder::GetKeyEventOrder()
{
    return 3.6f;
}

void CNewUIDuelLadder::SendRequest(BYTE op, BYTE arg)
{
    if (SocketClient == NULL)
        return;

    SocketClient->ToGameServer()->SendDuelLadderRequest(op, arg);
}

void CNewUIDuelLadder::Toggle()
{
    if (IsVisible())
    {
        Show(false);
        return;
    }

    Show(true);
    SendRequest(0, m_CurrentBracket); // initial top-10
    SendRequest(1, 0);                // own profile
}

void CNewUIDuelLadder::SetTopData(BYTE bracket, BYTE count, const BYTE* data, int dataLen)
{
    m_CurrentBracket = bracket;
    if (count > MAX_ENTRIES)
        count = MAX_ENTRIES;

    m_EntryCount = 0;
    for (int i = 0; i < count; i++)
    {
        const int off = i * ENTRY_BYTES;
        if (off + ENTRY_BYTES > dataLen)
            break;

        Entry& e = m_Entries[i];
        memcpy(e.name, data + off, NAME_LEN);
        e.name[NAME_LEN] = 0;
        for (int j = 0; j < NAME_LEN; j++)
        {
            if (e.name[j] != 0 && (unsigned char)e.name[j] < 0x20)
                e.name[j] = '?';
        }

        e.classNumber = data[off + NAME_LEN];
        e.rating = (unsigned int)data[off + NAME_LEN + 1]
            | ((unsigned int)data[off + NAME_LEN + 2] << 8)
            | ((unsigned int)data[off + NAME_LEN + 3] << 16)
            | ((unsigned int)data[off + NAME_LEN + 4] << 24);
        e.wins = (unsigned int)data[off + NAME_LEN + 5]
            | ((unsigned int)data[off + NAME_LEN + 6] << 8)
            | ((unsigned int)data[off + NAME_LEN + 7] << 16)
            | ((unsigned int)data[off + NAME_LEN + 8] << 24);
        e.losses = (unsigned int)data[off + NAME_LEN + 9]
            | ((unsigned int)data[off + NAME_LEN + 10] << 8)
            | ((unsigned int)data[off + NAME_LEN + 11] << 16)
            | ((unsigned int)data[off + NAME_LEN + 12] << 24);

        m_EntryCount++;
    }
}

void CNewUIDuelLadder::SetProfileData(BYTE bracket, BYTE tier, unsigned int rating, unsigned int wins, unsigned int losses, unsigned short rankInBracket)
{
    m_ProfileBracket = bracket;
    m_ProfileTier = tier;
    m_ProfileRating = rating;
    m_ProfileWins = wins;
    m_ProfileLosses = losses;
    m_ProfileRank = rankInBracket;
}

bool CNewUIDuelLadder::Update()
{
    if (IsVisible())
    {
        if (m_BtnTabRankings.UpdateMouseEvent())
        {
            m_CurrentTab = 0;
        }

        if (m_BtnTabProfile.UpdateMouseEvent())
        {
            m_CurrentTab = 1;
            SendRequest(1, 0);
        }

        for (int i = 0; i < 5; i++)
        {
            if (m_BtnBracket[i].UpdateMouseEvent())
            {
                m_CurrentBracket = (BYTE)(i + 1);
                m_CurrentTab = 0;
                SendRequest(0, m_CurrentBracket);
            }
        }

        if (m_BtnClose.UpdateMouseEvent())
        {
            g_pNewUISystem->Hide(INTERFACE_DUELLADDER);
        }
    }

    return true;
}

bool CNewUIDuelLadder::UpdateMouseEvent()
{
    if (!CheckMouseIn(m_Pos.x, m_Pos.y, WINDOW_WIDTH, WINDOW_HEIGHT))
        return true;

    return false;
}

bool CNewUIDuelLadder::UpdateKeyEvent()
{
    if (IsVisible())
    {
        if (IsPress(VK_ESCAPE) == true)
        {
            g_pNewUISystem->Hide(INTERFACE_DUELLADDER);
            return false;
        }
    }
    return true;
}

bool CNewUIDuelLadder::Render()
{
    EnableAlphaTest();
    glColor4f(1.f, 1.f, 1.f, 1.f);

    g_pRenderText->SetFont(g_hFont);
    g_pRenderText->SetTextColor(0xFFFFFFFF);
    g_pRenderText->SetBgColor(0);

    RenderImage(IMAGE_BASE_WINDOW_BACK, m_Pos.x, m_Pos.y, float(WINDOW_WIDTH), float(WINDOW_HEIGHT));
    RenderImage(IMAGE_BASE_WINDOW_TOP, m_Pos.x, m_Pos.y, float(WINDOW_WIDTH), 64.f);
    RenderImage(IMAGE_BASE_WINDOW_LEFT, m_Pos.x, m_Pos.y + 64.f, 21.f, float(WINDOW_HEIGHT) - 64.f - 45.f);
    RenderImage(IMAGE_BASE_WINDOW_RIGHT, m_Pos.x + float(WINDOW_WIDTH) - 21.f, m_Pos.y + 64.f, 21.f, float(WINDOW_HEIGHT) - 64.f - 45.f);
    RenderImage(IMAGE_BASE_WINDOW_BOTTOM, m_Pos.x, m_Pos.y + float(WINDOW_HEIGHT) - 45.f, float(WINDOW_WIDTH), 45.f);

    g_pRenderText->SetFont(g_hFontBold);
    g_pRenderText->RenderText(m_Pos.x, m_Pos.y + 13, L"Duel Ladder", WINDOW_WIDTH, 0, RT3_SORT_CENTER);
    g_pRenderText->SetFont(g_hFont);

    m_BtnTabRankings.Render();
    m_BtnTabProfile.Render();

    if (m_CurrentTab == 0)
    {
        for (int i = 0; i < 5; i++)
            m_BtnBracket[i].Render();

        g_pRenderText->SetTextColor(0xFFCCCCCC);
        g_pRenderText->RenderText(m_Pos.x + 22, m_Pos.y + 110, L"  # Name        Rating   W    L", WINDOW_WIDTH - 44, 0, RT3_SORT_LEFT);

        g_pRenderText->SetTextColor(0xFFFFFFFF);
        for (int i = 0; i < m_EntryCount; i++)
        {
            int rowY = m_Pos.y + 130 + i * 22;
            wchar_t wname[NAME_LEN + 1] = { 0 };
            for (int k = 0; k < NAME_LEN && m_Entries[i].name[k]; k++)
                wname[k] = (wchar_t)(unsigned char)m_Entries[i].name[k];

            wchar_t line[160];
            std::swprintf(line, 160, L"%2d. %-10s %5u  %4u %4u",
                i + 1, wname, m_Entries[i].rating, m_Entries[i].wins, m_Entries[i].losses);
            g_pRenderText->RenderText(m_Pos.x + 22, rowY, line, WINDOW_WIDTH - 44, 0, RT3_SORT_LEFT);
        }

        if (m_EntryCount == 0)
        {
            g_pRenderText->SetTextColor(0xFFAAAAAA);
            g_pRenderText->RenderText(m_Pos.x + 22, m_Pos.y + 150, L"(no ranked players yet in this bracket)", WINDOW_WIDTH - 44, 0, RT3_SORT_LEFT);
        }

        g_pRenderText->SetTextColor(0xFF88CCFF);
        wchar_t bracketLabel[32];
        std::swprintf(bracketLabel, 32, L"Bracket T%u", (unsigned)m_CurrentBracket);
        g_pRenderText->RenderText(m_Pos.x, m_Pos.y + 365, bracketLabel, WINDOW_WIDTH, 0, RT3_SORT_CENTER);
    }
    else
    {
        g_pRenderText->SetTextColor(0xFFFFFFFF);
        wchar_t line[80];
        int row = 0;
        std::swprintf(line, 80, L"Reset Bracket   :  T%u", (unsigned)m_ProfileBracket);
        g_pRenderText->RenderText(m_Pos.x + 40, m_Pos.y + 120 + row++ * 28, line, WINDOW_WIDTH - 80, 0, RT3_SORT_LEFT);
        std::swprintf(line, 80, L"Skill Tier      :  %s", GetDuelTierName(m_ProfileTier));
        g_pRenderText->RenderText(m_Pos.x + 40, m_Pos.y + 120 + row++ * 28, line, WINDOW_WIDTH - 80, 0, RT3_SORT_LEFT);
        std::swprintf(line, 80, L"Rating          :  %u", m_ProfileRating);
        g_pRenderText->RenderText(m_Pos.x + 40, m_Pos.y + 120 + row++ * 28, line, WINDOW_WIDTH - 80, 0, RT3_SORT_LEFT);
        std::swprintf(line, 80, L"Wins            :  %u", m_ProfileWins);
        g_pRenderText->RenderText(m_Pos.x + 40, m_Pos.y + 120 + row++ * 28, line, WINDOW_WIDTH - 80, 0, RT3_SORT_LEFT);
        std::swprintf(line, 80, L"Losses          :  %u", m_ProfileLosses);
        g_pRenderText->RenderText(m_Pos.x + 40, m_Pos.y + 120 + row++ * 28, line, WINDOW_WIDTH - 80, 0, RT3_SORT_LEFT);
        std::swprintf(line, 80, L"Rank in Bracket :  #%u", (unsigned)m_ProfileRank);
        g_pRenderText->RenderText(m_Pos.x + 40, m_Pos.y + 120 + row++ * 28, line, WINDOW_WIDTH - 80, 0, RT3_SORT_LEFT);
    }

    m_BtnClose.Render();

    DisableAlphaBlend();
    return true;
}
