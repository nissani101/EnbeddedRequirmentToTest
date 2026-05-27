using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectionManager
{
    public class UdpSocketManager
    {
        private UdpClient? _udpClient;
        private bool _isListening;

        public void StartListening(string ipAddress, int port)
        {
            try
            {
                IPAddress localIp = IPAddress.Parse(ipAddress);
                IPEndPoint localEndPoint = new IPEndPoint(localIp, port);

                _udpClient = new UdpClient(localEndPoint);
                _isListening = true;

                Console.WriteLine($"[UDP] Socket opened and listening on: {ipAddress}:{port}");

                Task.Run(() => ListenLoop());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UDP Error] Failed to open socket: {ex.Message}");
            }
        }

        private void ListenLoop()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (_isListening)
            {
                try
                {
                    byte[] receivedBytes = _udpClient!.Receive(ref remoteEndPoint);
                    string receivedData = Encoding.UTF8.GetString(receivedBytes);
                    Console.WriteLine($"\n[UDP Received] From {remoteEndPoint.Address}:{remoteEndPoint.Port} -> {receivedData}");
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UDP Error] Error receiving data: {ex.Message}");
                }
            }
        }

        public async Task<string?> SendAndReceiveEchoAsync(string message, string ip, int port, int timeoutMs)
        {
            using var client = new UdpClient();
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(data, data.Length, ip, port);

                using var cts = new CancellationTokenSource(timeoutMs);
                var receiveTask = client.ReceiveAsync(cts.Token);
                
                var result = await receiveTask;
                return Encoding.UTF8.GetString(result.Buffer);
            }
            catch (OperationCanceledException)
            {
                // Timeout
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UDP Client Error] {ex.Message}");
                return null;
            }
        }

        public void StopListening()
        {
            _isListening = false;
            if (_udpClient != null)
            {
                _udpClient.Close();
                _udpClient.Dispose();
                Console.WriteLine("[UDP] Socket closed successfully.");
            }
        }
    }
}
