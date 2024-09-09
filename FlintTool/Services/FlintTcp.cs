using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FlintTool.Services {
    public class FlintTcp : FlintProtocol {
        private int port;
        private IPAddress address;
        private TcpClient client;

        public FlintTcp(int port, IPAddress address) {
            this.port = port;
            this.address = address;
        }

        public bool Connect() {
            try {
                client = new TcpClient();
                client.Connect(address, port);
                return true;
            }
            catch {
                return false;
            }
        }

        public bool Write(byte[] data, int offset, int length) {
            lock(this) {
                if(client == null)
                    return false;
                client.GetStream().Write(data, offset, length);
                return true;
            }
        }

        public int Read(byte[] buffer, int offset, int length, int timeout) {
            NetworkStream stream = client.GetStream();
            stream.ReadTimeout = timeout;
            return stream.Read(buffer, offset, length);
        }

        public bool IsConnected() {
            if(client == null || !client.Connected)
                return false;
            return true;
        }

        public void Disconnect() {
            if(client != null) {
                client.Close();
                client = null;
            }
        }
    }
}
