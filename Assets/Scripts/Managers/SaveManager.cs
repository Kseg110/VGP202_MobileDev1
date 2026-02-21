using UnityEngine;

public static class SaveManager
{
    public static void SavePlayer(Vector3 position, int health, int score, string weaponName)
    {
        PlayerPrefs.SetFloat("PlayerX", position.x);
        PlayerPrefs.SetFloat("PlayerY", position.y);
        PlayerPrefs.SetFloat("PlayerZ", position.z);
        PlayerPrefs.SetInt("PlayerHealth", health);
        PlayerPrefs.SetInt("PlayerScore", score);
        PlayerPrefs.SetString("PlayerWeapon", weaponName);
        Debug.Log($"SaveManager: Saved weapon name: {weaponName}");
        PlayerPrefs.Save();
    }

    public static Vector3 LoadPosition()
    {
        return new Vector3(
            PlayerPrefs.GetFloat("PlayerX", 0),
            PlayerPrefs.GetFloat("PlayerY", 0),
            PlayerPrefs.GetFloat("PlayerZ", 0)
        );
    }

    public static int LoadHealth() => PlayerPrefs.GetInt("PlayerHealth", 6);
    public static int LoadScore() => PlayerPrefs.GetInt("PlayerScore", 0);
    public static string LoadWeapon() => PlayerPrefs.GetString("PlayerWeapon", "");
}
