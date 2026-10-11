using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Game.Hints
{
    /// <summary>
    /// On-screen help: a name label floating over each plushie (the selected one highlighted), a bottom
    /// status bar naming the selected plushie with the key hints, and a controls panel toggled by the
    /// Help action (? or F1).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlushieHud : MonoBehaviour
    {
        public const string StatusLabelName = "hud-selected";
        public const string HelpPanelName = "hud-help";

        static readonly Color s_SelectedBackground = new Color(0.95f, 0.73f, 0.29f);
        static readonly Color s_IdleBackground = new Color(0f, 0f, 0f, 0.55f);

        [SerializeField] PlushieRoster m_Roster;
        [SerializeField] float m_LabelHeightAbovePlushie = 0.9f;

        VisualElement m_Root;
        Label m_Status;
        VisualElement m_HelpPanel;
        Label[] m_NameLabels;
        Collider2D[] m_Colliders;
        InputAction m_Help;
        Camera m_Camera;

        void OnEnable()
        {
            m_Root = GetComponent<UIDocument>().rootVisualElement;
            if (m_Root.Q<Label>(StatusLabelName) == null)
                Build();
            m_Status = m_Root.Q<Label>(StatusLabelName);
            m_HelpPanel = m_Root.Q(HelpPanelName);
            m_NameLabels = m_Root.Query<Label>(className: "hud-name").ToList().ToArray();
            m_Camera = Camera.main;
            m_Colliders = new Collider2D[m_Roster.Plushies.Count];
            for (var i = 0; i < m_Colliders.Length; i++)
                m_Colliders[i] = m_Roster.Plushies[i].GetComponent<Collider2D>();
            if (InputSystem.actions != null)
                m_Help = InputSystem.actions.FindAction("Player/Help");
            if (m_Roster != null)
                m_Roster.ActiveChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (m_Roster != null)
                m_Roster.ActiveChanged -= Refresh;
        }

        void Update()
        {
            if (m_Help != null && m_Help.WasPressedThisFrame())
                m_HelpPanel.style.display = m_HelpPanel.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void LateUpdate()
        {
            if (m_Roster == null || m_Camera == null || m_Root.panel == null)
                return;
            var plushies = m_Roster.Plushies;
            for (var i = 0; i < plushies.Count && i < m_NameLabels.Length; i++)
            {
                var bounds = m_Colliders[i].bounds;
                var world = new Vector3(bounds.center.x, bounds.max.y + m_LabelHeightAbovePlushie, 0f);
                var panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(m_Root.panel, world, m_Camera);
                m_NameLabels[i].style.left = panelPosition.x;
                m_NameLabels[i].style.top = panelPosition.y;
            }
        }

        void Refresh()
        {
            if (m_Roster == null || m_Status == null)
                return;
            var active = m_Roster.Active;
            m_Status.text = HudText.SelectedLine(active == null ? null : active.DisplayName) + "     |     " + HudText.ControlsSummary;
            var plushies = m_Roster.Plushies;
            for (var i = 0; i < plushies.Count && i < m_NameLabels.Length; i++)
            {
                var selected = plushies[i] == active;
                var label = m_NameLabels[i];
                label.text = plushies[i].DisplayName;
                label.style.backgroundColor = selected ? s_SelectedBackground : s_IdleBackground;
                label.style.color = selected ? Color.black : Color.white;
                label.style.unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        void Build()
        {
            foreach (var plushie in m_Roster.Plushies)
            {
                var label = new Label(plushie.DisplayName);
                label.AddToClassList("hud-name");
                label.pickingMode = PickingMode.Ignore;
                label.style.position = Position.Absolute;
                // Anchor the label's bottom-centre to the computed point above the plushie.
                label.style.translate = new Translate(Length.Percent(-50), Length.Percent(-100));
                label.style.fontSize = 18;
                label.style.paddingTop = label.style.paddingBottom = 2;
                label.style.paddingLeft = label.style.paddingRight = 10;
                label.style.borderTopLeftRadius = label.style.borderTopRightRadius = 6;
                label.style.borderBottomLeftRadius = label.style.borderBottomRightRadius = 6;
                m_Root.Add(label);
            }

            var status = new Label { name = StatusLabelName, pickingMode = PickingMode.Ignore };
            status.style.position = Position.Absolute;
            status.style.left = 0;
            status.style.right = 0;
            status.style.bottom = 0;
            status.style.unityTextAlign = TextAnchor.MiddleCenter;
            status.style.fontSize = 18;
            status.style.color = Color.white;
            status.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
            status.style.paddingTop = status.style.paddingBottom = 8;
            m_Root.Add(status);

            var help = new VisualElement { name = HelpPanelName, pickingMode = PickingMode.Ignore };
            help.style.position = Position.Absolute;
            help.style.left = Length.Percent(50);
            help.style.top = Length.Percent(50);
            help.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            help.style.backgroundColor = new Color(0.1f, 0.07f, 0.05f, 0.92f);
            help.style.paddingTop = help.style.paddingBottom = 18;
            help.style.paddingLeft = help.style.paddingRight = 26;
            help.style.borderTopLeftRadius = help.style.borderTopRightRadius = 10;
            help.style.borderBottomLeftRadius = help.style.borderBottomRightRadius = 10;
            help.style.display = DisplayStyle.None;
            var title = new Label(HudText.HelpTitle);
            title.style.fontSize = 26;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = s_SelectedBackground;
            help.Add(title);
            var body = new Label(HudText.HelpBody);
            body.style.fontSize = 18;
            body.style.color = Color.white;
            body.style.whiteSpace = WhiteSpace.Pre;
            help.Add(body);
            m_Root.Add(help);
        }
    }
}
