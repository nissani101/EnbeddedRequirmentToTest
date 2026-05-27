using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ConnectionManager;

// Force AutoFlush to ensure real-time terminal output
Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

if (args.Length < 1)
{
    Console.WriteLine("Usage: UdpTerminalTool <server|client|tcpserver|tcpclient> [ip] [port]");
    return;
}

string mode = args[0].ToLower();
string ip = args.Length > 1 ? args[1] : "127.0.0.1";
int port = args.Length > 2 ? int.Parse(args[2]) : 0;

void Log(string message) => Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");

void StartMonitor(int listenPort, string title)
{
    Console.Title = title;
    Log($"{title} started. Listening for logs on port {listenPort}...");
    using var listener = new UdpClient(listenPort);
    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
    while (true)
    {
        try
        {
            byte[] bytes = listener.Receive(ref remoteEP);
            string message = Encoding.UTF8.GetString(bytes);
            Console.WriteLine(message);
        }
        catch (Exception ex) { Log($"[Monitor Error] {ex.Message}"); }
    }
}

if (mode == "server")
{
    if (port == 0) port = 5000;
    Console.Title = $"UDP Echo Server - {port}";
    Log($"Starting UDP Echo Server on port {port}...");
    using var server = new UdpEchoServer();
    server.Start(port);
    Log("Server is LIVE. Monitoring incoming packets...");
    while (true) { Thread.Sleep(1000); }
}
else if (mode == "client")
{
    StartMonitor(5001, "UDP Monitor Client");
}
else if (mode == "tcpserver")
{
    if (port == 0) port = 5002;
    Console.Title = $"TCP Echo Server - {port}";
    Log($"Starting TCP Echo Server on {ip}:{port}...");
    using var server = new TcpEchoServer();
    server.Start(ip, port);
    Log("Server is LIVE. Waiting for connections...");
    while (true) { Thread.Sleep(1000); }
}
else if (mode == "tcpclient")
{
    StartMonitor(5003, "TCP Monitor Client");
}
