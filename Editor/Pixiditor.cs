using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Piximate.Editor
{
    public class Pixiditor : EditorWindow
    {
        private string   clipName       = "NewAnimClip";
        private string   saveFolderPath = "Assets";
        private float    frameRate      = 10f;
        private bool     loop           = true;
        private Sprite[] frames         = new Sprite[0];

        private bool   isPreviewing = false;
        private int    previewFrame = 0;
        private double lastFrameTime;
        private double previewTimer;

        private Vector2 mainScroll;
        private Vector2 stripScroll;

        private int     _dragFromIndex  = -1;
        private int     _dragToIndex    = -1;
        private bool    _dragging       = false;

        private AnimClip           _proxy;
        private SerializedObject   _so;
        private SerializedProperty _framesProp;

        private GUIStyle _titleStyle;
        private GUIStyle _mutedStyle;
        private GUIStyle _monoStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _nsStyle;
        private GUIStyle _emptyLabelStyle;
        private GUIStyle _dropLabelStyle;
        private GUIStyle _fpsStyle;
        private bool     _stylesReady;

        private Texture2D _checkerTex;
        private int       _checkerTexSize = -1;

        static readonly Color C_BG      = new Color(0.118f, 0.118f, 0.125f);
        static readonly Color C_SURFACE = new Color(0.165f, 0.165f, 0.175f);
        static readonly Color C_BORDER  = new Color(0.26f,  0.26f,  0.28f);
        static readonly Color C_ACCENT  = new Color(0.27f,  0.75f,  0.55f);
        static readonly Color C_WARN    = new Color(0.92f,  0.60f,  0.22f);
        static readonly Color C_DANGER  = new Color(0.88f,  0.33f,  0.33f);
        static readonly Color C_TEXT    = new Color(0.88f,  0.88f,  0.90f);
        static readonly Color C_MUTED   = new Color(0.50f,  0.50f,  0.54f);

        [MenuItem("Window/Piximate/Anim Clip Editor")]
        public static void Open()
        {
            var w = GetWindow<Pixiditor>("Anim Clip Editor");
            w.minSize = new Vector2(380, 600);
        }

        void OnEnable()
        {
            _stylesReady = false;
            RebuildProxy();
            EditorApplication.update += Tick;
            Repaint();
        }

        void OnDisable()
        {
            EditorApplication.update -= Tick;
            _stylesReady = false;
            _so         = null;
            _framesProp = null;
            if (_proxy != null) DestroyImmediate(_proxy);
            _proxy = null;
            if (_checkerTex != null) DestroyImmediate(_checkerTex);
            _checkerTex = null;
        }

        void RebuildProxy()
        {
            if (_proxy != null) DestroyImmediate(_proxy);
            _proxy = CreateInstance<AnimClip>();
            _proxy.SetFrames(frames ?? new Sprite[0]);
            _proxy.SetFrameRate(frameRate);
            _proxy.SetLoop(loop);
            _so         = new SerializedObject(_proxy);
            _framesProp = _so.FindProperty("frames");
        }

        void Tick()
        {
            if (_proxy == null || !isPreviewing || frames == null || frames.Length == 0) return;
            double now = EditorApplication.timeSinceStartup;
            previewTimer += now - lastFrameTime;
            lastFrameTime = now;
            double dur = 1.0 / Mathf.Max(0.01f, frameRate);
            if (previewTimer >= dur)
            {
                previewTimer -= dur;
                previewFrame++;
                if (previewFrame >= frames.Length)
                    previewFrame = loop ? 0 : frames.Length - 1;
                Repaint();
            }
        }

        void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleLeft,
            };
            _titleStyle.normal.textColor = C_TEXT;

            _mutedStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 10 };
            _mutedStyle.normal.textColor = C_MUTED;

            _monoStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize  = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _monoStyle.normal.textColor = C_ACCENT;

            _cardStyle = new GUIStyle()
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin  = new RectOffset(8, 8, 0, 0),
            };

            _sectionStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, fontStyle = FontStyle.Bold };
            _sectionStyle.normal.textColor = C_MUTED;

            _nsStyle = new GUIStyle(_mutedStyle) { alignment = TextAnchor.MiddleRight };

            _emptyLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 10 };
            _emptyLabelStyle.normal.textColor = C_MUTED;

            _dropLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 10 };

            _fpsStyle = new GUIStyle(_mutedStyle) { alignment = TextAnchor.MiddleRight };
        }

        Texture2D GetCheckerTex(int size)
        {
            int cell = 12;
            if (_checkerTex != null && _checkerTexSize == size) return _checkerTex;

            if (_checkerTex != null) DestroyImmediate(_checkerTex);
            _checkerTexSize = size;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode   = TextureWrapMode.Repeat;

            Color dark  = new Color(0.15f, 0.15f, 0.16f);
            Color light = new Color(0.20f, 0.20f, 0.21f);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = ((x / cell + y / cell) % 2 == 0) ? dark : light;

            tex.SetPixels(pixels);
            tex.Apply();
            _checkerTex = tex;
            return tex;
        }

        void OnGUI()
        {
            if (_proxy == null) RebuildProxy();
            if (_proxy == null) return;
            EnsureStyles();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), C_BG);

            DrawTopBar();

            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

            Sp(6);
            DrawPreviewCard();
            Sp(6);
            DrawSettingsCard();
            Sp(6);
            DrawFramesCard();
            Sp(6);
            DrawSaveCard();
            Sp(12);

            EditorGUILayout.EndScrollView();

            if (_so != null)
            {
                _so.ApplyModifiedPropertiesWithoutUndo();
                if (_proxy != null) frames = _proxy.Frames;
            }
        }

        void DrawTopBar()
        {
            Rect bar = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bar, C_SURFACE);
            EditorGUI.DrawRect(new Rect(bar.x, bar.yMax - 1, bar.width, 1), C_BORDER);
            EditorGUI.DrawRect(new Rect(bar.x, bar.y + 9, 3, bar.height - 18), C_ACCENT);

            GUI.Label(new Rect(bar.x + 14, bar.y, bar.width - 130, bar.height),
                "Anim Clip Editor", _titleStyle);

            GUI.Label(new Rect(bar.xMax - 114, bar.y, 106, bar.height),
                "PixelCut", _nsStyle);
        }

        void DrawPreviewCard()
        {
            SectionLabel("PREVIEW");
            BeginCard(out _);

            float avail  = EditorGUIUtility.currentViewWidth - 40f;
            float size   = Mathf.Clamp(avail, 60f, 240f);

            Rect outer  = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(true));
            float cx    = outer.x + (outer.width - size) * 0.5f;
            Rect canvas = new Rect(cx, outer.y, size, size);

            int texSize = Mathf.ClosestPowerOfTwo((int)size);
            GUI.DrawTexture(canvas, GetCheckerTex(texSize));
            DrawBorder(canvas, C_BORDER);

            bool hasFrames = frames != null && frames.Length > 0;
            if (hasFrames)
            {
                if (previewFrame >= frames.Length) previewFrame = 0;
                Sprite s = frames[previewFrame];
                if (s != null) DrawSpriteFit(canvas, s, 8f);
            }
            else
            {
                GUI.Label(canvas, "No frames — add sprites below", _emptyLabelStyle);
            }

            Sp(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                string ctr = hasFrames ? $"{previewFrame + 1:D2}/{frames.Length:D2}" : "--/--";
                GUILayout.Label(ctr, _monoStyle, GUILayout.Width(44));

                GUILayout.FlexibleSpace();

                bool playing = isPreviewing && hasFrames;

                Btn("◀◀", C_SURFACE, hasFrames ? C_TEXT : C_MUTED, 30, 28, () => {
                    if (hasFrames) { isPreviewing = false; previewFrame = (previewFrame - 1 + frames.Length) % frames.Length; }
                });
                Sp(3);

                Btn(playing ? "||" : "▶", playing ? C_WARN : C_ACCENT, C_BG, 44, 28, () => {
                    if (!hasFrames) return;
                    if (!isPreviewing) { isPreviewing = true; lastFrameTime = EditorApplication.timeSinceStartup; previewTimer = 0; }
                    else isPreviewing = false;
                });
                Sp(3);

                Btn("■", C_SURFACE, C_DANGER, 30, 28, () =>
                    { isPreviewing = false; previewFrame = 0; previewTimer = 0; });
                Sp(3);

                Btn("▶▶", C_SURFACE, hasFrames ? C_TEXT : C_MUTED, 30, 28, () => {
                    if (hasFrames) { isPreviewing = false; previewFrame = (previewFrame + 1) % frames.Length; }
                });

                GUILayout.FlexibleSpace();

                GUILayout.Label($"{frameRate:0.#} fps", _fpsStyle, GUILayout.Width(46));
            }

            Sp(7);

            EditorGUI.BeginDisabledGroup(!hasFrames);
            EditorGUI.BeginChangeCheck();
            int sv = (int)EditorGUILayout.Slider(GUIContent.none, previewFrame, 0, hasFrames ? frames.Length - 1 : 1);
            if (EditorGUI.EndChangeCheck()) { previewFrame = sv; isPreviewing = false; }
            EditorGUI.EndDisabledGroup();

            Sp(6);
            DrawFrameStrip();

            EndCard();
        }

        void DrawFrameStrip()
        {
            const float THUMB = 44f;
            const float GAP   = 3f;
            int count = frames != null ? frames.Length : 0;

            var sep = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sep, C_BORDER);
            Sp(5);

            Rect view    = GUILayoutUtility.GetRect(0, THUMB + 18f, GUILayout.ExpandWidth(true));
            float totalW = count * THUMB + Mathf.Max(0, count - 1) * GAP;

            bool  centered = totalW <= view.width;
            float offsetX  = centered ? (view.width - totalW) * 0.5f : 0f;
            float contW    = centered ? view.width : totalW;

            stripScroll = GUI.BeginScrollView(view, stripScroll,
                new Rect(0, 0, contW, THUMB), !centered, false);

            Event e = Event.current;

            for (int i = 0; i < count; i++)
            {
                float x      = offsetX + i * (THUMB + GAP);
                Rect  r      = new Rect(x, 0, THUMB, THUMB);
                bool  active = (i == previewFrame);

                bool isDropTarget = _dragging && _dragToIndex == i;

                EditorGUI.DrawRect(r, isDropTarget
                    ? new Color(C_ACCENT.r, C_ACCENT.g, C_ACCENT.b, 0.35f)
                    : active
                        ? new Color(C_ACCENT.r, C_ACCENT.g, C_ACCENT.b, 0.18f)
                        : C_SURFACE);

                if (frames[i] != null) DrawSpriteFit(r, frames[i], 2f);

                if (active || isDropTarget) EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2, r.width, 2), C_ACCENT);
                else                        DrawBorder(r, C_BORDER);

                var idxStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize  = 8,
                    alignment = TextAnchor.LowerRight,
                };
                idxStyle.normal.textColor = new Color(C_MUTED.r, C_MUTED.g, C_MUTED.b, 0.7f);
                GUI.Label(new Rect(r.x, r.y, r.width - 2, r.height - 2), i.ToString(), idxStyle);

                if (r.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        previewFrame   = i;
                        isPreviewing   = false;
                        _dragging      = true;
                        _dragFromIndex = i;
                        _dragToIndex   = i;
                        e.Use();
                    }

                    if (e.type == EventType.MouseDown && e.button == 1)
                    {
                        int captured = i;
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Remove Frame"), false, () => RemoveFrame(captured));
                        menu.AddItem(new GUIContent("Duplicate Frame"), false, () => DuplicateFrame(captured));
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Move Left"),  captured == 0,              () => SwapFrames(captured, captured - 1));
                        menu.AddItem(new GUIContent("Move Right"), captured == count - 1,      () => SwapFrames(captured, captured + 1));
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Clear All Frames"), false, () => {
                            if (EditorUtility.DisplayDialog("Clear Frames", "Remove all frames?", "Clear", "Cancel"))
                                ClearFrames();
                        });
                        menu.ShowAsContext();
                        e.Use();
                    }

                    if (_dragging && e.type == EventType.MouseDrag)
                    {
                        _dragToIndex = i;
                        Repaint();
                        e.Use();
                    }
                }
            }

            if (_dragging && e.type == EventType.MouseUp)
            {
                if (_dragFromIndex >= 0 && _dragToIndex >= 0 && _dragFromIndex != _dragToIndex)
                    MoveFrame(_dragFromIndex, _dragToIndex);
                _dragging      = false;
                _dragFromIndex = -1;
                _dragToIndex   = -1;
                e.Use();
            }

            GUI.EndScrollView();
        }

        void DrawSettingsCard()
        {
            SectionLabel("CLIP SETTINGS");
            BeginCard(out _);

            EditorGUI.indentLevel++;
            clipName  = EditorGUILayout.TextField("Name",       clipName);
            frameRate = Mathf.Max(0.1f, EditorGUILayout.FloatField("Frame Rate", frameRate));
            loop      = EditorGUILayout.Toggle("Loop",          loop);
            EditorGUI.indentLevel--;

            EndCard();
        }

        void DrawFramesCard()
        {
            SectionLabel("FRAMES");
            BeginCard(out _);

            _so.Update();
            EditorGUILayout.PropertyField(_framesProp, new GUIContent("Sprites"), true);
            _so.ApplyModifiedPropertiesWithoutUndo();
            if (_proxy != null) frames = _proxy.Frames;

            Sp(8);
            DrawDropZone();

            Sp(8);
            var sep = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sep, C_BORDER);
            Sp(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Load clip", _mutedStyle, GUILayout.Width(60));
                var loaded = (AnimClip)EditorGUILayout.ObjectField(
                    GUIContent.none, null, typeof(AnimClip), false);
                if (loaded != null) LoadClip(loaded);
            }

            EndCard();
        }

        void DrawDropZone()
        {
            Rect r       = GUILayoutUtility.GetRect(0, 44, GUILayout.ExpandWidth(true));
            bool hovering = r.Contains(Event.current.mousePosition)
                            && DragAndDrop.objectReferences.Length > 0;

            EditorGUI.DrawRect(r, hovering
                ? new Color(C_ACCENT.r, C_ACCENT.g, C_ACCENT.b, 0.10f)
                : new Color(0.12f, 0.12f, 0.13f));
            DrawDashedBorder(r, hovering ? C_ACCENT : C_BORDER);

            _dropLabelStyle.normal.textColor = hovering ? C_ACCENT : C_MUTED;
            GUI.Label(r, hovering ? "Release to add sprites" : "▸  Drop sprites / spritesheets here", _dropLabelStyle);

            if (r.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated)
                    { DragAndDrop.visualMode = DragAndDropVisualMode.Copy; Event.current.Use(); Repaint(); }
                else if (Event.current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    var toAdd = new List<Sprite>();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is Sprite sp)
                            toAdd.Add(sp);
                        else if (obj is Texture2D tex)
                            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(tex)))
                                if (a is Sprite s) toAdd.Add(s);
                    }

                    if (toAdd.Count > 0)
                    {
                        var current = new List<Sprite>(frames ?? new Sprite[0]);
                        current.AddRange(toAdd);
                        frames = current.ToArray();
                        RebuildProxy();
                    }

                    Event.current.Use();
                    Repaint();
                }
            }
        }

        void DrawSaveCard()
        {
            SectionLabel("SAVE");
            BeginCard(out _);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Folder", _mutedStyle, GUILayout.Width(46));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(GUIContent.none, saveFolderPath, GUILayout.ExpandWidth(true));
                EditorGUI.EndDisabledGroup();
                Btn("Browse…", C_SURFACE, C_TEXT, 62, 18, PickFolder);
            }

            Sp(2);
            string pn = string.IsNullOrWhiteSpace(clipName) ? "<n>" : clipName;
            GUILayout.Label($"  → {saveFolderPath}/{pn}.asset", _mutedStyle);

            Sp(10);
            Btn("💾   Save AnimClip Asset", C_ACCENT, C_BG, 0, 32, CreateClip);

            Sp(4);
            Btn("↺   Reset to Defaults", C_SURFACE, C_MUTED, 0, 26, () =>
            {
                if (EditorUtility.DisplayDialog("Reset", "Clear all data and restore defaults?", "Reset", "Cancel"))
                    ResetToDefaults();
            });

            EndCard();
        }

        void ResetToDefaults()
        {
            clipName       = "NewAnimClip";
            saveFolderPath = "Assets";
            frameRate      = 10f;
            loop           = true;
            frames         = new Sprite[0];
            isPreviewing   = false;
            previewFrame   = 0;
            previewTimer   = 0;
            RebuildProxy();
            Repaint();
        }

        void BeginCard(out Rect bg)
        {
            bg = EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUI.DrawRect(bg, C_SURFACE);
            DrawBorder(bg, C_BORDER);
        }
        void EndCard() => EditorGUILayout.EndVertical();

        void SectionLabel(string text)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                GUILayout.Label(text, _sectionStyle);
            }
            Sp(2);
        }

        static void Sp(float px) => GUILayout.Space(px);

        void Btn(string label, Color bg, Color fg, float w, float h, System.Action onClick)
        {
            var pBg = GUI.backgroundColor;
            var pFg = GUI.contentColor;
            GUI.backgroundColor = bg;
            GUI.contentColor    = fg;
            var opts = w > 0
                ? new GUILayoutOption[] { GUILayout.Width(w),          GUILayout.Height(h) }
                : new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(h) };
            if (GUILayout.Button(label, opts)) onClick?.Invoke();
            GUI.backgroundColor = pBg;
            GUI.contentColor    = pFg;
        }

        static void DrawBorder(Rect r, Color c)
        {
            EditorGUI.DrawRect(new Rect(r.x,        r.y,        r.width, 1),        c);
            EditorGUI.DrawRect(new Rect(r.x,        r.yMax - 1, r.width, 1),        c);
            EditorGUI.DrawRect(new Rect(r.x,        r.y,        1,       r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y,        1,       r.height), c);
        }

        static void DrawDashedBorder(Rect r, Color c)
        {
            DrawBorder(r, c);
            Rect inner = new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2);
            DrawBorder(inner, new Color(c.r, c.g, c.b, c.a * 0.25f));
        }

        static void DrawSpriteFit(Rect canvas, Sprite s, float padding)
        {
            Texture2D tex = s.texture;
            Rect      sr  = s.textureRect;
            var uv = new Rect(sr.x / tex.width, sr.y / tex.height,
                              sr.width / tex.width, sr.height / tex.height);

            float aspect = sr.width / sr.height;
            float maxW   = canvas.width  - padding * 2;
            float maxH   = canvas.height - padding * 2;
            float drawW  = maxW;
            float drawH  = drawW / aspect;
            if (drawH > maxH) { drawH = maxH; drawW = drawH * aspect; }

            Rect dst = new Rect(
                canvas.x + (canvas.width  - drawW) * 0.5f,
                canvas.y + (canvas.height - drawH) * 0.5f,
                drawW, drawH);
            GUI.DrawTextureWithTexCoords(dst, tex, uv);
        }

        void RemoveFrame(int index)
        {
            var list = new List<Sprite>(frames);
            list.RemoveAt(index);
            frames = list.ToArray();
            if (previewFrame >= frames.Length) previewFrame = Mathf.Max(0, frames.Length - 1);
            RebuildProxy();
            Repaint();
        }

        void DuplicateFrame(int index)
        {
            var list = new List<Sprite>(frames);
            list.Insert(index + 1, frames[index]);
            frames = list.ToArray();
            RebuildProxy();
            Repaint();
        }

        void SwapFrames(int a, int b)
        {
            if (a < 0 || b < 0 || a >= frames.Length || b >= frames.Length) return;
            (frames[a], frames[b]) = (frames[b], frames[a]);
            previewFrame = b;
            RebuildProxy();
            Repaint();
        }

        void MoveFrame(int from, int to)
        {
            var list = new List<Sprite>(frames);
            var item = list[from];
            list.RemoveAt(from);
            list.Insert(to, item);
            frames       = list.ToArray();
            previewFrame = to;
            RebuildProxy();
            Repaint();
        }

        void ClearFrames()
        {
            frames       = new Sprite[0];
            previewFrame = 0;
            isPreviewing = false;
            RebuildProxy();
            Repaint();
        }

        void AppendFrames(IEnumerable<Sprite> toAdd)
        {
            var list = new List<Sprite>(frames ?? new Sprite[0]);
            list.AddRange(toAdd);
            frames = list.ToArray();
        }

        void LoadClip(AnimClip clip)
        {
            clipName     = clip.name;
            frameRate    = clip.FrameRate;
            loop         = clip.Loop;
            frames       = clip.Frames != null ? (Sprite[])clip.Frames.Clone() : new Sprite[0];
            previewFrame = 0;
            isPreviewing = false;
            RebuildProxy();
            Repaint();
        }

        void PickFolder()
        {
            string abs    = Path.GetFullPath(saveFolderPath);
            string chosen = EditorUtility.OpenFolderPanel("Choose Save Folder", abs, "");
            if (string.IsNullOrEmpty(chosen)) return;

            string root = Path.GetFullPath(Application.dataPath + "/..").Replace('\\', '/');
            chosen = chosen.Replace('\\', '/');
            if (!chosen.StartsWith(root)) { BadFolder(); return; }

            string rel = chosen.Substring(root.Length).TrimStart('/');
            if (!rel.StartsWith("Assets")) { BadFolder(); return; }
            saveFolderPath = rel;
        }

        static void BadFolder() =>
            EditorUtility.DisplayDialog("Invalid Folder",
                "Please choose a folder inside the project's Assets directory.", "OK");

        void CreateClip()
        {
            if (string.IsNullOrWhiteSpace(clipName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a clip name.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(saveFolderPath))
            {
                string[] parts   = saveFolderPath.Split('/');
                string   current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{saveFolderPath}/{clipName}.asset");

            var clip = CreateInstance<AnimClip>();
            clip.SetFrames(frames ?? new Sprite[0]);
            clip.SetFrameRate(frameRate);
            clip.SetLoop(loop);

            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = clip;
            Debug.Log($"[AnimClipEditor] Saved → {path}");
        }
    }
}