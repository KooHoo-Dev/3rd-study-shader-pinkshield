// 지면 범위 표시를 플레이 중 계속 되풀이해 재생하는 테스트용 컴포넌트. GroundIndicatorCharge 와 같은 오브젝트에 붙여 쓴다.
using System.Collections;
using UnityEngine;

namespace ShaderGallery.Samples
{
    [RequireComponent(typeof(GroundIndicatorCharge))]
    public class GroundIndicatorTester : MonoBehaviour
    {
        // 한 번 시전하는 데 걸리는 시간 (초)
        [SerializeField] float castDuration = 1.5f;
        // 시전이 끝나고 다음 시전까지 쉬는 시간 (초)
        [SerializeField] float interval = 1f;
        // 켜 두면 플레이 시작 직후 바로 첫 시전을 한다
        [SerializeField] bool startImmediately = true;

        // 예고가 끝나면 이어서 터뜨릴 불꽃 스킬. 비워 두면 예고만 되풀이한다
        [SerializeField] FireSkillCaster fire;

        GroundIndicatorCharge charge;

        void Awake()
        {
            charge = GetComponent<GroundIndicatorCharge>();
        }

        void OnEnable()
        {
            StartCoroutine(Loop());
        }

        IEnumerator Loop()
        {
            if (!startImmediately)
            {
                yield return new WaitForSeconds(interval);
            }
            while (true)
            {
                charge.Cast(castDuration);
                // 시전이 시작될 때까지 한 프레임 기다린 뒤, 끝날 때까지 기다린다.
                yield return null;
                while (charge.IsCasting)
                {
                    yield return null;
                }
                // 예고가 끝나면 불꽃 스킬을 터뜨리고, 불꽃이 사라질 때까지 기다린다.
                if (fire != null)
                {
                    fire.Play();
                    yield return null;
                    while (fire.IsPlaying)
                    {
                        yield return null;
                    }
                }
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
