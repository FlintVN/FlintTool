using System.Diagnostics;
using System.IO.Ports;
using System.Threading;

namespace FlintTool.Services {
    public class FlintUart : FlintProtocol {
        private SerialPort serialPort;
        private string port;
        private int baurdRate;

        public FlintUart(string port, int baurdRate) {
            this.serialPort = null;
            this.port = port;
            this.baurdRate = baurdRate;
        }

        public string Port {
            get => port;
        }

        public bool Connect() {
            if(serialPort == null || !serialPort.IsOpen) {
                try {
                    serialPort = new SerialPort();
                    serialPort.PortName = port;
                    serialPort.BaudRate = baurdRate;
                    serialPort.Parity = Parity.None;
                    serialPort.DataBits = 8;
                    serialPort.StopBits = StopBits.One;
                    serialPort.Open();
                    return serialPort.IsOpen;
                }
                catch {
                    return false;
                }
            }
            return true;
        }

        public bool Write(byte[] data, int offset, int length) {
            if(serialPort == null || serialPort.IsOpen) {
                try {
                    serialPort.Write(data, offset, length);
                    return true;
                }
                catch {
                    return false;
                }
            }
            return false;
        }

        public int Read(byte[] buffer, int offset, int length, int timeout) {
            int byteToRead = serialPort.BytesToRead;
            if(byteToRead > 0)
                return serialPort.Read(buffer, 0, length);
            return 0;
        }

        public bool IsConnected() {
            if(serialPort == null)
                return false;
            return serialPort.IsOpen;
        }

        public void Disconnect() {
            if(serialPort == null || serialPort.IsOpen)
                serialPort.Close();
        }
    }
}
