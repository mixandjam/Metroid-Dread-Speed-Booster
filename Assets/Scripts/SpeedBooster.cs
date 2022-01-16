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
    private Animator anim;

    //Coroutines
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
    private Color runColor, sparkColor;

    [Header("Effects")]

    [Header("Run")]
    [SerializeField] private ParticleSystem circleChargeParticle;
    [SerializeField] private ParticleSystem chestParticle;
    [SerializeField] private ParticleSystem feetParticle;
    [SerializeField] private ParticleSystem flashParticle;
    [SerializeField] private GameObject distortion;
    [SerializeField] private GameObject shineSparkMesh;

    [Header("Store")]
    public ParticleSystem storeParticle;
    public ParticleSystem spChargeParticle;

    [Header("Dash")]
    public ParticleSystem impactPartile;
    public ParticleSystem trailParticle;
    public Transform trailParent;

    [Header("Settings")]
    [SerializeField] private float chargeTime = 1.5f;
    public int storedEnergyCooldown;

    //Impulse Sources
    private CinemachineImpulseSource characterImpulseSource;
    private CinemachineImpulseSource flashImpulseSource;
    private CinemachineImpulseSource storeImpulseSource;
    private CinemachineImpulseSource chargeImpulseSource;
    private CinemachineImpulseSource trailImpulseSource;

    // Start is called before the first frame update
    void Start()
    {
        movement = GetComponent<MovementInput>();
        anim = GetComponent<Animator>();

        rendererMaterials = new Material[characterRenderers.Length];
        for (int i = 0; i < characterRenderers.Length; i++)
        {
            rendererMaterials[i] = characterRenderers[i].material;
        }

        characterImpulseSource = GetComponent<CinemachineImpulseSource>();
        flashImpulseSource = flashParticle.GetComponent<CinemachineImpulseSource>();
        storeImpulseSource = storeParticle.GetComponent<CinemachineImpulseSource>();
        chargeImpulseSource = spChargeParticle.GetComponent<CinemachineImpulseSource>();
        trailImpulseSource = trailParticle.GetComponent<CinemachineImpulseSource>();

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
                GenerateCameraShake(characterImpulseSource);
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

            //effects
            MaterialChange(0.13f, 2.1f, 1,0, runColor);
            chestParticle.Play();
            circleChargeParticle.Play();
        }
        else
        {
            StopCoroutine(speedCharge);

            //effects
            MaterialChange(0, 1.25f, 0,0, sparkColor);
            chestParticle.Stop();
            circleChargeParticle.Stop();
        }

        IEnumerator ChargeCoroutine()
        {
            yield return new WaitForSeconds(chargeTime);
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
            feetParticle.Stop();
            return;
        }

        //efects
        feetParticle.Play();
        flashParticle.Play();
        MaterialChange(0.16f, 2.1f, 1,1, runColor);

        //shakes
        GenerateCameraShake(flashImpulseSource);
        Rumble(.2f, .25f, .75f);
    }

    void SetDistortionEffect(float amount)
    {
        distortion.GetComponent<Renderer>().material.SetFloat("_EffectAmount", amount);
    }

    void SetShineSparkEffect(float amount)
    {
        shineSparkMesh.GetComponent<Renderer>().material.SetFloat("_EffectAmount", amount);
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
            MaterialChange(0.125f, 1.25f, 0,0, sparkColor);
            GenerateCameraShake(characterImpulseSource);
            Rumble(.2f, .25f, .75f);
        }
        else
        {
            StopCoroutine(storedEnergyCoroutine);
            if (fadeOut)
                DOVirtual.Float(.125f, 0, .2f, FresnelChange);

            if (impact)
            {
                //effects
                DOVirtual.Float(.2f, 0, .1f, BlinkMaterial);
                GenerateCameraShake(characterImpulseSource);
                Rumble(.2f, .25f, .75f);
            }
        }

        if (!chargingSpeedBooster)
        {
            chestParticle.Stop();
            feetParticle.Stop();
        }
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
            anim.SetTrigger("StoreEnergy");

            StartCoroutine(CrouchCoroutine());
            storedEnergyCoroutine = StartCoroutine(StoredEnergyCooldown());

            //effects
            storeParticle.Play();
            GenerateCameraShake(storeImpulseSource);

            IEnumerator CrouchCoroutine()
            {
                movement.canMove = false;

                yield return new WaitForSeconds(.8f);
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

        movement.canMove = false;

        StopCoroutine(storedEnergyCoroutine);
        StartCoroutine(DashCoroutine());

        //animation
        anim.ResetTrigger("UseShineSpark");
        anim.SetTrigger("ChargeShineSpark");

        //effects
        spChargeParticle.Play();
        GenerateCameraShake(chargeImpulseSource);
        DOVirtual.Float(0, .08f, .8f, BlinkMaterial);

        Rumble(1, .10f, .1f);

        IEnumerator DashCoroutine()
        {
            chargingShineSpark = true;
            movement.chargeDash = true;

            yield return new WaitForSeconds(1f);

            anim.SetTrigger("UseShineSpark");

            chargingShineSpark = false;
            activeShineSpark = true;

            movement.SetDashVector();
            movement.chargeDash = false;
            movement.isDashing = true;

            //effects
            spChargeParticle.Stop();
            trailParticle.Play();
            trailImpulseSource.m_ImpulseDefinition.m_AmplitudeGain = .5f;
            GenerateCameraShake(trailImpulseSource);
            DOVirtual.Float(0, 1, .1f, SetShineSparkEffect);
            StopRumble();
            Rumble(5, .01f, .05f);

            yield return new WaitUntil(() => movement.dashBreak);

            movement.dashBreak = false;
            movement.isDashing = false;
            activeShineSpark = false;

            //effects
            trailParticle.Stop();
            trailImpulseSource.m_ImpulseDefinition.m_AmplitudeGain = 0;
            DOVirtual.Float(1, 0, .1f, SetShineSparkEffect);
            impactPartile.Play();
            GenerateCameraShake(flashImpulseSource);
            StopRumble();
            Rumble(.5f, .3f, .5f);
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

    void StopRumble()
    {
        if (rumbleCoroutine != null)
        {
            StopCoroutine(rumbleCoroutine);
            Gamepad.current.SetMotorSpeeds(0, 0);
        }
    }

    #endregion

    public void StopShineSparkBeam()
    {
        StopRumble();
        trailParticle.Stop();
        trailImpulseSource.m_ImpulseDefinition.m_AmplitudeGain = 0;
        DOVirtual.Float(1, 0, .1f, SetShineSparkEffect);
        BlinkMaterial(0);
    }

    public void SetShineSparkAngle(float angle)
    {
        trailParent.localEulerAngles = new Vector3(angle, 0, 0);
    }

    private void OnApplicationQuit()
    {
        StopRumble();
    }

    void GenerateCameraShake(CinemachineImpulseSource source)
    {
        source.GenerateImpulse();
    }
}
