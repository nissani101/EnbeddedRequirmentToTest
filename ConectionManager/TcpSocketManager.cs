using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectionManager
{
    public class TcpSocketManager
    {
        public async Task<string?> SendAndReceiveEchoAsync(string message, string ip, int port, int timeoutMs)
        {
            using var client = new TcpClient();
            try
            {
                var cts = new CancellationTokenSource(timeoutMs);
                await client.ConnectAsync(ip, port, cts.Token);
                
                using var stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length, cts.Token);

                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                
                if (bytesRead == 0) return null;
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            catch (OperationCanceledException) { return null; }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP Client Error] {ex.Message}");
                return null;
            }
        }
    }
}
