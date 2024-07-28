
#ifndef __WINDOWS_TCP_H
#define __WINDOWS_TCP_H

#include <future>
#include <winsock2.h>

class WindowsTcp {
private:
    HANDLE hThread;
    SOCKET server;
    SOCKET client;
    char rxBuff[20480];
public:
    WindowsTcp(uint16_t port, const char *address);

    bool sendData(uint8_t *data, uint32_t length);
    void receiveTask(bool (*rxCallback)(WindowsTcp *, uint8_t *, uint32_t));

    ~WindowsTcp(void);
private:
    WindowsTcp(const WindowsTcp &) = delete;
    void operator=(const WindowsTcp &) = delete;
};

#endif /* __WINDOWS_TCP_H */
