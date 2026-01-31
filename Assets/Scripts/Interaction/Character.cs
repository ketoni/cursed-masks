using System.IO;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Character")]
public class Character : ScriptableObject
{
    public string characterName;
    public string commonAssetCategory;
    public string commonAssetName;
    [Min(1)]
    public float speechSpeed = 10f;
    public string speechSoundPath;
    public TMP_FontAsset dialogueFont;

    public string CommonAssetPath(string root)
    {
        // Given any root path, returns the common asset load path for this Character
        return Path.Combine(root, commonAssetCategory, commonAssetName);
    }

    public string CommonAssetFilename(string variant)
    {
        // The common filename is `assetCategory[_assetName]_variant`, 
        // where assetName part will be omitted if it's not defined.
        // Examples:
        // * Side_Character_Someone_Portrait.png (Someone: name, Portrait.png: variant)
        // * Test_Character_Portrait.png (name not defined)
        var filename = commonAssetCategory;
        if (commonAssetName != "")
        {
            filename += $"_{commonAssetName}";
        }
        return filename + $"_{variant}";
    }

    public string AssetPath(string rootPath, string fileVariant)
    {
        // Returns the full asset path for an asset variant for this Character.
        // For a root path `Assets/Images/Portraits`
        // and Character data: commonAssetCategory: `Side_Character`; commonAssetName: `Someone`,
        // and variant named `Portrait.png`,
        // this function would then return
        // `Assets/Images/Portraits/Side_Character/Someone/Side_Character_Someone_Portrait.png`
        return Path.Combine(CommonAssetPath(rootPath), CommonAssetFilename(fileVariant));
    }
}
