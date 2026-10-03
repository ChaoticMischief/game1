using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Diagnostics
{
    /// <summary>
    /// Shows <see cref="Application.version"/> in the bottom-left corner so every screenshot and bug
    /// report identifies the exact build (see <see cref="BuildInfo.ShouldShowOverlay"/> for when).
    /// Lives in the Main scene and persists across scene loads.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class BuildInfoOverlay : MonoBehaviour
    {
        public const string LabelName = "build-info";

        static BuildInfoOverlay s_Instance;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root.Q<Label>(LabelName) != null)
                return;
            if (!BuildInfo.ShouldShowOverlay(Application.version, Debug.isDebugBuild, Application.isEditor))
            {
                root.style.display = DisplayStyle.None;
                return;
            }

            var label = new Label(BuildInfo.DisplayText(Application.version, Application.isEditor))
            {
                name = LabelName,
                pickingMode = PickingMode.Ignore,
            };
            label.style.position = Position.Absolute;
            label.style.left = 6;
            label.style.bottom = 4;
            label.style.fontSize = 11;
            label.style.color = new Color(1f, 1f, 1f, 0.6f);
            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
            label.style.paddingLeft = label.style.paddingRight = 4;
            label.style.paddingTop = label.style.paddingBottom = 1;
            root.Add(label);
        }

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }
    }
}
