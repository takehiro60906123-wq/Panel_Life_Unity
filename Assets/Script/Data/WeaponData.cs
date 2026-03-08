using UnityEngine;

public enum WeaponType
{
    None,
    Sword,
    GreatSword
}

[System.Serializable]
public class WeaponData
{
    public WeaponType weaponType;
    public string weaponName;
    public int maxLink;
    public int baseAttack;
}