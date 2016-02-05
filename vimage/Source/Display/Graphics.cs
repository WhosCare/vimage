using SFML.Graphics;
using System.Collections.Generic;
using System;
using DevIL.Unmanaged;
using Tao.OpenGl;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SFML.Window;

namespace vimage
{
    /// <summary>
    /// Graphics Manager.
    /// Loads and stores Textures and AnimatedImageDatas.
    /// </summary>
    class Graphics
    {
        private static List<Texture> Textures = new List<Texture>();
        private static List<string> TextureFileNames = new List<string>();

        private static List<AnimatedImageData> AnimatedImageDatas = new List<AnimatedImageData>();
        private static List<string> AnimatedImageDataFileNames = new List<string>();

        public const uint MAX_TEXTURES = 20;
        public const uint MAX_ANIMATIONS = 6;
        public static int TextureMaxSize = 16000;

        public static void Init()
        {
            Texture.Bind(new Texture(1, 1));
            Gl.glGetIntegerv(Gl.GL_MAX_TEXTURE_SIZE, out TextureMaxSize);
            Texture.Bind(null);
        }

        public static AdvancedImage GetAdvancedImage(string fileName, bool smooth = true)
        {
            AdvancedImage image = new AdvancedImage(fileName);
            return image;
        }
        public static Sprite GetSprite(string fileName, bool smooth = false)
        {
            Sprite sprite = new Sprite(GetTexture(fileName));
            sprite.Texture.Smooth = smooth;

            return sprite;
        }
        public static Texture GetTexture(string fileName)
        {
            int index = TextureFileNames.IndexOf(fileName);

            if (index >= 0)
            {
                // Texture Already Exists
                // move it to the end of the array and return it
                Texture texture = Textures[index];
                string name = TextureFileNames[index];

                Textures.RemoveAt(index);
                TextureFileNames.RemoveAt(index);
                Textures.Add(texture);
                TextureFileNames.Add(name);

                return Textures[Textures.Count - 1];
            }
            else
            {
                // New Texture
                Texture texture = null;
                int imageID = IL.GenerateImage();
                IL.BindImage(imageID);

                IL.Enable(ILEnable.AbsoluteOrigin);
                IL.SetOriginLocation(DevIL.OriginLocation.UpperLeft);

                bool loaded = false;
                using (FileStream fileStream = File.OpenRead(fileName))
                    loaded = IL.LoadImageFromStream(fileStream);


                if (IL.GetImageInfo().Width > TextureMaxSize || IL.GetImageInfo().Height > TextureMaxSize)
                {
                    System.Windows.Forms.MessageBox.Show("Image exceeds the GPU's maximum supported texture size (" + TextureMaxSize + ").", "vimage");
                    return null;
                }

                if (loaded)
                {
                    texture = GetTextureFromBoundImage();

                    Textures.Add(texture);
                    TextureFileNames.Add(fileName);

                    // Limit amount of Textures in Memory
                    if (Textures.Count > MAX_TEXTURES)
                    {
                        Textures[0].Dispose();
                        Textures.RemoveAt(0);
                        TextureFileNames.RemoveAt(0);
                    }
                }
                IL.DeleteImages(new ImageID[] { imageID });

                return texture;
            }
        }
        private static Texture GetTextureFromBoundImage(int imageNum = 0)
        {
            IL.ActiveImage(imageNum);
            
            bool success = IL.ConvertImage(DevIL.DataFormat.RGBA, DevIL.DataType.UnsignedByte);
            
            if (!success)
                return null;

            int width = IL.GetImageInfo().Width;
            int height = IL.GetImageInfo().Height;

            Texture texture = new Texture((uint)width, (uint)height);
            Texture.Bind(texture);
            {
                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
                Gl.glTexImage2D(
                    Gl.GL_TEXTURE_2D, 0, IL.GetInteger(ILIntegerMode.ImageBytesPerPixel),
                    width, height, 0,
                    IL.GetInteger(ILIntegerMode.ImageFormat), ILDefines.IL_UNSIGNED_BYTE,
                    IL.GetData()
                    );
            }
            Texture.Bind(null);

            return texture;
        }

        public static List<List<Texture>> GetTextures(string fileName)
        {
            // New Texture
            List<List<Texture>> textures = null;
            int imageID = IL.GenerateImage();
            IL.BindImage(imageID);

            IL.Enable(ILEnable.AbsoluteOrigin);
            IL.SetOriginLocation(DevIL.OriginLocation.UpperLeft);

            bool loaded = false;
            using (FileStream fileStream = File.OpenRead(fileName))
                loaded = IL.LoadImageFromStream(fileStream);

            if (loaded)
            {
                textures = GetTexturesFromBoundImage();
            }
            IL.DeleteImages(new ImageID[] { imageID });

            return textures;
        }
        private static List<List<Texture>> GetTexturesFromBoundImage(int imageNum = 0)
        {
            IL.ActiveImage(imageNum);

            bool success = IL.ConvertImage(DevIL.DataFormat.RGBA, DevIL.DataType.UnsignedByte);

            if (!success)
                return null;

            int width = IL.GetImageInfo().Width;
            int height = IL.GetImageInfo().Height;

            int sectionSize = TextureMaxSize;

            List<List<Texture>> textures = new List<List<Texture>>();
            Vector2u amount = new Vector2u((uint)Math.Ceiling(width / (float)sectionSize), (uint)Math.Ceiling(height / (float)sectionSize));

            if (amount.X == 1 && amount.Y == 1)
            {
                Texture texture = new Texture((uint)width, (uint)height);
                Texture.Bind(texture);
                {
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
                    Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
                    Gl.glTexImage2D(
                        Gl.GL_TEXTURE_2D, 0, IL.GetInteger(ILIntegerMode.ImageBytesPerPixel),
                        width, height, 0,
                        IL.GetInteger(ILIntegerMode.ImageFormat), ILDefines.IL_UNSIGNED_BYTE,
                        IL.GetData()
                        );
                }
                Texture.Bind(null);
                textures.Add(new List<Texture>());
                textures[0].Add(texture);
            }
            else
            {
                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);

                Console.WriteLine(width + "x" + height + " image cut into " + amount.X + " by " + amount.Y + " pieces.");

                Vector2i currentSize = new Vector2i(width, height);
                Vector2i pos = new Vector2i();

                for (int iy = 0; iy < amount.Y; iy++)
                {
                    textures.Add(new List<Texture>());

                    int h = Math.Min(currentSize.Y, sectionSize);
                    currentSize.Y -= h;
                    currentSize.X = width;

                    for (int ix = 0; ix < amount.X; ix++)
                    {
                        int w = Math.Min(currentSize.X, sectionSize);
                        currentSize.X -= w;

                        Texture texture = new Texture((uint)w, (uint)h);
                        Texture.Bind(texture);
                        {
                            Console.WriteLine(w + "  " + h);

                            IntPtr partPtr = Marshal.AllocHGlobal((w * h) * 4);
                            IL.CopyPixels(pos.X, pos.Y, 0, w, h, 1, DevIL.DataFormat.RGBA, DevIL.DataType.UnsignedByte, partPtr);
                            //IntPtr partPtr = Marshal.AllocHGlobal(part.Length);
                            //Marshal.Copy(part, 0, partPtr, part.Length);

                            Gl.glTexImage2D(
                                Gl.GL_TEXTURE_2D, 0, IL.GetInteger(ILIntegerMode.ImageBytesPerPixel),
                                w, h, 0,
                                IL.GetInteger(ILIntegerMode.ImageFormat), ILDefines.IL_UNSIGNED_BYTE,
                                partPtr
                                );
                        }
                        Texture.Bind(null);

                        textures[iy].Add(texture);

                        pos.X += w;
                    }
                    pos.Y += h;
                    pos.X = 0;
                }
            }

            return textures;
        }

        /// <param name="filename">Animated Image (ie: animated gif).</param>
        public static AnimatedImage GetAnimatedImage(string fileName)
        {
            return new AnimatedImage(GetAnimatedImageData(fileName));
        }
        /// <param name="filename">Animated Image (ie: animated gif).</param>
        public static AnimatedImageData GetAnimatedImageData(string fileName)
        {
            int index = AnimatedImageDataFileNames.IndexOf(fileName);

            if (index >= 0)
            {
                // AnimatedImageData Already Exists
                // move it to the end of the array and return it
                AnimatedImageData data = AnimatedImageDatas[index];
                string name = AnimatedImageDataFileNames[index];

                AnimatedImageDatas.RemoveAt(index);
                AnimatedImageDataFileNames.RemoveAt(index);
                AnimatedImageDatas.Add(data);
                AnimatedImageDataFileNames.Add(name);

                return AnimatedImageDatas[AnimatedImageDatas.Count-1];
            }
            else
            {
                // New AnimatedImageData
                System.Drawing.Image image = System.Drawing.Image.FromFile(fileName);
                AnimatedImageData data = new AnimatedImageData();

                //// Get Frame Duration
                int frameDuration = 0;
                try
                {
                    System.Drawing.Imaging.PropertyItem frameDelay = image.GetPropertyItem(0x5100);
                    frameDuration = (frameDelay.Value[0] + frameDelay.Value[1] * 256) * 10;
                }
                catch { }
                if (frameDuration > 10)
                    data.FrameDuration = frameDuration;
                else
                    data.FrameDuration = AnimatedImage.DEFAULT_FRAME_DURATION;
                
                //// Store AnimatedImageData
                AnimatedImageDatas.Add(data);
                AnimatedImageDataFileNames.Add(fileName);

                // Limit amount of Animations in Memory
                if (AnimatedImageDatas.Count > MAX_ANIMATIONS)
                {
                    for (int i = 0; i < AnimatedImageDatas[0].Frames.Count; i++)
                        AnimatedImageDatas[0].Frames[i].Dispose();
                    
                    AnimatedImageDatas.RemoveAt(0);
                    AnimatedImageDataFileNames.RemoveAt(0);
                }

                //// Get Frames
                LoadingAnimatedImage loadingAnimatedImage = new LoadingAnimatedImage(image, data);
                Thread loadFramesThread = new Thread(new ThreadStart(loadingAnimatedImage.LoadFrames));
                loadFramesThread.IsBackground = true;
                loadFramesThread.Start();

                while (data.Frames.Count <= 0); // wait for at least one frame to be loaded
                
                return data;
            }
        }

    }

    class LoadingAnimatedImage
    {
        private System.Drawing.Image Image;
        private ImageManipulation.OctreeQuantizer Quantizer;
        private AnimatedImageData Data;

        public LoadingAnimatedImage(System.Drawing.Image image, AnimatedImageData data)
        {
            Image = image;
            Data = data;
        }

        public void LoadFrames()
        {
            System.Drawing.Imaging.FrameDimension frameDimension = new System.Drawing.Imaging.FrameDimension(Image.FrameDimensionsList[0]);
            Data.FramesCount = Image.GetFrameCount(frameDimension);

            for (int i = 0; i < Image.GetFrameCount(frameDimension); i++)
            {
                Image.SelectActiveFrame(frameDimension, i);
                Quantizer = new ImageManipulation.OctreeQuantizer(255, 8);

                System.Drawing.Bitmap quantized = Quantizer.Quantize(Image);
                MemoryStream stream = new MemoryStream();
                quantized.Save(stream, System.Drawing.Imaging.ImageFormat.Gif);
                Data.Frames.Add(new Texture(stream));

                stream.Dispose();

                Data.Frames[i].Smooth = true;
            }
        }
    }

}
