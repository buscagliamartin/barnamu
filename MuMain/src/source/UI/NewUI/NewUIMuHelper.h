#pragma once

#include <array>
#include <vector>

#include "UI/NewUI/NewUIBase.h"
#include "UI/NewUI/NewUIManager.h"
#include "UI/NewUI/NewUI3DRenderMng.h"
#include "UI/NewUI/Widgets/NewUIButton.h"
#include "MUHelper/MuHelper.h"

namespace SEASON3B
{
    class CNewUIMuHelper : public CNewUIObj
    {
    public:
        CNewUIMuHelper();
        ~CNewUIMuHelper();

        bool Create(CNewUIManager* pNewUIMng, int x, int y);
        void Release();

        void Show(bool bShow);
        bool Render();
        bool Update();
        bool UpdateMouseEvent();
        bool UpdateKeyEvent();

        float GetLayerDepth();
        float GetKeyEventOrder();

    public:
        void Reset();
        void LoadSavedConfig(const MUHelper::ConfigData& config);
        void AssignSkill(int iSkill);
        static int GetIntFromTextInput(wchar_t* pstrInput);

    private:
        enum IMAGE_LIST
        {
            IMAGE_BASE_WINDOW_BACK = BITMAP_INTERFACE_NEW_MESSAGEBOX_BEGIN + 3,				//. newui_msgbox_back.jpg
            IMAGE_BASE_WINDOW_TOP = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN,			//. newui_item_back01.tga	(190,64)
            IMAGE_BASE_WINDOW_LEFT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 2,		//. newui_item_back02-l.tga	(21,320)
            IMAGE_BASE_WINDOW_RIGHT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 3,		//. newui_item_back02-r.tga	(21,320)
            IMAGE_BASE_WINDOW_BOTTOM = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 4,	//. newui_item_back03.tga	(190,45)
            IMAGE_BASE_WINDOW_BTN_EXIT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 17,		//. newui_exit_00.tga
            //--
            IMAGE_TABLE_SQUARE = BITMAP_INTERFACE_NEW_INVENTORY_BASE_BEGIN,	//. newui_item_box.tga
            IMAGE_TABLE_TOP_LEFT,			//. newui_item_table01(L).tga (14,14)
            IMAGE_TABLE_TOP_RIGHT,			//. newui_item_table01(R).tga (14,14)
            IMAGE_TABLE_BOTTOM_LEFT,		//. newui_item_table02(L).tga (14,14)
            IMAGE_TABLE_BOTTOM_RIGHT,	//. newui_item_table02(R).tga (14,14)
            IMAGE_TABLE_TOP_PIXEL,			//. newui_item_table03(up).tga (1, 14)
            IMAGE_TABLE_BOTTOM_PIXEL,	//. newui_item_table03(dw).tga (1,14)
            IMAGE_TABLE_LEFT_PIXEL,		//. newui_item_table03(L).tga (14,1)
            IMAGE_TABLE_RIGHT_PIXEL,		//. newui_item_table03(R).tga (14,1)
            //--
            IMAGE_WINDOW_TAB_BTN = BITMAP_GUILDINFO_BEGIN,
            //--
            IMAGE_MACROUI_HELPER_OPTIONBUTTON = BITMAP_INTERFACE_MACROUI_BEGIN + 1,		// newui_position02.tga			(70, 25)
            IMAGE_MACROUI_HELPER_INPUTNUMBER = BITMAP_INTERFACE_MACROUI_BEGIN + 2,
            IMAGE_MACROUI_HELPER_INPUTSTRING = BITMAP_INTERFACE_MACROUI_BEGIN + 3,
            //-- Buttons
            IMAGE_CHAINFO_BTN_STAT = BITMAP_INTERFACE_NEW_CHAINFO_WINDOW_BEGIN + 1,
            IMAGE_CLEARNESS_BTN = BITMAP_CURSEDTEMPLE_BEGIN + 4,
            IMAGE_IGS_BUTTON = BITMAP_IGS_MSGBOX_BUTTON,
            IMAGE_CHECKBOX_BTN = BITMAP_OPTION_BEGIN + 5,

            //-- Skills
            IMAGE_SKILL1 = BITMAP_INTERFACE_NEW_SKILLICON_BEGIN,
            IMAGE_SKILL2,
            IMAGE_COMMAND,
            IMAGE_SKILL3,
            IMAGE_SKILLBOX,
            IMAGE_SKILLBOX_USE,
            IMAGE_NON_SKILL1,
            IMAGE_NON_SKILL2,
            IMAGE_NON_COMMAND,
            IMAGE_NON_SKILL3,
        };

        enum CLASS_LIST_ {
            Dark_Wizard = 0,
            Dark_Knight,
            Fairy_Elf,
            Magic_Gladiator,
            Dark_Lord,
            Summoner,
            Rage_Fighter,
        };

        static constexpr int MAX_SKILLS_SLOT = 6;
        static constexpr int WINDOW_WIDTH = 190;
        static constexpr int WINDOW_HEIGHT = 429;

        typedef struct
        {
            int iNumTab;
            BYTE class_character[MAX_CLASS];
            CNewUIButton* btn;
        } CButtonTap;

        typedef struct
        {
            int iNumTab;
            BYTE class_character[MAX_CLASS];
            CNewUICheckBox* box;
        } CheckBoxTap;

        typedef struct
        {
            int iNumTab;
            int s_ImgIndex;
            POINT m_Pos;
            POINT m_Size;
            BYTE class_character[MAX_CLASS];
        } cTexture;

        typedef struct
        {
            int iNumTab;
            POINT m_Pos;
            std::wstring m_Name;
            BYTE class_character[MAX_CLASS];
        } cTextName;

        typedef std::map<int, CButtonTap> cButtonMap;
        typedef std::map<int, CheckBoxTap> cCheckBoxMap;
        typedef std::map<int, cTexture> cTextureMap;
        typedef std::map<int, cTextName> cTextNameMap;

    private:
        void RenderBtnList();
        int UpdateMouseBtnList();
        void RegisterBtnCharacter(BYTE class_character, int Identifier);
        void RegisterButton(int Identifier, CButtonTap button);
        void InsertButton(int imgindex, int x, int y, int sx, int sy, bool overflg, bool isimgwidth, bool bClickEffect, bool MoveTxt,std::wstring btname,std::wstring tooltiptext, int Identifier, int iNumTab);
        //--
        void RenderBoxList();
        int UpdateMouseBoxList();
        void RegisterBoxCharacter(BYTE class_character, int Identifier);
        void RegisterCheckBox(int Identifier, CheckBoxTap button);
        void InsertCheckBox(int imgindex, int x, int y, int sx, int sy, bool overflg,std::wstring btname, int Identifier, int iNumTab);
        //--
        void RenderIconList();
        int UpdateMouseIconList();
        void RegisterIconCharacter(BYTE class_character, int Identifier);
        void RegisterIcon(int Identifier, cTexture button);
        void InsertIcon(int imgindex, int x, int y, int sx, int sy, int Identifier, int iNumTab);
        //--
        void RenderTextList();
        void RegisterTextCharacter(BYTE class_character, int Identifier);
        void RegisterText(int Identifier, cTextName button);
        void InsertText(int x, int y,std::wstring Name, int Identifier, int iNumTab);

        void InitText();
        void InitImage();
        void InitButtons();
        void InitCheckBox();
        void InitTextboxInput();
        void SetPos(int x, int y);
        void RenderBack(int x, int y, int width, int height);

        int GetSkillIndex(int iSkill);
        bool IsSkillAssigned(int iSkill);
        void RenderSkillIcon(int iSkill, float x, float y, float width, float height);
        
        void InitConfig();
        void SaveConfig();
        void ApplyConfig();

        void LoadImages();
        void UnloadImages();

        void ApplyConfigFromCheckbox(int iCheckboxId, bool bState);
        void ApplyConfigFromSkillSlot(int iSlot, int iSkill);
        void SaveExtraItem();
        void RemoveExtraItem();

    private:
        CNewUIManager* m_pNewUIMng;
        CUITextInputBox m_Skill2DelayInput;
        CUITextInputBox m_Skill3DelayInput;
        CUITextInputBox m_ItemInput;
        CUIExtraItemListBox m_ItemFilter;

        POINT m_Pos;
        POINT m_SubPos;
        CNewUIRadioGroupButton m_TabBtn;
        int m_iCurrentOpenTab;
        int m_iCurrentOpenSubWin;
        bool m_bSubWinOpen;
        cButtonMap m_ButtonList;
        cCheckBoxMap m_CheckBoxList;
        cTextNameMap m_TextNameList;
        cTextureMap m_IconList;
        int m_iSelectedSkillSlot;
        std::array<int, MAX_SKILLS_SLOT> m_aiSelectedSkills;
    };

    class CNewUIMuHelperSkillList : public CNewUIObj
    {

    public:
        CNewUIMuHelperSkillList();
        ~CNewUIMuHelperSkillList();

        bool Create(CNewUIManager* pNewUIMng, CNewUI3DRenderMng* pNewUI3DRenderMng);
        void Release();

        bool UpdateMouseEvent();
        bool UpdateKeyEvent();
        bool Update();
        bool Render();
        void RenderSkillInfo();
        float GetLayerDepth();

        void Reset();

        int UpdateMouseSkillList();
        void FilterByAttackSkills();
        void FilterByBuffSkills();

        static void UI2DEffectCallback(LPVOID pClass, DWORD dwParamA, DWORD dwParamB);

    private:
        enum IMAGE_LIST
        {
            IMAGE_SKILL1 = BITMAP_INTERFACE_NEW_SKILLICON_BEGIN,
            IMAGE_SKILL2,
            IMAGE_COMMAND,
            IMAGE_SKILL3,
            IMAGE_SKILLBOX,
            IMAGE_SKILLBOX_USE,
            IMAGE_NON_SKILL1,
            IMAGE_NON_SKILL2,
            IMAGE_NON_COMMAND,
            IMAGE_NON_SKILL3,
        };

        enum EVENT_STATE
        {
            EVENT_NONE = 0,

            EVENT_BTN_HOVER_SKILLLIST,
            EVENT_BTN_DOWN_SKILLLIST,
        };

        typedef struct
        {
            int skillId;    // ActionSkillType value
            int slotIndex;  // index into CharacterAttribute->Skill[] for tooltip lookup
            POINT location;
            SIZE area;
            bool isVisible;
        } cSkillIcon;

    private:
        void LoadImages();
        void UnloadImages();

        void PrepareSkillsToRender();
        void RenderSkillIcon(int iIndex, float x, float y, float width, float height);

        bool IsAttackSkill(int iSkillType);
        bool IsBuffSkill(int iSkillType);
        bool IsHealingSkill(int iSkillType);
        bool IsDefenseSkill(int iSkillType);

    private:
        std::map<int, cSkillIcon> m_skillIconMap;

        CNewUIManager* m_pNewUIMng;
        CNewUI3DRenderMng* m_pNewUI3DRenderMng;

        bool m_bFilterByAttackSkills;
        bool m_bFilterByBuffSkills;
        bool m_bRenderSkillInfo;
        int m_iRenderSkillInfoType;
        int m_iRenderSkillInfoPosX;
        int m_iRenderSkillInfoPosY;
        std::vector<int> m_aiSkillsToRender;   // skill type values
        std::vector<int> m_aiSkillSlots;        // parallel: CharacterAttribute->Skill[] slot index

        EVENT_STATE m_EventState;
    };

    class CNewUIMuHelperExt : public CNewUIObj
    {
    public:
        CNewUIMuHelperExt();
        ~CNewUIMuHelperExt();

        bool Create(CNewUIManager* pNewUIMng, int x, int y);
        void Release();

        bool Render();
        bool Update();
        bool UpdateMouseEvent();
        bool UpdateKeyEvent();

        float GetLayerDepth();
        float GetKeyEventOrder();

    public:
        void Toggle(int iPage);
        void Save();
        void Reset();
        void ApplySavedConfig();
        void InitConfig();

    private:
        enum IMAGE_LIST
        {
            IMAGE_BASE_WINDOW_BACK = BITMAP_INTERFACE_NEW_MESSAGEBOX_BEGIN + 3,				//. newui_msgbox_back.jpg
            IMAGE_BASE_WINDOW_TOP = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN,			//. newui_item_back01.tga	(190,64)
            IMAGE_BASE_WINDOW_LEFT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 2,		//. newui_item_back02-l.tga	(21,320)
            IMAGE_BASE_WINDOW_RIGHT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 3,		//. newui_item_back02-r.tga	(21,320)
            IMAGE_BASE_WINDOW_BOTTOM = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 4,	//. newui_item_back03.tga	(190,45)
            IMAGE_BASE_WINDOW_BTN_EXIT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 17,		//. newui_exit_00.tga
            //--
            IMAGE_TABLE_SQUARE = BITMAP_INTERFACE_NEW_INVENTORY_BASE_BEGIN,	//. newui_item_box.tga
            IMAGE_TABLE_TOP_LEFT,			//. newui_item_table01(L).tga (14,14)
            IMAGE_TABLE_TOP_RIGHT,			//. newui_item_table01(R).tga (14,14)
            IMAGE_TABLE_BOTTOM_LEFT,		//. newui_item_table02(L).tga (14,14)
            IMAGE_TABLE_BOTTOM_RIGHT,	//. newui_item_table02(R).tga (14,14)
            IMAGE_TABLE_TOP_PIXEL,			//. newui_item_table03(up).tga (1, 14)
            IMAGE_TABLE_BOTTOM_PIXEL,	//. newui_item_table03(dw).tga (1,14)
            IMAGE_TABLE_LEFT_PIXEL,		//. newui_item_table03(L).tga (14,1)
            IMAGE_TABLE_RIGHT_PIXEL,		//. newui_item_table03(R).tga (14,1)
            //--
            IMAGE_WINDOW_TAB_BTN = BITMAP_GUILDINFO_BEGIN,
            //--
            IMAGE_MACROUI_HELPER_OPTIONBUTTON = BITMAP_INTERFACE_MACROUI_BEGIN + 1,		// newui_position02.tga			(70, 25)
            IMAGE_MACROUI_HELPER_INPUTNUMBER = BITMAP_INTERFACE_MACROUI_BEGIN + 2,
            IMAGE_MACROUI_HELPER_INPUTSTRING = BITMAP_INTERFACE_MACROUI_BEGIN + 3,
            //-- Buttons
            IMAGE_CHAINFO_BTN_STAT = BITMAP_INTERFACE_NEW_CHAINFO_WINDOW_BEGIN + 1,
            IMAGE_CLEARNESS_BTN = BITMAP_CURSEDTEMPLE_BEGIN + 4,
            IMAGE_IGS_BUTTON = BITMAP_IGS_MSGBOX_BUTTON,

            IMAGE_OPTION_BTN_CHECK = BITMAP_OPTION_BEGIN + 5,
            IMAGE_OPTION_VOLUME_BACK = BITMAP_OPTION_BEGIN + 8,
            IMAGE_OPTION_VOLUME_COLOR = BITMAP_OPTION_BEGIN + 9,

        };

        static constexpr int WINDOW_WIDTH = 190;
        static constexpr int WINDOW_HEIGHT = 429;

    private:
        void InitText();
        void InitImage();
        void InitButtons();
        void InitCheckBox();
        void SetPos(int x, int y);
        void RenderBackPane(int x, int y, int width, int height, const wchar_t* pszHeader);
        void RenderHpLevel(int x, int y, int width, int height, int level, const wchar_t* pszLabel);
        void LoadImages();
        void UnloadImages();

    private:
        CNewUIManager* m_pNewUIMng;

        POINT m_Pos;
        CNewUICheckBox m_BtnPreConHuntRange;
        CNewUICheckBox m_BtnPreConAttacking;

        CNewUICheckBox m_BtnSubConMoreThanTwo;
        CNewUICheckBox m_BtnSubConMoreThanThree;
        CNewUICheckBox m_BtnSubConMoreThanFour;
        CNewUICheckBox m_BtnSubConMoreThanFive;

        CNewUICheckBox m_BtnPartyHeal;
        CNewUICheckBox m_BtnPartyDuration;
        CUITextInputBox m_BuffTimeInput;

        CNewUIButton m_BtnSave;
        CNewUIButton m_BtnReset;
        CNewUIButton m_BtnClose;

    private:
        int m_iCurrentPage;
        int m_iCurrentHealThreshold;
        int m_iCurrentPartyHealThreshold;
        int m_iCurrentPotionThreshold;
    };

    // BarnaMu: per-account Jewel Bank window, opened from the MU Helper menu.
    class CNewUIJewelBank : public CNewUIObj, public INewUI3DRenderObj
    {
    public:
        CNewUIJewelBank();
        ~CNewUIJewelBank();

        bool Create(CNewUIManager* pNewUIMng, CNewUI3DRenderMng* pNewUI3DRenderMng, int x, int y);
        void Release();

        bool Render();
        void Render3D();
        bool Update();
        bool UpdateMouseEvent();
        bool UpdateKeyEvent();
        bool IsVisible() const override;

        float GetLayerDepth();
        float GetKeyEventOrder();

    public:
        void Toggle();
        void SetBalances(const unsigned int* pBalances);

        static constexpr int ITEM_COUNT = 17;

    public:
        enum IMAGE_LIST
        {
            IMAGE_BASE_WINDOW_BACK = BITMAP_INTERFACE_NEW_MESSAGEBOX_BEGIN + 3,
            IMAGE_BASE_WINDOW_TOP = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN,
            IMAGE_BASE_WINDOW_LEFT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 2,
            IMAGE_BASE_WINDOW_RIGHT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 3,
            IMAGE_BASE_WINDOW_BOTTOM = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 4,
            IMAGE_BASE_WINDOW_BTN_EXIT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 17,
            IMAGE_ITEM_BOX = BITMAP_INTERFACE_NEW_INVENTORY_BASE_BEGIN,
            IMAGE_TABLE_TOP_LEFT,
            IMAGE_TABLE_TOP_RIGHT,
            IMAGE_TABLE_BOTTOM_LEFT,
            IMAGE_TABLE_BOTTOM_RIGHT,
            IMAGE_TABLE_TOP_PIXEL,
            IMAGE_TABLE_BOTTOM_PIXEL,
            IMAGE_TABLE_LEFT_PIXEL,
            IMAGE_TABLE_RIGHT_PIXEL,
            IMAGE_IGS_BUTTON = BITMAP_IGS_MSGBOX_BUTTON,
            IMAGE_ROUND_BUTTON = BITMAP_CATAPULT_BEGIN + 1,
            IMAGE_JEWEL_BANK_BACK = BITMAP_EFFECT_TEXTURE_END - 1,
        };

    private:
        static constexpr int WINDOW_WIDTH = 620;
        static constexpr int WINDOW_HEIGHT = 430;
        static constexpr int ROW_START_Y = 64;
        static constexpr int ROW_HEIGHT = 20;

        void SetPos(int x, int y);
        void InitButtons();
        void LoadImages();
        void UnloadImages();
        void SendRequest(BYTE op, BYTE arg1, WORD arg2, WORD arg3);
        void RenderBack();
        void RenderTable();
        void RenderBankButton(CNewUIButton& button, const wchar_t* glyph);

    private:
        CNewUIManager* m_pNewUIMng;
        CNewUI3DRenderMng* m_pNewUI3DRenderMng;
        POINT m_Pos;
        unsigned int m_Balances[ITEM_COUNT];
        CNewUIButton m_BtnDepSingle[ITEM_COUNT];
        CNewUIButton m_BtnDepPack[ITEM_COUNT];
        CNewUIButton m_BtnWdrSingle[ITEM_COUNT];
        CNewUIButton m_BtnWdrPack[ITEM_COUNT];
        CNewUIButton m_BtnClose;
    };

    // BarnaMu: account-wide Auction House window, replacing the personal store player path.
    class CNewUIAuctionHouse : public CNewUIObj, public INewUI3DRenderObj
    {
    public:
        struct ListingView
        {
            unsigned int ListingNumber;
            unsigned int Price;
            unsigned short ItemType;
            BYTE ItemLevel;
            BYTE Currency;
            BYTE JewelSlot;
            BYTE Status;
            wchar_t ItemName[48];
            wchar_t SellerName[12];
        };

        CNewUIAuctionHouse();
        ~CNewUIAuctionHouse();

        bool Create(CNewUIManager* pNewUIMng, CNewUI3DRenderMng* pNewUI3DRenderMng, int x, int y);
        void Release();

        bool Render();
        void Render3D();
        bool Update();
        bool UpdateMouseEvent();
        bool UpdateKeyEvent();
        bool IsVisible() const override;

        float GetLayerDepth();
        float GetKeyEventOrder();

        void Toggle();
        void SetListingsHeader(BYTE view, BYTE page, BYTE count);
        void AddListing(const ListingView& listing);
        void SetStatusMessage(const wchar_t* message);
        bool IsCreateListingView() const;
        bool TrySetCreateListingItemFromInventorySlot(int slot);

    public:
        enum IMAGE_LIST
        {
            IMAGE_BASE_WINDOW_BTN_EXIT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 17,
            IMAGE_TABLE_TOP_LEFT = BITMAP_EFFECT_TEXTURE_BEGIN + 55,
            IMAGE_TABLE_TOP_RIGHT,
            IMAGE_TABLE_BOTTOM_LEFT,
            IMAGE_TABLE_BOTTOM_RIGHT,
            IMAGE_TABLE_TOP_PIXEL,
            IMAGE_TABLE_BOTTOM_PIXEL,
            IMAGE_TABLE_LEFT_PIXEL,
            IMAGE_TABLE_RIGHT_PIXEL,
            IMAGE_IGS_BUTTON = BITMAP_IGS_MSGBOX_BUTTON,
            IMAGE_ROUND_BUTTON = BITMAP_CATAPULT_BEGIN + 2,
            IMAGE_AUCTION_HOUSE_BACK = BITMAP_EFFECT_TEXTURE_END - 2,
        };

    private:
        static constexpr int WINDOW_WIDTH = 430;
        static constexpr int WINDOW_HEIGHT = 286;
        static constexpr int MAX_ROWS = 10;

        void SetPos(int x, int y);
        void LoadImages();
        void UnloadImages();
        void InitButtons();
        void SendRequest(BYTE op, BYTE arg1, BYTE currency, BYTE jewelSlot, unsigned int arg2, unsigned int arg3);
        void RequestCurrentView();
        void SetView(BYTE view);
        void SetFilter(BYTE filter);
        void ClearCreateSelection();
        void AddSelectedInventoryItemToCreateListing();
        void SetStatusMessages(const wchar_t* line1, const wchar_t* line2);
        bool ProcessMouseButtons();
        bool SendSelectedListingAction(BYTE op, const wchar_t* action);
        bool SendCreateListingAction();
        bool TryGetSellPrice(unsigned int& price) const;
        int GetSelectedInventorySlot() const;
        void RenderBack();
        void RenderTable();
        void RenderFilters();
        void RenderDetails();
        void RenderCreateListing();
        void RenderFooter();
        void RenderContextAction();
        void RenderListingActionButton(CNewUIButton& button, const wchar_t* text, bool enabled, int tone);
        const wchar_t* GetCurrencyText(BYTE currency, BYTE jewelSlot) const;
        const wchar_t* GetStatusText(BYTE status) const;
        const wchar_t* GetViewTitle() const;
        const wchar_t* GetActionText() const;

    private:
        CNewUIManager* m_pNewUIMng;
        CNewUI3DRenderMng* m_pNewUI3DRenderMng;
        POINT m_Pos;
        BYTE m_CurrentView;
        BYTE m_CurrentPage;
        BYTE m_Filter;
        BYTE m_SellCurrency;
        BYTE m_SellJewelSlot;
        int m_CreateSlot;
        short m_CreateItemType;
        int m_CreateItemLevel;
        int m_SelectedRow;
        int m_HoveredRow;
        int m_RowCount;
        ListingView m_Listings[MAX_ROWS];
        wchar_t m_StatusMessage[128];
        wchar_t m_StatusMessage2[128];
        CUITextInputBox m_PriceInput;
        CNewUIButton m_BtnHelp;
        CNewUIButton m_BtnMinimize;
        CNewUIButton m_BtnBrowse;
        CNewUIButton m_BtnMine;
        CNewUIButton m_BtnDeliveries;
        CNewUIButton m_BtnPayouts;
        CNewUIButton m_BtnCreate;
        CNewUIButton m_BtnMyBidsDisabled;
        CNewUIButton m_BtnWatchlistDisabled;
        CNewUIButton m_BtnHistoryDisabled;
        CNewUIButton m_BtnRefresh;
        CNewUIButton m_BtnPrev;
        CNewUIButton m_BtnNext;
        CNewUIButton m_BtnFilterAll;
        CNewUIButton m_BtnFilterZen;
        CNewUIButton m_BtnFilterWCoin;
        CNewUIButton m_BtnFilterJewel;
        CNewUIButton m_BtnFilterApply;
        CNewUIButton m_BtnFilterReset;
        CNewUIButton m_BtnAdvancedFiltersDisabled;
        CNewUIButton m_BtnSellZen;
        CNewUIButton m_BtnSellWCoin;
        CNewUIButton m_BtnSellJewel;
        CNewUIButton m_BtnSellJewelType;
        CNewUIButton m_BtnAddItem;
        CNewUIButton m_BtnClearItem;
        CNewUIButton m_BtnPostListing;
        CNewUIButton m_BtnBuy;
        CNewUIButton m_BtnCancelListing;
        CNewUIButton m_BtnReceiveItem;
        CNewUIButton m_BtnClaimPayout;
        CNewUIButton m_BtnPlaceBidDisabled;
        CNewUIButton m_BtnCompareDisabled;
        CNewUIButton m_BtnReportDisabled;
        CNewUIButton m_BtnClose;
    };

    // BarnaMu: PvP Duel Ladder window, opened with the L hotkey.
    class CNewUIDuelLadder : public CNewUIObj
    {
    public:
        CNewUIDuelLadder();
        ~CNewUIDuelLadder();

        bool Create(CNewUIManager* pNewUIMng, int x, int y);
        void Release();

        bool Render();
        bool Update();
        bool UpdateMouseEvent();
        bool UpdateKeyEvent();

        float GetLayerDepth();
        float GetKeyEventOrder();

    public:
        void Toggle();
        void SetTopData(BYTE bracket, BYTE count, const BYTE* data, int dataLen);
        void SetProfileData(BYTE bracket, BYTE tier, unsigned int rating, unsigned int wins, unsigned int losses, unsigned short rankInBracket);

        static constexpr int MAX_ENTRIES = 10;
        static constexpr int NAME_LEN = 10;
        static constexpr int ENTRY_BYTES = NAME_LEN + 1 + 4 + 4 + 4; // 23

    private:
        enum IMAGE_LIST
        {
            IMAGE_BASE_WINDOW_BACK = BITMAP_INTERFACE_NEW_MESSAGEBOX_BEGIN + 3,
            IMAGE_BASE_WINDOW_TOP = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN,
            IMAGE_BASE_WINDOW_LEFT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 2,
            IMAGE_BASE_WINDOW_RIGHT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 3,
            IMAGE_BASE_WINDOW_BOTTOM = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 4,
            IMAGE_BASE_WINDOW_BTN_EXIT = BITMAP_INTERFACE_NEW_PERSONALINVENTORY_BEGIN + 17,
            IMAGE_IGS_BUTTON = BITMAP_IGS_MSGBOX_BUTTON,
        };

        static constexpr int WINDOW_WIDTH = 380;
        static constexpr int WINDOW_HEIGHT = 440;

        struct Entry
        {
            char name[NAME_LEN + 1];
            BYTE classNumber;
            unsigned int rating;
            unsigned int wins;
            unsigned int losses;
        };

        void SetPos(int x, int y);
        void InitButtons();
        void LoadImages();
        void UnloadImages();
        void SendRequest(BYTE op, BYTE arg);

    private:
        CNewUIManager* m_pNewUIMng;
        POINT m_Pos;

        int m_CurrentTab;       // 0 = Rankings, 1 = My Profile
        BYTE m_CurrentBracket;  // 1..5
        int m_EntryCount;
        Entry m_Entries[MAX_ENTRIES];

        BYTE m_ProfileBracket;
        BYTE m_ProfileTier;
        unsigned int m_ProfileRating;
        unsigned int m_ProfileWins;
        unsigned int m_ProfileLosses;
        unsigned short m_ProfileRank;

        CNewUIButton m_BtnTabRankings;
        CNewUIButton m_BtnTabProfile;
        CNewUIButton m_BtnBracket[5];
        CNewUIButton m_BtnClose;
    };

}
