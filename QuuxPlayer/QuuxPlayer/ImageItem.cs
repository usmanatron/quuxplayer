/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{   

    internal class ImageData
    {
        public Image Image { get; set; }
        public byte[] Data { get; set; }
        private int hashCode;

        public ImageData()
        {
            this.Image = null;
            this.Data = null;
            hashCode = 0;
        }
        public void SetHashCode()
        {
            if (Data == null)
            {
                hashCode = 0;
            }
            else
            {
                System.Diagnostics.Debug.Assert((Data.Length % 4) == 0);

                for (int i = 5; i < Data.Length; i += 107)
                {
                    hashCode ^= (Data[i] | (Data[i + 1] << 8) | (Data[i + 2] << 16) | (Data[i + 3] << 24));
                }
            }
        }
        public override int GetHashCode()
        {
            return hashCode;
        }
    }

    internal class ImageItem
    {
        private const string DIB_STRING = "DeviceIndependentBitmap";

        private static Dictionary<string, ImageItem> items;

        private float aspectRatio = 1.0f;

        public float AspectRatio
        {
            get
            {
                if (imageData.Image == null && imageData.Data != null)
                    generateImage();

                return aspectRatio;
            }
            private set
            {
                aspectRatio = value;
            }
        }
        private static void addToItems(string Key, ImageItem Item)
        {
            if (Item != null && items.ContainsValue(Item))
            {
                Item.imageData = items.Values.First(i => i != null && i.Equals(Item)).imageData;
            }
            
            if (items.ContainsKey(Key))
                items.Remove(Key);
            
            items.Add(Key, Item);
        }
        private ImageData imageData = new ImageData();

        private bool Failed { get; set; }
        private string filePath = String.Empty;
        private ImageType it = ImageType.Unknown;

        public Source Src = Source.Unknown;

        static ImageItem()
        {
            items = new Dictionary<string, ImageItem>();
            addToItems(String.Empty, null);
        }

        private static ImageItem ImageItemFromImage(Image Image)
        {
            ImageItem ii = new ImageItem();
            ii.setImage(Image, false);
            ii.Src = Source.Unknown;

            if (!ii.Failed)
                return ii;
            else
                return null;
        }
        public static ImageItem ImageItemFromClipboard()
        {
            if (Clipboard.ContainsImage())
            {
                ImageItem ii = ImageItemFromImage(Clipboard.GetImage());
                ii.Src = Source.Clipboard;
                return ii;
            }
            else
            {
                return null;
            }
        }
        public bool Save(string NewFilePath)
        {
            if (Directory.Exists(Path.GetDirectoryName(NewFilePath)))
            {
                System.Diagnostics.Debug.Assert(this.Src != Source.File);
                System.Diagnostics.Debug.Assert(!File.Exists(NewFilePath));

                try
                {
                    ensureDataOK();

                    if (imageData.Data != null)
                    {
                        BinaryWriter bw = new BinaryWriter(File.OpenWrite(NewFilePath));
                        bw.Write(this.imageData.Data);
                        bw.Close();
                        this.Src = Source.File;
                        this.filePath = NewFilePath;
                        addToItems(NewFilePath, this);
                        return true;
                    }
                }
                catch
                {
                }
            }
            return false;
        }

        private enum ImageType { Unknown, JPG, GIF, PNG, TIF, BMP, MemoryBitmap };
        public enum Source { Unknown, Embedded, File, Download, Clipboard, Drag };
        
        public static ImageItem ImageFromLastFM(Track Track)
        {
            return ImageFromLastFM(Track.Artist, Track.Album);
        }
        public static ImageItem ImageFromLastFM(string Artist, string Album)
        {
            string key = Artist + Album;

            if (items.ContainsKey(key))
                return items[key];

            if (Artist.Length == 0 || Album.Length == 0)
            {
                addToItems(key, null);
                return null;
            }

            try
            {
                string url = LastFM.GetAlbumImageURL(Artist, Album);

                if (url.Length > 0)
                {
                    System.Net.HttpWebRequest httpRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(url);
                    System.Net.HttpWebResponse httpResponse = (System.Net.HttpWebResponse)httpRequest.GetResponse();

                    httpRequest.ServicePoint.Expect100Continue = false;

                    Stream imageStream = httpResponse.GetResponseStream();

                    ImageItem ii = new ImageItem();
                    ii.filePath = key;

                    List<byte[]> bytes = new List<byte[]>();
                    int length = 0;
                    int numRead;
                    byte[] bb = new byte[100000];

                    while ((numRead = imageStream.Read(bb, 0, 100000)) > 0)
                    {
                        if (numRead < 100000)
                        {
                            byte[] bbbb = new byte[numRead];
                            Array.Copy(bb, bbbb, numRead);
                            bb = bbbb;
                        }
                        bytes.Add(bb);
                        length += numRead;
                        bb = new byte[100000];
                    }

                    byte[] bbb = new byte[length];
                    int cursor = 0;
                    foreach (byte[] b in bytes)
                    {
                        Array.Copy(b, 0, bbb, cursor, Math.Min(length, b.Length));
                        length -= b.Length;
                        cursor += b.Length;
                    }

                    ii.imageData.Data = bbb;
                    ii.Src = Source.Download;

                    httpResponse.Close();
                    imageStream.Close();

                    ii.it = typeFromExtension(url);

                    addToItems(key, ii);
                    return ii;
                }
                else
                {
                    addToItems(key, null);
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                if (StringUtil.HasParentheticalChars(Album))
                    return ImageFromLastFM(Artist, StringUtil.RemoveParentheticalChars(Album));
            }
            addToItems(key, null);
            return null;
        }

        public static ImageItem ImageItemFromDrag(IDataObject Data)
        {
            string s = getGraphicFile(Data);

            if (s.Length > 0)
            {
                return ImageItemFromGraphicsFile(s);
            }
            else if (Data.GetDataPresent(typeof(Metafile)))
            {
                Image i = (Image)Data.GetData(typeof(Metafile));
                ImageItem ii = new ImageItem();
                ii.setImage(i, true);
                ii.Src = Source.Drag;
                return ii;
            }
            else if (Data.GetDataPresent(typeof(Bitmap)))
            {
                Image i = (Image)Data.GetData(typeof(Bitmap));
                return ImageItemFromImage(i);
            }
            else if (Data.GetDataPresent(DIB_STRING))
            {
                MemoryStream ms = (MemoryStream)Data.GetData(DIB_STRING);

                Bitmap b = bitmapFromDIB(ms);

                ms.Close();
                ms.Dispose();

                ImageItem ii = ImageItemFromImage(b);
                ii.Src = Source.Drag;
                return ii;
            }
            else
            {
                return null;
            }
        }
        public static ImageItem ImageItemFromAudioFile(string FilePath)
        {
            if (items.ContainsKey(FilePath))
                return items[FilePath];

            ImageItem ii = new ImageItem(FilePath);
            ii.Src = Source.Embedded;

            if (ii.Failed)
            {
                addToItems(FilePath, null);
                return null;
            }
            else
            {
                addToItems(FilePath, ii);
                return ii;
            }
        }
        private static string stringFromBytes(byte[] bb)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 0x1000; i++)
                if (bb[i] >= 32 && bb[i] <= 127)
                    sb.Append((char)bb[i]);

            return sb.ToString();
        }
        private ImageItem(ImageItem Template, string FileName)
        {
            this.imageData = Template.imageData;
            this.filePath = FileName;
            this.Failed = false;

            System.Diagnostics.Debug.Assert(Template.Equals(this));
        }
        private void generateImage()
        {
            MemoryStream ms = new MemoryStream(this.imageData.Data);
         
            Image i = new Bitmap(ms);

            this.setImage(i, true);
            
            ms.Close();
        }
        public static ImageItem ImageItemFromGraphicsFile(string FilePath)
        {
            if (items.ContainsKey(FilePath))
                return items[FilePath];

            BinaryReader br = null;
            try
            {
                br = new BinaryReader(File.OpenRead(FilePath));

                byte[] bb = new byte[br.BaseStream.Length];
                bb = br.ReadBytes(bb.Length);

                ImageItem ii = new ImageItem();
                ii.imageData.Data = bb;
                ii.generateImage();
                ii.Src = Source.File;

                if (!ii.Failed)
                {
                    ii.imageData.Data = bb;
                    ii.filePath = FilePath;
                    ii.it = typeFromExtension(FilePath);
                    addToItems(FilePath, ii);
                    return ii;
                }
                else
                {
                    addToItems(FilePath, null);
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                addToItems(FilePath, null);
                return null;
            }
            finally
            {
                if (br != null)
                    br.Close();
            }
            
        }
        private void setImageType()
        {
            Guid g = this.Image.RawFormat.Guid;

            if (g == ImageFormat.Bmp.Guid)
                this.it = ImageType.BMP;
            else if (g == ImageFormat.Gif.Guid)
                this.it = ImageType.GIF;
            else if (g == ImageFormat.Jpeg.Guid)
                this.it = ImageType.JPG;
            else if (g == ImageFormat.Png.Guid)
                this.it = ImageType.PNG;
            else if (g == ImageFormat.Tiff.Guid)
                this.it = ImageType.TIF;
        }
        private ImageItem(string FilePath)
        {
            try
            {
                TagLib.File file = TagLib.File.Create(FilePath);
                TagLib.Tag tag = file.Tag;

                if (tag.Pictures.Length > 0)
                {
                    MemoryStream ms = new MemoryStream(tag.Pictures[0].Data.Data);
                    imageData.Data = new byte[ms.Length];
                    ms.Read(imageData.Data, 0, (int)ms.Length);
                    ms.Position = 0;

                    this.setImage(new Bitmap(ms), true);

                    setImageType();

                    ms.Close();
                    ms.Dispose();

                    System.Diagnostics.Debug.Assert(this.Image.RawFormat.Guid != System.Drawing.Imaging.ImageFormat.MemoryBmp.Guid);

                    this.Failed = false;
                    this.filePath = FilePath;
                    
                }
                else
                {
                    this.Failed = true;
                }
            }
            catch
            {
                this.Failed = true;
            }
        }
        private ImageItem()
        {
        }

        public Image Image
        {
            get
            {
                if (imageData.Image == null && imageData.Data != null)
                    generateImage();
                return imageData.Image;
            }
        }
        private void setImage(Image Image, bool PreserveData)
        {
            if (!PreserveData)
            {
                this.imageData = new ImageData();
            }
            
            imageData.Image = Image;
            this.AspectRatio = (float)imageData.Image.Width / (float)imageData.Image.Height;
        }

        public byte[] ImageBytesForEmbed
        {
            get
            {
                if (!ensureDataOK())
                    convertToJpeg();
                return imageData.Data;
            }
        }
        public static void RegisterAsEmbedded(Track Track)
        {
            addToItems(Track.FilePath, Track.Cover);
            Track.ForceEmbeddedImageNull = false;
        }
        private bool ensureDataOK()
        {
            try
            {
                if (imageData.Data != null)
                {
                    switch (this.it)
                    {
                        case ImageType.Unknown:
                        case ImageType.MemoryBitmap:
                            convertToJpeg();
                            break;
                        default:
                            return true;
                    }

                    if (this.it == ImageType.Unknown)
                    {
                        return false;
                    }

                    MemoryStream ms = new MemoryStream();

                    Image.Save(ms, Image.RawFormat);
                    ms.Position = 0;
                    this.imageData.Data = new byte[ms.Length];

                    ms.Read(imageData.Data, 0, (int)ms.Length);

                    ms.Close();

                    return true;
                }
                else if (Image == null)
                {
                    return false;
                }
                else
                {
                    MemoryStream ms = new MemoryStream();

                    if (this.Image.RawFormat.Guid == ImageFormat.MemoryBmp.Guid)
                        this.Image.Save(ms, ImageFormat.Jpeg);
                    else
                        this.Image.Save(ms, this.Image.RawFormat);

                    ms.Position = 0;
                    this.imageData.Data = new byte[(int)ms.Length];
                    ms.Read(this.imageData.Data, 0, (int)ms.Length);
                    ms.Close();

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return false;
        }
        private void convertToJpeg()
        {
            MemoryStream ms = null;

            try
            {
                if (this.Image.RawFormat.Guid == ImageFormat.Jpeg.Guid)
                {
                    this.it = ImageType.JPG;
                    return;
                }

                ms = new MemoryStream();
                
                ImageCodecInfo ici = ImageCodecInfo.GetImageEncoders().First(ie => ie.FilenameExtension.ToLowerInvariant().Contains("jpg"));
                EncoderParameters ep = new EncoderParameters();
                ep.Param = new EncoderParameter[1];
                ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)90);
                
                this.Image.Save(ms,
                                ici,
                                ep);

                ms.Position = 0;
                this.imageData.Data = new byte[ms.Length];
                ms.Read(imageData.Data, 0, (int)ms.Length);

                ms.Close();

                this.setImage(Image.FromStream(new MemoryStream(this.imageData.Data)), true);

                this.it = ImageType.JPG;
            }
            catch
            {
                this.it = ImageType.Unknown;
            }
            finally
            {
                if (ms != null)
                    ms.Close();
            }
        }

        public override bool Equals(object obj)
        {
            ImageItem ii;
            if ((ii = obj as ImageItem) != null)
            {
                return bytesEqual(ii.imageData.Data, this.imageData.Data);
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            int hc = imageData.GetHashCode();
            if (hc == 0)
            {
                imageData.SetHashCode();
                hc = imageData.GetHashCode();
            }
            return hc;
        }
        private static bool bytesEqual(byte[] b1, byte[] b2)
        {
            if (b1 == null || b2 == null)
                return false;

            if (b1.Length == b2.Length)
            {
                for (int i = 0; i < b1.Length; i++)
                {
                    if (b1[i] != b2[i])
                        return false;
                }
                return true;
            }
            return false;
        }
        private static string getGraphicFile(IDataObject Data)
        {
            if (Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] ss = (string[])Data.GetData(DataFormats.FileDrop);
                foreach (string s in ss)
                {
                    if (typeFromExtension(s) != ImageType.Unknown)
                        return s;
                }
            }
            return String.Empty;
        }
        private static ImageType typeFromExtension(string FilePath)
        {
            switch (Path.GetExtension(FilePath).ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    return ImageType.JPG;
                case ".png":
                    return ImageType.PNG;
                case ".gif":
                    return ImageType.GIF;
                case ".bmp":
                    return ImageType.BMP;
                case ".tif":
                case ".tiff":
                    return ImageType.TIF;
                default:
                    return ImageType.Unknown;
            }
        }
        public static bool DragHasImage(IDataObject Data)
        {
            if (Data.GetDataPresent(typeof(Metafile)) ||
                Data.GetDataPresent(typeof(Bitmap)) ||
                Data.GetDataPresent(DIB_STRING) ||
                getGraphicFile(Data).Length > 0)
            {
                return true;
                
            }
            return false;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        private static Bitmap bitmapFromDIB(MemoryStream dib)
        {
            // get byte array of device independent bitmap
            byte[] dibBytes = dib.ToArray();

            // get the handle for the byte array and "pin" that memory (i.e. prevent
            // garbage collector from gobbling it up right away)...
            GCHandle hdl = GCHandle.Alloc(dibBytes, GCHandleType.Pinned);
            // marshal our data into a BITMAPINFOHEADER struct per Win32
            // definition of BITMAPINFOHEADER

            BITMAPINFOHEADER dibHdr = (BITMAPINFOHEADER)Marshal.PtrToStructure(hdl.AddrOfPinnedObject(),
                                                                               typeof(BITMAPINFOHEADER));

            bool is555 = true;
            Bitmap bmp = null;
            if (dibHdr.biBitCount == 8)
            {
                // set our pointer to end of BITMAPINFOHEADER
                Int64 jumpTo = hdl.AddrOfPinnedObject().ToInt64() + dibHdr.biSize;

                bmp = new Bitmap(dibHdr.biWidth, dibHdr.biHeight, PixelFormat.Format8bppIndexed);
                bmp.SetResolution((100f * (float)dibHdr.biXPelsPerMeter) / 2.54f,
                                  (100f * (float)dibHdr.biYPelsPerMeter) / 2.54f);

                ColorPalette palette = bmp.Palette;
                IntPtr ptr = IntPtr.Zero;
                int colors = (int)(dibBytes.Length - (bmp.Width * bmp.Height) - dibHdr.biSize);

                for (int i = 0; i < 0x100; i++)
                {
                    ptr = new IntPtr(jumpTo);
                    uint bmiColor = (uint)Marshal.ReadInt32(ptr);

                    int r = (int)((bmiColor & 0xFF0000) >> 16);
                    int g = (int)((bmiColor & 0xFF00) >> 8);
                    int b = (int)((bmiColor & 0xFF));

                    palette.Entries[i] = Color.FromArgb(r, g, b);
                    jumpTo += 4;
                }
                bmp.Palette = palette;
                BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                               ImageLockMode.WriteOnly,
                                               PixelFormat.Format8bppIndexed);

                jumpTo -= hdl.AddrOfPinnedObject().ToInt64();

                Marshal.Copy(dibBytes, (int)jumpTo, bd.Scan0, bd.Stride * bd.Height);
                bmp.UnlockBits(bd);
            }

            else if ((dibHdr.biBitCount == 16) && (dibHdr.biCompression == 3))
            {
                Int64 jumpTo = (Int64)(dibHdr.biClrUsed * (uint)4 + dibHdr.biSize);
                IntPtr ptr = new IntPtr(hdl.AddrOfPinnedObject().ToInt64() + jumpTo);

                ushort redMask = (ushort)Marshal.ReadInt16(ptr);
                ptr = new IntPtr(ptr.ToInt64() + (2 * Marshal.SizeOf(typeof(UInt16))));
                ushort greenMask = (ushort)Marshal.ReadInt16(ptr);
                ptr = new IntPtr(ptr.ToInt64() + (2 * Marshal.SizeOf(typeof(UInt16))));
                ushort blueMask = (ushort)Marshal.ReadInt16(ptr);
                is555 = ((redMask == 0x7C00) && (greenMask == 0x03E0) && (blueMask == 0x001F));
            }
            hdl.Free();

            if (dibHdr.biPlanes != 1 || (dibHdr.biCompression != 0 && dibHdr.biCompression != 3))
                return null;

            if (bmp == null)
            {
                // we need to know beforehand the pixel-depth of our bitmap
                PixelFormat fmt = PixelFormat.Format24bppRgb;
                switch (dibHdr.biBitCount)
                {
                    case 32:
                        fmt = PixelFormat.Format32bppRgb;
                        break;
                    case 24:
                        fmt = PixelFormat.Format24bppRgb;
                        break;
                    case 16:
                        fmt = (is555) ? PixelFormat.Format16bppRgb555 : PixelFormat.Format16bppRgb565;
                        break;
                    default:
                        return null;
                }
                bmp = new Bitmap(dibHdr.biWidth, dibHdr.biHeight, fmt);

                BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                             ImageLockMode.WriteOnly, fmt);

                Marshal.Copy(dibBytes, Marshal.SizeOf(dibHdr), bd.Scan0, bd.Stride * bd.Height);

                bmp.UnlockBits(bd);
            }

            if (dibHdr.biHeight > 0)
            {
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            }
            return bmp;
        }
        public override string ToString()
        {
            return this.GetHashCode().ToString();
        }
    }
}
