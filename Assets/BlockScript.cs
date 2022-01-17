using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cinemachine;

public class BlockScript : MonoBehaviour
{
    [SerializeField] BoxCollider collider;
    ParticleSystem destroy;
    Renderer renderer;
    CinemachineImpulseSource impulse;
    [SerializeField] float timeToReturn = 2;

    // Start is called before the first frame update
    void Start()
    {
        collider = GetComponent<BoxCollider>();
        destroy = GetComponentInChildren<ParticleSystem>();
        renderer = GetComponent<Renderer>();
        impulse = GetComponent<CinemachineImpulseSource>();
    }

    public void Hit()
    {
        destroy.Play();
        renderer.material.SetFloat("_Transparency", 0);
        collider.enabled = false;

        impulse.GenerateImpulse();

        StartCoroutine(SpawnBack());

        IEnumerator SpawnBack()
        {
            yield return new WaitForSeconds(timeToReturn);
            DOVirtual.Float(0, 1,.3f, SpawnEffect);
            collider.enabled = true;
        }
    }

    void SpawnEffect(float alpha)
    {
        renderer.material.SetFloat("_Transparency", alpha);
    }
}
