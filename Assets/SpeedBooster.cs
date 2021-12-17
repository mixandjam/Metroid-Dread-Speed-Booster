using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using DG.Tweening;

public class SpeedBooster : MonoBehaviour
{

    private MovementInput movement;
    private Coroutine speedCharge;
    private Coroutine rumbleCoroutine;

    [Header("States")]
    [SerializeField]
    private bool chargingSpeedBooster;
    [SerializeField]
    private bool activeSpeedBooster;
    [SerializeField]
    private bool activeShineSpark;

    [SerializeField]
    private Renderer[] characterRenderers;
    private Material[] rendererMaterials;


    [ColorUsage(true,true)]
    [SerializeField]
    private Color chargeColor, activeColor;

    [Header("Particles")]
    [SerializeField]
    public ParticleSystem chestParticle;
    public ParticleSystem feetParticle;
    public GameObject distortion;

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

    // Update is called once per frame
    void Update()
    {
        if((chargingSpeedBooster || activeSpeedBooster) && movement.characterVelocity == 0 && !activeShineSpark)
        {
            ChargeSpeedBoost(false);
            SpeedBoost(false);
        }
    }

    void ChargeSpeedBoost(bool state)
    {
        chargingSpeedBooster = state;

        if (state)
        {
            speedCharge = StartCoroutine(ChargeCoroutine());
            MaterialChange(0.13f, 2.1f, 1,0, chargeColor);
            ChargeShineSpark(false);
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

    void SpeedBoost(bool state)
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

    void ChargeShineSpark(bool state)
    {
        activeShineSpark = state;

        if (state)
        {

            DOVirtual.Float(0, 1, .1f, BlinkMaterial).OnComplete(()=> DOVirtual.Float(1, 0, .3f, BlinkMaterial));
            MaterialChange(0.125f, 1.25f, 0,0, activeColor);
            GetComponent<CinemachineImpulseSource>().GenerateImpulse();
            Rumble(.2f, .25f, .75f);

        }
        else
        {

        }

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

    void BlinkMaterial(float x)
    {
        foreach (Material m in rendererMaterials)
            m.SetFloat("_ExtraShineAmount", x);
    }

    public bool isActive()
    {
        return activeSpeedBooster;
    }

    public bool isFullyCharged()
    {
        return activeShineSpark;
    }

    #region Input

    void OnSpeedBoost()
    {
        ChargeSpeedBoost(true);
    }

    void OnDown()
    {
        if (activeSpeedBooster)
        {
            activeShineSpark = true;
            SpeedBoost(false);
            ChargeShineSpark(true);
            GetComponent<Animator>().SetTrigger("ChargeShineSpark");
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
