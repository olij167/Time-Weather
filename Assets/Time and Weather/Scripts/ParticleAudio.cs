using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleAudio : MonoBehaviour
{
    public ParticleSystem particles;

    private int currentParticleCount;

    public AudioClip[] bornAudio;
    public AudioClip[] killAudio;
    public AudioSource audioSource;

    private void Update()
    {
        if (particles.particleCount < currentParticleCount && killAudio.Length > 0)
        {
            int r = Random.Range(0, bornAudio.Length);

            if (killAudio[r] != null)
            {
                audioSource.clip = killAudio[r];
                audioSource.volume = 1f;
                audioSource.Play();
                StartCoroutine(FadeAudio.StartFade(audioSource, killAudio[r].length - (killAudio[r].length / 3), 0f));
            }

        }

        if (particles.particleCount > currentParticleCount && bornAudio.Length > 0)
        {
            int r = Random.Range(0, bornAudio.Length);

            if (bornAudio[r] != null)
            {
                audioSource.clip = bornAudio[r];
                audioSource.volume = 1f;
                audioSource.Play();
                StartCoroutine(FadeAudio.StartFade(audioSource, bornAudio[r].length - (bornAudio[r].length / 3), 0f));
            }
        }

        currentParticleCount = particles.particleCount;
    }
}
