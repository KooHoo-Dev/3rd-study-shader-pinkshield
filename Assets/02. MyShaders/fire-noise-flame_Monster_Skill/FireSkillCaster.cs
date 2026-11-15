// 몬스터 불꽃 스킬: _Stage 를 0 에서 1, 2, 3 단계로 올려 불꽃을 키우고 푸른 불로 바꾼 뒤, 0 으로 줄여 없앤다. 불꽃 Quad 의 MeshRenderer 에 붙여 쓴다.
using System.Collections;
using UnityEngine;

namespace ShaderGallery.Samples
{
    [RequireComponent(typeof(Renderer))]
    public class FireSkillCaster : MonoBehaviour
    {
        static readonly int Stage = Shader.PropertyToID("_Stage");

        // 각 단계를 유지하는 시간 (초). 1, 2, 3 단계 순서
        [SerializeField] float[] stageHold = { 0.8f, 0.8f, 1.0f };
        // 단계 사이를 부드럽게 넘어가는 시간 (초)
        [SerializeField] float growSeconds = 0.3f;
        // 스킬이 끝나고 불꽃이 줄어 사라지는 시간 (초)
        [SerializeField] float vanishSeconds = 0.4f;

        Renderer target;
        Material material;

        /// <summary>지금 재생 중인지 (사라지는 동안 포함).</summary>
        public bool IsPlaying { get; private set; }

        void Awake()
        {
            target = GetComponent<Renderer>();
            // 이 불꽃 전용 복사본을 쓴다. 다른 몬스터의 불꽃이 함께 커지지 않는다.
            material = target.material;
            Hide();
        }

        /// <summary>1 → 2 → 3 단계로 키우고 유지한 뒤 사라지게 한다.</summary>
        public void Play()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            StopAllCoroutines();
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            IsPlaying = true;
            target.enabled = true;
            float current = 0f;
            for (int s = 1; s <= 3; s++)
            {
                yield return Ramp(current, s, growSeconds);
                current = s;
                float hold = s - 1 < stageHold.Length ? stageHold[s - 1] : 0.5f;
                yield return new WaitForSeconds(hold);
            }
            // 스킬이 끝나면 불꽃을 줄여 없앤다.
            yield return Ramp(current, 0f, vanishSeconds);
            Hide();
        }

        IEnumerator Ramp(float from, float to, float seconds)
        {
            // Time.deltaTime 으로 쌓아 일시정지와 슬로모션을 따른다.
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                material.SetFloat(Stage, Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            material.SetFloat(Stage, to);
        }

        void Hide()
        {
            IsPlaying = false;
            if (material != null)
            {
                material.SetFloat(Stage, 0f);
            }
            if (target != null)
            {
                target.enabled = false;
            }
        }

        void OnDisable()
        {
            StopAllCoroutines();
            Hide();
        }

        void OnDestroy()
        {
            Destroy(material);
        }
    }
}
