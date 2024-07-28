
#include <iostream>
#include "windows_tcp.h"
#include "windows_uart.h"
#include "flint_debugger.h"

using namespace std;

WindowsTcp *tcp = NULL;
WindowsUart *uart = NULL;

static void cleanup(void) {
    if(tcp) {
        tcp->~WindowsTcp();
        delete tcp;
    }
    if(uart) {
        uart->~WindowsUart();
        delete uart;
    }
}

static BOOL WINAPI consoleHandler(DWORD signal) {
    if(signal == CTRL_C_EVENT) {
        cleanup();
        exit(0);
    }
    return TRUE;
}

static void uartDisconnectHandler(WindowsUart *uart) {
    std::cout << uart->port;
    std::cout << " is disconnected" << std::endl;
    cleanup();
    exit(0);
}

static bool tcpRxHandler(WindowsTcp *tcp, uint8_t *data, uint32_t size) {
    if(uart->sendData(data, size)) {
        uint8_t rxBuff[20480 + 1];
        uint32_t rxSize = uart->readData(rxBuff, sizeof(rxBuff) - 1);
        if(rxSize > 0)
            tcp->sendData(rxBuff, rxSize);
    }
    else
        std::cout << "Error: Could not send data to " << uart->port << std::endl;
    if((FlintDbgCmd)data[0] == DBG_CMD_TERMINATE) {
        bool endDbg = data[4] != 0;
        return !endDbg;
    }
    return true;
}

int main(int argc, char *argv[]) {
    if(!SetConsoleCtrlHandler(consoleHandler, TRUE)) {
        std::cout << "Error: Could not set control handler" << std::endl;
        return 1;
    }
    const char *port = 0;
    if(argc > 1)
        port = argv[1];
    else {
        std::cout << "Error: Please specify COM port" << std::endl;
        return 1;
    }
    tcp = new WindowsTcp(5555, "127.0.0.1");
    uart = new WindowsUart(port, 921600, uartDisconnectHandler);
    if(uart->connect()) {
        Sleep(10);
        std::cout << "FlintJVM debug server is started" << std::endl;
        tcp->receiveTask(tcpRxHandler);
    }
    else
        std::cout << "Can't connect to " << port << std::endl;
    return 1;
}
