using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectionManager
{
    public class UdpEchoServer : IDisposable
    {
        private UdpClient? _server;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        public void Start(int port)
        {
            if (_isRunning) return;

            try
            {
                _server = new UdpClient(port);
                _cts = new CancellationTokenSource();
                _isRunning = true;

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [UDP Server] Started on port {port}");
                Console.Out.Flush();

                Task.Run(() => ListenAndEcho(_cts.Token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [UDP Server] Failed to start: {ex.Message}");
                Console.Out.Flush();
            }
        }

        private async Task ListenAndEcho(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _server!.ReceiveAsync(token);
                    string receivedData = Encoding.UTF8.GetString(result.Buffer);

                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [UDP Server] Received: \"{receivedData}\" from {result.RemoteEndPoint}");
                    Console.Out.Flush();

                    // Echo back the same bytes to the sender
                    await _server.SendAsync(result.Buffer, result.Buffer.Length, result.RemoteEndPoint);

                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [UDP Server] Echoed back to {result.RemoteEndPoint}");
                    Console.Out.Flush();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [UDP Server Error] {ex.Message}");
                        Console.Out.Flush();
                    }
                }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _server?.Close();
            _isRunning = false;
        }

        public void Dispose()
        {
            Stop();
            _server?.Dispose();
            _cts?.Dispose();
        }
    }
}
