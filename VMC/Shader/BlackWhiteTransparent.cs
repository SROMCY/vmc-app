using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace VMC
{
    public class BlackWhiteTransparent : ShaderEffect
    {
        private static readonly PixelShader bwtPixelShader = new PixelShader { UriSource = new Uri("./Shader/bwt.ps", UriKind.Relative) };

        public BlackWhiteTransparent()
        {
            this.PixelShader = bwtPixelShader;
            UpdateShaderValue(InputProperty);
        }

        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(BlackWhiteTransparent), 0);
    }
}
