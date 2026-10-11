using System;
using System.Collections.Generic;
using Game.Room;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// The player slot: reads the project-wide Input System actions and hands them to the active
    /// plushie. Inactive plushies get no input, so they keep standing on whatever they were on.
    /// Next/Previous switch plushies.
    /// </summary>
    public sealed class PlushieRoster : MonoBehaviour, IRoomResettable
    {
        [SerializeField] PlushieController[] m_Plushies = new PlushieController[0];

        PlushieSelection m_Selection;
        InputAction m_Move;
        InputAction m_Jump;
        InputAction m_Next;
        InputAction m_Previous;
        InputAction[] m_Direct;

        /// <summary>Raised whenever the active plushie changes (including on start and room reset).</summary>
        public event Action ActiveChanged;

        public IReadOnlyList<PlushieController> Plushies => m_Plushies;

        public PlushieController Active => m_Plushies.Length == 0 ? null : m_Plushies[Selection.Index];

        // Created on first use, since other components may ask for the active plushie in their own OnEnable.
        PlushieSelection Selection => m_Selection ?? (m_Selection = new PlushieSelection(m_Plushies.Length));

        void OnEnable()
        {
            var actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError("No project-wide Input System actions are assigned.", this);
                return;
            }
            m_Move = actions.FindAction("Player/Move", true);
            m_Jump = actions.FindAction("Player/Jump", true);
            m_Next = actions.FindAction("Player/Next", true);
            m_Previous = actions.FindAction("Player/Previous", true);
            m_Direct = new InputAction[m_Plushies.Length];
            for (var i = 0; i < m_Plushies.Length; i++)
            {
                var actionName = m_Plushies[i].SelectAction;
                m_Direct[i] = string.IsNullOrEmpty(actionName) ? null : actions.FindAction(actionName, true);
            }
            ApplyActive();
        }

        void Update()
        {
            if (m_Move == null || m_Plushies.Length == 0)
                return;

            if (m_Next.WasPressedThisFrame() && Selection.Next())
                ApplyActive();
            else if (m_Previous.WasPressedThisFrame() && Selection.Previous())
                ApplyActive();
            for (var i = 0; i < m_Direct.Length; i++)
            {
                if (m_Direct[i] != null && m_Direct[i].WasPressedThisFrame() && Selection.Select(i))
                    ApplyActive();
            }

            var active = Active;
            active.MoveInput = Mathf.Clamp(m_Move.ReadValue<Vector2>().x, -1f, 1f);
            if (m_Jump.WasPressedThisFrame())
                active.RequestJump();
        }

        public void ResetState()
        {
            Selection.Reset();
            ApplyActive();
        }

        void ApplyActive()
        {
            for (var i = 0; i < m_Plushies.Length; i++)
            {
                var isActive = i == Selection.Index;
                m_Plushies[i].SetActive(isActive);
                if (!isActive)
                    m_Plushies[i].MoveInput = 0f;
            }
            ActiveChanged?.Invoke();
        }
    }
}
