
#ifndef __WINDOWS_UART_H
#define __WINDOWS_UART_H

#include <stdint.h>
#include <windows.h>

class WindowsUart {
public:
    const char * const port;
private:
    HANDLE hSerial;
    uint32_t baudRate;
    void (*disconnectCallback)(WindowsUart *);
    char portPath[16];
public:
    WindowsUart(const char *port, uint32_t baudRate, void (*disconnectCallback)(WindowsUart *) = 0);

    bool connect(void);
    bool isConnect(void) const;
    uint32_t readData(uint8_t *buff, uint32_t size) const;
    bool sendData(uint8_t *data, uint32_t size) const;

    static void checkConnectTask(WindowsUart *uart);

    ~WindowsUart(void);
};

#endif /* __WINDOWS_UART_H */
