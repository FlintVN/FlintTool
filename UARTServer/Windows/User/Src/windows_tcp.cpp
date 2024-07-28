
#include "windows_tcp.h"

WindowsTcp::WindowsTcp(uint16_t port, const char *address) {
    WSADATA wsaData;
    SOCKADDR_IN serverAddr;

    WSAStartup(MAKEWORD(2, 2), &wsaData);
    server = socket(AF_INET, SOCK_STREAM, 0);

    serverAddr.sin_addr.s_addr = inet_addr(address);
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_port = htons(port);

    ::bind(server, reinterpret_cast<SOCKADDR *>(&serverAddr), sizeof(serverAddr));
    listen(server, 0);
}

bool WindowsTcp::sendData(uint8_t *data, uint32_t length) {
    return send(client, (const char *)data, length, 0) == length;
}

void WindowsTcp::receiveTask(bool (*rxCallback)(WindowsTcp *, uint8_t *, uint32_t)) {
    SOCKADDR_IN clientAddr;
    int clientAddrSize = sizeof(clientAddr);

    while(1) {
        if((client = accept(server, reinterpret_cast<SOCKADDR *>(&clientAddr), &clientAddrSize)) != INVALID_SOCKET) {
            while(1) {
                int dataSize = recv(client, rxBuff, sizeof(rxBuff), 0);
                if(dataSize > 0) {
                    if(!rxCallback(this, (uint8_t *)rxBuff, dataSize))
                        break;
                }
                else
                    break;
            }
        }
    }
}

WindowsTcp::~WindowsTcp(void) {

}
