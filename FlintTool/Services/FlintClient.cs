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

namespace FlintTool.Services {
    public class FlintClient {
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
            DBG_CMD_CLOSE_FILE,
            DBG_CMD_READ_FILE_INFO,
            DBG_CMD_DELETE_FILE,
            DBG_CMD_OPEN_DIR,
            DBG_CMD_READ_DIR,
            DBG_CMD_CREATE_DIR,
            DBG_CMD_CLOSE_DIR,
            DBG_CMD_DELETE_DIR,
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
            int rxDataLengthReceived = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            while(stopwatch.ElapsedMilliseconds < timeout) {
                int length = protocol.Read(data, 0, data.Length, timeout);
                if(length > 0) {
                    if(rxData == null) {
                        if(length >= 7) {
                            int dataLength = data[1] | (data[2] << 8) | (data[3] << 16);
                            if(dataLength >= length) {
                                rxData = new byte[length];
                                Array.Copy(data, rxData, length);
                                rxDataLengthReceived = length;
                            }
                        }
                    }
                    else {
                        Array.Copy(data, rxData, length);
                        rxDataLengthReceived += length;
                    }
                    if(rxData != null && rxDataLengthReceived >= rxData.Length) {
                        stopwatch.Stop();
                        return rxData;
                    }
                }
                Thread.Sleep(1);
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
            int length = 1 + 3 + (data != null ? data.Length : 0) + 2;
            byte[] txData = new byte[length];
            txData[0] = (byte)cmd;
            txData[1] = (byte)((length >>> 0) & 0xFF);
            txData[2] = (byte)((length >>> 8) & 0xFF);
            txData[3] = (byte)((length >>> 16) & 0xFF);
            ushort crc = (ushort)(txData[0] + txData[1] + txData[2] + txData[3]);
            if(data != null) {
                for(int i = 0; i < data.Length; i++) {
                    txData[i + 4] = data[i];
                    crc += data[i];
                }
            }
            txData[txData.Length - 2] = (byte)((crc >>> 0) & 0xFF);
            txData[txData.Length - 1] = (byte)((crc >>> 8) & 0xFF);

            protocol.Write(txData, 0, txData.Length);

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

        private static ushort CalcCrc(byte[] data) {
            ushort crc = 0;
            for(int i = 0; i < data.Length; i++)
                crc += data[i];
            return crc;
        }

        private static int PutConstUtf8ToBuffer(byte[] buff, byte[] utf8Bytes, int buffOffset) {
            buff[buffOffset++] = (byte)((utf8Bytes.Length >>> 0) & 0xFF);
            buff[buffOffset++] = (byte)((utf8Bytes.Length >>> 8) & 0xFF);
            ushort crc = CalcCrc(utf8Bytes);
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
