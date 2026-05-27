using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectionManager
{
    public class TcpEchoServer : IDisposable
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        public void Start(string ipAddress, int port)
        {
            if (_isRunning) return;

            try
            {
                IPAddress localAddr = IPAddress.Parse(ipAddress);
                _listener = new TcpListener(localAddr, port);
                _cts = new CancellationTokenSource();
                _isRunning = true;

                _listener.Start();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [TCP Server] Started on {ipAddress}:{port}");
                Console.Out.Flush();

                Task.Run(() => ListenLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [TCP Server] Failed to start: {ex.Message}");
                Console.Out.Flush();
            }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener!.AcceptTcpClientAsync(token);
                    _ = HandleClientAsync(client, token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [TCP Server Error] {ex.Message}");
                        Console.Out.Flush();
                    }
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[4096];
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                        if (bytesRead == 0) break;

                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [TCP Server] Received: \"{data}\" from {client.Client.RemoteEndPoint}");
                        Console.Out.Flush();

                        // Echo back
                        await stream.WriteAsync(buffer, 0, bytesRead, token);
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [TCP Server] Echoed back to {client.Client.RemoteEndPoint}");
                        Console.Out.Flush();
                    }
                    catch { break; }
                }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _isRunning = false;
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
