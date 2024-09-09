using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlintTool.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FlintTool.ViewModels {
    public class FileInfoItemViewModel {
        public FileInfoItemViewModel(FlintFileInfo fileInfo) {
            IconSource = GetIconSource(fileInfo);
            IsFile = fileInfo.IsFile;
            Name = fileInfo.Name;
            DateModified = fileInfo.DateTime.ToString("dd/MM/yyyy hh:mm tt");
            if(fileInfo.IsFile)
                Type = Path.GetExtension(fileInfo.Name).ToUpper().Replace(".", "") + " File";
            else
                Type = "File folder";
            Size = fileInfo.IsFile ? FileSizeToString(fileInfo.Size) : "";
        }

        private string FileSizeToString(uint value) {
            if(value < 0x0400)
                return value.ToString() + " B";
            else if((double)value < 0x100000)
                return ((double)value / 0x0400).ToString("0.00") + " KB";
            else if((double)value < 0x40000000)
                return ((double)value / 0x100000).ToString("0.00") + " MB";
            else
                return ((double)value / 0x40000000).ToString("0.00") + " GB";
        }

        private string GetIconSource(FlintFileInfo fileInfo) {
            if(fileInfo.IsFile) {
                try {
                    string extensionName = Path.GetExtension(fileInfo.Name.ToUpper()).Remove(0, 1);
                    switch(extensionName) {
                        case "JS":
                            return "ms-appx:///Assets/js_file_icon.png";
                        case "ISO":
                            return "ms-appx:///Assets/iso_file_icon.png";
                        case "DPF":
                            return "ms-appx:///Assets/pdf_file_icon.png";
                        case "PPT":
                            return "ms-appx:///Assets/ppt_file_icon.png";
                        case "XLS":
                            return "ms-appx:///Assets/xls_file_icon.png";
                        case "SVG":
                            return "ms-appx:///Assets/svg_file_icon.png";
                        case "SQL":
                            return "ms-appx:///Assets/sql_file_icon.png";
                        case "CLASS":
                            return "ms-appx:///Assets/class_file_icon.png";
                        case "JAVA":
                            return "ms-appx:///Assets/java_file_icon.png";
                        case "TXT":
                        case "TEXT":
                            return "ms-appx:///Assets/txt_file_icon.png";
                        case "DOC":
                        case "DOCX":
                            return "ms-appx:///Assets/word_file_icon.png";
                        case "ZIP":
                        case "RAR":
                        case "7Z":
                            return "ms-appx:///Assets/zip_file_icon.png";
                        case "WAV":
                        case "MP3":
                        case "M4A":
                        case "WMA":
                        case "AAC":
                        case "OGG":
                        case "MKA":
                        case "APE":
                        case "AIFF":
                        case "FLAC":
                            return "ms-appx:///Assets/music_file_icon.png";
                        case "MP4":
                        case "3GP":
                        case "AVI":
                        case "FLV":
                        case "MPEG":
                            return "ms-appx:///Assets/video_file_icon.png";
                        case "PNG":
                        case "JPG":
                        case "GIF":
                        case "BMP":
                        case "TIF":
                        case "ICO":
                        case "TIFF":
                        case "JPEG":
                        case "HEIF":
                        case "HEIC":
                        case "WEBP":
                            return "ms-appx:///Assets/img_file_icon.png";
                        default:
                            return "ms-appx:///Assets/bin_file_icon.png";
                    }
                }
                catch {
                    return "ms-appx:///Assets/bin_file_icon.png";
                }
            }
            else
                return "ms-appx:///Assets/folder.png";
        }

        public string IconSource {
            get; private set;
        }

        public bool IsFile {
            get; private set;
        }

        public string Name {
            get; private set;
        }

        public string DateModified {
            get; private set;
        }

        public string Type {
            get; private set;
        }

        public string Size {
            get; private set;
        }
    }
}
