using System;
using System.Collections;
using System.Text.Json;
using System.Threading.Tasks;
using DeoVR.Net.Quic;
using DeoVR.Net.Quic.Core;
using DeoVR.QuicNet.Haptics;
using Microsoft.Quic;
using TMPro;
using UnityEngine;

public class HapticsExample : MonoBehaviour
{
    [SerializeField] private RectTransform _gauge;
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private TextMeshProUGUI _signalCounterLabel;
    [SerializeField] private TextMeshProUGUI _lastSignalLabel;

    private readonly string _deviceId = Guid.NewGuid().ToString();
    private HapticApi _hapticApi;
    private QuicContext _quicContext;
    private QuicConnection _connection;
    private QuicStream _stream;
    private HapticStream _hapticStream;

    private Signal _s1;
    private Signal _s2;
    private float _t1;
    private float _t2;
    private int _signalsCount = 0;

    private void Start()
    {
        _hapticApi = new HapticApi("https://haptics-stg.infomediji.com", _deviceId);
        _quicContext = Quic.Open(new QuicSettings
        {
            CustomAlpn = "haptics",
            CredentialFlags = QUIC_CREDENTIAL_FLAGS.CLIENT | QUIC_CREDENTIAL_FLAGS.NO_CERTIFICATE_VALIDATION
        });

    }

    private void OnDestroy()
    {
        _hapticStream?.Dispose();
        _stream?.Dispose();
        _connection?.Dispose();
        _quicContext?.Dispose();

        _hapticApi.Dispose();
    }

    private void Update()
    {
        UpdateGauge();
    }

    private IEnumerator ReadFramesCouroutine()
    {
        while (_hapticStream != null)
        {
            if (_s2 != null)
            {
                yield return new WaitForEndOfFrame();
                continue;
            }

            var time = Time.unscaledTime * 1000; // milliseconds time

            if (!_hapticStream.Stream.IsActive)
                continue;

            if (!_hapticStream.ReadNextFrame(out var frame))
                continue;

            if (frame.FrameType != FrameType.Signal)
                continue;

            _signalCounterLabel.text = $"{_signalsCount++} | {_hapticStream.FramesCount}";

            var signal = frame.AsSignal();
            _lastSignalLabel.text = $"{signal}";
            if (_s1 == null)
            {
                _s1 = signal;
                _t1 = time;
            }
            else
            {
                _t2 = _t1 + (float)(signal.TimestampMs - _s1.TimestampMs);
                if (_t2 > time)
                    _s2 = signal;
            }
        }
    }

    private void UpdateGauge()
    {
        // t = Mathf.InverseLerp(-1f, 1f, Mathf.Sin(Time.unscaledTime));
        if (_hapticStream == null)
            return;
        if (!_hapticStream.Stream.IsActive)
            return;

        var time = Time.unscaledTime * 1000; // milliseconds time
        if (_s1 != null && _s2 != null)
        {
            var moment = Mathf.InverseLerp(_t1, _t2, time);
            var value = Mathf.Lerp((float)_s1.Value, (float)_s2.Value, moment);
            SetGauge(value);

            if (time > _t2)
            {
                _s1 = _s2;
                _t1 = _t2;
                _s2 = null;
            }
        }
    }

    private void SetGauge(float value)
    {
        value = Mathf.InverseLerp(-1f, 1f, value);
        value = Mathf.Clamp01(value);

        _label.text = $"{value:0.0}";
        _signalCounterLabel.text = $"{_signalsCount} | {_hapticStream?.FramesCount ?? 0}";

        var parentHeight = ((RectTransform)_gauge.parent).rect.height;
        _gauge.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value * parentHeight);
    }

    public async void Connect()
    {
        Disconnect();

        Debug.Log("Connect");
        if (_connection?.IsActive ?? false) return;
        if (_connection?.IsOpen ?? false) return;

        Debug.Log("Loading publications");
        var publications = await GetPublications(_hapticApi);
        if (publications == null) return;

        var publication = publications[0];
        //if (publications.Length > 1)
        //{
        //    Debug.Log("Available publications:");
        //    for (var i = 0; i < publications.Length; i++)
        //    {
        //        Debug.Log($"{i}: {JsonSerializer.Serialize(publications[i])}");
        //    }
        //    Debug.Log("Enter publication index to subscribe:");
        //    var id = int.Parse(Console.ReadLine());
        //    publication = publications[id];
        //}

        Debug.Log($"Subscribing to publication: {publication.publication_id}");
        var auth = await Subscribe(_hapticApi, publication);
        if (auth == null)
        {
            Debug.LogError("Failed to subscribe");
            return;
        }

        Debug.Log("Creating quic connection");
        _connection = _quicContext.CreateConnection(new QuicConnectionSettings
        {
            Host = "46.101.110.207",
            Port = 50000
        });

        Debug.Log("Connecting");
        await _connection.OpenAsync();
        if (!_connection.IsActive)
        {
            Debug.LogError("Failed to connect");
            return;
        }

        Debug.Log("Open stream");
        _hapticStream = new HapticStream(auth.jwt_key);
        _stream = _connection.CreateStream(new QuicStreamSettings { }, _hapticStream);
        await _stream.OpenAsync();
        if (!_stream.IsActive)
        {
            Debug.LogError("Failed to open stream");
            return;
        }
        Debug.Log("Reading signals");
        StartCoroutine(ReadFramesCouroutine());
    }

    public void Disconnect()
    {
        StopAllCoroutines();
        Debug.Log("Disconnect");
        _s1 = null;
        _s2 = null;
        SetGauge(-1f);
        _signalsCount = 0;
        //_signalCounterLabel.text = "0";

        if (_connection == null) return;
        if (!_connection.IsActive) return;
        if (!_connection.IsOpen) return;

        _hapticStream?.Dispose();
        _stream?.Dispose();
        _connection?.Dispose();

        _hapticStream = null;
        _stream = null;
        _connection = null;
    }

    private async Task<HapticApi.Publication[]> GetPublications(HapticApi hapticApi)
    {
        var publications = await hapticApi.GetPublications();
        if (publications == null || publications.Count == 0)
        {
            Debug.LogError("No publications available");
            return null;
        }
        return publications.ToArray();
    }

    private async Task<HapticApi.AuthorizeResponse> Subscribe(HapticApi hapticApi, HapticApi.Publication publication)
    {
        Debug.Log($"Subscribing to publication: {JsonSerializer.Serialize(publication)}");

        var auth = await hapticApi.AuthSubsciber(publication.publication_id, _deviceId);
        if (string.IsNullOrEmpty(auth.jwt_key))
        {
            Debug.LogError("Authorization failed");
            return null;
        }

        Debug.Log($"Subscription authorized. JWT={auth.jwt_key}");
        return auth;
    }
}
