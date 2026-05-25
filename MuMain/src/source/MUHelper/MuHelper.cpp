#include "stdafx.h"

#include <thread>
#include <atomic>
#include <chrono>
#include <cmath>

#include "Engine/AI/ZzzAI.h"
#include "Engine/Object/ZzzCharacter.h"
#include "Engine/Object/ZzzInterface.h"
#include "UI/NewUI/NewUISystem.h"
#include "Core/Utilities/Log/muConsoleDebug.h"
#include "GameLogic/Skills/SkillManager.h"
#include "GameLogic/Social/PartyManager.h"
#include "World/MapInfra/MapManager.h"
#include "Network/Server/WSclient.h"

#include "MuHelper.h"

constexpr int MAX_ACTIONABLE_DISTANCE = 10;
constexpr int DEFAULT_DURABILITY_THRESHOLD = 50;
constexpr float BASIC_ATTACK_DISTANCE = 1.5f;
constexpr int MUHELPER_FULL_WORK_INTERVAL_MS = 250;
constexpr int MUHELPER_FULL_WORK_TICKS = MUHELPER_FULL_WORK_INTERVAL_MS / MUHelper::MUHELPER_TIMER_INTERVAL_MS;

SpinLock _targetsLock;
SpinLock _itemsLock;

// Movement/target globals are defined in ZzzInterface.cpp.
extern MovementSkill g_MovementSkill;
extern int SelectedCharacter;
extern int TargetX;
extern int TargetY;
extern int Attacking;
extern int ActionTarget;

namespace MUHelper
{
	MovementSkill& g_MovementSkill = ::g_MovementSkill;
	int& SelectedCharacter = ::SelectedCharacter;
	int& TargetX = ::TargetX;
	int& TargetY = ::TargetY;
	int& Attacking = ::Attacking;
	int& ActionTarget = ::ActionTarget;

    CMuHelper g_MuHelper;

    namespace
    {
    bool IsBuffActive(CHARACTER* pTargetChar, eBuffState buff)
    {
        return g_isCharacterBuff((&pTargetChar->Object), buff) != FALSE;
    }

    bool ShouldCastTimedBuff(CHARACTER* pTargetChar, eBuffState buff, bool bTimerActivatedBuffOngoing)
    {
        return !IsBuffActive(pTargetChar, buff) || bTimerActivatedBuffOngoing;
    }

    bool IsRageFighterBuffSkill(ActionSkillType iSkill)
    {
        switch (gSkillManager.MasterSkillToBaseSkillIndex(iSkill))
        {
        case AT_SKILL_ATT_UP_OURFORCES:
        case AT_SKILL_HP_UP_OURFORCES:
        case AT_SKILL_DEF_UP_OURFORCES:
            return true;
        default:
            return false;
        }
    }

    int SimulateRageFighterBuffSkill(ActionSkillType iSkill)
    {
        const int iSkillIndex = g_pSkillList->GetSkillIndex(iSkill);
        if (iSkillIndex == -1)
        {
            return 0;
        }

        if (!gSkillManager.AreSkillAttributeRequirementsMet(iSkill)
            || !CheckSkillUseCondition(&Hero->Object, iSkill)
            || !CheckMana(Hero, iSkill))
        {
            return 0;
        }

        Hero->CurrentSkill = iSkillIndex;
        g_MovementSkill.m_iSkill = iSkillIndex;
        g_MovementSkill.m_bMagic = true;
        g_MovementSkill.m_iTarget = -1;
        SelectedCharacter = -1;
        TargetX = Hero->PositionX;
        TargetY = Hero->PositionY;

        UseSkillRagefighter(Hero, &Hero->Object);
        return 1;
    }
    }

    void CALLBACK CMuHelper::TimerProc(HWND hwnd, UINT uMsg, UINT_PTR idEvent, DWORD dwTime)
    {
        g_MuHelper.WorkLoop(hwnd, uMsg, idEvent, dwTime);
    }

    void CMuHelper::Save(const ConfigData& config)
    {
        m_config = config;

        PRECEIVE_MUHELPER_DATA netData;
        ConfigDataSerDe::Serialize(m_config, netData);

        SocketClient->ToGameServer()->SendMuHelperSaveDataRequest(reinterpret_cast<BYTE*>(&netData), sizeof(netData));
    }

    void CMuHelper::Load(const ConfigData& config)
    {
        m_config = config;
    }

    ConfigData CMuHelper::GetConfig() const {
        return m_config;
    }

    void CMuHelper::Toggle()
    {
        if (m_bActive)
        {
            TriggerStop();
        }
        else
        {
            TriggerStart();
        }
    }

    void CMuHelper::TriggerStart()
    {
        if (!Hero->SafeZone)
            SocketClient->ToGameServer()->SendMuHelperStatusChangeRequest(0);
    }

    void CMuHelper::TriggerStop()
    {
        SocketClient->ToGameServer()->SendMuHelperStatusChangeRequest(1);
    }

    void CMuHelper::Start()
    {
        if (m_bActive)
        {
            return;
        }

        m_iTotalCost = 0;
        m_iComboState = 0;
        m_iCurrentBuffIndex = 0;
        m_iCurrentBuffPartyIndex = 0;
        m_iCurrentHealPartyIndex = 0;
        m_iCurrentTarget = -1;
        m_iCurrentSkill = (ActionSkillType)m_config.aiSkill[0];
        m_iCurrentItem = MAX_ITEMS;

        m_iSecondsElapsed = 0;
        m_iElapsedMilliseconds = 0;
        m_iLastBuffTimerSecond = -1;
        m_mapLastBuffCastSecond.clear();

        m_bTimerActivatedBuffOngoing = false;
        m_bPetActivated = false;

        m_iLoopCounter = MUHELPER_FULL_WORK_TICKS - 1;

        m_bActive = true;
        g_ConsoleDebug->Write(MCD_NORMAL, L"[MU Helper] Started");
    }

    void CMuHelper::Stop()
    {
        m_bActive = false;
        g_ConsoleDebug->Write(MCD_NORMAL, L"[MU Helper] Stopped");
    }

    void CMuHelper::WorkLoop(HWND hWnd, UINT uMsg, UINT_PTR idEvent, DWORD dwTime)
    {
        if (!m_bActive)
        {
            return;
        }

        if (Hero->SafeZone)
        {
            g_ConsoleDebug->Write(MCD_NORMAL, L"[MU Helper] Entered safezone. Stopping.");
            TriggerStop();
            return;
        }

        m_iElapsedMilliseconds += MUHELPER_TIMER_INTERVAL_MS;
        while (m_iElapsedMilliseconds >= 1000)
        {
            m_iSecondsElapsed++;
            m_iElapsedMilliseconds -= 1000;
        }

        if (++m_iLoopCounter >= MUHELPER_FULL_WORK_TICKS)
        {
            m_iLoopCounter = 0;
            Work();
        }
        else
        {
            try
            {
                Attack();
            }
            catch (...)
            {
                g_ConsoleDebug->Write(MCD_NORMAL, L"[MU Helper] Attack tick exception occurred. Ignoring...");
            }
        }
    }

    void CMuHelper::Work()
    {
        try
        {
            if (!ActivatePet())
            {
                return;
            }

            if (!Buff())
            {
                return;
            }

            if (!RecoverHealth())
            {
                return;
            }

            if (!ObtainItem())
            {
                return;
            }

            Attack();

            RepairEquipments();
        }
        catch (...)
        {
            g_ConsoleDebug->Write(MCD_NORMAL, L"[MU Helper] Exception occurred. Ignoring...");
        }
    }

    void CMuHelper::AddTarget(int iTargetId, bool bIsAttacking)
    {
        if (!m_bActive)
        {
            return;
        }

        CHARACTER* pTarget = FindCharacterByKey(iTargetId);
        if (!pTarget || pTarget == Hero || !IsMonster(pTarget))
        {
            return;
        }

        _targetsLock.lock();

        m_setTargets.insert(iTargetId);

        if (bIsAttacking)
        {
            m_setTargetsAttacking.insert(iTargetId);
        }

        _targetsLock.unlock();

        if (m_config.bUseSelfDefense)
        {
            m_iCurrentTarget = iTargetId;
        }
    }

    void CMuHelper::DeleteTarget(int iTargetId)
    {
        _targetsLock.lock();

        m_setTargets.erase(iTargetId);
        m_setTargetsAttacking.erase(iTargetId);

        _targetsLock.unlock();

        if (iTargetId == m_iCurrentTarget)
        {
            m_iCurrentTarget = -1;
        }
    }

    void CMuHelper::DeleteAllTargets()
    {
        _targetsLock.lock();

        m_setTargets.clear();
        m_setTargetsAttacking.clear();

        _targetsLock.unlock();
    }

    int CMuHelper::ComputeDistanceFromTarget(CHARACTER* pTarget)
    {
        const POINT posHero = { Hero->PositionX, Hero->PositionY };

        const POINT posCurrent = { pTarget->PositionX, pTarget->PositionY };
        const POINT posNext    = { pTarget->TargetX,   pTarget->TargetY };

        return std::min(
            ComputeDistanceBetween(posHero, posCurrent),
            ComputeDistanceBetween(posHero, posNext)
        );
    }

    int CMuHelper::ComputeDistanceBetween(POINT posA, POINT posB)
    {
        int iDx = posA.x - posB.x;
        int iDy = posA.y - posB.y;

        return static_cast<int>(std::ceil(std::sqrt(iDx * iDx + iDy * iDy)));
    }

    float CMuHelper::GetAttackRange(ActionSkillType iSkill)
    {
        if (iSkill == (ActionSkillType)MUHELPER_BASIC_ATTACK_ID)
        {
            return BASIC_ATTACK_DISTANCE;
        }

        return gSkillManager.GetSkillDistance(iSkill, Hero);
    }

    bool CMuHelper::IsTargetInSkillRange(int iTargetId, ActionSkillType iSkill)
    {
        const int iIndex = FindCharacterIndex(iTargetId);
        if (iIndex == MAX_CHARACTERS_CLIENT)
        {
            return false;
        }

        CHARACTER* pTarget = &CharactersClient[iIndex];
        if (!pTarget || pTarget->Dead > 0 || !pTarget->Object.Live)
        {
            return false;
        }

        const float fRange = GetAttackRange(iSkill);
        if (fRange <= 0.f)
        {
            return false;
        }

        TargetX = static_cast<int>(pTarget->Object.Position[0] / TERRAIN_SCALE);
        TargetY = static_cast<int>(pTarget->Object.Position[1] / TERRAIN_SCALE);

        return CheckTile(Hero, &Hero->Object, fRange)
            && CheckWall(Hero->PositionX, Hero->PositionY, TargetX, TargetY);
    }

    int CMuHelper::CountTargetsInSkillRange(ActionSkillType iSkill, bool bOnlyAttacking)
    {
        int iCount = 0;

        std::set<int> setTargets;
        {
            _targetsLock.lock();
            setTargets = bOnlyAttacking ? m_setTargetsAttacking : m_setTargets;
            _targetsLock.unlock();
        }

        for (const int& iTargetId : setTargets)
        {
            const int iIndex = FindCharacterIndex(iTargetId);
            if (iIndex == MAX_CHARACTERS_CLIENT)
            {
                continue;
            }

            CHARACTER* pTarget = &CharactersClient[iIndex];
            if (IsMonster(pTarget) && IsTargetInSkillRange(iTargetId, iSkill))
            {
                ++iCount;
            }
        }

        return iCount;
    }

    int CMuHelper::GetNearestTarget(ActionSkillType iSkill)
    {
        int iClosestMonsterId = -1;
        int iMinDistance = 0x7FFFFFFF;

        std::set<int> setTargets;
        {
            _targetsLock.lock();
            setTargets = m_setTargets;
            _targetsLock.unlock();
        }

        for (const int& iMonsterId : setTargets)
        {
            int iIndex = FindCharacterIndex(iMonsterId);
            if (iIndex == MAX_CHARACTERS_CLIENT)
            {
                continue;
            }

            CHARACTER* pTarget = &CharactersClient[iIndex];

            if (!IsMonster(pTarget))
            {
                continue;
            }

            if (!IsTargetInSkillRange(iMonsterId, iSkill))
            {
                continue;
            }

            int iDistance = ComputeDistanceFromTarget(pTarget);
            if (iDistance < iMinDistance)
            {
                iMinDistance = iDistance;
                iClosestMonsterId = iMonsterId;
            }
        }

        return iClosestMonsterId;
    }

    void CMuHelper::CleanupTargets()
    {
        std::set<int> setTargets;
        {
            _targetsLock.lock();
            setTargets = m_setTargets;
            _targetsLock.unlock();
        }

        for (const int& iMonsterId : setTargets)
        {
            int iIndex = FindCharacterIndex(iMonsterId);
            if (iIndex == MAX_CHARACTERS_CLIENT)
            {
                DeleteTarget(iMonsterId);
                continue;
            }

            CHARACTER* pTarget = &CharactersClient[iIndex];
            if (!pTarget || (pTarget && (pTarget->Dead > 0 || !pTarget->Object.Live)))
            {
                DeleteTarget(iMonsterId);
                continue;
            }
        }
    }
	
	void CMuHelper::AddPvpAttacker(int iAttackerId)
    {
        if (!m_bActive)
            return;

        CHARACTER* pAttacker = FindCharacterByKey(iAttackerId);
        if (!pAttacker || pAttacker == Hero || !IsMonster(pAttacker))
            return;

        m_iCurrentTarget = iAttackerId;
        _targetsLock.lock();
        m_setTargets.insert(iAttackerId);
        m_setTargetsAttacking.insert(iAttackerId);
        _targetsLock.unlock();
    }


    int CMuHelper::ActivatePet()
    {
        if (!m_config.bUseDarkRaven)
        {
            return 1;
        }

        if (m_bPetActivated)
        {
            return 1;
        }

        if (m_config.iDarkRavenMode == PET_ATTACK_CEASE)
        {
            SocketClient->ToGameServer()->SendPetCommandRequest(PetType::DarkRaven, PetCommandMode::Normal, 0xFFFF);
        }
        else if (m_config.iDarkRavenMode == PET_ATTACK_AUTO)
        {
            SocketClient->ToGameServer()->SendPetCommandRequest(PetType::DarkRaven, PetCommandMode::AttackRandom, 0xFFFF);
        }
        else if (m_config.iDarkRavenMode == PET_ATTACK_TOGETHER)
        {
            SocketClient->ToGameServer()->SendPetCommandRequest(PetType::DarkRaven, PetCommandMode::AttackWithOwner, 0xFFFF);
        }

        m_bPetActivated = true;
        return 1;
    }

    int CMuHelper::Buff()
    {
        if (!HasAssignedBuffSkill())
        {
            return 1;
        }

        if (m_config.bSupportParty && g_pPartyManager->IsPartyActive())
        {
            const int iPartyCount = std::min<int>(PartyNumber, sizeof(Party) / sizeof(Party[0]));
            if (iPartyCount <= 0)
            {
                return 1;
            }

            if (m_iCurrentBuffPartyIndex >= iPartyCount)
            {
                m_iCurrentBuffPartyIndex = 0;
            }

            PARTY_t* pMember = &Party[m_iCurrentBuffPartyIndex];
            CHARACTER* pChar = g_pPartyManager->GetPartyMemberChar(pMember);

            if (pChar != NULL
                && pMember->Map == gMapManager.WorldActive
                && ComputeDistanceFromTarget(pChar) <= MAX_ACTIONABLE_DISTANCE)
            {
                if (ShouldActivateBuffTimer(m_config.bBuffDurationParty))
                {
                    m_bTimerActivatedBuffOngoing = true;
                }

                // Cast failed (hero is animating) — don't block attack, retry same target next tick
                if (BuffTarget(pChar, (ActionSkillType)m_config.aiBuff[m_iCurrentBuffIndex]) == 0)
                {
                    return 1;
                }
            }

            m_iCurrentBuffPartyIndex = (m_iCurrentBuffPartyIndex + 1) % iPartyCount;
        }
        else
        {
            if (ShouldActivateBuffTimer(m_config.bBuffDuration))
            {
                m_bTimerActivatedBuffOngoing = true;
            }

            // Cast failed (hero is animating) — don't block attack, retry same slot next tick
            if (BuffTarget(Hero, (ActionSkillType)m_config.aiBuff[m_iCurrentBuffIndex]) == 0)
            {
                return 1;
            }

            // Cast succeeded or buff already active — advance to next slot
            m_iCurrentBuffIndex = (m_iCurrentBuffIndex + 1) % m_config.aiBuff.size();
            if (m_iCurrentBuffIndex == 0)
            {
                m_bTimerActivatedBuffOngoing = false;
            }
            return 1;
        }

        // Party path: advance buff skill index only after all party members have been processed
        if (m_iCurrentBuffPartyIndex == 0)
        {
            m_iCurrentBuffIndex = (m_iCurrentBuffIndex + 1) % m_config.aiBuff.size();

            if (m_iCurrentBuffIndex == 0)
            {
                m_bTimerActivatedBuffOngoing = false;
            }
        }

        return 1;
    }

    bool CMuHelper::ShouldActivateBuffTimer(bool bUseBuffDuration)
    {
        if (bUseBuffDuration
            || m_config.iBuffCastInterval <= 0
            || m_bTimerActivatedBuffOngoing
            || m_iSecondsElapsed <= 0)
        {
            return false;
        }

        if (m_iSecondsElapsed % m_config.iBuffCastInterval != 0)
        {
            return false;
        }

        if (m_iLastBuffTimerSecond == m_iSecondsElapsed)
        {
            return false;
        }

        m_iLastBuffTimerSecond = m_iSecondsElapsed;
        return true;
    }

    int CMuHelper::BuffTarget(CHARACTER* pTargetChar, ActionSkillType iBuffSkill)
    {
        if (pTargetChar == nullptr)
        {
            return 1;
        }

        const ActionSkillType iBaseBuffSkill = gSkillManager.MasterSkillToBaseSkillIndex(iBuffSkill);

        if (iBaseBuffSkill == AT_SKILL_ATTACK
            && ShouldCastTimedBuff(pTargetChar, eBuff_Attack, m_bTimerActivatedBuffOngoing))
        {
            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        if (iBaseBuffSkill == AT_SKILL_DEFENSE
            && ShouldCastTimedBuff(pTargetChar, eBuff_Defense, m_bTimerActivatedBuffOngoing))
        {
            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        if (iBaseBuffSkill == AT_SKILL_INFINITY_ARROW && !IsBuffActive(pTargetChar, eBuff_InfinityArrow))
        {
            if (pTargetChar != Hero)
            {
                return 1;
            }
            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        if (iBaseBuffSkill == AT_SKILL_SOUL_BARRIER
            && ShouldCastTimedBuff(pTargetChar, eBuff_WizDefense, m_bTimerActivatedBuffOngoing))
        {
            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        if (iBaseBuffSkill == AT_SKILL_SWELL_LIFE
            && ShouldCastTimedBuff(pTargetChar, eBuff_Life, m_bTimerActivatedBuffOngoing))
        {
            if (m_iComboState == 2)
            {
                return 1;
            }

            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        if (iBaseBuffSkill == AT_SKILL_EXPANSION_OF_WIZARDRY && !IsBuffActive(pTargetChar, eBuff_SwellOfMagicPower))
        {
            if (pTargetChar != Hero)
            {
                return 1;
            }
            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        if (iBaseBuffSkill == AT_SKILL_ADD_CRITICAL && !IsBuffActive(pTargetChar, eBuff_AddCriticalDamage))
        {
            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        if (iBaseBuffSkill == AT_SKILL_ALICE_BERSERKER && !IsBuffActive(pTargetChar, eBuff_Berserker))
        {
            if (pTargetChar != Hero)
            {
                return 1;
            }
            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        if (iBaseBuffSkill == AT_SKILL_ALICE_THORNS && !IsBuffActive(pTargetChar, eBuff_Thorns))
        {
            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        if (iBaseBuffSkill == AT_SKILL_ATT_UP_OURFORCES
            && ShouldCastTimedBuff(pTargetChar, eBuff_Att_up_Ourforces, m_bTimerActivatedBuffOngoing))
        {
            if (pTargetChar != Hero)
            {
                return 1;
            }

            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        if (iBaseBuffSkill == AT_SKILL_HP_UP_OURFORCES
            && ShouldCastTimedBuff(pTargetChar, eBuff_Hp_up_Ourforces, m_bTimerActivatedBuffOngoing))
        {
            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        if (iBaseBuffSkill == AT_SKILL_DEF_UP_OURFORCES
            && ShouldCastTimedBuff(pTargetChar, eBuff_Def_up_Ourforces, m_bTimerActivatedBuffOngoing))
        {
            return SimulateBuffSkill(iBuffSkill, pTargetChar->Key);
        }

        return 1;
    }

    int CMuHelper::ConsumePotion()
    {
        int64_t iLife = CharacterAttribute->Life;
        int64_t iLifeMax = CharacterAttribute->LifeMax;

        if (m_config.bUseHealPotion && iLifeMax > 0 && iLife > 0)
        {
            int64_t iRemaining = (iLife * 100 + iLifeMax - 1) / iLifeMax;
            if (iRemaining <= m_config.iPotionThreshold)
            {
                int iPotionIndex = g_pMyInventory->FindHealingItemIndex();
                if (iPotionIndex != -1)
                {
                    SendRequestUse(iPotionIndex, 0);
                }
            }
        }

        return 1;
    }

    int CMuHelper::RecoverHealth()
    {
        if (!Heal())
        {
            return 0;
        }
        
        if (!DrainLife())
        {
            return 0;
        }

        if (!ConsumePotion())
        {
            return 0;
        }

        return 1;
    }

    int CMuHelper::Heal()
    {
        if (!m_config.bAutoHeal)
        {
            return 1;
        }

        auto iHealingSkill = GetHealingSkill();
        if (iHealingSkill == AT_SKILL_UNDEFINED)
        {
            return 1;
        }

        if (m_config.bAutoHealParty && g_pPartyManager->IsPartyActive())
        {
            PARTY_t* pMember = &Party[m_iCurrentHealPartyIndex];
            CHARACTER* pChar = g_pPartyManager->GetPartyMemberChar(pMember);
            int iHealResult = 1;

            if (pChar != NULL)
            {
                if (pChar == Hero)
                {
                    iHealResult = HealSelf(iHealingSkill);
                }
                else if (pMember->Map == gMapManager.WorldActive
                    && pMember->stepHP * 10 <= m_config.iHealPartyThreshold
                    && ComputeDistanceFromTarget(pChar) <= MAX_ACTIONABLE_DISTANCE)
                {
                    iHealResult = SimulateSkill(iHealingSkill, true, pChar->Key);
                }
            }

            m_iCurrentHealPartyIndex = (m_iCurrentHealPartyIndex + 1) % (sizeof(Party) / sizeof(Party[0]));

            return iHealResult;
        }
        else
        {
            return HealSelf(iHealingSkill);
        }

        return 1;
    }

    int CMuHelper::HealSelf(ActionSkillType iHealingSkill)
    {
        int64_t iLife = CharacterAttribute->Life;
        int64_t iLifeMax = CharacterAttribute->LifeMax;
        int64_t iRemaining = (iLife * 100 + iLifeMax - 1) / iLifeMax;

        if (iRemaining <= m_config.iHealThreshold)
        {
            return SimulateSkill(iHealingSkill, true, HeroKey);
        }

        return 1;
    }

    int CMuHelper::DrainLife()
    {
        if (!m_config.bUseDrainLife)
        {
            return 1;
        }

        auto iDrainLife = GetDrainLifeSkill();
        if (iDrainLife == AT_SKILL_UNDEFINED)
        {
            return 1;
        }

        int64_t iLife = CharacterAttribute->Life;
        int64_t iLifeMax = CharacterAttribute->LifeMax;
        int64_t iRemaining = (iLife * 100 + iLifeMax - 1) / iLifeMax;

        if (iRemaining <= m_config.iHealThreshold)
        {
            m_iCurrentTarget = GetNearestTarget(iDrainLife);
            if (m_iCurrentTarget != -1)
            {
                return SimulateSkill(iDrainLife, true, m_iCurrentTarget);
            }
        }

        return 1;
    }

    int CMuHelper::RepairEquipments()
    {
        if (m_config.bRepairItem)
        {
            for (int i = 0; i < MAX_EQUIPMENT; i++)
            {
                ITEM* pItem = &CharacterMachine->Equipment[i];
                if (!pItem || pItem->Type == -1)
                {
                    continue;
                }

                ITEM_ATTRIBUTE* pAttr = &ItemAttribute[pItem->Type];
                if (!pAttr)
                {
                    continue;
                }

                int iLevel = pItem->Level;
                int iDurability = pItem->Durability;
                int iMaxDurability = CalcMaxDurability(pItem, pAttr, iLevel);

                int64_t iHealth = (iDurability * 100 + iMaxDurability - 1) / iMaxDurability;

                if (iHealth <= DEFAULT_DURABILITY_THRESHOLD)
                {
                    int64_t iGoldCost = CalcSelfRepairCost(ItemValue(pItem, 2), iDurability, iMaxDurability, pItem->Type);
                    if (iGoldCost <= CharacterMachine->Gold)
                    {
                        SocketClient->ToGameServer()->SendRepairItemRequest(i, 1);
                    }
                }
            }
        }

        return 1;
    }

    int CMuHelper::Attack()
    {
        CleanupTargets();

        if (m_config.bUseCombo)
        {
            for (int i = 0; i < m_config.aiSkill.size(); i++)
            {
                if (m_config.aiSkill[i] == 0)
                {
                    return 0;
                }
            }

            m_iCurrentSkill = (ActionSkillType)m_config.aiSkill[m_iComboState];
        }
        else
        {
            m_iCurrentSkill = SelectAttackSkill();
        }

        if (m_iCurrentSkill <= AT_SKILL_UNDEFINED)
        {
            return 1;
        }

        if (m_iCurrentTarget != -1 && !IsTargetInSkillRange(m_iCurrentTarget, m_iCurrentSkill))
        {
            m_iCurrentTarget = -1;
        }

        if (m_iCurrentTarget == -1)
        {
            if (!m_setTargets.empty())
            {
                m_iCurrentTarget = GetNearestTarget(m_iCurrentSkill);
            }

            if (m_iCurrentTarget == -1)
            {
                m_iComboState = 0;
                return 0;
            }
        }

        if (m_config.bUseCombo)
        {
            return SimulateComboAttack();
        }

        if (m_iCurrentSkill == (ActionSkillType)MUHELPER_BASIC_ATTACK_ID)
        {
            return SimulateBasicAttack();
        }

        if (m_iCurrentSkill > AT_SKILL_UNDEFINED)
        {
            return SimulateAttack(m_iCurrentSkill);
        }

        return 1;
    }

    ActionSkillType CMuHelper::SelectAttackSkill()
    {
        // try skill 2 activation conditions
        if (m_config.aiSkill[1] > 0 && m_config.aiSkill[1] < MAX_SKILLS)
        {
            const ActionSkillType iSkill = (ActionSkillType)m_config.aiSkill[1];

            if ((m_config.aiSkillCondition[1] & ON_TIMER)
                && m_config.aiSkillInterval[1] != 0
                && m_iSecondsElapsed % m_config.aiSkillInterval[1] == 0)
            {
                return iSkill;
            }

            if (m_config.aiSkillCondition[1] & ON_CONDITION)
            {
                if (m_config.aiSkillCondition[1] & ON_MOBS_NEARBY)
                {
                    int iCount = CountTargetsInSkillRange(iSkill, false);

                    if (((m_config.aiSkillCondition[1] & ON_MORE_THAN_TWO_MOBS) && iCount >= 2)
                        || ((m_config.aiSkillCondition[1] & ON_MORE_THAN_THREE_MOBS) && iCount >= 3)
                        || ((m_config.aiSkillCondition[1] & ON_MORE_THAN_FOUR_MOBS) && iCount >= 4)
                        || ((m_config.aiSkillCondition[1] & ON_MORE_THAN_FIVE_MOBS) && iCount >= 5))
                    {
                        return iSkill;
                    }
                }
                else if (m_config.aiSkillCondition[1] & ON_MOBS_ATTACKING)
                {
                    int iCount = CountTargetsInSkillRange(iSkill, true);

                    if (((m_config.aiSkillCondition[1] & ON_MORE_THAN_TWO_MOBS) && iCount >= 2)
                        || ((m_config.aiSkillCondition[1] & ON_MORE_THAN_THREE_MOBS) && iCount >= 3)
                        || ((m_config.aiSkillCondition[1] & ON_MORE_THAN_FOUR_MOBS) && iCount >= 4)
                        || ((m_config.aiSkillCondition[1] & ON_MORE_THAN_FIVE_MOBS) && iCount >= 5))
                    {
                        return iSkill;
                    }
                }
            }
        }

        // try skill 3 activation conditions
        if (m_config.aiSkill[2] > 0 && m_config.aiSkill[2] < MAX_SKILLS)
        {
            const ActionSkillType iSkill = (ActionSkillType)m_config.aiSkill[2];

            if ((m_config.aiSkillCondition[2] & ON_TIMER)
                && m_config.aiSkillInterval[2] != 0
                && m_iSecondsElapsed % m_config.aiSkillInterval[2] == 0)
            {
                return iSkill;
            }

            if (m_config.aiSkillCondition[2] & ON_CONDITION)
            {
                if (m_config.aiSkillCondition[2] & ON_MOBS_NEARBY)
                {
                    int iCount = CountTargetsInSkillRange(iSkill, false);

                    if (((m_config.aiSkillCondition[2] & ON_MORE_THAN_TWO_MOBS) && iCount >= 2)
                        || ((m_config.aiSkillCondition[2] & ON_MORE_THAN_THREE_MOBS) && iCount >= 3)
                        || ((m_config.aiSkillCondition[2] & ON_MORE_THAN_FOUR_MOBS) && iCount >= 4)
                        || ((m_config.aiSkillCondition[2] & ON_MORE_THAN_FIVE_MOBS) && iCount >= 5))
                    {
                        return iSkill;
                    }
                }
                else if (m_config.aiSkillCondition[2] & ON_MOBS_ATTACKING)
                {
                    int iCount = CountTargetsInSkillRange(iSkill, true);

                    if (((m_config.aiSkillCondition[2] & ON_MORE_THAN_TWO_MOBS) && iCount >= 2)
                        || ((m_config.aiSkillCondition[2] & ON_MORE_THAN_THREE_MOBS) && iCount >= 3)
                        || ((m_config.aiSkillCondition[2] & ON_MORE_THAN_FOUR_MOBS) && iCount >= 4)
                        || ((m_config.aiSkillCondition[2] & ON_MORE_THAN_FIVE_MOBS) && iCount >= 5))
                    {
                        return iSkill;
                    }
                }
            }
        }

        // If a valid skill is assigned to slot 0, use it; otherwise fall back to basic physical attack
        if (m_config.aiSkill[0] > 0 && m_config.aiSkill[0] < static_cast<int>(MAX_SKILLS))
        {
            return (ActionSkillType)m_config.aiSkill[0];
        }

        return (ActionSkillType)MUHELPER_BASIC_ATTACK_ID;
    }

    int CMuHelper::SimulateComboAttack()
    {
        for (int i = 0; i < m_config.aiSkill.size(); i++)
        {
            if (m_config.aiSkill[i] == 0)
            {
                return 0;
            }
        }

        if (SimulateAttack((ActionSkillType)m_config.aiSkill[m_iComboState]))
        {
            m_iComboState = (m_iComboState + 1) % 3;
        }

        return 1;
    }

    int CMuHelper::SimulateBasicAttack()
    {
        if (m_iCurrentTarget == -1)
        {
            return 0;
        }

        const int iCharIndex = FindCharacterIndex(m_iCurrentTarget);
        if (iCharIndex == MAX_CHARACTERS_CLIENT)
        {
            DeleteTarget(m_iCurrentTarget);
            return 0;
        }

        CHARACTER* pTarget = &CharactersClient[iCharIndex];
        if (pTarget->Dead > 0 || !IsMonster(pTarget))
        {
            DeleteTarget(m_iCurrentTarget);
            return 0;
        }

        const int iPreviousSelectedCharacter = SelectedCharacter;
        const int iPreviousActionTarget = ActionTarget;
        SelectedCharacter = iCharIndex;
        TargetX = static_cast<int>(pTarget->Object.Position[0] / TERRAIN_SCALE);
        TargetY = static_cast<int>(pTarget->Object.Position[1] / TERRAIN_SCALE);

        if (!IsTargetInSkillRange(m_iCurrentTarget, (ActionSkillType)MUHELPER_BASIC_ATTACK_ID))
        {
            m_iCurrentTarget = -1;
            return 0;
        }

        TargetX = static_cast<int>(pTarget->Object.Position[0] / TERRAIN_SCALE);
        TargetY = static_cast<int>(pTarget->Object.Position[1] / TERRAIN_SCALE);

        g_MovementSkill.m_iSkill = AT_SKILL_UNDEFINED;
        g_MovementSkill.m_bMagic = false;
        g_MovementSkill.m_iTarget = iCharIndex;
        ActionTarget = iCharIndex;
        Attacking = 1;
        Hero->MovementType = MOVEMENT_ATTACK;

        // Call Action() directly — basic attack doesn't go through ExecuteSkill,
        // and the engine's input loop only forwards Attacking=1 when IsAutoAttack() is on.
        Action(Hero, &Hero->Object, true);

        SelectedCharacter = iPreviousSelectedCharacter;
        ActionTarget = iPreviousActionTarget;

        return 1;
    }

    int CMuHelper::SimulateAttack(ActionSkillType iSkill)
    {
        return SimulateSkill(iSkill, true, m_iCurrentTarget);
    }

    int CMuHelper::SimulateBuffSkill(ActionSkillType iSkill, int iTarget)
    {
        const int iPreviousCurrentSkill = Hero->CurrentSkill;
        int iRestoreCurrentSkill = iPreviousCurrentSkill;
        ActionSkillType iRestoreSkill = m_iCurrentSkill;
        if (iRestoreSkill == (ActionSkillType)MUHELPER_BASIC_ATTACK_ID || iRestoreSkill <= AT_SKILL_UNDEFINED)
        {
            iRestoreSkill = (ActionSkillType)m_config.aiSkill[0];
        }

        const int iRestoreSkillIndex = g_pSkillList->GetSkillIndex(iRestoreSkill);
        if (iRestoreSkillIndex != -1)
        {
            iRestoreCurrentSkill = iRestoreSkillIndex;
        }

        constexpr int BUFF_CAST_RETRY_DELAY_SECONDS = 2;
        const std::pair<int, int> buffCastKey(static_cast<int>(iSkill), iTarget);
        auto itLastBuffCast = m_mapLastBuffCastSecond.find(buffCastKey);
        if (itLastBuffCast != m_mapLastBuffCastSecond.end()
            && m_iSecondsElapsed - itLastBuffCast->second < BUFF_CAST_RETRY_DELAY_SECONDS)
        {
            Hero->CurrentSkill = iRestoreCurrentSkill;
            return 1;
        }

        const MovementSkill previousMovementSkill = g_MovementSkill;
        const int iPreviousSelectedCharacter = SelectedCharacter;
        const int iPreviousTargetX = TargetX;
        const int iPreviousTargetY = TargetY;

        const int iResult = IsRageFighterBuffSkill(iSkill)
            ? SimulateRageFighterBuffSkill(iSkill)
            : SimulateSkill(iSkill, true, iTarget);

        if (iResult == 1)
        {
            m_mapLastBuffCastSecond[buffCastKey] = m_iSecondsElapsed;
        }

        Hero->CurrentSkill = iRestoreCurrentSkill;
        g_MovementSkill = previousMovementSkill;
        SelectedCharacter = iPreviousSelectedCharacter;
        TargetX = iPreviousTargetX;
        TargetY = iPreviousTargetY;

        return iResult;
    }

    int CMuHelper::SimulateSkill(ActionSkillType iSkill, bool bTargetRequired, int iTarget)
    {
        const int iSkillIndex = g_pSkillList->GetSkillIndex(iSkill);
        if (iSkillIndex == -1)
        {
            return 0;
        }

        g_MovementSkill.m_iSkill = iSkillIndex;
        g_MovementSkill.m_bMagic = true;

        const float fSkillDistance = gSkillManager.GetSkillDistance(iSkill, Hero);
        const bool bSelfPositionSkill = IsSelfPositionSkill(iSkill);

        if (bTargetRequired)
        {
            if (bSelfPositionSkill)
            {
                TargetX = Hero->PositionX;
                TargetY = Hero->PositionY;

                g_MovementSkill.m_iTarget = -1;

                if (iTarget != -1)
                {
                    const int iCharIndex = FindCharacterIndex(iTarget);
                    if (iCharIndex != MAX_CHARACTERS_CLIENT)
                    {
                        CHARACTER* pCurrentTarget = &CharactersClient[iCharIndex];
                        if (pCurrentTarget->Dead > 0 || !IsMonster(pCurrentTarget))
                        {
                            DeleteTarget(iTarget);
                            return 0;
                        }

                        if (!IsTargetInSkillRange(iTarget, iSkill))
                        {
                            m_iCurrentTarget = -1;
                            return 0;
                        }

                        TargetX = Hero->PositionX;
                        TargetY = Hero->PositionY;
                    }
                    else
                    {
                        DeleteTarget(iTarget);
                        return 0;
                    }
                }
            }
            else
            {
                if (iTarget == -1)
                {
                    return 0;
                }

                const int iCharIndex = FindCharacterIndex(iTarget);
                if (iCharIndex == MAX_CHARACTERS_CLIENT)
                {
                    DeleteTarget(iTarget);
                    return 0;
                }

                SelectedCharacter = iCharIndex;

                CHARACTER* pTarget = &CharactersClient[iCharIndex];
                if (pTarget->Dead > 0)
                {
                    DeleteTarget(iTarget);
                    return 0;
                }

                const bool bCurrentCombatTarget = (iTarget == m_iCurrentTarget);
                if (bCurrentCombatTarget && !IsMonster(pTarget))
                {
                    DeleteTarget(iTarget);
                    return 0;
                }

                g_MovementSkill.m_iTarget = iCharIndex;

                TargetX = (int)(pTarget->Object.Position[0] / TERRAIN_SCALE);
                TargetY = (int)(pTarget->Object.Position[1] / TERRAIN_SCALE);

                bool bTargetNear = CheckTile(Hero, &Hero->Object, fSkillDistance);
                bool bNoWall = CheckWall(Hero->PositionX, Hero->PositionY, TargetX, TargetY);

                if (!bTargetNear || !bNoWall)
                {
                    if (bCurrentCombatTarget)
                    {
                        m_iCurrentTarget = -1;
                        return 0;
                    }

                    return 1;
                }
            }
        }
        else
        {
            TargetX = Hero->PositionX;
            TargetY = Hero->PositionY;
        }

        int iSkillResult = ExecuteSkill(Hero, iSkill, fSkillDistance);
        if (iSkillResult == -1 && iTarget != -1)
        {
            DeleteTarget(iTarget);
        }

        return (int)(iSkillResult == 1);
    }

    bool CMuHelper::HasAssignedBuffSkill()
    {
        for (int i = 0; i < m_config.aiBuff.size(); i++)
        {
            if (m_config.aiBuff[i] != 0)
            {
                return true;
            }
        }

        return false;
    }

    ActionSkillType CMuHelper::GetHealingSkill()
    {
        std::vector<ActionSkillType> aiHealingSkills =
        {
            AT_SKILL_HEALING,
            AT_SKILL_HEALING_STR,
        };

        for (int i = 0; i < aiHealingSkills.size(); i++)
        {
            int iSkillIndex = g_pSkillList->GetSkillIndex(aiHealingSkills[i]);
            if (iSkillIndex != -1)
            {
                return aiHealingSkills[i];
            }
        }

        return AT_SKILL_UNDEFINED;
    }

    // Matches AttackWizard() behavior in ZzzInterface.cpp for these skill IDs.
    bool CMuHelper::IsSelfPositionSkill(ActionSkillType iSkill)
    {
        return (
            iSkill == AT_SKILL_NOVA_BEGIN ||
            iSkill == AT_SKILL_NOVA ||
            iSkill == AT_SKILL_HELL_FIRE ||
            iSkill == AT_SKILL_HELL_FIRE_STR ||
            iSkill == AT_SKILL_INFERNO ||
            iSkill == AT_SKILL_INFERNO_STR ||
            iSkill == AT_SKILL_INFERNO_STR_MG
        );
    }

    ActionSkillType CMuHelper::GetDrainLifeSkill()
    {
        std::vector<ActionSkillType> aiDrainLifeSkills =
        {
            AT_SKILL_ALICE_DRAINLIFE_MASTERY,
            AT_SKILL_ALICE_DRAINLIFE_STR,
            AT_SKILL_ALICE_DRAINLIFE
        };

        for (int i = 0; i < aiDrainLifeSkills.size(); i++)
        {
            int iSkillIndex = g_pSkillList->GetSkillIndex(aiDrainLifeSkills[i]);
            if (iSkillIndex != -1)
            {
                return aiDrainLifeSkills[i];
            }
        }

        return AT_SKILL_UNDEFINED;
    }

    int CMuHelper::ObtainItem()
    {
        if (m_iCurrentItem == MAX_ITEMS)
        {
            m_iCurrentItem = SelectItemToObtain();
            if (m_iCurrentItem == MAX_ITEMS)
            {
                return 1;
            }
        }

        ITEM_t* pDrop = &Items[m_iCurrentItem];
        ITEM* pItem = &pDrop->Item;

        if (!pDrop->Object.Live)
        {
            DeleteItem(m_iCurrentItem);
            return 1;
        }

        TargetX = (int)(Items[m_iCurrentItem].Object.Position[0] / TERRAIN_SCALE);
        TargetY = (int)(Items[m_iCurrentItem].Object.Position[1] / TERRAIN_SCALE);

        if (SendGetItem == -1)
        {
            SendGetItem = m_iCurrentItem;
            SocketClient->ToGameServer()->SendPickupItemRequest(m_iCurrentItem);
            DeleteItem(m_iCurrentItem);
        }

        return 1;
    }

    bool CMuHelper::ShouldObtainItem(int iItemId)
    {
        ITEM_t* pDrop = &Items[iItemId];
        ITEM* pItem = &pDrop->Item;

        if ((m_config.bPickZen && IsMoneyItem(pItem))
            || (m_config.bPickJewel && IsJewelItem(pItem))
            || (m_config.bPickAncient && IsAncientItem(pItem))
            || (m_config.bPickExcellent && IsExcellentItem(pItem)))
        {
            return true;
        }

        if (m_config.bPickExtraItems)
        {
            std::wstring strDisplayName = GetItemDisplayName(pItem);

            for (const auto& str : m_config.aExtraItems)
            {
                // Check if the search keyword is in the item's display name
                if (strDisplayName.find(str) != std::wstring::npos)
                {
                    return true;
                }
            }
        }

        return m_config.bPickAllItems;
    }

    void CMuHelper::AddItem(int iItemId, POINT posWhere)
    {
        _itemsLock.lock();
        m_setItems.insert(iItemId);
        _itemsLock.unlock();
    }

    void CMuHelper::DeleteItem(int iItemId)
    {
        _itemsLock.lock();
        m_setItems.erase(iItemId);
        _itemsLock.unlock();

        if (iItemId == m_iCurrentItem)
        {
            m_iCurrentItem = MAX_ITEMS;
        }
    }

    int CMuHelper::SelectItemToObtain()
    {
        int iClosestItemId = MAX_ITEMS;
        int iMinDistance = 0x7FFFFFFF;

        std::set<int> setItems;
        {
            _itemsLock.lock();
            setItems = m_setItems;
            _itemsLock.unlock();
        }

        for (const int& iItemId : setItems)
        {
            if (!ShouldObtainItem(iItemId))
            {
                continue;
            }

            int iItemX = (int)(Items[iItemId].Object.Position[0] / TERRAIN_SCALE);
            int iItemY = (int)(Items[iItemId].Object.Position[1] / TERRAIN_SCALE);

            int iDistance = ComputeDistanceBetween({ Hero->PositionX, Hero->PositionY }, { iItemX, iItemY });
            if (iDistance < iMinDistance)
            {
                iMinDistance = iDistance;
                iClosestItemId = iItemId;
            }
        }

        return iClosestItemId;
    }
}
