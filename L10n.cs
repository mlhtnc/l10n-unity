using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using System;
using System.Linq;

public class L10n
{
    #region Private Members

    #region Variables

    private static L10n instance;

    /// <summary>
    /// Current language which will serialized
    /// </summary>
    [JsonProperty]
    private string language;

    /// <summary>
    /// 
    /// </summary>
    private Dictionary<string, string> languageToFilePath;

    /// <summary>
    /// Translations
    /// </summary>
    private Dictionary<string, string> translations;

    #endregion

    #region Constants

    private const string KeyNotAddedMessage = " IS NOT ADDED";
    private const string ValueIsEmptyMessage = " IS EMPTY";
    private const string NoTranslationFileFound = "NO TRANSLATION FILE FOUND";

    private const string TranslationResourcesPath = "L10n";

    private const string PlayerPrefsKey = "com.company.l10n.l10n.json";

    private static readonly string L10nDirectoryPath = Path.Combine(
        Application.persistentDataPath,
        "L10n"
    );

    private static readonly string L10nFilePath = Path.Combine(
        L10nDirectoryPath,
        "l10n.json"
    );

    #endregion

    /// <summary>
    /// This constructor for Json Deserializer, it will give us deserialized
    /// language then we can initialize by it
    /// </summary>
    [JsonConstructor]
    private L10n(string language)
    {
        InitializeL10n(language);
    }

    /// <summary>
    /// This constructor for first time creations, we will initalize
    /// </summary>
    private L10n()
    {
        // Initialize with default language
        InitializeL10n();
    }

    private void InitializeL10n(string language = null)
    {
        // We call this to get file names and their paths
        ReadTranslationFileNames();

        // We need to do this after reading translation file names to
        // languageToFilePath dictionary.

        if (language == null)
            this.language = GetDeviceLanguage();
        else
            this.language = language;

        // Change language resources by given language, it may be
        // default language or deserialized language.
        LoadLanguageResources(this.language);
    }

    /// <summary>
    /// DOC:
    /// </summary>
    /// <returns></returns>
    private static L10n Initialize()
    {
        // If already an instance exist -also it means that we already
        // serialized the object-, we can return instance back.
        if (instance == null)
        {
            // If deserialized object is not null then it means that we already
            // serialized the object, we can set instance and return.
            L10n deserialized = Deserialize();
            if (deserialized != null)
            {
                instance = deserialized;
            }
            // If deserialized object is null then it means that we are at
            // the beginning of the game or some of the files are corrupted.
            // So we'll create a new instance and then Serialize that object.
            else
            {
                instance = new L10n();

                // Note that we serialize here because user may not call
                // ChangeLanguage() during the game. Serialize is also called
                // in Language prop.

            }
        }

        return instance;
    }

    private string GetDeviceLanguage()
    {
        string deviceLang = Application.systemLanguage.ToString().ToLower();

        if(languageToFilePath.ContainsKey(deviceLang) == false)
        {
            deviceLang = "english";
        }

        return deviceLang;
    }

    /// <summary>
    /// DOC:
    /// </summary>
    private void ReadTranslationFileNames()
    {
        var translationFiles = Resources.LoadAll<TextAsset>(TranslationResourcesPath);
        languageToFilePath = new Dictionary<string, string>();

        foreach (var translationFile in translationFiles)
        {
            string name = translationFile.name;
            string path = Path.Combine(
                TranslationResourcesPath,
                translationFile.name
            );

            languageToFilePath.Add(name, path);
        }
    }

    /// <summary>
    /// DOC:
    /// </summary>
    /// <param name="language"></param>
    private void LoadLanguageResources(string language)
    {
        this.language = language;

        string assetPath = Path.Combine(TranslationResourcesPath, language);
        var translationAsset = Resources.Load<TextAsset>(assetPath);

        if(translationAsset == null)
        {
            Debug.LogError($"You forgot to add translation file for {language}");
            translations = null;
            return;
        }

        translations = new Dictionary<string, string>();
        // translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(
        //     translationAsset.text,
        //     new JsonSerializerSettings
        //     {
        //         TypeNameHandling = TypeNameHandling.Auto
        //     }
        // );

        var translationsWithSections = (IDictionary<string,object>)Extensions.
                DeserializeToDictionaryOrList(translationAsset.text);

        var sectionsList = translationsWithSections.Keys.ToList();
        for (int i = 0; i < sectionsList.Count; i++)
        {
            var sectionKey = sectionsList[i];
            var sectionDict = (IDictionary<string,object>)translationsWithSections[sectionKey];
            
            var keys = sectionDict.Keys.ToList();
            for (int j = 0; j < keys.Count; j++)
            {
                var key = keys[j];
                var value = (string)sectionDict[key];
                translations.Add(key,value);
            }
        } 
    }

    /// <summary>
    /// DOC:
    /// </summary>
    public static void Serialize()
    {
        // If L10n base path is not exist, create the directory
        if (Directory.Exists(L10nDirectoryPath) == false)
        {
            Directory.CreateDirectory(L10nDirectoryPath);
        }

        // Serialize the L10n object
        string serialized = JsonConvert.SerializeObject(
            instance,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            }
        );

        // Save hash of the serialized json to PlayerPrefs
        string hashOfTheSerializedJson = ComputeSHA1Hash(serialized);
        PlayerPrefs.SetString(PlayerPrefsKey, hashOfTheSerializedJson);

        using (var streamWriter = File.CreateText(L10nFilePath))
        {
            streamWriter.WriteLine(serialized);
        }
    }

    /// <summary>
    /// It will try to deserialize. If deserialization is successful
    /// then it will return deserialized object otherwise it will return null
    /// </summary>
    /// <returns>If successful, deserialized object. Otherwise null</returns>
    private static L10n Deserialize()
    {
        // If directory or file is not exist then return null
        if (Directory.Exists(L10nDirectoryPath) == false ||
            File.Exists(L10nFilePath) == false)
        {
            Debug.Log("L10n: L10n directory or the l10n.json does not exist");
            return null;
        }

        // Get serialized json from storage
        string serializedJson = "";
        using (StreamReader streamReader = File.OpenText(L10nFilePath))
        {
            string line = "";
            while ((line = streamReader.ReadLine()) != null)
            {
                serializedJson += line;
            }
        }

        string previousHash = PlayerPrefs.GetString(PlayerPrefsKey);
        string currHash = ComputeSHA1Hash(serializedJson);

        if (previousHash != currHash)
        {
            Debug.Log("L10n: Hashes do not match");
            return null;
        }

        L10n deserialized = JsonConvert.DeserializeObject<L10n>(
            serializedJson,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            }
        );

        return deserialized;
    }

    /// <summary>
    /// DOC:
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static string ComputeSHA1Hash(string text)
    {
        byte[] textAsBytes = Encoding.UTF8.GetBytes(text);
        byte[] hashAsBytes = new System.Security.Cryptography.SHA1Managed().ComputeHash(textAsBytes, 0, Encoding.UTF8.GetByteCount(text));
        string hashAsHex = "";

        foreach (byte x in hashAsBytes)
        {
            hashAsHex += string.Format("{0:x2}", x);
        }

        return hashAsHex;
    }

    #endregion

    #region Public Members

    /// <summary>
    /// Gets fired whenever language changes. 
    /// </summary>
    public static event Action LanguageChanged;

    /// <summary>
    /// DOC:
    /// </summary>
    /// <value></value>
    public static L10n t
    {
        get => Initialize();
    }

    /// <summary>
    /// DOC:
    /// </summary>
    /// <value></value>
    public static string Language
    {
        set
        {
            Initialize();
            instance.LoadLanguageResources(value);
            LanguageChanged?.Invoke();
        }
        get { return instance.language; }
    }

    /// <summary>
    /// Get translations to given key in current language
    /// </summary>
    /// <value></value>
    public string this[string key]
    {
        get
        {
            if(translations == null)
                return NoTranslationFileFound;

            if (translations.ContainsKey(key))
            {
                if (string.IsNullOrEmpty(translations[key].Trim()))
                {
                    return key + ValueIsEmptyMessage;
                }
                
                return translations[key];
            }

            return key + KeyNotAddedMessage;
        }
    }

    #endregion
}
