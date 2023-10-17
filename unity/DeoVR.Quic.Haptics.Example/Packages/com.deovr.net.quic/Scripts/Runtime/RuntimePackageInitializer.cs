using AOT;
using DeoVR.Net.Quic;
using UnityEngine;

public class RuntimePackageInitializer
{
    //#if PLATFORM_ANDROID
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoadRuntimeMethod()
    {
        // Your initialization logic here

        Debug.Log("Initializing QUIC callbacks!");
        unsafe
        {
            Quic.ConnectionCallback = ConnectionCallback;
            Quic.StreamCallback = StreamCallback;
        }
    }

    [MonoPInvokeCallback(typeof(Quic.CallbackDelegate))]
    public static unsafe int ConnectionCallback(void* handle, void* context, void* evnt) => Quic.HandleConnectionEvent(handle, context, evnt);

    [MonoPInvokeCallback(typeof(Quic.CallbackDelegate))]
    public static unsafe int StreamCallback(void* handle, void* context, void* evnt) => Quic.HandleStreamEvent(handle, context, evnt);
//#endif
}