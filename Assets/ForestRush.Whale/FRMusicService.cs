using System;
using System.Collections;
using ForestRush.Whale;
using UnityEngine;
namespace ForestRush
{
    public enum MusicFadeMode
    {
        /// <summary>
        /// 先淡出再淡入
        /// </summary>
        FadeOutFadeIn,

        /// <summary>
        /// 交叉淡入淡出
        /// </summary>
        CrossFade,

        /// <summary>
        /// 淡出再播放
        /// </summary>
        FadeOutCutIn,

        /// <summary>
        /// 停止再淡入
        /// </summary>
        CutOutFadeIn,
    }

    public class FRMusicService : MonoBehaviour
    {

        private static FRMusicService s_instance;
        private static bool s_initialized;
        
        public static FRMusicService Instance
        {
            get
            {
                if (!s_initialized && s_instance == null)
                {
                    var go = new GameObject("FRMusicService");
                    s_instance = go.AddComponent<FRMusicService>();
                    s_initialized = true;
                }
                return s_instance;
            }
        }
        
        
        private FRAudioEmitter musicEmitter;
        
        private void FadeOut(float fadeOut)
        {
            if (musicEmitter.IsValid())
                musicEmitter.FadeOut(fadeOut);
        }

        public void Play(string key, MusicFadeMode mode = MusicFadeMode.CrossFade, float fadeTime = 1.0f, bool cutInIfNotMusic = true)
        {
            if (!musicEmitter.IsValid() && cutInIfNotMusic)
            {
                musicEmitter = FRAudioManager.Instance.Play(key);
                return;
            }
            StopAllCoroutines();
            StartCoroutine(PlayCoroutine(mode, fadeTime, () =>
            {
                musicEmitter = FRAudioManager.Instance.FadeIn(key, fadeTime);
            }));
        }

        public void Play(AudioName key, MusicFadeMode mode = MusicFadeMode.CrossFade, float fadeTime = 1.0f, bool cutInIfNotMusic = true)
        {
            if (!musicEmitter.IsValid() && cutInIfNotMusic)
            {
                musicEmitter = FRAudioManager.Instance.Play(key);
                return;
            }
            StopAllCoroutines();
            StartCoroutine(PlayCoroutine(mode, fadeTime, () =>
            {
                musicEmitter = FRAudioManager.Instance.FadeIn(key, fadeTime);
            }));
        }


        private IEnumerator PlayCoroutine(MusicFadeMode mode, float fadeTime, Action playMusic)
        {
            switch (mode)
            {
                case MusicFadeMode.FadeOutFadeIn:
                    FadeOut(fadeTime);
                    yield return new WaitForSecondsRealtime(fadeTime);
                    break;
                case MusicFadeMode.CrossFade:
                    FadeOut(fadeTime);
                    break;
                case MusicFadeMode.FadeOutCutIn:
                    FadeOut(fadeTime);
                    yield return new WaitForSecondsRealtime(fadeTime);
                    break;
                case MusicFadeMode.CutOutFadeIn:
                    musicEmitter.Stop();
                    break;
            }
            playMusic();
        }

        public void Stop(float fadeOut = 1.0f)
        {
            FadeOut(fadeOut);
        }
    }
}
