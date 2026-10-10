using Game.Room;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Game.Hints
{
    /// <summary>
    /// The Man in the Moon hint button in the top-right corner (a placeholder box until Sarah's art
    /// exists), plus the "room complete" message. Click, tap, or press Interact to reveal the next hint
    /// tier. Hint text is data set on the component.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HintOverlay : MonoBehaviour
    {
        public const string ButtonName = "hint-button";
        public const string HintLabelName = "hint-text";
        public const string CompleteLabelName = "room-complete";

        [SerializeField] RoomController m_Room;
        [SerializeField, TextArea] string[] m_Hints = new string[0];

        HintSequence m_Sequence;
        Button m_Button;
        Label m_HintLabel;
        Label m_CompleteLabel;
        InputAction m_Interact;

        void OnEnable()
        {
            m_Sequence = new HintSequence(m_Hints);
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root.Q<Button>(ButtonName) == null)
                Build(root);
            m_HintLabel = root.Q<Label>(HintLabelName);
            m_CompleteLabel = root.Q<Label>(CompleteLabelName);
            if (InputSystem.actions != null)
                m_Interact = InputSystem.actions.FindAction("Player/Interact");
            if (m_Room != null)
                m_Room.Completed += OnRoomCompleted;
        }

        void Build(VisualElement root)
        {
            m_Button = new Button(ShowNextHint) { name = ButtonName, text = "Man in\nthe Moon\n(hint)" };
            m_Button.style.position = Position.Absolute;
            m_Button.style.top = 16;
            m_Button.style.right = 16;
            m_Button.style.width = 120;
            m_Button.style.height = 120;
            m_Button.style.fontSize = 16;
            m_Button.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_Button.style.backgroundColor = new Color(0.25f, 0.3f, 0.55f);
            m_Button.style.color = Color.white;
            m_Button.style.borderTopLeftRadius = m_Button.style.borderTopRightRadius = 60;
            m_Button.style.borderBottomLeftRadius = m_Button.style.borderBottomRightRadius = 60;
            root.Add(m_Button);

            root.Add(MakeLabel(HintLabelName, 150));

            var complete = MakeLabel(CompleteLabelName, 16);
            complete.style.left = 16;
            complete.style.right = StyleKeyword.Auto;
            complete.style.fontSize = 28;
            complete.text = "Room complete! The plushies escaped.";
            root.Add(complete);
        }

        void OnDisable()
        {
            if (m_Room != null)
                m_Room.Completed -= OnRoomCompleted;
        }

        void Update()
        {
            if (m_Interact != null && m_Interact.WasPressedThisFrame())
                ShowNextHint();
            if (m_CompleteLabel != null && m_Room != null && !m_Room.IsComplete)
                m_CompleteLabel.style.display = DisplayStyle.None;
        }

        void ShowNextHint()
        {
            m_HintLabel.text = m_Sequence.Count == 0 ? "No hints here." : m_Sequence.Next();
            m_HintLabel.style.display = DisplayStyle.Flex;
        }

        void OnRoomCompleted() => m_CompleteLabel.style.display = DisplayStyle.Flex;

        static Label MakeLabel(string labelName, float top)
        {
            var label = new Label { name = labelName };
            label.style.position = Position.Absolute;
            label.style.top = top;
            label.style.right = 16;
            label.style.maxWidth = 360;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 18;
            label.style.color = Color.white;
            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            label.style.paddingTop = label.style.paddingBottom = 8;
            label.style.paddingLeft = label.style.paddingRight = 12;
            label.style.display = DisplayStyle.None;
            return label;
        }
    }
}
