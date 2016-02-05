using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;
using SFML.Window;

namespace vimage
{
    class AdvancedImage : DisplayObject
    {

        private List<List<Texture>> Textures;

        private bool _Smooth = true;
        public bool Smooth
        {
            get { return _Smooth; }
            set
            {
                _Smooth = value;
                for (int iy = 0; iy < Textures.Count; iy++)
                {
                    for (int ix = 0; ix < Textures[iy].Count; ix++)
                        Textures[iy][ix].Smooth = _Smooth;
                }
            }
        }

        public AdvancedImage(string fileName)
        {
            Textures = Graphics.GetTextures(fileName);

            if (Textures.Count == 1 && Textures[0].Count == 1)
            {
                Sprite sprite = new Sprite(Textures[0][0]);
                AddChild(sprite);
                Size = Textures[0][0].Size;
            }
            else
            {
                Vector2u currentPos = new Vector2u();
                uint width = 0;
                uint height = 0;

                for (int iy = 0; iy < Textures.Count; iy++)
                {
                    height += Textures[iy][0].Size.Y;

                    for (int ix = 0; ix < Textures[iy].Count; ix++)
                    {
                        if (iy == 0)
                            width += Textures[iy][ix].Size.X;

                        Sprite sprite = new Sprite(Textures[iy][ix]);
                        sprite.Position = new Vector2f(currentPos.X, currentPos.Y);
                        AddChild(sprite);

                        currentPos.X += Textures[iy][ix].Size.X;
                    }
                    currentPos.Y += Textures[iy][0].Size.Y;
                    currentPos.X = 0;
                }
                Size = new Vector2u(width, height);
            }
        }

    }
}
