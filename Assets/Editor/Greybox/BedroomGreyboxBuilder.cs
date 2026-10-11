using System.IO;
using Game.Hints;
using Game.Player;
using Game.Room;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

namespace Game.Editor
{
    /// <summary>
    /// Builds the version 1 bedroom greybox in Main.unity: one room, a bear and a rabbit, a
    /// pressure plate, a door, a lever on a shelf, an exit, and the hint overlay. Re-running replaces
    /// the "Bedroom" object, so tweak the numbers here and rebuild. All shapes are plain tinted
    /// squares; Sarah's art replaces them later. Layout and tuning are described in
    /// docs/design/core-loop.md.
    /// </summary>
    public static class BedroomGreyboxBuilder
    {
        const string k_ScenePath = "Assets/Scenes/Main.unity";
        const string k_SpritePath = "Assets/Art/Greybox/white.png";
        const string k_RoomName = "Bedroom";

        static readonly string[] k_Hints =
        {
            "The door won't open by itself. Something heavy might help.",
            "The bear is heavy enough to hold the plate down. The rabbit jumps higher than the bear.",
            "Park the bear on the plate, switch to the rabbit, hop over the bear, go through the door and jump up to the lever on the shelf. Then the bear can follow."
        };

        [MenuItem("Game/Build Bedroom Greybox")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(k_ScenePath);
            var sprite = EnsureWhiteSprite();

            var existing = GameObject.Find(k_RoomName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            SetupCamera();

            var room = new GameObject(k_RoomName);
            var roomController = room.AddComponent<RoomController>();

            Block("Backdrop", room, sprite, new Vector2(0, 3), new Vector2(22, 12), new Color(0.97f, 0.88f, 0.74f), -10, false);
            Block("Floor", room, sprite, new Vector2(0, -0.5f), new Vector2(22, 1), new Color(0.55f, 0.38f, 0.25f), 0, true);
            Block("WallLeft", room, sprite, new Vector2(-11, 3), new Vector2(1, 12), new Color(0.8f, 0.65f, 0.5f), 0, true);
            Block("WallRight", room, sprite, new Vector2(11, 3), new Vector2(1, 12), new Color(0.8f, 0.65f, 0.5f), 0, true);
            Block("Shelf", room, sprite, new Vector2(6, 1.525f), new Vector2(3, 0.15f), new Color(0.6f, 0.42f, 0.28f), 0, true);

            var plate = Block("PressurePlate", room, sprite, new Vector2(-3, 0.15f), new Vector2(1.8f, 0.3f), new Color(0.55f, 0.35f, 0.35f), 1, true, trigger: true);
            var plateComponent = plate.AddComponent<PressurePlate>();
            Set(plateComponent, "m_RequiredMass", 3f);
            Set(plateComponent, "m_Visual", plate.GetComponent<SpriteRenderer>());

            var lever = Block("Lever", room, sprite, new Vector2(6, 1.75f), new Vector2(0.7f, 0.3f), new Color(0.55f, 0.35f, 0.35f), 1, true, trigger: true);
            var leverComponent = lever.AddComponent<PressurePlate>();
            Set(leverComponent, "m_RequiredMass", 0.5f);
            Set(leverComponent, "m_Latches", true);
            Set(leverComponent, "m_Visual", lever.GetComponent<SpriteRenderer>());

            var door = Block("Door", room, sprite, new Vector2(2, 2), new Vector2(0.6f, 4), new Color(0.45f, 0.3f, 0.2f), 2, true);
            var doorComponent = door.AddComponent<Door>();
            SetArray(doorComponent, "m_Plates", new Object[] { plateComponent, leverComponent });

            var exit = Block("Exit", room, sprite, new Vector2(9, 1.5f), new Vector2(1.8f, 3), new Color(0.5f, 0.85f, 0.5f, 0.45f), -1, true, trigger: true);
            exit.AddComponent<ExitZone>();

            var kill = Block("KillZone", room, sprite, new Vector2(0, -8), new Vector2(30, 2), Color.clear, 0, true, trigger: true);
            kill.AddComponent<KillZone>();

            var bear = Plushie("Bear", room, sprite, new Vector2(-7.5f, 1), new Vector2(1.3f, 1.3f), new Color(0.55f, 0.35f, 0.2f),
                mass: 5f, moveSpeed: 4f, jumpHeight: 1f);
            var rabbit = Plushie("Rabbit", room, sprite, new Vector2(-9.3f, 1), new Vector2(0.7f, 1f), new Color(0.95f, 0.85f, 0.85f),
                mass: 1f, moveSpeed: 5f, jumpHeight: 2.2f);

            var roster = new GameObject("PlushieRoster");
            roster.transform.SetParent(room.transform);
            var rosterComponent = roster.AddComponent<PlushieRoster>();
            SetArray(rosterComponent, "m_Plushies", new Object[] { bear, rabbit });

            BuildHintOverlay(roomController);
            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Built the bedroom greybox in " + k_ScenePath);
        }

        static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6.3f; // CameraFit widens this when the window is narrower than the room
            if (cam.GetComponent<CameraFit>() == null)
                cam.gameObject.AddComponent<CameraFit>();
            cam.transform.SetPositionAndRotation(new Vector3(0, 2.8f, -10), Quaternion.identity);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.15f, 0.12f);
        }

        static GameObject Block(string name, GameObject parent, Sprite sprite, Vector2 position, Vector2 size, Color color, int order, bool collider, bool trigger = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (collider)
                go.AddComponent<BoxCollider2D>().isTrigger = trigger;
            return go;
        }

        static PlushieController Plushie(string name, GameObject parent, Sprite sprite, Vector2 position, Vector2 size, Color color,
            float mass, float moveSpeed, float jumpHeight)
        {
            var go = Block(name, parent, sprite, position, size, color, 10, true);
            var body = go.AddComponent<Rigidbody2D>();
            body.mass = mass;
            body.gravityScale = 2f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Small yellow diamond above the plushie that is currently controlled.
            var marker = new GameObject("ActiveMarker");
            marker.transform.SetParent(go.transform, false);
            marker.transform.SetLocalPositionAndRotation(new Vector3(0, 0.5f + 0.35f / size.y, 0), Quaternion.Euler(0, 0, 45));
            marker.transform.localScale = new Vector3(0.25f / size.x, 0.25f / size.y, 1);
            var markerRenderer = marker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = sprite;
            markerRenderer.color = new Color(1f, 0.9f, 0.2f);
            markerRenderer.sortingOrder = 11;
            marker.SetActive(false); // the roster shows it on the active plushie at runtime

            var controller = go.AddComponent<PlushieController>();
            Set(controller, "m_DisplayName", name);
            Set(controller, "m_MoveSpeed", moveSpeed);
            Set(controller, "m_JumpHeight", jumpHeight);
            Set(controller, "m_ActiveMarker", marker);
            return controller;
        }

        static void BuildHintOverlay(RoomController room)
        {
            var existing = Object.FindAnyObjectByType<HintOverlay>();
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            // Reuse the panel settings of the build-info overlay so both UIs share one panel.
            PanelSettings panelSettings = null;
            foreach (var document in Object.FindObjectsByType<UIDocument>())
            {
                if (document.panelSettings != null)
                {
                    panelSettings = document.panelSettings;
                    break;
                }
            }

            var go = new GameObject("HintOverlay");
            var uiDocument = go.AddComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;
            var overlay = go.AddComponent<HintOverlay>();
            Set(overlay, "m_Room", room);
            SetStrings(overlay, "m_Hints", k_Hints);
        }

        static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        static Sprite EnsureWhiteSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(k_SpritePath);
            if (existing != null)
                return existing;

            Directory.CreateDirectory(Path.GetDirectoryName(k_SpritePath)!);
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(k_SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(k_SpritePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(k_SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 4; // 4 pixels = 1 world unit, so scale equals size
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(k_SpritePath);
        }

        static void Set(Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            switch (value)
            {
                case float f: property.floatValue = f; break;
                case bool b: property.boolValue = b; break;
                case string s: property.stringValue = s; break;
                case Object o: property.objectReferenceValue = o; break;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetStrings(Object target, string field, string[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
