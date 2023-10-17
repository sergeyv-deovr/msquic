using AOT;
using DeoVR.Net.Quic;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

[Preserve]
public static class RuntimePackageInitializer
{
#if UNITY_ANDROID
    [Preserve]
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void OnBeforeSceneLoadRuntimeMethod()
    {
        if (Application.platform != RuntimePlatform.Android)
            return;

        unsafe
        {
            Quic.ConnectionCallback = ConnectionCallback;
            Quic.StreamCallback = StreamCallback;
        }
        Debug.Log("QUIC callbacks initialized");
    }

    [MonoPInvokeCallback(typeof(Quic.CallbackDelegate))]
    public static unsafe int ConnectionCallback(void* handle, void* context, void* evnt) => Quic.HandleConnectionEvent(handle, context, evnt);

    [MonoPInvokeCallback(typeof(Quic.CallbackDelegate))]
    public static unsafe int StreamCallback(void* handle, void* context, void* evnt) => Quic.HandleStreamEvent(handle, context, evnt);
#endif
}
