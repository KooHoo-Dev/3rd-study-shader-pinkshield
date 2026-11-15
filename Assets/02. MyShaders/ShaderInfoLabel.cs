// 직접 만든 셰이더 3종(지면 범위 표시, 불꽃 스킬, 창밖 풍경)의 설명을 Game 뷰의 정해진 자리에 띄운다.
// 플레이를 시작하면 해당 셰이더를 쓰는 오브젝트를 자동으로 찾아 설명 패널을 붙인다. 씬을 고칠 필요가 없다.
// 배치: 지면 범위 표시는 왼쪽 중단 위, 불꽃 스킬은 그 아래, 창밖 풍경은 오른쪽 도로 아래.
// 패널에는 이름, 설명, 원리 한 줄, 머티리얼의 현재 값(실시간)이 보인다. 왼쪽 위 버튼으로 모든 패널을 숨기거나 다시 보인다.
using System.Collections.Generic;
using UnityEngine;

namespace ShaderGallery.Samples
{
    public class ShaderInfoLabel : MonoBehaviour
    {
        public enum Dock
        {
            LeftMiddleTop,
            LeftMiddleBottom,
            RightBelowRoad
        }

        struct Info
        {
            public string title;
            public string description;
            public string how;
            public string[] props;
            public Dock dock;
            public Color accent;
        }

        // 셰이더 이름 -> 보여 줄 설명과 자리. 새 셰이더를 만들면 여기에 한 줄을 더하면 된다.
        static readonly Dictionary<string, Info> Presets = new Dictionary<string, Info>
        {
            {
                "ShaderGallery/GroundAoeIndicator", new Info
                {
                    title = "지면 범위 표시",
                    description = "몬스터 공격 예고. 바닥에 범위를 그리고, 시전이 차오를수록 붉게 깜빡이며 줄무늬로 위험을 알리고 끝에서 번쩍인다.",
                    how = "깊이 텍스처로 바닥 위치를 되살려 원, 고리, 부채꼴을 투영한다.",
                    props = new[] { "_Fill", "_PulseSpeed", "_PulseDepth", "_StripeStrength", "_Radius" },
                    dock = Dock.LeftMiddleTop,
                    accent = new Color(1f, 0.38f, 0.25f)
                }
            },
            {
                "ShaderGallery/FireNoiseFlameSkill", new Info
                {
                    title = "불꽃 스킬",
                    description = "몬스터 화염. 1, 2, 3단계로 커지고 기본 불에서 푸른 불로 바뀌며, 스킬이 끝나면 줄어들어 사라진다.",
                    how = "노이즈 두 층을 곱해 불꽃 모양으로 깎고, 단계 값으로 크기와 색을 바꾼다.",
                    props = new[] { "_Stage", "_Intensity", "_FlowSpeed" },
                    dock = Dock.LeftMiddleBottom,
                    accent = new Color(1f, 0.7f, 0.2f)
                }
            },
            {
                "ShaderGallery/WindowView", new Info
                {
                    title = "창밖 풍경",
                    description = "창 한 장으로 거리를 내다본다. 맞은편 건물의 입체 방, 도로를 오가는 차량과 사람, 밤의 불빛이 보인다.",
                    how = "눈에서 창을 지나는 시선이 건물, 도로, 차량 층과 만나는 점을 계산해 시차가 생긴다.",
                    props = new[] { "_Night", "_LightsOn", "_CarPeriod", "_WalkSpeed", "_BuildingDistance" },
                    dock = Dock.RightBelowRoad,
                    accent = new Color(0.35f, 0.65f, 1f)
                }
            },
        };

        static bool showAll = true;
        static Texture2D whiteTex;
        static ShaderInfoLabel buttonOwner;

        [SerializeField] string title;
        [SerializeField, TextArea] string description;
        [SerializeField, TextArea] string how;
        [SerializeField] string[] props;
        [SerializeField] Dock dock;
        [SerializeField] Color accent = Color.white;
        [Tooltip("값을 읽을 렌더러. 비우면 이 오브젝트의 렌더러를 쓴다.")]
        [SerializeField] Renderer target;

        Material material;
        GUIStyle titleStyle, bodyStyle, howStyle, nameStyle, valueStyle, buttonStyle;

        // 플레이를 시작하면 세 셰이더를 쓰는 렌더러를 찾아 설명 패널을 붙인다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                var mat = r.sharedMaterial;
                if (mat == null || mat.shader == null || !Presets.TryGetValue(mat.shader.name, out var info))
                {
                    continue;
                }
                if (r.GetComponent<ShaderInfoLabel>() != null)
                {
                    continue;
                }
                var label = r.gameObject.AddComponent<ShaderInfoLabel>();
                label.title = info.title;
                label.description = info.description;
                label.how = info.how;
                label.props = info.props;
                label.dock = info.dock;
                label.accent = info.accent;
                label.target = r;
            }
        }

        void Start()
        {
            if (target == null)
            {
                target = GetComponent<Renderer>();
            }
            if (target != null)
            {
                // 다른 스크립트가 만든 복사본과 같은 것을 읽는다 (renderer.material 은 이미 만들어졌으면 그대로 돌려준다).
                material = target.material;
            }
        }

        void OnGUI()
        {
            if (whiteTex == null)
            {
                whiteTex = new Texture2D(1, 1);
                whiteTex.SetPixel(0, 0, Color.white);
                whiteTex.Apply();
            }
            float scale = Mathf.Clamp(Screen.height / 720f, 0.8f, 2f);
            BuildStyles(scale);

            // 여러 패널 중 하나만 숨기기 버튼을 그린다.
            if (buttonOwner == null || !buttonOwner.isActiveAndEnabled)
            {
                buttonOwner = this;
            }
            if (buttonOwner == this)
            {
                DrawToggle(scale);
            }
            if (!showAll)
            {
                return;
            }

            float pad = 12f * scale;
            float width = 330f * scale;
            float inner = width - pad * 2f - 4f * scale;
            float rowH = 18f * scale;
            float gap = 6f * scale;

            GUIContent titleC = new GUIContent(title);
            GUIContent bodyC = new GUIContent(description);
            GUIContent howC = new GUIContent(how);
            float titleH = titleStyle.CalcHeight(titleC, inner);
            float bodyH = bodyStyle.CalcHeight(bodyC, inner);
            float howH = howStyle.CalcHeight(howC, inner);

            int rows = 0;
            if (material != null && props != null)
            {
                foreach (string p in props)
                {
                    if (material.HasProperty(p))
                    {
                        rows++;
                    }
                }
            }
            float height = pad + titleH + gap + bodyH + gap + howH + pad * 0.5f;
            if (rows > 0)
            {
                height += gap + 1f + gap + rows * rowH;
            }
            height += pad * 0.5f;

            Rect rect = PlaceRect(width, height, scale);

            // 배경, 왼쪽 강조 막대
            Fill(rect, new Color(0.06f, 0.07f, 0.09f, 0.86f));
            Fill(new Rect(rect.x, rect.y, 4f * scale, rect.height), accent);

            float x = rect.x + pad + 4f * scale;
            float y = rect.y + pad;
            titleStyle.normal.textColor = accent;
            GUI.Label(new Rect(x, y, inner, titleH), titleC, titleStyle);
            y += titleH + gap;
            GUI.Label(new Rect(x, y, inner, bodyH), bodyC, bodyStyle);
            y += bodyH + gap;
            GUI.Label(new Rect(x, y, inner, howH), howC, howStyle);
            y += howH + pad * 0.5f;

            if (rows > 0)
            {
                y += gap;
                Fill(new Rect(x, y, inner, 1f), new Color(1f, 1f, 1f, 0.15f));
                y += 1f + gap;
                foreach (string p in props)
                {
                    if (!material.HasProperty(p))
                    {
                        continue;
                    }
                    GUI.Label(new Rect(x, y, inner * 0.65f, rowH), p, nameStyle);
                    GUI.Label(new Rect(x + inner * 0.65f, y, inner * 0.35f, rowH), material.GetFloat(p).ToString("0.00"), valueStyle);
                    y += rowH;
                }
            }
        }

        // 자리: 왼쪽 중단 위 / 아래는 화면 세로 가운데를 경계로 위아래로, 오른쪽은 도로 아래(화면 아래쪽 28% 부근)에 둔다.
        Rect PlaceRect(float width, float height, float scale)
        {
            float margin = 12f * scale;
            float half = Screen.height * 0.5f;
            float x = margin;
            float y;
            switch (dock)
            {
                case Dock.LeftMiddleBottom:
                    y = half + margin * 0.5f;
                    break;
                case Dock.RightBelowRoad:
                    x = Screen.width - width - margin;
                    y = Screen.height * 0.72f;
                    break;
                default:
                    y = half - height - margin * 0.5f;
                    break;
            }
            y = Mathf.Clamp(y, margin, Screen.height - height - margin);
            return new Rect(x, y, width, height);
        }

        void BuildStyles(float scale)
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
                bodyStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
                howStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontStyle = FontStyle.Italic };
                nameStyle = new GUIStyle(GUI.skin.label);
                valueStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
                buttonStyle = new GUIStyle(GUI.skin.button);
                bodyStyle.normal.textColor = new Color(0.9f, 0.92f, 0.95f);
                howStyle.normal.textColor = new Color(0.62f, 0.67f, 0.75f);
                nameStyle.normal.textColor = new Color(0.62f, 0.67f, 0.75f);
                nameStyle.alignment = TextAnchor.MiddleLeft;
                valueStyle.normal.textColor = Color.white;
            }
            titleStyle.fontSize = Mathf.RoundToInt(17 * scale);
            bodyStyle.fontSize = Mathf.RoundToInt(13 * scale);
            howStyle.fontSize = Mathf.RoundToInt(12 * scale);
            nameStyle.fontSize = Mathf.RoundToInt(12 * scale);
            valueStyle.fontSize = Mathf.RoundToInt(12 * scale);
            buttonStyle.fontSize = Mathf.RoundToInt(13 * scale);
        }

        void DrawToggle(float scale)
        {
            float margin = 12f * scale;
            if (GUI.Button(new Rect(margin, margin, 150f * scale, 28f * scale), showAll ? "셰이더 설명 숨기기" : "셰이더 설명 보기", buttonStyle))
            {
                showAll = !showAll;
            }
        }

        static void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, whiteTex);
            GUI.color = old;
        }

        void OnDestroy()
        {
            if (buttonOwner == this)
            {
                buttonOwner = null;
            }
        }
    }
}
