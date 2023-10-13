//
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DeoVR.QuicNet;
using DeoVR.QuicNet.Core;
using DeoVR.QuicNet.Haptics;
using Google.Protobuf;
using Microsoft.Quic;

namespace MsQuicTool
{
    class Program
    {
        private static string _deviceId = Guid.NewGuid().ToString();
        private static bool _interrupted;

        public static async Task Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            var t = new Thread(() => {
                while(!_interrupted)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.C)
                        _interrupted = true;
                }
            });
            t.IsBackground = true;
            t.Start();

            // This code lets us pass in an argument of where to search for the library at.
            // Very helpful for testing
            if (args.Length > 0)
            {
                ConfigureLibLookup(args[0]);
            }
            //TestGoogleConnection();
            await TestHapticStream();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            _interrupted = true;
        }

        private static void TestGoogleConnection()
        {
            using var quic = new QuicContext(new QuicSettings { Version = QuicVersion.Default });
            using var connection = quic.CreateConnection(new QuicConnectionSettings { Host = "google.com" }, new SimpleConnectionHandler());

            Console.WriteLine("Open connection");
            connection.OpenAsync();
            while (connection.IsOpen && connection.IsActive)
            {
                Console.WriteLine("...");
                Thread.Sleep(1000);
            }
        }

        private static async Task TestHapticStream()
        {
            using var hapticApi = new HapticApi("https://haptics-stg.infomediji.com", _deviceId);
            var publications = await GetPublications(hapticApi);
            if (publications == null) return;

            var publication = publications[0];
            if (publications.Length > 1)
            {
                Console.WriteLine("Available publications:");
                for (var i = 0; i < publications.Length; i++)
                {
                    Console.WriteLine($"{i}: {JsonSerializer.Serialize(publications[i])}");
                }
                Console.Write("Enter publication index to subscribe:");
                var id = int.Parse(Console.ReadLine());
                publication = publications[id];
            }

            var auth = await Subscribe(hapticApi, publication);
            if (auth == null)
            {
                Console.WriteLine("Failed to subscribe");
                return;
            }

            unsafe
            {
                Quic.ConnectionCallback = ConnectionCallback;
                Quic.StreamCallback = StreamCallback;
            }

            using var quic = Quic.Open(new QuicSettings
            {
                CustomAlpn = "haptics",
                CredentialFlags = QUIC_CREDENTIAL_FLAGS.CLIENT | QUIC_CREDENTIAL_FLAGS.NO_CERTIFICATE_VALIDATION
            });


            using var connection = quic.CreateConnection(new QuicConnectionSettings
            {
                Host = "46.101.110.207",
                Port = 50000
            }, new SimpleConnectionHandler());

            Console.WriteLine("Connect");
            await connection.OpenAsync();
            if (!connection.IsActive)
            {
                Console.WriteLine("Failed to connect");
                return;
            }
            Console.WriteLine("Open stream");

            using var streamHandler = new HapticStream(auth.jwt_key);
            using var stream = connection.CreateStream(new QuicStreamSettings { }, streamHandler);
            await stream.OpenAsync();
            if (!stream.IsActive)
            {
                Console.WriteLine("Failed to open stream");
                return;
            }
            Console.WriteLine("Event loop");
            while (!_interrupted)
            {
                if (!connection.IsActive || !stream.IsActive)
                    break;

                if (streamHandler.ReadNextFrame(out var frame))
                {
                    if (frame.FrameType == FrameType.Signal)
                        Console.WriteLine($"Signal: {frame.AsSignal()}");
                    else
                        Console.WriteLine($"Raw frame: {frame.FrameType}");
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            Console.WriteLine("Shutting down");
        }

        private static async Task<HapticApi.Publication[]> GetPublications(HapticApi hapticApi)
        {
            var publications = await hapticApi.GetPublications();
            if (publications == null || publications.Count == 0)
            {
                Console.WriteLine("No publications available");
                return null;
            }
            return publications.ToArray();
        }

        private static async Task<HapticApi.AuthorizeResponse> Subscribe(HapticApi hapticApi, HapticApi.Publication publication)
        {
            Console.WriteLine($"Subscribing to publication: {JsonSerializer.Serialize(publication)}");

            var auth = await hapticApi.AuthSubsciber(publication.publication_id, _deviceId);
            if (string.IsNullOrEmpty(auth.jwt_key))
            {
                Console.WriteLine("Authorization failed");
                return null;
            }

            Console.WriteLine($"Subscription authorized. JWT={auth.jwt_key}");
            return auth;
        }

        public static void ConfigureLibLookup(string path)
        {
            NativeLibrary.SetDllImportResolver(typeof(MsQuic).Assembly, (libraryName, assembly, searchPath) =>
            {
                if (libraryName != "msquic") return IntPtr.Zero;
                if (NativeLibrary.TryLoad(path, out var ptr))
                {
                    return ptr;
                }
                return IntPtr.Zero;
            });
        }


        // For Android:
        //[MonoPInvokeCallback(typeof(UnmanagedDelegate))]
        private static unsafe int ConnectionCallback(void* handle, void* context, void* evnt)
        {
            Console.WriteLine($"Connection event: {((QUIC_CONNECTION_EVENT*) evnt)->Type}");
            return Quic.HandleConnectionEvent(handle, context, evnt);
        }

        // For Android:
        //[MonoPInvokeCallback(typeof(UnmanagedDelegate))]
        private static unsafe int StreamCallback(void* handle, void* context, void* evnt)
        {
            Console.WriteLine($"Stream event: {((QUIC_STREAM_EVENT*)evnt)->Type}");
            return Quic.HandleStreamEvent(handle, context, evnt);
        }
    }

    class SimpleConnectionHandler : QuicConnectionEventHandler
    {
    }

    class SimpleStreamHandler : QuicStreamEventHandler
    {
        protected override void Disposing()
        {
        }

    }
}
