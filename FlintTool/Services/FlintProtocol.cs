namespace FlintTool.Services {
    public interface FlintProtocol {
        bool Connect();
        bool Write(byte[] data, int offset, int length);
        int Read(byte[] buffer, int offset, int length, int timeout);
        bool IsConnected();
        void Disconnect();
    }
}
