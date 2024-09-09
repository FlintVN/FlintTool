using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using Windows.Devices.Bluetooth.Advertisement;
using static System.Runtime.InteropServices.JavaScript.JSType;
using FlintTool.Models;
using Microsoft.VisualBasic;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Shapes;

namespace FlintTool.Services {
    public class FlintClient {
        private static readonly ushort[] crc16Table = {
            0x0000, 0xC0C1, 0xC181, 0x0140, 0xC301, 0x03C0, 0x0280, 0xC241,
            0xC601, 0x06C0, 0x0780, 0xC741, 0x0500, 0xC5C1, 0xC481, 0x0440,
            0xCC01, 0x0CC0, 0x0D80, 0xCD41, 0x0F00, 0xCFC1, 0xCE81, 0x0E40,
            0x0A00, 0xCAC1, 0xCB81, 0x0B40, 0xC901, 0x09C0, 0x0880, 0xC841,
            0xD801, 0x18C0, 0x1980, 0xD941, 0x1B00, 0xDBC1, 0xDA81, 0x1A40,
            0x1E00, 0xDEC1, 0xDF81, 0x1F40, 0xDD01, 0x1DC0, 0x1C80, 0xDC41,
            0x1400, 0xD4C1, 0xD581, 0x1540, 0xD701, 0x17C0, 0x1680, 0xD641,
            0xD201, 0x12C0, 0x1380, 0xD341, 0x1100, 0xD1C1, 0xD081, 0x1040,
            0xF001, 0x30C0, 0x3180, 0xF141, 0x3300, 0xF3C1, 0xF281, 0x3240,
            0x3600, 0xF6C1, 0xF781, 0x3740, 0xF501, 0x35C0, 0x3480, 0xF441,
            0x3C00, 0xFCC1, 0xFD81, 0x3D40, 0xFF01, 0x3FC0, 0x3E80, 0xFE41,
            0xFA01, 0x3AC0, 0x3B80, 0xFB41, 0x3900, 0xF9C1, 0xF881, 0x3840,
            0x2800, 0xE8C1, 0xE981, 0x2940, 0xEB01, 0x2BC0, 0x2A80, 0xEA41,
            0xEE01, 0x2EC0, 0x2F80, 0xEF41, 0x2D00, 0xEDC1, 0xEC81, 0x2C40,
            0xE401, 0x24C0, 0x2580, 0xE541, 0x2700, 0xE7C1, 0xE681, 0x2640,
            0x2200, 0xE2C1, 0xE381, 0x2340, 0xE101, 0x21C0, 0x2080, 0xE041,
            0xA001, 0x60C0, 0x6180, 0xA141, 0x6300, 0xA3C1, 0xA281, 0x6240,
            0x6600, 0xA6C1, 0xA781, 0x6740, 0xA501, 0x65C0, 0x6480, 0xA441,
            0x6C00, 0xACC1, 0xAD81, 0x6D40, 0xAF01, 0x6FC0, 0x6E80, 0xAE41,
            0xAA01, 0x6AC0, 0x6B80, 0xAB41, 0x6900, 0xA9C1, 0xA881, 0x6840,
            0x7800, 0xB8C1, 0xB981, 0x7940, 0xBB01, 0x7BC0, 0x7A80, 0xBA41,
            0xBE01, 0x7EC0, 0x7F80, 0xBF41, 0x7D00, 0xBDC1, 0xBC81, 0x7C40,
            0xB401, 0x74C0, 0x7580, 0xB541, 0x7700, 0xB7C1, 0xB681, 0x7640,
            0x7200, 0xB2C1, 0xB381, 0x7340, 0xB101, 0x71C0, 0x7080, 0xB041,
            0x5000, 0x90C1, 0x9181, 0x5140, 0x9301, 0x53C0, 0x5280, 0x9241,
            0x9601, 0x56C0, 0x5780, 0x9741, 0x5500, 0x95C1, 0x9481, 0x5440,
            0x9C01, 0x5CC0, 0x5D80, 0x9D41, 0x5F00, 0x9FC1, 0x9E81, 0x5E40,
            0x5A00, 0x9AC1, 0x9B81, 0x5B40, 0x9901, 0x59C0, 0x5880, 0x9841,
            0x8801, 0x48C0, 0x4980, 0x8941, 0x4B00, 0x8BC1, 0x8A81, 0x4A40,
            0x4E00, 0x8EC1, 0x8F81, 0x4F40, 0x8D01, 0x4DC0, 0x4C80, 0x8C41,
            0x4400, 0x84C1, 0x8581, 0x4540, 0x8701, 0x47C0, 0x4680, 0x8641,
            0x8201, 0x42C0, 0x4380, 0x8341, 0x4100, 0x81C1, 0x8081, 0x4040
        };

        private const int TCP_TIMEOUT_DEFAULT = 200;

        private FlintProtocol protocol;

        private enum FlintDbgCmd {
            DBG_CMD_ENTER_DEBUG = 0,
            DBG_CMD_READ_VM_INFO,
            DBG_CMD_READ_STATUS,
            DBG_CMD_READ_STACK_TRACE,
            DBG_CMD_ADD_BKP,
            DBG_CMD_REMOVE_BKP,
            DBG_CMD_REMOVE_ALL_BKP,
            DBG_CMD_RUN,
            DBG_CMD_STOP,
            DBG_CMD_RESTART,
            DBG_CMD_TERMINATE,
            DBG_CMD_STEP_IN,
            DBG_CMD_STEP_OVER,
            DBG_CMD_STEP_OUT,
            DBG_CMD_SET_EXCP_MODE,
            DBG_CMD_READ_EXCP_INFO,
            DBG_CMD_READ_LOCAL,
            DBG_CMD_WRITE_LOCAL,
            DBG_CMD_READ_FIELD,
            DBG_CMD_WRITE_FIELD,
            DBG_CMD_READ_ARRAY,
            DBG_CMD_READ_SIZE_AND_TYPE,
            DBG_CMD_READ_CONSOLE,
            DBG_CMD_OPEN_FILE,
            DBG_CMD_READ_FILE,
            DBG_CMD_WRITE_FILE,
            DBG_CMD_SEEK_FILE,
            DBG_CMD_CLOSE_FILE,
            DBG_CMD_FILE_INFO,
            DBG_CMD_DELETE_FILE,
            DBG_CMD_OPEN_DIR,
            DBG_CMD_READ_DIR,
            DBG_CMD_CREATE_DIR,
            DBG_CMD_CLOSE_DIR,
            DBG_CMD_UNKNOW = 0xFF,
        };

        private enum FlintDbgRespCode {
            DBG_RESP_OK = 0,
            DBG_RESP_BUSY = 1,
            DBG_RESP_FAIL = 2,
            DBG_RESP_CRC_FAIL = 3,
            DBG_RESP_LENGTH_INVAILD = 4,
            DBG_RESP_UNKNOW = 0xFF,
        }

        private class FlintDataResponse {
            public FlintDataResponse(FlintDbgCmd cmd, FlintDbgRespCode responseCode, byte[] data) {
                Cmd = cmd;
                Data = data;
                ResponseCode = responseCode;
            }

            public FlintDbgCmd Cmd {
                get; private set;
            }

            public byte[] Data {
                get; private set;
            }

            public FlintDbgRespCode ResponseCode {
                get; private set;
            }
        }

        public FlintClient(FlintProtocol protocol) {
            this.protocol = protocol;
        }

        private byte[] ReadData(int timeout) {
            byte[] data = new byte[20480];
            byte[] rxData = null;
            int rxDataTotalLength = 0;
            int rxDataLengthReceived = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while(stopwatch.ElapsedMilliseconds < timeout) {
                int length = protocol.Read(data, 0, data.Length, timeout);
                if(length > 0) {
                    if(rxDataLengthReceived < 7) {
                        if(rxData == null)
                            rxData = new byte[Math.Max(7, length)];
                        int index = 0;
                        while((rxDataLengthReceived < 7) && (index < length)) {
                            rxData[rxDataLengthReceived] = data[index];
                            rxDataLengthReceived++;
                            index++;
                        }
                        if(rxDataLengthReceived == 7) {
                            rxDataTotalLength = rxData[1] | (rxData[2] << 8) | (rxData[3] << 16);
                            if(rxDataTotalLength != rxData.Length) {
                                byte[] buff = new byte[rxDataTotalLength];
                                Array.Copy(rxData, buff, rxData.Length);
                                rxData = buff;
                            }
                        }
                        while(index < length) {
                            rxData[rxDataLengthReceived] = data[index];
                            rxDataLengthReceived++;
                            index++;
                        }
                    }
                    else {
                        Array.Copy(data, 0, rxData, rxDataLengthReceived, length);
                        rxDataLengthReceived += length;
                    }
                    if(rxData != null && (rxDataTotalLength > 0) && (rxDataLengthReceived >= rxDataTotalLength)) {
                        stopwatch.Stop();
                        return rxData;
                    }
                }
                Thread.Yield();
            }
            stopwatch.Stop();
            return null;
        }

        public bool Connect() {
            return protocol.Connect();
        }

        public void Disconnect() {
            protocol.Disconnect();
        }

        public bool IsConnected() {
            return protocol.IsConnected();
        }

        private FlintDataResponse SendCmd(FlintDbgCmd cmd, byte[] data, int timeout = TCP_TIMEOUT_DEFAULT) {
            lock(this) {
                int length = 1 + 3 + (data != null ? data.Length : 0) + 2;
                byte[] txData = new byte[length];
                txData[0] = (byte)cmd;
                txData[1] = (byte)((length >>> 0) & 0xFF);
                txData[2] = (byte)((length >>> 8) & 0xFF);
                txData[3] = (byte)((length >>> 16) & 0xFF);
                if(data != null) {
                    for(int i = 0; i < data.Length; i++)
                        txData[i + 4] = data[i];
                }
                ushort crc = CalcCrc(txData, 0, txData.Length - 2);
                txData[txData.Length - 2] = (byte)((crc >>> 0) & 0xFF);
                txData[txData.Length - 1] = (byte)((crc >>> 8) & 0xFF);

                if(!protocol.Write(txData, 0, txData.Length))
                    return null;

                byte[] rxData = ReadData(timeout);

                if(rxData != null) {
                    FlintDbgCmd respCmd = (FlintDbgCmd)(rxData[0] & 0x7F);
                    FlintDbgRespCode respCode = (FlintDbgRespCode)rxData[4];
                    ushort crc1 = (ushort)(rxData[rxData.Length - 2] | (rxData[rxData.Length - 1] << 8));
                    ushort crc2 = 0;
                    for(int i = 0; i < rxData.Length - 2; i++)
                        crc2 += rxData[i];
                    if(crc1 == crc2) {
                        byte[] respData = new byte[rxData.Length - 7];
                        Array.Copy(rxData, 5, respData, 0, respData.Length);
                        return new FlintDataResponse(respCmd, respCode, respData);
                    }
                }
                return null;
            }
        }

        private static ushort CalcCrc(byte[] data, int offset, int length) {
            ushort crc = 0xFFFF;
            for(int i = 0; i < length; i++)
                crc = (ushort)((crc16Table[(crc ^ data[i + offset]) & 0xFF] ^ (crc >> 8)) & 0xFFFF);
            return (ushort)(0xFFFF & ~crc);
        }

        private static int PutConstUtf8ToBuffer(byte[] buff, byte[] utf8Bytes, int buffOffset) {
            buff[buffOffset++] = (byte)((utf8Bytes.Length >>> 0) & 0xFF);
            buff[buffOffset++] = (byte)((utf8Bytes.Length >>> 8) & 0xFF);
            ushort crc = CalcCrc(utf8Bytes, 0, utf8Bytes.Length);
            buff[buffOffset++] = (byte)((crc >>> 0) & 0xFF);
            buff[buffOffset++] = (byte)((crc >>> 8) & 0xFF);
            Array.Copy(utf8Bytes, 0, buff, buffOffset, utf8Bytes.Length);
            return buffOffset + utf8Bytes.Length + 1;
        }

        private static ushort ReadU16(byte[] data, int offset) {
            ushort ret = data[offset];
            ret |= (ushort)(data[offset + 1] << 8);
            return ret;
        }

        private static uint ReadU32(byte[] data, int offset) {
            uint ret = data[offset];
            ret |= (uint)data[offset + 1] << 8;
            ret |= (uint)data[offset + 2] << 16;
            ret |= (uint)data[offset + 3] << 24;
            return ret >>> 0;
        }

        private static ulong ReadU64(byte[] data, int offset) {
            ulong ret = data[offset];
            ret |= (ulong)data[offset + 1] << 8;
            ret |= (ulong)data[offset + 2] << 16;
            ret |= (ulong)data[offset + 3] << 24;
            ret |= (ulong)data[offset + 4] << 32;
            ret |= (ulong)data[offset + 5] << 40;
            ret |= (ulong)data[offset + 6] << 48;
            ret |= (ulong)data[offset + 7] << 56;
            return ret;
        }

        public bool EnterDebugModeRequest() {
            FlintDataResponse resp = SendCmd(FlintDbgCmd.DBG_CMD_ENTER_DEBUG, null, 100);
            if(resp != null && resp.Cmd == FlintDbgCmd.DBG_CMD_ENTER_DEBUG && resp.ResponseCode == FlintDbgRespCode.DBG_RESP_OK)
                return true;
            else
                return false;
        }

        public bool TerminateRequest(bool includeDebugger) {
            FlintDataResponse resp = SendCmd(FlintDbgCmd.DBG_CMD_TERMINATE, new byte[1] { (byte)(includeDebugger ? 1 : 0) }, 5000);
            if(resp != null && resp.Cmd == FlintDbgCmd.DBG_CMD_TERMINATE && resp.ResponseCode == FlintDbgRespCode.DBG_RESP_OK)
                return true;
            else
                return false;
        }

        public bool DeleteFileRequest(string fileName) {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(fileName);
            byte[] cmdData = new byte[4 + utf8Bytes.Length + 1];
            PutConstUtf8ToBuffer(cmdData, utf8Bytes, 0);
            FlintDataResponse resp = SendCmd(FlintDbgCmd.DBG_CMD_DELETE_FILE, cmdData, 1000);
            if(resp != null && resp.Cmd == FlintDbgCmd.DBG_CMD_DELETE_FILE && resp.ResponseCode == FlintDbgRespCode.DBG_RESP_OK)
                return true;
            else
                return false;
        }

        public bool OpenDirRequest(string path) {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(path);
            byte[] cmdData = new byte[4 + utf8Bytes.Length + 1];
            PutConstUtf8ToBuffer(cmdData, utf8Bytes, 0);
            FlintDataResponse resp = SendCmd(FlintDbgCmd.DBG_CMD_OPEN_DIR, cmdData);
            if(resp != null && resp.Cmd == FlintDbgCmd.DBG_CMD_OPEN_DIR && resp.ResponseCode == FlintDbgRespCode.DBG_RESP_OK)
                return true;
            else
                return false;
        }

        public FlintFileInfo ReadDirRequest() {
            FlintDataResponse resp = SendCmd(FlintDbgCmd.DBG_CMD_READ_DIR, null);
            if(resp != null && resp.Cmd == FlintDbgCmd.DBG_CMD_READ_DIR && resp.ResponseCode == FlintDbgRespCode.DBG_RESP_OK) {
                if(resp.Data != null && resp.Data.Length > 0) {
                    byte attribute = resp.Data[0];
                    ushort nameLength = (ushort)(resp.Data[1] | (resp.Data[2] << 8));
                    ushort nameCrc = (ushort)(resp.Data[3] | (resp.Data[4] << 8));
                    string name = Encoding.UTF8.GetString(resp.Data, 5, nameLength);
                    int index = 5 + nameLength + 1;
                    uint size = ReadU32(resp.Data, index);
                    index += 4;
                    long unixTime = (long)ReadU64(resp.Data, index);
                    DateTime time = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
                    return new FlintFileInfo(name, attribute, time, size);
                }
                return null;
            }
            else
                throw new Exception("Error while reading directory");
        }

        public bool ClosedDirRequest() {
            FlintDataResponse resp = SendCmd(FlintDbgCmd.DBG_CMD_CLOSE_DIR, null);
            if(resp != null && resp.Cmd == FlintDbgCmd.DBG_CMD_CLOSE_DIR && resp.ResponseCode == FlintDbgRespCode.DBG_RESP_OK)
                return true;
            else
                return false;
        }
    }
}
