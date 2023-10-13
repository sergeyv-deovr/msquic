using AOT;
using DeoVR.QuicNet;
using DeoVR.QuicNet.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class QuicCallbacks
{
    private static bool _isConfigured = false;

    public static void Configure()
    {
#if PLATFORM_ANDROID
        if (_isConfigured) return;
        _isConfigured = true;
        unsafe
        {
            Quic.ConnectionCallback = ConnectionCallback;
            Quic.StreamCallback = StreamCallback;
        }
#endif
    }

#if PLATFORM_ANDROID
    [MonoPInvokeCallback(typeof(Quic.CallbackDelegate))]
    public static unsafe int ConnectionCallback(void* handle, void* context, void* evnt) => Quic.HandleConnectionEvent(handle, context, evnt);

    [MonoPInvokeCallback(typeof(Quic.CallbackDelegate))]
    public static unsafe int StreamCallback(void* handle, void* context, void* evnt) => Quic.HandleStreamEvent(handle, context, evnt);
#endif
}
