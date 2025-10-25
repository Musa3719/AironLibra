using UMA;
using UnityEngine;
using UnityEngine.UI;

public class HairBeardSelectionUI : MonoBehaviour
{
    public string _RecipeName;
    public bool _IsBeard;

    private UMATextRecipe _recipe;
    private void Awake()
    {
        _recipe = UMAGlobalContext.Instance.GetRecipe(_RecipeName, false);
    }
    public void Clicked()
    {
        if (_IsBeard)
        {
            CharacterCreation._Instance.SetBeard(_recipe);
        }
        else
        {
            CharacterCreation._Instance.SetHair(_recipe);
        }
    }
}
