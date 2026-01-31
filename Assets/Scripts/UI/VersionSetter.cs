using TMPro;
using UnityEngine;

public class VersionSetter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        #if !UNITY_EDITOR
        var file = Resources.Load<TextAsset>("version");
        if(file != null)
        {
            Debug.Log($"Game Version: {file.text}");
            DialogueManager.Instance.SetVariable("$versionText", file.text);
            GetComponent<TextMeshProUGUI>().text = file.text;
        }
        #endif
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
