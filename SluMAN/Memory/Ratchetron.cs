using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Linq.Expressions;

namespace racman
{
    public class Ratchetron : IPS3API
    {
        string ip
        {
            get;
            set;
        }

        private int port = 9671;

        private const int ConnectTimeoutMs = 5000;

        // How long a request waits for its reply. The server sometimes never answers, e.g. a
        // memory read just as the game closes; without a limit the caller and every request
        // queued behind it wait forever, the UI thread included.
        private const int RequestTimeoutMs = 5000;

        private const int ReconnectIntervalMs = 2000;

        // Set by Disconnect, so a lost connection isn't reopened after the user closed it.
        private volatile bool disconnectRequested = false;

        // Whether the data channel was opened, so a reconnect opens it again.
        private bool dataChannelWanted = false;

        private TcpClient client;
        private UdpClient udpClient;
        private NetworkStream stream;
        private bool connected = false;

        private IPEndPoint remoteEndpoint;

        private List<int> memorySubs = new List<int>();
        private Dictionary<int, Action<byte[]>> memSubCallbacks = new Dictionary<int, Action<byte[]>>();
        private Dictionary<int, uint> memSubTickUpdates = new Dictionary<int, uint>();
        private Dictionary<int, UInt32> frozenAddresses = new Dictionary<int, uint>();

        private Action onDisconnectCallback;
        private Action onReconnectCallback;


        public Ratchetron(string ip) : base(ip)
        {
            this.ip = ip;
        }

        public void setDisconnectCallback(Action action) => onDisconnectCallback = action;
        public void setReconnectCallback(Action action) => onReconnectCallback = action;

        public override bool Connect()
        {
            try
            {
                // Windows waits about 20 seconds before giving up on a host that doesn't answer.
                this.client = new TcpClient();
                IAsyncResult connecting = this.client.BeginConnect(this.ip, this.port, null, null);
                if (!connecting.AsyncWaitHandle.WaitOne(ConnectTimeoutMs))
                {
                    this.client.Close();
                    return false;
                }
                this.client.EndConnect(connecting);
                this.client.NoDelay = true;

                this.stream = client.GetStream();

                this.stream.ReadTimeout = ConnectTimeoutMs;
                byte[] connMsg = ReadExactly(6);
                this.stream.ReadTimeout = RequestTimeoutMs;

                uint apiRev = ReadUInt32BE(connMsg, 2);

                if (apiRev < 2)
                {
                    MessageBox.Show("The Ratchetron module loaded on your PS3 is too old, you need to restart your PS3 to load the new version.");
                    return false;
                }

                if (connMsg[0] == 0x01)
                {
                    this.remoteEndpoint = new IPEndPoint(IPAddress.Parse(this.ip), 0);

                    this.connected = true;

#if DEBUG
                    this.EnableDebugMessages();
#endif

                    return true;
                }
            } catch (SocketException)
            {
            } catch (Exception)
            {
                // who cares about error handling anyway?
            }

            // Also stops a half-done handshake from leaving the socket open.
            this.client.Close();
            return false;
        }

        public override bool Disconnect()
        {
            this.disconnectRequested = true;
            this.ReleaseAllSubs();
            this.connected = false;
            if (this.udpClient != null)
            {
                this.udpClient.Close();
            }
            if (this.client != null)
            {
                this.client.Close();
            }

            return true;
        }

        public override string getGameTitleID()
        {
            if (!connected)
            {
                throw new Exception("I ain't connected");
            }

            byte[] cmd = { 0x06 };
            byte[] titleIdBuf;

            lock (requestLock)
            {
                WriteStream(cmd, 0, 1);
                titleIdBuf = ReadExactly(16);
            }

            return System.Text.Encoding.Default.GetString(titleIdBuf).Replace("\0", string.Empty);
        }

        public int[] GetPIDList()
        {
            if (!connected)
            {
                throw new Exception("I ain't connected");
            }

            byte[] cmd = { 0x03 };
            byte[] pidListBuf;

            lock (requestLock)
            {
                WriteStream(cmd, 0, 1);
                pidListBuf = ReadExactly(64);
            }

            int[] pids = new int[16];

            for (int i = 0; i < 64; i += 4)
            {
                pids[i / 4] = (int)ReadUInt32BE(pidListBuf, i);
            }

            return pids;
        }

        public void EnableDebugMessages()
        {
            byte[] cmd = { 0x0d };

            WriteStream(cmd, 0, 1);
        }

        public override int getCurrentPID()
        {
            return this.GetPIDList()[2];
        }

        // Held for a whole request and its reply, so replies can't be read by the wrong caller
        // when the UI, Lua mods and subscription callbacks use the connection at the same time.
        private readonly object requestLock = new object();

        // Guards the subscription tables, which the data channel thread reads.
        private readonly object subsLock = new object();

        private void WriteStream(byte[] array, int offset, int count)
        {
            lock (requestLock)
            {
                if (this.stream.CanWrite)
                {
                    try
                    {
                        this.stream.Write(array, offset, count);
                    }
                    catch (IOException)
                    {
                        ConnectionLost();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Reads exactly <paramref name="count"/> bytes. A single Read can return fewer bytes than
        /// asked for; the rest arrive in later reads.
        /// </summary>
        private byte[] ReadExactly(int count)
        {
            byte[] buffer = new byte[count];
            int read = 0;
            while (read < count)
            {
                int n;
                try
                {
                    n = stream.Read(buffer, read, count - read);
                }
                catch (IOException)
                {
                    // Also a reply that didn't arrive within RequestTimeoutMs.
                    ConnectionLost();
                    throw;
                }
                if (n <= 0)
                {
                    ConnectionLost();
                    throw new IOException("The connection to the PS3 was closed.");
                }
                read += n;
            }
            return buffer;
        }

        /// <summary>
        /// Called when a request fails on a live connection. The connection can't be used again:
        /// a reply that turns up late would be read as the answer to the next request. Closes it
        /// and reconnects in the background, then runs the same callbacks as a game restart, since
        /// the server dropped this connection's subscriptions with it.
        /// </summary>
        private void ConnectionLost()
        {
            if (!this.connected || this.disconnectRequested)
            {
                return;
            }
            this.connected = false;

            Console.WriteLine("The PS3 stopped answering; reconnecting.");
            func.Status("The PS3 stopped answering. Reconnecting...", true);

            try { this.client.Close(); } catch { }
            if (this.udpClient != null)
            {
                try { this.udpClient.Close(); } catch { }
            }
            lock (subsLock)
            {
                this.memorySubs.Clear();
                this.memSubCallbacks.Clear();
                this.memSubTickUpdates.Clear();
                this.frozenAddresses.Clear();
            }

            Thread reconnectThread = new Thread(ReconnectAfterLoss);
            reconnectThread.IsBackground = true;
            reconnectThread.Name = "Ratchetron reconnect";
            reconnectThread.Start();
        }

        private void ReconnectAfterLoss()
        {
            while (!this.disconnectRequested)
            {
                Thread.Sleep(ReconnectIntervalMs);

                bool reconnected;
                // Held so no request uses the connection before the data channel is back.
                lock (requestLock)
                {
                    if (this.disconnectRequested)
                    {
                        return;
                    }
                    reconnected = Connect();
                    if (reconnected && this.dataChannelWanted)
                    {
                        try
                        {
                            OpenDataChannel();
                        }
                        catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
                        {
                            // Lost again; ConnectionLost started a new reconnect.
                            return;
                        }
                    }
                }

                if (reconnected)
                {
                    Console.WriteLine("Reconnected to the PS3.");
                    func.Status("Reconnected to the PS3.");

                    // The game may have closed or changed while nothing was listening, so check
                    // it the same way as after a restart. This also subscribes again.
                    Action disconnected = onDisconnectCallback;
                    if (disconnected != null)
                    {
                        disconnected();
                    }
                    RunReconnectCallback();
                    return;
                }
            }
        }

        // 1 while a reconnect callback runs. A reconnect after a lost connection and the game
        // starting can both trigger one; the second would subscribe everything twice.
        private int reconnectCallbackRunning = 0;

        private void RunReconnectCallback()
        {
            Action reconnect = onReconnectCallback;
            if (reconnect == null || Interlocked.CompareExchange(ref reconnectCallbackRunning, 1, 0) != 0)
            {
                return;
            }
            try
            {
                reconnect();
            }
            finally
            {
                Interlocked.Exchange(ref reconnectCallbackRunning, 0);
            }
        }

        private static uint ReadUInt32BE(byte[] buffer, int offset)
        {
            return (uint)(buffer[offset] << 24 | buffer[offset + 1] << 16 | buffer[offset + 2] << 8 | buffer[offset + 3]);
        }
        
        public override void WriteMemory(int pid, uint address, uint size, byte[] memory)
        {
            var cmdBuf = new List<byte>();
            cmdBuf.Add(0x05);
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)pid).Reverse());
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)address).Reverse());
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)size).Reverse());
            cmdBuf.AddRange(memory);


            this.WriteStream(cmdBuf.ToArray(), 0, cmdBuf.Count);
        }

        public override byte[] ReadMemory(int pid, uint address, uint size)
        {
            var cmdBuf = new List<byte>();
            cmdBuf.Add(0x04);
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)pid).Reverse());
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)address).Reverse());
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)size).Reverse());

#if DEBUG
            var watch = new System.Diagnostics.Stopwatch();

            watch.Start();
#endif

            byte[] memory;
            lock (requestLock)
            {
                this.WriteStream(cmdBuf.ToArray(), 0, cmdBuf.Count);
                memory = ReadExactly((int)size);
            }

#if DEBUG
            watch.Stop();

            //Console.WriteLine($"Request for {size} bytes memory at {address.ToString("X")} took: {watch.ElapsedMilliseconds} ms");
#endif 
            return memory;
        }

        public override void Notify(string message)
        {
            var cmdBuf = new List<byte>();
            cmdBuf.Add(0x02);
            var payload = Encoding.ASCII.GetBytes(message);
            uint length = (uint)(payload.Length + 1);
            cmdBuf.AddRange(BitConverter.GetBytes(length).Reverse());
            cmdBuf.AddRange(payload);
            cmdBuf.Add(0x00); // null terminating character to avoid strings looking messed up

            this.WriteStream(cmdBuf.ToArray(), 0, cmdBuf.Count);

        }

        // Live data (subscriptions) arrives over UDP from the PS3. Windows Firewall can drop it
        // silently while everything sent over TCP still works.
        private long receivedDataPackets = 0;

        /// <summary>How many live data packets have arrived since connecting.</summary>
        public long ReceivedDataPackets => Interlocked.Read(ref receivedDataPackets);

        /// <summary>
        /// Raised when the data channel is open and has subscriptions but nothing arrives, which
        /// almost always means a firewall is dropping it. Raised on a background thread.
        /// </summary>
        public event Action DataChannelSilent;

        // The server resends every subscription about every 500 ms, so this is many missed packets.
        private const int SilentAfterMs = 6000;

        private void WatchForSilentDataChannel()
        {
            Thread.Sleep(SilentAfterMs);
            if (!this.connected || ReceivedDataPackets > 0)
            {
                return;
            }
            int subscriptionCount;
            lock (subsLock)
            {
                subscriptionCount = this.memSubCallbacks.Count;
            }
            if (subscriptionCount == 0)
            {
                // Nothing subscribed, so nothing would have been sent.
                return;
            }

            Console.WriteLine($"No live data received in {SilentAfterMs / 1000} s; a firewall is probably blocking it.");
            Action handler = DataChannelSilent;
            if (handler != null)
            {
                handler();
            }
        }

        private void DataChannelReceive()
        {
            IPEndPoint end = new IPEndPoint(IPAddress.Any, 0);
            // A reconnect opens a new socket with its own thread; this one stops with its socket.
            UdpClient udp = this.udpClient;

            while (this.connected && udp == this.udpClient)
            {
                try
                {
                    byte[] cmdBuf = udp.Receive(ref end);
                    Interlocked.Increment(ref receivedDataPackets);
                    byte command = cmdBuf[0];

                    switch (command)
                    {
                        case 0x06:
                            {
                                int memSubID = (int)ReadUInt32BE(cmdBuf, 1);
                                int size = (int)ReadUInt32BE(cmdBuf, 5);
                                uint tickUpdated = ReadUInt32BE(cmdBuf, 9);

                                Action<byte[]> callback = null;
                                lock (subsLock)
                                {
                                    uint lastTick;
                                    if (this.memSubTickUpdates.TryGetValue(memSubID, out lastTick) && lastTick != tickUpdated)
                                    {
                                        this.memSubTickUpdates[memSubID] = tickUpdated;
                                        this.memSubCallbacks.TryGetValue(memSubID, out callback);
                                    }
                                }

                                if (callback != null)
                                {
                                    size = Math.Max(0, Math.Min(size, cmdBuf.Length - 13));
                                    byte[] value = new byte[size];
                                    Array.Copy(cmdBuf, 13, value, 0, size);
                                    Array.Reverse(value);
                                    callback(value);
                                }

                                break;
                            }
                        // for opening/closing: 1 extra byte for coming in/out
                        case 0x08:
                            {
                                byte enteringOrLeaving = cmdBuf[1];
                                Console.WriteLine($"Got new IS_INGAME: {enteringOrLeaving}");
                                if (enteringOrLeaving == 0 && onDisconnectCallback != null) // out of game
                                {
                                    onDisconnectCallback();
                                } 
                                else if (enteringOrLeaving == 1)
                                {
                                    RunReconnectCallback();
                                }

                                break;
                            }

                    }
                } catch (SocketException)
                {
                    // Who gives a shit
                } catch (ObjectDisposedException)
                {
                    // Disconnect() closed the socket between the loop check and Receive.
                    break;
                }
            }
        }

        public void OpenDataChannel()
        {
            this.dataChannelWanted = true;
            byte[] data = new byte[1024];
            int port = 4000;
            bool udpStarted = false;
            while (!udpStarted)
            {
                try
                {
                    IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);
                    this.udpClient = new UdpClient(ipep);
                    udpStarted = true;
                }
                catch (SocketException)
                {
                    if (port++ > 5000)
                    {
                        // Binding fails only when the ports are taken; a firewall doesn't stop it.
                        MessageBox.Show("SluMAN couldn't open a local port for live data (it tried 4000 to 5000). Another program may be using them. Close it and attach again.", "Couldn't open a port");
                        return;
                    }
                }
            }

            var assignedPort = ((IPEndPoint)this.udpClient.Client.LocalEndPoint).Port;
            
            var cmdBuf = new List<byte>();
            cmdBuf.Add(0x09);
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)assignedPort).Reverse());

            byte[] returnValue;
            lock (requestLock)
            {
                this.WriteStream(cmdBuf.ToArray(), 0, cmdBuf.Count);
                returnValue = ReadExactly(1);
            }

            if (returnValue[0] == 128) { 
                Console.WriteLine("Waiting for connection on port " + assignedPort);

                //this.udpClient.Send(new byte[] { 0x01 }, 1, remoteEndpoint);

                Thread dataThread = new Thread(this.DataChannelReceive);
                dataThread.IsBackground = true; // Critical: Mark as background thread so app can exit
                dataThread.Start();

                Thread watchThread = new Thread(this.WatchForSilentDataChannel);
                watchThread.IsBackground = true;
                watchThread.Start();
            } else if (returnValue[0] == 2)
            {
                Console.WriteLine("Tried to open data channel, but server says we already have one open.");
                udpClient.Close();
            } else
            {
                Console.WriteLine("Server error trying to open data channel.");
                udpClient.Close();
            }
        }

        public override int SubMemory(int pid, uint address, uint size, MemoryCondition condition, byte[] memory, Action<byte[]> callback)
        {
            var cmdBuf = new List<byte>();
            cmdBuf.Add(0x0a);
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)pid).Reverse());
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)address).Reverse());
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)size).Reverse());
            cmdBuf.AddRange(new byte[] { (byte)condition });
            cmdBuf.AddRange(memory);

            int memSubID;
            lock (requestLock)
            {
                this.WriteStream(cmdBuf.ToArray(), 0, cmdBuf.Count);
                memSubID = (int)ReadUInt32BE(ReadExactly(4), 0);
            }

            lock (subsLock)
            {
                this.memorySubs.Add(memSubID);
                this.memSubCallbacks[memSubID] = callback;
                this.memSubTickUpdates[memSubID] = 0;
            }

            Console.WriteLine($"Subscribed to address {address.ToString("X")} with subscription ID {memSubID}");

            return memSubID;
        }

        public override int FreezeMemory(int pid, uint address, uint size, MemoryCondition condition, byte[] memory)
        {
            var cmdBuf = new List<byte>();
            cmdBuf.Add(0x0b);
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)pid).Reverse());
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)address).Reverse());
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)size).Reverse());
            cmdBuf.AddRange(new byte[] { (byte)condition });
            cmdBuf.AddRange(memory);

            int memSubID;
            lock (requestLock)
            {
                this.WriteStream(cmdBuf.ToArray(), 0, cmdBuf.Count);
                memSubID = (int)ReadUInt32BE(ReadExactly(4), 0);
            }

            Console.WriteLine($"Froze address {address.ToString("X")} with subscription ID {memSubID}");

            lock (subsLock)
            {
                frozenAddresses[memSubID] = address;
            }

            return memSubID;
        }

        public void ReleaseAllSubs()
        {
            int[] allSubsCopy;
            lock (subsLock)
            {
                allSubsCopy = this.memorySubs.ToArray();
            }
            foreach (var sub in allSubsCopy)
            {
                try
                {
                    this.ReleaseSubID(sub);
                }
                catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    // The connection is gone, and the server drops its subscriptions with it.
                }
            }
        }

        public override void ReleaseSubID(int memSubID)
        {   
            var cmdBuf = new List<byte>();
            cmdBuf.Add(0x0c);
            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)memSubID).Reverse());

            // Forget the subscription first, so a release that fails on a dropped connection
            // doesn't leave its callback running.
            lock (subsLock)
            {
                this.memSubCallbacks.Remove(memSubID);
                this.memSubTickUpdates.Remove(memSubID);
                this.frozenAddresses.Remove(memSubID);
                this.memorySubs.Remove(memSubID);
            }

            lock (requestLock)
            {
                this.WriteStream(cmdBuf.ToArray(), 0, cmdBuf.Count);
                ReadExactly(1);
            }

            Console.WriteLine($"Released memory subscription ID {memSubID}");


            // we're ignoring the results because yolo
        }

        public override int MemSubIDForAddress(uint address)
        {
            lock (subsLock)
            {
                foreach (KeyValuePair<int, uint> entry in frozenAddresses)
                {
                    if (address == entry.Value)
                    {
                        return entry.Key;
                    }
                }
            }
            return -1;
        }


        public int OpenFile(string remotePath) {
            var cmdBuf = new List<byte> {
                0x10,  // Open file command
                0, 0, 0, 0     // Flags (unused)
            };

            cmdBuf.AddRange(BitConverter.GetBytes((UInt32)remotePath.Length + 1).Reverse());
            cmdBuf.AddRange(Encoding.ASCII.GetBytes(remotePath));
            cmdBuf.Add(0x0);

            byte[] fileHandleBuf;
            lock (requestLock)
            {
                WriteStream(cmdBuf.ToArray(), 0, cmdBuf.Count);
                fileHandleBuf = ReadExactly(4);
            }

            int fileHandle = BitConverter.ToInt32(fileHandleBuf, 0);

            return fileHandle;
        }

        public override void WriteFile(string remotePath, byte[] buffer) {
            // Held for the whole transfer, so another request can't land between the chunks.
            lock (requestLock)
            {
                WriteFileLocked(remotePath, buffer);
            }
        }

        private void WriteFileLocked(string remotePath, byte[] buffer)
        {
            int fileHandle = OpenFile(remotePath);

            var cmdBuf = new List<byte> {
                    0x11,  // Write file command
                };

            cmdBuf.AddRange(BitConverter.GetBytes(fileHandle));
            cmdBuf.AddRange(BitConverter.GetBytes(buffer.Length).Reverse());
            WriteStream(cmdBuf.ToArray(), 0, cmdBuf.Count);

            // Split up into 1024 byte chunks
            for (int i = 0; i < buffer.Length; i += 2048) {
                int chunkSize = Math.Min(2048, buffer.Length - i);

                WriteStream(buffer, i, chunkSize);
            }

            // Close file by sending a write command with 0 file size
            var closeCmdBuf = new List<byte> {
                0x11,  // Write file command
            };

            closeCmdBuf.AddRange(BitConverter.GetBytes(fileHandle));
            closeCmdBuf.AddRange(BitConverter.GetBytes(0)); // 0 size to indicate end of file

            WriteStream(closeCmdBuf.ToArray(), 0, closeCmdBuf.Count);
        }

        public override void WriteFile(string remotePath, string filePath) {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException("The specified file does not exist.", filePath);
            }

            var file = File.OpenRead(filePath);

            var buffer = new byte[file.Length];
            file.Read(buffer, 0, (int)file.Length);
            file.Close();

            WriteFile(remotePath, buffer);
        }
    }
}
