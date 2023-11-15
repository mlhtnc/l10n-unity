using UnityEngine;
using TMPro;

public class L10nComponent : MonoBehaviour
{
    #region Variables

    [SerializeField]
    private string key;

    private TextMeshProUGUI text;

    #endregion

    #region Unity Methods

    private void Start()
    {
        text = GetComponent<TextMeshProUGUI>();

        if(string.IsNullOrEmpty(key))
        {
            Debug.LogError($"You forgot to add key for {text.gameObject.name}");
            return;
        }

        text.SetText(L10n.t[key]);

        L10n.LanguageChanged += OnLanguageChanged;
    }

    #endregion

    #region Callbacks

    private void OnLanguageChanged()
    {
        text.SetText(L10n.t[key]);
    }

    #endregion
}
