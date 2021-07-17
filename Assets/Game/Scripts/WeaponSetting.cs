using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Weapon;

[CreateAssetMenu(menuName = "Custom/Weapon Setting", fileName = "Weapon")]
public class WeaponSetting : ScriptableObject
{
    public TriggerType triggerType = TriggerType.Manual;
    public WeaponType weaponType = WeaponType.Raycast;
    public float fireRate = 0.5f;
    public float reloadTime = 2.0f;
    public int clipSize = 4;
    public float damage = 1.0f;

    [AmmoType]
    public int ammoType = -1;

    public Projectile projectilePrefab;
    public float projectileLaunchForce = 200.0f;
    public AdvancedSettings advancedSettings;
    
    [Header("Animation Clips")]
    public AnimationClip FireAnimationClip;
    public AnimationClip ReloadAnimationClip;

    [Header("Audio Clips")]
    public AudioClip FireAudioClip;
    public AudioClip ReloadAudioClip;
    
    [Header("Visual Settings")]
    public LineRenderer PrefabRayTrail;
    public bool DisabledOnEmpty;
    
    [Header("Visual Display")]
    public AmmoDisplay AmmoDisplay;
}
