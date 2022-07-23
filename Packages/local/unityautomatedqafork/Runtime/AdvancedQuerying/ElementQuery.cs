using System.Collections.Generic;
using Unity.RecordedPlayback;
#if AQA_USE_TMP
using TMPro;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.AutomatedQA
{
    public class ElementQuery : MonoBehaviour
    {
        private AQALogger logger;

        //TODO: Add support for ALL other characters, at least in property values such as text & textContains, by supplying a back slash to escape/identify the following character as NOT a command character.
        /*
            These characters are the only ones allowed to appear in the actual attribute or property values.
            Any other value appearing something like a GameObject's name will result in errors.
        */
        internal static readonly List<string> ValidQueryStringCharacters = new List<string>() { " ", "=", "*", "-", "_", ":", "/" };

        public static ElementQuery Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject(typeof(ElementQuery).ToString());
                    DontDestroyOnLoad(go);
                    go.transform.SetParent(CentralAutomationController.Instance.gameObject.transform);
                    _instance = go.AddComponent<ElementQuery>();
                }
                return _instance;
            }
        }
        private static ElementQuery _instance;

        private void Awake()
        {
            logger = new AQALogger();
        }

        public List<GameObject> ElementPool {
            get 
            {
                _elementPool.RemoveAll(x => x == null);
                return _elementPool;
            }
            set
            {
                _elementPool = value;
            } 
        }
        private List<GameObject> _elementPool = new List<GameObject>();

        /// <summary>
        /// Puts a new GameElement into the limited pool of GameObjects that have been registered as being of interest to Automated Testing.
        /// </summary>
        /// <param name="el"></param>
        public void RegisterElement(GameElement el)
        {
            ElementPool.Add(el.gameObject);
        }

        /// <summary>
        /// Verifies that properties and attributes have been used correctly, based on certain rules enforced to guarantee that unintended behavior & usage doesn't occur at scale.
        /// </summary>
        /// <param name="el"></param>
        public void ValidatePropertiesAndAttributes(GameElement el)
        {
            if (ElementPool.FindAll(x => !string.IsNullOrEmpty(el.Id) && x.GetComponent<GameElement>() && x.GetComponent<GameElement>().Id == el.Id).Count > 1)
            {
                logger.LogError($"Multiple elements have the same \"Id\" of \"{el.Id}\". The Id attribute must be unique.");
            }
            
            if (ElementPool.FindAll(x => string.IsNullOrEmpty(el.AutoId)).Count > 1)
            {
                logger.LogError($"Elements have null or empty \"AutoId\". Modify and save any scenes containing GameElement components to fix this.");
            }
            
            if (ElementPool.FindAll(x => !string.IsNullOrEmpty(el.AutoId) && x.GetComponent<GameElement>() && x.GetComponent<GameElement>().AutoId == el.AutoId).Count > 1)
            {
                logger.LogError($"Multiple elements have the same \"AutoId\" of \"{el.AutoId}\". The AutoId attribute must be unique.");
            }
        }

        /// <summary>
        /// Use supplied query selector string to identify a matching GameElement.
        /// </summary>
        /// <param name="querySelector"></param>
        /// <returns></returns>
        public static GameObject Find(string querySelector)
        {
            if(string.IsNullOrEmpty(querySelector))
            {
                return null;
            }
            List<GameObject> results = FindAll(querySelector, false);
            results = !results.Any() && querySelector.StartsWith("[") ? FindAll(querySelector, true) : results;
            return results.Any() ? results.First() : null;
        }

        /// <summary>
        /// Use supplied query selector string to identify a matching GameElement.
        /// </summary>
        /// <param name="querySelector"></param>
        /// <returns></returns>
        public static List<GameObject> FindAll(string querySelector)
        {
            List<GameObject> results = FindAll(querySelector, false);
            results = !results.Any() && querySelector.StartsWith("[") ? FindAll(querySelector, true) : results;
            return results;
        }

        private static List<GameObject> FindAll(string querySelector, bool useFullSceneGameObjectPool = false)
        {
            if (string.IsNullOrEmpty(querySelector))
            {
                return new List<GameObject>();
            }
            List<GameObject> filteredPool = Instance.ElementPool;
            if (useFullSceneGameObjectPool)
                filteredPool = Instance.GetAllActiveGameObjects();
            List<string> queryParts = !querySelector.Contains("[") && querySelector.Contains("/") ? new List<string> { querySelector } : BreakdownQuery(querySelector);

            // With every pass, the filtered pool will be further reduced until all that remains is a list of GameElement objects that match all of the query string.
            foreach (string queryPart in queryParts)
            {
                filteredPool = Instance.ReturnObjectsThatSatisfyQueryString(queryPart, filteredPool);
            }
            return filteredPool;
        }

        /// <summary>
        /// Create a query string to find an object in playback.
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static string ConstructQuerySelectorString(GameObject go)
        {
            string query = string.Empty;
            if (go.TryGetComponent(out GameElement ge))
            {
                if (!string.IsNullOrEmpty(ge.Id))
                {
                    query = $"#{ge.Id}";
                }
                else if (!string.IsNullOrEmpty(ge.AutoId))
                {
                    query = $"@{ge.AutoId}";
                }
                else if (ge.Classes.Any())
                {
                    query = $".{string.Join(".", ge.Classes).TrimEnd('.')}";
                }
            }
            return query;
        }

        /// <summary>
        /// Take multi-part query strings and break them into list of all individual pieces/filters.
        /// </summary>
        /// <param name="querySelector"></param>
        /// <returns></returns>
        private static List<string> BreakdownQuery(string querySelector)
        {

            // If this is a hierarchy path, do not break down the query
            List<char> nonAlphanumericChars = querySelector.ToCharArray().ToList().FindAll(x => !char.IsLetterOrDigit(x) && x != ':' && x != ' ');
            if (nonAlphanumericChars.Count == nonAlphanumericChars.FindAll(x => x == '/').Count)
            {
                return new List<string>() { querySelector };
            }

            AQALogger logger = new AQALogger();

            List<string> returnValues = new List<string>();
            char[] characters = querySelector.ToCharArray();
            int startIndex = 0;
            for (int x = 0; x < characters.Length; x++)
            {
               bool isLastCharacter = x == characters.Length - 1;
                // Spaces, equal signs, and asterisks do not represent the end of a query string. Underscores are treated like a letter or space and ignored.
               if (isLastCharacter || (!char.IsLetterOrDigit(characters[x]) && !ValidQueryStringCharacters.Contains(characters[x].ToString())))
               {
                   bool isClosingStatement = characters[x] == ']';
                   bool isStartOfQuery = startIndex == x;
                   if (x == 0 && isClosingStatement)
                   {
                       logger.LogError($"Invalid Query String: \"]\" cannot be the first character in a query string. querySelector: {querySelector}");
                   }
                   if (!isStartOfQuery  && characters[startIndex] == '[' && isLastCharacter && !isClosingStatement)
                   {
                       logger.LogError($"Invalid Query String: A starting bracket \"[\" was encountered, but a closing bracket \"]\" was not encountered before the expected end of the query. querySelector: {querySelector}");
                   }
                   if (!isStartOfQuery)
                   {
                       returnValues.Add(querySelector.Substring(startIndex, (isLastCharacter || isClosingStatement ? x + 1 : x) - startIndex));
                   }
                   startIndex = x;
               }
               if (isLastCharacter)
                   break;
            }

            return returnValues.FindAll(rv => !string.IsNullOrEmpty(rv) && rv != "]");
        }

        /// <summary>
        /// Interprets individual identifiers from a full query string, and returns a subset of the pool of GameObjects that match the identifier.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="currentPool"></param>
        /// <returns></returns>
        private List<GameObject> ReturnObjectsThatSatisfyQueryString(string query, List<GameObject> currentPool)
        {
            List<GameObject> newPool = new List<GameObject>();
            string queryIdentifier = query.Substring(1, query.Length - 1);
            switch (query.Substring(0, 1))
            {
                case ".":
                    newPool = currentPool.FindAll(x => x.GetComponent<GameElement>() && x.GetComponent<GameElement>().Classes.ToList().FindAll(y => y.ToLower() == queryIdentifier.ToLower()).Any());
                    break;
                case "#":
                    newPool = currentPool.FindAll(x => x.GetComponent<GameElement>() && x.GetComponent<GameElement>().Id == queryIdentifier);
                    break;
                case "@":
                    newPool = currentPool.FindAll(x => x.GetComponent<GameElement>() && x.GetComponent<GameElement>().AutoId == queryIdentifier);
                    break;
                case "[":
                    string[] queryPieces = queryIdentifier.Replace("]", string.Empty).Split('=');
                    newPool.AddRange(ReturnObjectsBasedOnQueryStringWithBracket(queryPieces, currentPool.Any() ? currentPool : GetAllActiveGameObjects()));
                    break;
                default:
                    if (query.Contains("/"))
                    {
                        // Find objects with the matching object name. Then move through parents to filter out only GameObjects with the full, matching hierarchy path.
                        string[] pieces = query.Split(new string[] { "/" }, System.StringSplitOptions.None);
                        List<GameObject> objectsWithMatchingName = FindAll($"[name={pieces.Last()}]", true);
                        foreach (GameObject obj in objectsWithMatchingName)
                        {
                            int matchCount = 1;
                            GameObject currentGameObject = obj;
                            if (currentGameObject == null)
                            {
                                break;
                            }
                            for (int p = pieces.Length - 2; p >= 0; p--)
                            {
                                if (currentGameObject.transform.parent == null)
                                {
                                    break;
                                }
                                GameObject parent = currentGameObject.transform.parent.gameObject;
                                if (parent.name == pieces[p])
                                {
                                    matchCount++;
                                    currentGameObject = parent;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (matchCount == pieces.Length)
                                newPool.Add(obj);
                        }
                    }
                    break;
            }
            return newPool;
        }

        /// <summary>
        /// Handle any attributes and properties that use brackets.
        /// </summary>
        /// <param name="queryPieces"></param>
        /// <param name="currentPool"></param>
        /// <returns></returns>
        private List<GameObject> ReturnObjectsBasedOnQueryStringWithBracket(string[] queryPieces, List<GameObject> currentPool)
        {
            List<GameObject> newPool = new List<GameObject>();
            string key = queryPieces[0].ToLower();
            string value = queryPieces[1].ToLower();
            switch (key)
            {
                case "text":
                    foreach (GameObject go in currentPool)
                    {
                        Text text = go.GetComponentInChildren<Text>();
                        InputField field = go.GetComponent<InputField>();
#if AQA_USE_TMP
                        TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>();
                        TMP_InputField tmpInput = go.GetComponent<TMP_InputField>();
#endif
                        if (text != null)
                        {
                            if (text.text.ToLower() == value)
                                newPool.Add(go);
                        }
                        else if (field != null)
                        {
                            if (field.text.ToLower() == value)
                                newPool.Add(go);
                        }
#if AQA_USE_TMP
                        else if (tmpText != null)
                        {
                            if (tmpText.text.ToLower() == value)
                                newPool.Add(go);
                        }
                        else if (tmpInput != null)
                        {
                            if (tmpInput.text.ToLower() == value)
                                newPool.Add(go);
                        }
#endif
                    }
                    break;
                case "text*":
                    foreach (GameObject go in currentPool)
                    {
                        Text text = go.GetComponentInChildren<Text>();
                        InputField field = go.GetComponentInChildren<InputField>();
#if AQA_USE_TMP
                        TMP_Text tmpText = go.GetComponentInChildren<TMP_Text>();
                        TMP_InputField tmpInput = go.GetComponent<TMP_InputField>();
#endif

                        if (text != null)
                        {
                            if (text.text.ToLower() == value || text.text.ToLower().Contains(value))
                                newPool.Add(go);
                        }
                        else if (field != null)
                        {
                            if (field.text.ToLower() == value || field.text.ToLower().Contains(value))
                                newPool.Add(go);
                        }
#if AQA_USE_TMP
                        else if (tmpText != null)
                        {
                            if (tmpText.text.ToLower() == value || tmpText.text.ToLower().Contains(value))
                                newPool.Add(go);
                        }
                        else if (tmpInput != null)
                        {
                            if (tmpInput.text.ToLower() == value || tmpInput.text.ToLower().Contains(value))
                                newPool.Add(go);
                        }
#endif
                    }
                    break;
                case "type":
                    newPool = currentPool.FindAll(x => GetGameObjectTypes(x).Contains(value));
                    break;
                case "name":
                    newPool = currentPool.FindAll(x => x.name.ToLower() == value);
                    break;
                default:
                    // Assume this is a custom-defined property. Search for GameElements with matching properties.
                    foreach (GameObject go in currentPool)
                    {
                        if (go.TryGetComponent(out GameElement ge))
                        {
                            bool isMatch = key.EndsWith("*") ?
                                ge.Properties.ToList().FindAll(x => x.PropertyName.ToLower() == key.Replace("*", string.Empty) && x.PropertyValue.ToLower().Contains(value)).Any() :
                                ge.Properties.ToList().FindAll(x => x.PropertyName.ToLower() == key.ToLower() && x.PropertyValue.ToLower() == value.ToLower()).Any();
                            if (isMatch)
                            {
                                newPool.Add(go);
                            }
                        }
                    }
                    break;
            }
            return newPool;
        }

        /// <summary>
        /// Find what component "types" are associated with this GameElement.
        /// </summary>
        /// <param name="el"></param>
        /// <returns></returns>
        private List<string> GetGameObjectTypes(GameObject el)
        {
            List<string> types = new List<string>();
            if (el.TryGetComponent(out Button b))
            {
                types.Add("button");
            }
            if (el.TryGetComponent(out InputField inFi))
            {
                types.Add("input");
            }
            if (el.TryGetComponent(out Toggle to))
            {
                types.Add("toggle");
            }
            if (el.TryGetComponent(out Text te))
            {
                types.Add("text");
            }
            if (el.TryGetComponent(out Slider sl))
            {
                types.Add("slider");
            }
            if (el.TryGetComponent(out Dropdown dd))
            {
                types.Add("dropdown");
            }
            if (el.TryGetComponent(out Scrollbar sb))
            {
                types.Add("scroll");
            }
            return types;
        }

        public List<GameObject> GetAllActiveGameObjects()
        {
            List<GameObject> results = new List<GameObject>();
            var scenes = GetOpenScenes();
            foreach (var scene in scenes)
            {
                results.AddRange(GetChildren(scene.GetRootGameObjects().ToList()));
            }
            GameObject temp = null;

            // Find all DontDestroyOnLoad GameObjects and add them to the total pool if they are not already contained in the results.
            try
            {
                temp = new GameObject();
                DontDestroyOnLoad(temp);
                Scene dontDestroyOnLoad = temp.scene;
                DestroyImmediate(temp);
                temp = null;
                List<GameObject> ddolObjs = GetChildren(dontDestroyOnLoad.GetRootGameObjects().ToList().FindAll(x => !x.name.EndsWith(typeof(RecordedPlaybackController).ToString()) && !x.name.EndsWith(typeof(CentralAutomationController).ToString())));
                foreach (GameObject ddolObj in ddolObjs)
                {
                    if(!results.Contains(ddolObj))
                        results.Add(ddolObj);
                }
            }
            finally
            {
                if (temp != null)
                    DestroyImmediate(temp);
            }
            return results.FindAll(x => x.activeInHierarchy && x.activeSelf);
        }

        public static List<GameObject> GetChildren(List<GameObject> objs)
        {
            List<GameObject> results = new List<GameObject>();
            foreach (GameObject obj in objs)
            {
                results.Add(obj);
                foreach (Transform trans in obj.transform)
                {
                    results.AddRange(GetChildren(new List<GameObject>() { trans.gameObject }));
                }
            }
            return results;
        }

        private List<Scene> GetOpenScenes()
        {
            var scenes = new List<Scene>();
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene thisScene = SceneManager.GetSceneAt(s);
                if (thisScene.isLoaded)
                {
                    scenes.Add(thisScene);
                }
            }
            return scenes;
        }
    }
}