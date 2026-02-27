using UnityEngine;
using System.Collections.Generic;

namespace Singleton
{
    /// <summary>
    /// 공용 게이트:
    /// - Release 중/앱 종료 중 Instance 접근 및 재생성을 막기 위한 전역 상태
    /// </summary>
    public static class SingletonGate
    {
        public static bool IsAble { get; private set; } = true;

        // 외부에서 직접 set 못하게 하고, depth로 관리
        public static bool IsReleasing => _releaseDepth > 0;

        // release 중첩 카운트
        private static int _releaseDepth = 0;

        public static bool IsBlocked => !IsAble || IsReleasing;

        public static void Enable() => IsAble = true;
        public static void Disable() => IsAble = false;

        public static void BeginRelease()
        {
            // 첫 진입이면 0 -> 1
            _releaseDepth++;
            if (_releaseDepth < 0) _releaseDepth = 1;
        }

        public static void EndRelease()
        {
            _releaseDepth--;
            if (_releaseDepth < 0) _releaseDepth = 0;
        }

        // 선택: 강제 리셋이 필요할 때 사용
        public static void ResetReleaseState()
        {
            _releaseDepth = 0;
        }
    }

    /// <summary>
    /// SingletonGateHook는 싱글톤 시스템의 공용 게이트를 관리하는 MonoBehaviour입니다.
    /// 자동으로 씬에 배치되며, 앱 시작 시 자동으로 설치되어 싱글톤 시스템이 정상적으로 작동하도록 보장합니다.
    /// </summary>
    public sealed class SingletonGateHook : MonoBehaviour
    {
        private static SingletonGateHook s_instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInstall()
        {
            if (s_instance != null) return;

            var go = new GameObject("[Bootstrap] SingletonGateHook");
            DontDestroyOnLoad(go);

            go.AddComponent<SingletonGateHook>();
            s_instance = go.GetComponent<SingletonGateHook>();

            // 기본 상태(선택)
            SingletonGate.Enable();
        }

        private void Awake()
        {
            // 만약 씬에 수동으로도 넣었을 때 중복 제거
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                s_instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            SingletonGate.Disable();
        }
    }

    /// <summary>
    /// 이 인터페이스를 구현한 싱글톤만 "즉시 초기화(등록 시 바인딩+초기화)"를 허용합니다.
    /// 기본은 일괄 초기화(InitSingletons 호출 시).
    /// </summary>
    public interface IAllowImmediateInitSingleton { }

    /// <summary>
    /// 싱글톤 Data는 MonoBehaviour가 아니므로, 씬에 존재하지 않고 코드에서 직접 생성됩니다.
    /// </summary>
    namespace Data
    {
        public abstract class SingletonData
        {
            // HashSet은 중복을 방지하고, List는 순서를 보장하기 위해 사용
            private static readonly HashSet<SingletonData> singletonSet = new HashSet<SingletonData>();
            private static readonly List<SingletonData> singletonList = new List<SingletonData>();

            protected static void PushSingleton(SingletonData _obj)
            {
                if (_obj != null)
                {
                    if (!singletonSet.Add(_obj))
                        return;

                    singletonList.Add(_obj);
                }
            }

            public static bool InitSingletons()
            {
                singletonList.RemoveAll(x => x == null);

                int count = singletonList.Count;
                for (int n = 0; n < count; ++n)
                {
                    if (singletonList[n] != null)
                    {
                        if (singletonList[n].Initialize() == false)
                            return false;
                    }
                }

                return true;
            }

            public static void ReleaseSingletons()
            {
                SingletonGate.BeginRelease();
                singletonList.RemoveAll(x => x == null);

                try
                {
                    // 역순으로 해제
                    int count = singletonList.Count;
                    for (int n = count - 1; n >= 0; --n)
                    {
                        if (singletonList[n] != null)
                        {
                            singletonList[n].ReleaseSingleton();
                        }
                    }
                    singletonList.Clear();
                    singletonSet.Clear();
                }
                finally
                {
                    SingletonGate.EndRelease();
                }
            }

            public abstract bool Initialize();
            protected abstract void ReleaseSingleton();
        }

        public abstract class SingletonData<T> : SingletonData where T : SingletonData, new()
        {
            protected static T m_instance = null;
            public static T Instance
            {
                get
                {
                    if (SingletonGate.IsBlocked)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        throw new System.Exception($"SingletonData.Instance({typeof(T).Name}) accessed while releasing/quitting.");
#else
                        return null;
#endif
                    }

                    if (m_instance == null)
                    {
                        m_instance = new T();
                        PushSingleton(m_instance);
                    }
                    return m_instance;
                }
            }

            private bool _initialized = false;

            public bool IsInitialized { get => _initialized; }

            public static bool HasInstance
            {
                get
                {
                    if (SingletonGate.IsBlocked) return false;
                    return m_instance != null;
                }
            }

            public static bool TryGetInstance(out T inst)
            {
                inst = null;
                if (SingletonGate.IsBlocked) return false;

                inst = m_instance;
                return inst != null;
            }

            public override bool Initialize()
            {
                if (!_initialized)
                {
                    _initialized = InitInstance();
                    return _initialized;
                }

                return true;
            }

            protected override void ReleaseSingleton()
            {
                _initialized = false;
                ReleaseInstance();

                m_instance = null;
            }

            protected abstract bool InitInstance();
            protected abstract void ReleaseInstance();
        }
    }

    /// <summary>
    /// 싱글톤 Component는 MonoBehaviour이므로, 씬에 존재해야 하며, Awake에서 Instance에 바인딩됩니다.
    /// </summary>
	namespace Component
    {
        public abstract class SingletonComponent : MonoBehaviour
        {
            // 추가적인 내부 보호(선택): 공용 게이트 외에 내부 플래그로도 방어
            protected static bool _isProcessing_Release = false;

            private static readonly HashSet<SingletonComponent> singletonSet = new HashSet<SingletonComponent>();
            private static readonly List<SingletonComponent> singletonList = new List<SingletonComponent>();

            private static GameObject DontDestroyParent = null;

            private bool _didAwakeSingleton = false;

            internal void EnsureAwakeSingletonOnce()
            {
                if (!_didAwakeSingleton)
                {
                    _didAwakeSingleton = true;
                    AwakeSingleton();
                }
            }

            internal void ResetAwakeSingletonFlag()
            {
                _didAwakeSingleton = false;
            }

            protected static void PushSingleton(SingletonComponent _obj)
            {
                if (_obj != null)
                {
                    InitInstanceDontDestroyParent();

                    if (_obj.transform.parent != DontDestroyParent.transform)
                        _obj.transform.SetParent(DontDestroyParent.transform, true);

                    if (!singletonSet.Add(_obj))
                        return;

                    singletonList.Add(_obj);

                    if (!SingletonGate.IsBlocked && !_isProcessing_Release && _obj is IAllowImmediateInitSingleton)
                    {
                        _obj.EnsureAwakeSingletonOnce();
                        _obj.Initialize();
                    }
                }
            }

            /// <summary>
            /// 일괄 초기화: 아직 바인딩 안 된 것은 바인딩, 아직 초기화 안 된 것은 초기화.
            /// 즉시 초기화된 인스턴스도 여기서 안전하게 "누락 보정"됨.
            /// </summary>
			public static bool InitSingletons()
            {
                singletonList.RemoveAll(x => x == null);

                int count = singletonList.Count;

                for (int n = 0; n < count; n++)
                {
                    if (singletonList[n] != null)
                    {
                        singletonList[n].EnsureAwakeSingletonOnce();
                    }
                }

                for (int n = 0; n < count; ++n)
                {
                    if (singletonList[n] != null)
                    {
                        if (singletonList[n].Initialize() == false)
                            return false;
                    }
                }

                return true;
            }

            public static void ReleaseSingletons()
            {
                SingletonGate.BeginRelease();
                singletonList.RemoveAll(x => x == null);
                _isProcessing_Release = true;

                try
                {
                    int count = singletonList.Count;

                    //코루틴 종료
                    for (int n = count - 1; n >= 0; --n)
                    {
                        if (singletonList[n] != null)
                            singletonList[n].StopAllCoroutines();
                    }

                    //하위의 컴포넌트들이 해제중인 매니져에 접근하면 다시 생성되기 때문에
                    //모두 비활성화 시킨 후 해제 시키도록 한다
                    for (int n = count - 1; n >= 0; --n)
                    {
                        SingletonComponent _component = singletonList[n];
                        if (_component != null && _component.gameObject.activeInHierarchy)
                            _component.gameObject.SetActive(false);
                    }

                    //비활성화가 완료되면 모두 해제
                    for (int n = count - 1; n >= 0; --n)
                    {
                        if (singletonList[n] != null)
                        {
                            singletonList[n].ResetAwakeSingletonFlag();
                            singletonList[n].ReleaseSingleton();
                        }
                    }

                    singletonList.Clear();
                    singletonSet.Clear();

                    RenewDontDestroyParent();
                }
                finally
                {
                    _isProcessing_Release = false;
                    SingletonGate.EndRelease();
                }
            }

            protected abstract void AwakeSingleton();
            public abstract bool Initialize();
            protected abstract void ReleaseSingleton();

            private static void InitInstanceDontDestroyParent()
            {
                if (DontDestroyParent == null)
                {
                    DontDestroyParent = new GameObject("Singletons");
                    GameObject.DontDestroyOnLoad(DontDestroyParent);
                }
            }

            private static void RenewDontDestroyParent()
            {
                if (DontDestroyParent != null)
                {
                    GameObject.Destroy(DontDestroyParent);
                    DontDestroyParent = null;
                    InitInstanceDontDestroyParent();
                }
            }
        }

        public abstract class SingletonComponent<T> : SingletonComponent where T : MonoBehaviour
        {
            protected static T m_instance;

            public static bool IsQuitting()
            {
                return (SingletonGate.IsBlocked || _isProcessing_Release);
            }

            public static T Instance
            {
                get
                {
                    if (IsQuitting())
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        throw new System.Exception("Instance(" + typeof(T) + ") already destroyed on application quit.");
#else
                        return null;
#endif
                    }

                    if (m_instance == null)
                    {
#if UNITY_2023_1_OR_NEWER
                        T _instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
                        T _instance = FindObjectOfType<T>(true);
#endif

                        if (_instance == null)
                        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            throw new System.Exception(
                                $"[{typeof(T).Name}] not found in scene.\n" +
                                $"Place it in the scene (or a bootstrap prefab) BEFORE calling InitSingletons()."
                            );
#else
                            return null;
#endif
                        }
                        else
                        {
                            m_instance = _instance;
                            PushSingleton(m_instance as SingletonComponent);
                        }
                    }

                    return m_instance;
                }
            }

            private bool _initialized = false;
            public bool isInitialized { get { return _initialized; } }

            public static bool HasInstance
            {
                get
                {
                    if (IsQuitting()) return false;
                    return m_instance != null;
                }
            }

            public static bool TryGetInstance(out T inst)
            {
                inst = null;
                if (IsQuitting()) return false;

                inst = m_instance;
                return inst != null;
            }

            public override bool Initialize()
            {
                if (!_initialized)
                {
                    _initialized = true;
                    return InitInstance();
                }

                return true;
            }

            protected override void AwakeSingleton()
            {
                AwakeInstance();
            }

            protected override void ReleaseSingleton()
            {
                _initialized = false;

                if (null != m_instance)
                {
                    ReleaseInstance();
                    Destroy(m_instance.gameObject);
                }

                m_instance = null;
            }

            protected abstract void AwakeInstance();
            protected abstract bool InitInstance();
            protected abstract void ReleaseInstance();


            private void Awake()
            {
                useGUILayout = false;

                if (m_instance == null)
                {
                    m_instance = this as T;
                    PushSingleton(m_instance as SingletonComponent);
                }
                else if (this != m_instance)
                {
                    Destroy(this.gameObject);
                }
            }

            private void OnApplicationQuit()
            {
                if (_initialized)
                {
                    //종료되고 있을때는 ReleaseSingleton()을 호출하지 말것
                    ReleaseInstance();
                }

                m_instance = null;
            }

            private void OnDestroy()
            {
                if (m_instance == this)
                    m_instance = null;
            }
        }
    }
}
