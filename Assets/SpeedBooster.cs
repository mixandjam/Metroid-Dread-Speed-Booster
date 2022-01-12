using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using DG.Tweening;

public class SpeedBooster : MonoBehaviour
{
    //Components
    private MovementInput movement;
    private Coroutine speedCharge;
    private Coroutine rumbleCoroutine;
    private Coroutine storedEnergyCoroutine;

    [Header("States")]
    [SerializeField] private bool chargingSpeedBooster;
    [SerializeField] private bool activeSpeedBooster;
    [SerializeField] public bool storedEnergy;
    [SerializeField] public bool chargingShineSpark;
    [SerializeField] public bool activeShineSpark;

    [Space]
    [SerializeField] private Renderer[] characterRenderers;
    private Material[] rendererMaterials;

    [ColorUsage(true,true)]
    [SerializeField]
    private Color chargeColor, activeColor;

    [Header("Effects")]
    [SerializeField]
    public ParticleSystem chestParticle;
    public ParticleSystem feetParticle;
    public ParticleSystem spChargeParticle;
    public GameObject distortion;

    [Header("Settings")]
    public int storedEnergyCooldown;

    // Start is called before the first frame update
    void Start()
    {
        movement = GetComponent<MovementInput>();

        rendererMaterials = new Material[characterRenderers.Length];
        for (int i = 0; i < characterRenderers.Length; i++)
        {
            rendererMaterials[i] = characterRenderers[i].material;
        }

    }


    public void StopAll(bool shake)
    {
        if ((chargingSpeedBooster || activeSpeedBooster) && !storedEnergy)
        {
            bool shakeTrigger = chargingSpeedBooster;

            if (!chargingSpeedBooster)
                SpeedBoost(false);
            ChargeSpeedBoost(false);


            if (shake && !shakeTrigger)
            {
                GetComponent<CinemachineImpulseSource>().GenerateImpulse();
                Rumble(.2f, .25f, .75f);
            }
        }
    }

    public void StopSpeedCharge()
    {
        ChargeSpeedBoost(false);
    }

    void ChargeSpeedBoost(bool state)
    {
        chargingSpeedBooster = state;

        if (state)
        {
            speedCharge = StartCoroutine(ChargeCoroutine());
            MaterialChange(0.13f, 2.1f, 1,0, chargeColor);
            chestParticle.Play();
        }
        else
        {
            StopCoroutine(speedCharge);
            MaterialChange(0, 1.25f, 0,0, activeColor);
            chestParticle.Stop();
        }

        IEnumerator ChargeCoroutine()
        {
            yield return new WaitForSeconds(1.5f);
            SpeedBoost(true);
        }
    }

    public void SpeedBoost(bool state)
    {
        chargingSpeedBooster = false;
        activeSpeedBooster = state;

        DOVirtual.Float(state ? 0 : 1, state ? 1 : 0, .1f, SetDistortionEffect);

        if (!state)
        {
            return;
        }

        feetParticle.Play();

        MaterialChange(0.16f, 2.1f, 1,1, chargeColor);

        GetComponent<CinemachineImpulseSource>().GenerateImpulse();

        Rumble(.2f, .25f, .75f);
    }

    void SetDistortionEffect(float amount)
    {
        distortion.GetComponent<Renderer>().material.SetFloat("_EffectAmount", amount);
    }

    float GetDistortionEffectAmount()
    {
        return distortion.GetComponent<Renderer>().material.GetFloat("_EffectAmount");
    }

    public void StoreEnergy(bool state, bool fadeOut, bool impact)
    {
        //Set boolean
        storedEnergy = state;

        if (state)
        {
            DOVirtual.Float(0, 1, .1f, BlinkMaterial).OnComplete(()=> DOVirtual.Float(1, 0, .3f, BlinkMaterial));
            MaterialChange(0.125f, 1.25f, 0,0, activeColor);
            GetComponent<CinemachineImpulseSource>().GenerateImpulse();
            Rumble(.2f, .25f, .75f);
        }
        else
        {
            StopCoroutine(storedEnergyCoroutine);
            if (fadeOut)
                DOVirtual.Float(.125f, 0, .2f, FresnelChange);

            if (impact)
            {
                GetComponent<CinemachineImpulseSource>().GenerateImpulse();
                GetComponent<CinemachineImpulseSource>().GenerateImpulse();
                Rumble(.2f, .25f, .75f);
            }
        }

        if(!chargingSpeedBooster)
            chestParticle.Stop();
            feetParticle.Stop();
    }

    void MaterialChange(float fresnelAmount, float fresnelEdge, int blinkFresnel,int extraBlink, Color fresnelColor)
    {
        foreach (Material m in rendererMaterials)
        {
            m.SetFloat("_FresnelAmount", fresnelAmount);
            m.SetFloat("_FresnelEdge", fresnelEdge);
            m.SetInt("_BlinkFresnel", blinkFresnel);
            m.SetInt("_ExtraBlink", extraBlink);
            m.SetColor("_FresnelColor", fresnelColor);
        }
    }

    void FresnelChange(float fresnelAmount)
    {
        foreach (Material m in rendererMaterials)
        {
            m.SetFloat("_FresnelAmount", fresnelAmount);
        }
    }

    void BlinkMaterial(float x)
    {
        foreach (Material m in rendererMaterials)
            m.SetFloat("_ExtraShineAmount", x);
    }

    public bool isActive()
    {
        return activeSpeedBooster;
    }

    public bool isEnergyStored()
    {
        return storedEnergy;
    }

    //All Inputs
    #region Input

    void OnSpeedBoost()
    {
        if (!chargingSpeedBooster && !activeSpeedBooster && movement.moveInput.x != 0 && !chargingShineSpark && !activeShineSpark && !movement.isSliding)
        {
            if (storedEnergy)
            {
                StopCoroutine(storedEnergyCoroutine);
                StoreEnergy(false, false, false);
            }

            ChargeSpeedBoost(true);
        }
    }

    void OnDown()
    {
        if (activeSpeedBooster && movement.isGrounded && !movement.isSliding)
        {
            StoreEnergy(true, false, false);
            SpeedBoost(false);
            GetComponent<Animator>().SetTrigger("StoreEnergy");

            StartCoroutine(CrouchCoroutine());
            storedEnergyCoroutine = StartCoroutine(StoredEnergyCooldown());

            IEnumerator CrouchCoroutine()
            {
                movement.canMove = false;
                yield return new WaitForSeconds(.5f);
                movement.canMove = true;
            }

            IEnumerator StoredEnergyCooldown()
            {
                yield return new WaitForSeconds(storedEnergyCooldown);
                StoreEnergy(false, true, false);
            }
        }
    }

    void OnDash()
    {
        if (!storedEnergy || !movement.canMove)
            return;

        StopCoroutine(storedEnergyCoroutine);

        movement.canMove = false;
        spChargeParticle.Play();

        GetComponent<Animator>().SetTrigger("ChargeShineSpark");
        StartCoroutine(DashCoroutine());
        IEnumerator DashCoroutine()
        {
            chargingShineSpark = true;
            movement.chargeDash = true;

            yield return new WaitForSeconds(1f);

            chargingShineSpark = false;
            activeShineSpark = true;

            movement.SetDashVector();
            movement.chargeDash = false;
            movement.isDashing = true;

            spChargeParticle.Stop();

            float t = 0;
            while (t < .2f)
            {
                t += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitUntil(() => movement.dashBreak);

            movement.dashBreak = false;
            movement.isDashing = false;
            activeShineSpark = false;
        }

    }

    void Rumble(float duration, float lowFrequency, float highFrequency)
    {
        if (rumbleCoroutine != null)
        {
            StopCoroutine(RumbleSequence());
            Gamepad.current.SetMotorSpeeds(0, 0);
        }

        rumbleCoroutine = StartCoroutine(RumbleSequence());

        IEnumerator RumbleSequence()
        {
            Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);
            yield return new WaitForSeconds(duration);
            Gamepad.current.SetMotorSpeeds(0, 0);
        }
    }

    #endregion


}
