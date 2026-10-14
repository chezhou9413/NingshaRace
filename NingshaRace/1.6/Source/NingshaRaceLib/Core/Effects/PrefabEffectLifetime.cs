#if RW_1_7
using System.Collections;
using UnityEngine;

namespace NingshaRaceLib.Core.Effects
{
    //通过引擎协程管理一次性预制体的延迟销毁。
    public sealed class PrefabEffectLifetime : MonoBehaviour
    {
        //保留旧 Destroy 延迟使用的游戏时间语义。
        public void Begin(float seconds) { StartCoroutine(Expire(seconds)); }

        //实例提前销毁时，其协程随组件一起取消。
        private IEnumerator Expire(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Object.Destroy(gameObject);
        }
    }
}
#endif
