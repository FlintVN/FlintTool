
#include "windows_uart.h"

void WindowsUart::checkConnectTask(WindowsUart *uart) {
    while(1) {
        Sleep(100);
        if(!uart->isConnect() && uart->disconnectCallback) {
            uart->disconnectCallback(uart);
            return;
        }
    }
}

WindowsUart::WindowsUart(const char *port, uint32_t baudRate, void (*disconnectCallback)(WindowsUart *)) : port(port) {
    this->hSerial = INVALID_HANDLE_VALUE;
    this->baudRate = baudRate;
    this->disconnectCallback = disconnectCallback;
    this->portPath[0] = '\\';
    this->portPath[1] = '\\';
    this->portPath[2] = '.';
    this->portPath[3] = '\\';
    uint8_t index = 0;
    while(port[index]) {
        this->portPath[index + 4] = port[index];
        index++;
    }
    this->portPath[index + 4] = 0;
}

bool WindowsUart::connect(void) {
    if(hSerial != INVALID_HANDLE_VALUE && isConnect())
        return true;
    hSerial = CreateFile(
        portPath,
        GENERIC_READ | GENERIC_WRITE,
        0,
        0,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        0
    );
    if(hSerial == INVALID_HANDLE_VALUE)
        return false;
    DCB dcbSerialParams = {0};
    dcbSerialParams.DCBlength = sizeof(dcbSerialParams);
    if(!GetCommState(hSerial, &dcbSerialParams))
        return false;
    dcbSerialParams.BaudRate = baudRate;
    dcbSerialParams.ByteSize = 8;
    dcbSerialParams.StopBits = ONESTOPBIT;
    dcbSerialParams.Parity = NOPARITY;
    if(!SetCommState(hSerial, &dcbSerialParams))
        return false;

    double oneByteTime = (double)10 * 1000 / baudRate;
    DWORD timeoutMultiplier = (DWORD)oneByteTime;
    DWORD intervalTimeout = (DWORD)(oneByteTime * 10);

    timeoutMultiplier = (timeoutMultiplier < 1) ? 1 : timeoutMultiplier;
    intervalTimeout = (intervalTimeout < 1) ? 1 : intervalTimeout;

    COMMTIMEOUTS timeouts = {0};
    timeouts.ReadIntervalTimeout = intervalTimeout;
    timeouts.ReadTotalTimeoutConstant = 10000;
    timeouts.ReadTotalTimeoutMultiplier = timeoutMultiplier;
    timeouts.WriteTotalTimeoutConstant = 10000;
    timeouts.WriteTotalTimeoutMultiplier = timeoutMultiplier;
    if(!SetCommTimeouts(hSerial, &timeouts))
        return false;

    return CreateThread(
        NULL,                                       /* Default security attributes */
        0x200000,                                   /* Default stack size */
        (LPTHREAD_START_ROUTINE)checkConnectTask,   /* Thread function */
        this,                                       /* Parameter to thread function */
        0,                                          /* Default creation flags */
        NULL
    );

    return true;
}

bool WindowsUart::isConnect(void) const {
    if(hSerial == INVALID_HANDLE_VALUE)
        return false;
    else {
        DWORD errors;
        COMSTAT status;
        if (!ClearCommError(hSerial, &errors, &status))
            return false;
        return true;
    }
}

uint32_t WindowsUart::readData(uint8_t *buff, uint32_t size) const {
    DWORD bytesRead = 0;
    if(hSerial == INVALID_HANDLE_VALUE)
        return 0;
    if(!ReadFile(hSerial, buff, size, &bytesRead, NULL))
        return 0;
    return bytesRead;
}

bool WindowsUart::sendData(uint8_t *data, uint32_t size) const {
    DWORD bytesWrite = 0;
    if(hSerial == INVALID_HANDLE_VALUE)
        return false;
    if(!WriteFile(hSerial, data, size, &bytesWrite, NULL))
        return false;
    return bytesWrite == size;
}

WindowsUart::~WindowsUart(void) {
    if(isConnect())
        CloseHandle(hSerial);
}
