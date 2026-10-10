using System;
using System.Threading;
using UnityEngine;

namespace Game
{
    // 企劃不做精緻動畫，簡單的逐格內插就夠，不另外引入補間套件
    internal static class Tween
    {
        public static async Awaitable Run(float duration, Action<float> apply, CancellationToken token)
        {
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                apply(time / duration);
                await Awaitable.NextFrameAsync(token);
            }
            apply(1);
        }

        public static float EaseOut(float t) => 1 - (1 - t) * (1 - t);
    }
}
