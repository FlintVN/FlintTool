using System;

namespace FlintTool.Models {
    public class FlintFileInfo {
        public static readonly int FILE_ATTR_RDO = 0x01;
        public static readonly int FILE_ATTR_HID = 0x02;
        public static readonly int FILE_ATTR_SYS = 0x04;
        public static readonly int FILE_ATTR_DIR = 0x10;
        public static readonly int FILE_ATTR_ARC = 0x20;

        public FlintFileInfo(string name, byte attribute, DateTime dateTime, uint size) {
            Name = name;
            Attribute = attribute;
            DateTime = dateTime;
            Size = size;
        }

        public string Name {
            get; set;
        }

        public byte Attribute {
            get; set;
        }

        public bool IsFile {
            get => (Attribute & FlintFileInfo.FILE_ATTR_DIR) == 0;
        }

        public bool IsDirectory {
            get => (Attribute & FlintFileInfo.FILE_ATTR_DIR) != 0;
        }

        public DateTime DateTime {
            get; set;
        }

        public uint Size {
            get; set;
        }
    }
}
