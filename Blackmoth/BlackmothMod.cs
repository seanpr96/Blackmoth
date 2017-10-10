using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Modding;
using GlobalEnums;
using UnityEngine;
using HutongGames.PlayMaker;

namespace Blackmoth
{
    public class BlackmothMod : Mod
    {
        private bool dashingUp;

        public override void Initialize()
        {
            ModHooks.ModLog("Blackmoth initializing!");

            ModHooks.Instance.AttackHook += SetNailDamage;
            ModHooks.Instance.DashVectorHook += CalculateDashVelocity;
            ModHooks.Instance.DashPressedHook += CheckForDash;

            ModHooks.ModLog("Blackmoth initialized!");
        }

        private bool CanDashDown()
        {
            return HeroController.instance.hero_state != ActorStates.no_input && HeroController.instance.hero_state != ActorStates.hard_landing && HeroController.instance.hero_state != ActorStates.dash_landing && (float)GetPrivateField("dashCooldownTimer").GetValue(HeroController.instance) <= 0f && !HeroController.instance.cState.backDashing && (!HeroController.instance.cState.attacking || (float)GetPrivateField("attack_time").GetValue(HeroController.instance) >= HeroController.instance.ATTACK_RECOVERY_TIME) && !HeroController.instance.cState.preventDash && (HeroController.instance.cState.onGround || !(bool)GetPrivateField("airDashed").GetValue(HeroController.instance) || HeroController.instance.cState.wallSliding || dashingUp) && HeroController.instance.playerData.equippedCharm_31;
        }

        private bool CanDashUp()
        {
            return HeroController.instance.hero_state != ActorStates.no_input && HeroController.instance.hero_state != ActorStates.hard_landing && HeroController.instance.hero_state != ActorStates.dash_landing && (float)GetPrivateField("dashCooldownTimer").GetValue(HeroController.instance) <= 0f && !HeroController.instance.cState.backDashing && (!HeroController.instance.cState.attacking || (float)GetPrivateField("attack_time").GetValue(HeroController.instance) >= HeroController.instance.ATTACK_RECOVERY_TIME) && !HeroController.instance.cState.preventDash && (HeroController.instance.cState.onGround || !(bool)GetPrivateField("airDashed").GetValue(HeroController.instance) || HeroController.instance.cState.wallSliding || HeroController.instance.dashingDown) && HeroController.instance.playerData.equippedCharm_31;
        }

        public void DownDash()
        {
            SetDashDamage();
            InvokePrivateMethod("ResetAttacksDash");
            InvokePrivateMethod("CancelBounce");
            ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.FOOTSTEPS_RUN);
            ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.FOOTSTEPS_WALK);
            ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.DASH);
            InvokePrivateMethod("ResetLook");
            HeroController.instance.cState.recoiling = false;
            if (HeroController.instance.cState.wallSliding)
            {
                HeroController.instance.FlipSprite();
            }
            else if (GameManager.instance.inputHandler.inputActions.right.IsPressed)
            {
                HeroController.instance.FaceRight();
            }
            else if (GameManager.instance.inputHandler.inputActions.left.IsPressed)
            {
                HeroController.instance.FaceLeft();
            }
            HeroController.instance.cState.dashing = true;
            GetPrivateField("dashQueueSteps").SetValue(HeroController.instance, 0);
            if (HeroController.instance.playerData.canShadowDash)
            {
                HeroController.instance.AddMPCharge(3);
            }
            HeroController.instance.dashBurst.transform.localPosition = new Vector3(-0.07f, 3.74f, 0.01f);
            HeroController.instance.dashBurst.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            HeroController.instance.dashingDown = true;
            dashingUp = false;
            if (HeroController.instance.playerData.hasDash)
            {
                GetPrivateField("dashCooldownTimer").SetValue(HeroController.instance, HeroController.instance.DASH_COOLDOWN_CH * 0.4f);
            }
            else
            {
                GetPrivateField("dashCooldownTimer").SetValue(HeroController.instance, HeroController.instance.DASH_COOLDOWN * 0.7f);
            }
            GetPrivateField("shadowDashTimer").SetValue(HeroController.instance, GetPrivateField("dashCooldownTimer").GetValue(HeroController.instance));
            HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
            HeroController.instance.cState.shadowDashing = true;
            ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, 1f);
            HeroController.instance.sharpShadowPrefab.SetActive(true);
            if (HeroController.instance.cState.shadowDashing)
            {
                GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashDownBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x, HeroController.instance.transform.position.y + 3.5f, HeroController.instance.transform.position.z + 0.00101f)));
                ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localEulerAngles = new Vector3(0f, 0f, 90f);
                HeroController.instance.shadowRechargePrefab.SetActive(true);
                FSMUtility.LocateFSM(HeroController.instance.shadowRechargePrefab, "Recharge Effect").SendEvent("RESET");
                HeroController.instance.shadowdashParticlesPrefab.GetComponent<ParticleSystem>().enableEmission = true;
                HeroController.instance.shadowRingPrefab.Spawn(HeroController.instance.transform.position);
            }
            else
            {
                HeroController.instance.dashBurst.SendEvent("PLAY");
                HeroController.instance.dashParticlesPrefab.GetComponent<ParticleSystem>().enableEmission = true;
            }
            if (HeroController.instance.cState.onGround && !HeroController.instance.cState.shadowDashing)
            {
                GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.backDashPrefab.Spawn(HeroController.instance.transform.position));
                ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(HeroController.instance.transform.localScale.x * -1f, HeroController.instance.transform.localScale.y, HeroController.instance.transform.localScale.z);
            }
        }

        public void InfiniDash()
        {
	        if ((!HeroController.instance.cState.onGround && !HeroController.instance.inAcid && !HeroController.instance.playerData.equippedCharm_31) || dashingUp)
	        {
                GetPrivateField("airDashed").SetValue(HeroController.instance, true);
	        }
	        SetDashDamage();
            InvokePrivateMethod("ResetAttacksDash");
            InvokePrivateMethod("CancelBounce");
            ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.FOOTSTEPS_RUN);
            ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.FOOTSTEPS_WALK);
            ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.DASH);
            InvokePrivateMethod("ResetLook");
	        HeroController.instance.cState.recoiling = false;
	        if (HeroController.instance.cState.wallSliding)
	        {
		        HeroController.instance.FlipSprite();
	        }
	        else if (GameManager.instance.inputHandler.inputActions.right.IsPressed)
	        {
		        HeroController.instance.FaceRight();
	        }
	        else if (GameManager.instance.inputHandler.inputActions.left.IsPressed)
	        {
		        HeroController.instance.FaceLeft();
	        }
	        HeroController.instance.cState.dashing = true;
            GetPrivateField("dashQueueSteps").SetValue(HeroController.instance, 0);
	        if (HeroController.instance.playerData.canShadowDash)
	        {
		        HeroController.instance.AddMPCharge(3);
	        }
	        if (!HeroController.instance.cState.onGround && GameManager.instance.inputHandler.inputActions.down.IsPressed && HeroController.instance.playerData.equippedCharm_31)
	        {
		        HeroController.instance.dashBurst.transform.localPosition = new Vector3(-0.07f, 3.74f, 0.01f);
		        HeroController.instance.dashBurst.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
		        HeroController.instance.dashingDown = true;
		        dashingUp = false;
	        }
	        else if (GameManager.instance.inputHandler.inputActions.up.IsPressed && HeroController.instance.playerData.equippedCharm_31)
	        {
		        HeroController.instance.dashBurst.transform.localPosition = new Vector3(-0.07f, -3.74f, 0.01f);
		        HeroController.instance.dashBurst.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
		        HeroController.instance.dashingDown = false;
		        dashingUp = true;
	        }
	        else
	        {
		        HeroController.instance.dashBurst.transform.localPosition = new Vector3(4.11f, -0.55f, 0.001f);
		        HeroController.instance.dashBurst.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
		        HeroController.instance.dashingDown = false;
		        dashingUp = false;
	        }
	        if (HeroController.instance.playerData.canDash)
	        {
                GetPrivateField("dashCooldownTimer").SetValue(HeroController.instance, HeroController.instance.DASH_COOLDOWN_CH * 0.4f);
	        }
	        else
	        {
                GetPrivateField("dashCooldownTimer").SetValue(HeroController.instance, HeroController.instance.DASH_COOLDOWN * 7f);
	        }
            GetPrivateField("shadowDashTimer").SetValue(HeroController.instance, GetPrivateField("dashCooldownTimer").GetValue(HeroController.instance));
	        HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
	        HeroController.instance.cState.shadowDashing = true;
            ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, 1f); HeroController.instance.sharpShadowPrefab.SetActive(true);
	        if (HeroController.instance.cState.shadowDashing)
	        {
		        if (HeroController.instance.dashingDown)
		        {
                    GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashDownBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x, HeroController.instance.transform.position.y + 3.5f, HeroController.instance.transform.position.z + 0.00101f)));
                    ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localEulerAngles = new Vector3(0f, 0f, 90f);
		        }
		        else if (dashingUp)
		        {
			        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashDownBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x, HeroController.instance.transform.position.y - 3.5f, HeroController.instance.transform.position.z + 0.00101f)));
                    ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localEulerAngles = new Vector3(0f, 0f, -90f);
		        }
		        else if (HeroController.instance.transform.localScale.x > 0f)
		        {
			        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x + 5.21f, HeroController.instance.transform.position.y - 0.58f, HeroController.instance.transform.position.z + 0.00101f)));
                    ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(1.919591f, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.y, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.z);
		        }
		        else
		        {
			        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x - 5.21f, HeroController.instance.transform.position.y - 0.58f, HeroController.instance.transform.position.z + 0.00101f)));
                    ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(-1.919591f, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.y, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.z);
		        }
		        HeroController.instance.shadowRechargePrefab.SetActive(true);
		        FSMUtility.LocateFSM(HeroController.instance.shadowRechargePrefab, "Recharge Effect").SendEvent("RESET");
		        HeroController.instance.shadowdashParticlesPrefab.GetComponent<ParticleSystem>().enableEmission = true;
		        HeroController.instance.shadowRingPrefab.Spawn(HeroController.instance.transform.position);
	        }
	        else
	        {
		        HeroController.instance.dashBurst.SendEvent("PLAY");
		        HeroController.instance.dashParticlesPrefab.GetComponent<ParticleSystem>().enableEmission = true;
	        }
	        if (HeroController.instance.cState.onGround && !HeroController.instance.cState.shadowDashing)
	        {
                GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.backDashPrefab.Spawn(HeroController.instance.transform.position));
                ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(HeroController.instance.transform.localScale.x * -1f, HeroController.instance.transform.localScale.y, HeroController.instance.transform.localScale.z);
	        }
        }


        public void SetDashDamage()
        {
	        HeroController.instance.playerData.nailDamage = 5 + HeroController.instance.playerData.nailSmithUpgrades * 4;
	        if (HeroController.instance.playerData.equippedCharm_16)
	        {
		        HeroController.instance.playerData.nailDamage = HeroController.instance.playerData.nailDamage * 2;
	        }
	        if (HeroController.instance.playerData.equippedCharm_25)
	        {
		        HeroController.instance.playerData.nailDamage = (int)((double)HeroController.instance.playerData.nailDamage * 1.5);
	        }
	        PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }


        public void SetNailDamage(AttackDirection dir = AttackDirection.normal)
        {
	        HeroController.instance.playerData.nailDamage = 1;
	        PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        }

        public void UpDash()
        {
            GetPrivateField("airDashed").SetValue(HeroController.instance, true);
	        SetDashDamage();
            InvokePrivateMethod("ResetAttacksDash");
            InvokePrivateMethod("CancelBounce");
            ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.FOOTSTEPS_RUN);
            ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.FOOTSTEPS_WALK);
            ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.DASH);
            InvokePrivateMethod("ResetLook");
	        HeroController.instance.cState.recoiling = false;
	        if (HeroController.instance.cState.wallSliding)
	        {
		        HeroController.instance.FlipSprite();
	        }
	        else if (GameManager.instance.inputHandler.inputActions.right.IsPressed)
	        {
		        HeroController.instance.FaceRight();
	        }
	        else if (GameManager.instance.inputHandler.inputActions.left.IsPressed)
	        {
		        HeroController.instance.FaceLeft();
	        }
	        HeroController.instance.cState.dashing = true;
            GetPrivateField("dashQueueSteps").SetValue(HeroController.instance, 0);
	        if (HeroController.instance.playerData.canShadowDash)
	        {
		        HeroController.instance.AddMPCharge(3);
	        }
	        HeroController.instance.dashBurst.transform.localPosition = new Vector3(-0.07f, -3.74f, 0.01f);
	        HeroController.instance.dashBurst.transform.localEulerAngles = new Vector3(0f, 0f, -90f);
	        HeroController.instance.dashingDown = false;
	        dashingUp = true;
	        if (HeroController.instance.playerData.hasDash)
	        {
                GetPrivateField("dashCooldownTimer").SetValue(HeroController.instance, HeroController.instance.DASH_COOLDOWN_CH * 0.4f);
	        }
	        else
	        {
                GetPrivateField("dashCooldownTimer").SetValue(HeroController.instance, HeroController.instance.DASH_COOLDOWN * 0.7f);
	        }
            GetPrivateField("shadowDashTimer").SetValue(HeroController.instance, GetPrivateField("dashCooldownTimer").GetValue(HeroController.instance));
	        HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
	        HeroController.instance.cState.shadowDashing = true;
            ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, 1f); HeroController.instance.sharpShadowPrefab.SetActive(true);
	        if (HeroController.instance.cState.shadowDashing)
	        {
                GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashDownBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x, HeroController.instance.transform.position.y - 3.5f, HeroController.instance.transform.position.z + 0.00101f)));
                ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localEulerAngles = new Vector3(0f, 0f, -90f);
		        HeroController.instance.shadowRechargePrefab.SetActive(true);
		        FSMUtility.LocateFSM(HeroController.instance.shadowRechargePrefab, "Recharge Effect").SendEvent("RESET");
		        HeroController.instance.shadowdashParticlesPrefab.GetComponent<ParticleSystem>().enableEmission = true;
		        HeroController.instance.shadowRingPrefab.Spawn(HeroController.instance.transform.position);
	        }
	        else
	        {
		        HeroController.instance.dashBurst.SendEvent("PLAY");
		        HeroController.instance.dashParticlesPrefab.GetComponent<ParticleSystem>().enableEmission = true;
	        }
	        if (HeroController.instance.cState.onGround && !HeroController.instance.cState.shadowDashing)
	        {
		        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.backDashPrefab.Spawn(HeroController.instance.transform.position));
                ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(HeroController.instance.transform.localScale.x * -1f, HeroController.instance.transform.localScale.y, HeroController.instance.transform.localScale.z);
	        }
        }

        public Vector2 CalculateDashVelocity()
        {
            float num;
            if (!PlayerData.instance.hasDash)
            {
                if (PlayerData.instance.equippedCharm_16 && HeroController.instance.cState.shadowDashing)
                {
                    num = HeroController.instance.DASH_SPEED_SHARP;
                }
                else
                {
                    num = HeroController.instance.DASH_SPEED;
                }
            }
            else if (PlayerData.instance.equippedCharm_16 && HeroController.instance.cState.shadowDashing)
            {
                num = HeroController.instance.DASH_SPEED_SHARP * 1.2f;
            }
            else
            {
                num = HeroController.instance.DASH_SPEED * 1.2f;
            }
            if (PlayerData.instance.equippedCharm_18)
            {
                num *= 1.2f;
            }
            if (PlayerData.instance.equippedCharm_13)
            {
                num *= 1.5f;
            }
            if (HeroController.instance.dashingDown)
            {
                return new Vector2(0f, -0.5f * num);
            }
            else if (dashingUp)
            {
                return new Vector2(0f, num);
            }
            else if (HeroController.instance.cState.facingRight)
            {
                if (HeroController.instance.CheckForBump(CollisionSide.right))
                {
                    return new Vector2(num, (float)GetPrivateField("BUMP_VELOCITY_DASH").GetValue(HeroController.instance));
                }
                else
                {
                    return new Vector2(num, 0f);
                }
            }
            else if (HeroController.instance.CheckForBump(CollisionSide.left))
            {
                return new Vector2(-num, (float)GetPrivateField("BUMP_VELOCITY_DASH").GetValue(HeroController.instance));
            }
            else
            {
                return new Vector2(-num, 0f);
            }
        }

        public void CheckForDash()
        {
            GetPrivateField("dashQueueSteps").SetValue(HeroController.instance, 0);
            GetPrivateField("dashQueuing").SetValue(HeroController.instance, false);

            SetDashDamage();
            if (GameManager.instance.inputHandler.inputActions.down.IsPressed && CanDashDown())
            {
                this.DownDash();
            }
            else if (GameManager.instance.inputHandler.inputActions.up.IsPressed && CanDashUp())
            {
                this.UpDash();
            }
            else if (HeroController.instance.hero_state != ActorStates.no_input && HeroController.instance.hero_state != ActorStates.hard_landing && HeroController.instance.hero_state != ActorStates.dash_landing && (float)GetPrivateField("dashCooldownTimer").GetValue(HeroController.instance) <= 0f && !HeroController.instance.cState.dashing && !HeroController.instance.cState.backDashing && (!HeroController.instance.cState.attacking || (float)GetPrivateField("attack_time").GetValue(HeroController.instance) >= HeroController.instance.ATTACK_RECOVERY_TIME) && !HeroController.instance.cState.preventDash && (HeroController.instance.cState.onGround || !(bool)GetPrivateField("airDashed").GetValue(HeroController.instance) || HeroController.instance.cState.wallSliding))
            {
                if (!HeroController.instance.cState.onGround && !HeroController.instance.inAcid && !HeroController.instance.playerData.equippedCharm_32)
                {
                    GetPrivateField("airDashed").SetValue(HeroController.instance, true);
                }
                SetDashDamage();
                InvokePrivateMethod("ResetAttacksDash");
                InvokePrivateMethod("CancelBounce");
                ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.FOOTSTEPS_RUN);
                ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.FOOTSTEPS_WALK);
                ((HeroAudioController)GetPrivateField("audioCtrl").GetValue(HeroController.instance)).StopSound(HeroSounds.DASH);
                InvokePrivateMethod("ResetLook");
                HeroController.instance.cState.recoiling = false;
                if (HeroController.instance.cState.wallSliding)
                {
                    HeroController.instance.FlipSprite();
                }
                else if (GameManager.instance.inputHandler.inputActions.right.IsPressed)
                {
                    HeroController.instance.FaceRight();
                }
                else if (GameManager.instance.inputHandler.inputActions.left.IsPressed)
                {
                    HeroController.instance.FaceLeft();
                }
                HeroController.instance.cState.dashing = true;
                GetPrivateField("dashQueueSteps").SetValue(HeroController.instance, 0);
                if (HeroController.instance.playerData.canShadowDash)
                {
                    HeroController.instance.AddMPCharge(3);
                }
                HeroController.instance.dashBurst.transform.localPosition = new Vector3(4.11f, -0.55f, 0.001f);
                HeroController.instance.dashBurst.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                HeroController.instance.dashingDown = false;
                dashingUp = false;
                if (HeroController.instance.playerData.hasDash)
                {
                    if (HeroController.instance.playerData.equippedCharm_32)
                    {
                        GetPrivateField("dashCooldownTimer").SetValue(HeroController.instance, 0f);
                    }
                    else
                    {
                        GetPrivateField("dashCooldownTimer").SetValue(HeroController.instance, HeroController.instance.DASH_COOLDOWN_CH * 0.4f);
                    }
                }
                else
                {
                    GetPrivateField("dashCooldownTimer").SetValue(HeroController.instance, HeroController.instance.DASH_COOLDOWN * 0.7f);
                }
                GetPrivateField("shadowDashTimer").SetValue(HeroController.instance, GetPrivateField("dashCooldownTimer").GetValue(HeroController.instance));
                HeroController.instance.proxyFSM.SendEvent("HeroCtrl-ShadowDash");
                HeroController.instance.cState.shadowDashing = true;
                ((AudioSource)GetPrivateField("audioSource").GetValue(HeroController.instance)).PlayOneShot(HeroController.instance.sharpShadowClip, 1f); HeroController.instance.sharpShadowPrefab.SetActive(true);
                if (HeroController.instance.cState.shadowDashing)
                {
                    if (HeroController.instance.transform.localScale.x > 0f)
                    {
                        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x + 5.21f, HeroController.instance.transform.position.y - 0.58f, HeroController.instance.transform.position.z + 0.00101f)));
                        ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(1.919591f, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.y, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.z);
                    }
                    else
                    {
                        GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.shadowdashBurstPrefab.Spawn(new Vector3(HeroController.instance.transform.position.x - 5.21f, HeroController.instance.transform.position.y - 0.58f, HeroController.instance.transform.position.z + 0.00101f)));
                        ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(-1.919591f, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.y, ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale.z);
                    }
                    HeroController.instance.shadowRechargePrefab.SetActive(true);
                    FSMUtility.LocateFSM(HeroController.instance.shadowRechargePrefab, "Recharge Effect").SendEvent("RESET");
                    HeroController.instance.shadowdashParticlesPrefab.GetComponent<ParticleSystem>().enableEmission = true;
                    HeroController.instance.shadowRingPrefab.Spawn(HeroController.instance.transform.position);
                }
                else
                {
                    HeroController.instance.dashBurst.SendEvent("PLAY");
                    HeroController.instance.dashParticlesPrefab.GetComponent<ParticleSystem>().enableEmission = true;
                }
                if (HeroController.instance.cState.onGround && !HeroController.instance.cState.shadowDashing)
                {
                    GetPrivateField("dashEffect").SetValue(HeroController.instance, HeroController.instance.backDashPrefab.Spawn(HeroController.instance.transform.position));
                    ((GameObject)GetPrivateField("dashEffect").GetValue(HeroController.instance)).transform.localScale = new Vector3(HeroController.instance.transform.localScale.x * -1f, HeroController.instance.transform.localScale.y, HeroController.instance.transform.localScale.z);
                }
            }
            else
            {
                GetPrivateField("dashQueueSteps").SetValue(HeroController.instance, 0);
                GetPrivateField("dashQueuing").SetValue(HeroController.instance, true);
            }

            if (GameManager.instance.inputHandler.inputActions.attack.WasPressed)
            {
                SetNailDamage();
            }
        }

        private FieldInfo GetPrivateField(string fieldName)
        {
            return HeroController.instance.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        private void InvokePrivateMethod(string methodName)
        {
            HeroController.instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(HeroController.instance, new object[] { });
        }
    }
}
