using UnityEngine;
using UMA;
using UMA.CharacterSystem;
using System.Collections.Generic;

public static class WorldAndNpcCreation
{
    public static bool ChangeGender(DynamicCharacterAvatar avatar, bool isToMale)
    {
        if (avatar == null) return true;

        if (isToMale)
        {
            if (avatar.activeRace.name.StartsWith("HumanFemale"))
            {
                avatar.ChangeRace("HumanMale", DynamicCharacterAvatar.ChangeRaceOptions.none);
            }
            return true;
        }
        else
        {
            if (avatar.activeRace.name.StartsWith("HumanMale"))
            {
                avatar.ChangeRace("HumanFemale", DynamicCharacterAvatar.ChangeRaceOptions.none);
            }
            return false;
        }
    }
    public static void ChangeColor(DynamicCharacterAvatar avatar, string colorName, Color newColor)
    {
        if (avatar == null) return;

        avatar.SetColorValue(colorName, newColor);

        if (avatar.BuildCharacterEnabled)
        {
            avatar.BuildCharacterEnabled = false;
            avatar.BuildCharacterEnabled = true;
        }
    }
    public static List<UMATextRecipe> GetRandomHair(bool isMale)
    {
        List<UMATextRecipe> uMATextRecipes = new List<UMATextRecipe>();
        ////////////////////////////////
        return uMATextRecipes;
    }
}
