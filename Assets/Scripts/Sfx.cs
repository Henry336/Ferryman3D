using System.Collections.Generic;
using UnityEngine;

// Plays clips from Resources/Audio by name. Missing clips are silently skipped so the game runs before audio lands.
public static class Sfx
{
    static AudioSource music, amb, oneShot, danger, moan;
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    static string musicName;
    public static bool On = true;

    static void Ensure()
    {
        if (oneShot) return;
        var go = new GameObject("Audio");
        Object.DontDestroyOnLoad(go);
        music = go.AddComponent<AudioSource>(); music.loop = true; music.volume = 0.45f;
        amb = go.AddComponent<AudioSource>(); amb.loop = true; amb.volume = 0.5f;
        oneShot = go.AddComponent<AudioSource>();
        danger = go.AddComponent<AudioSource>(); danger.loop = true; danger.volume = 0f;
        moan = go.AddComponent<AudioSource>(); moan.loop = true; moan.volume = 0f;
    }

    static AudioClip Clip(string name)
    {
        if (!cache.TryGetValue(name, out var c)) cache[name] = c = Resources.Load<AudioClip>("Audio/" + name);
        return c;
    }

    // cooldown stops the same sound stacking when an event fires every frame.
    public static void Play(string name, float volume = 1f, float cooldown = 0.05f)
    {
        Ensure();
        if (lastPlayed.TryGetValue(name, out var t) && Time.unscaledTime - t < cooldown) return;
        var c = Clip(name);
        if (!c) return;
        lastPlayed[name] = Time.unscaledTime;
        oneShot.pitch = Random.Range(0.94f, 1.06f);
        oneShot.PlayOneShot(c, volume);
    }

    public static void Music(string name)
    {
        Ensure();
        if (musicName == name) return;
        musicName = name;
        music.clip = Clip(name);
        if (music.clip) music.Play(); else music.Stop();
    }

    public static void Ambience(string name)
    {
        Ensure();
        if (amb.clip) return;
        amb.clip = Clip(name);
        if (amb.clip) amb.Play();
    }

    // 0..1 how close the boat is to capsizing; the heartbeat loop eases toward it and stays quiet.
    public static void Danger(float amount)
    {
        Ensure();
        if (!danger.clip) { danger.clip = Clip("sfx_danger_heartbeat"); if (!danger.clip) return; }
        float target = Mathf.Clamp01(amount) * 0.45f;
        danger.volume = Mathf.MoveTowards(danger.volume, target, Time.unscaledDeltaTime * 0.8f);
        if (danger.volume > 0.001f && !danger.isPlaying) danger.Play();
        if (danger.volume <= 0.001f && danger.isPlaying) danger.Stop();
    }

    // Ghostly moan bed layered over the river on the last legs; eases toward the target volume.
    public static void Moan(float target)
    {
        Ensure();
        if (!moan.clip) { moan.clip = Clip("sfx_ghost_moan"); if (!moan.clip) return; }
        moan.volume = Mathf.MoveTowards(moan.volume, target, Time.unscaledDeltaTime * 0.25f);
        if (moan.volume > 0.001f && !moan.isPlaying) moan.Play();
        if (moan.volume <= 0.001f && moan.isPlaying) moan.Stop();
    }

    public static void SetOn(bool on) { On = on; AudioListener.volume = on ? 1f : 0f; }
}
