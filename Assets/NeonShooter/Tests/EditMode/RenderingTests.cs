using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using NeonShooter.Unity;
namespace NeonShooter.Tests
{
    public sealed class RenderingTests
    {
        [Test]
        public void EveryGameShaderCompilesOnTheActiveGraphicsApi()
        {
            foreach(var shader in Resources.LoadAll<Shader>("Shaders"))
            {
                var material=new Material(shader);
                try
                {
                    for(int pass=0;pass<material.passCount;pass++) ShaderUtil.CompilePass(material,pass,true);
                    var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error");
                    Assert.That(errors,Is.Empty,shader.name+": "+string.Join("; ",errors.Select(m=>m.message)));
                    Assert.That(shader.isSupported,Is.True,shader.name);
                }
                finally { Object.DestroyImmediate(material); }
            }
        }
        [Test]
        public void BrowserControlsUseTheOriginalLimeTypefaceAndCompactTargets()
        {
            var style=ArcadeButtons.CreateStyle(Resources.Load<Font>("Fonts/NeuePower"));
            foreach(var state in new[]{style.normal,style.hover,style.active,style.focused,style.onNormal,style.onHover,style.onActive,style.onFocused})
            {
                Assert.That(state.textColor.r,Is.EqualTo(Mathf.GammaToLinearSpace(202/255f)).Within(.001f));
                Assert.That(state.textColor.g,Is.GreaterThan(.9f));
                Assert.That(state.textColor.b,Is.EqualTo(Mathf.GammaToLinearSpace(96/255f)).Within(.001f));
            }
            Assert.That(style.fontSize,Is.EqualTo(15));
            Assert.That(ArcadeButtons.Height,Is.EqualTo(44));
        }
        [Test]
        public void BrowserPaletteAndTypefacesArePreserved()
        {
            var cyan=VoxelAssets.ColorOf("cyan");
            Assert.That(cyan.r,Is.EqualTo(Mathf.GammaToLinearSpace(40/255f)).Within(.001));
            Assert.That(cyan.g,Is.EqualTo(Mathf.GammaToLinearSpace(234/255f)).Within(.001));
            Assert.That(cyan.b,Is.EqualTo(1).Within(.001));
            foreach(string font in new[]{"OffBit","OffBit101","nohemi","NeuePower"})
                Assert.That(Resources.Load<Font>("Fonts/"+font),Is.Not.Null,font);
            Assert.That(QualitySettings.activeColorSpace,Is.EqualTo(ColorSpace.Linear));
        }
        [Test]
        public void GameUsesUniversalRenderPipeline()
        {
            Assert.That(GraphicsSettings.defaultRenderPipeline,Is.Not.Null);
            Assert.That(GraphicsSettings.defaultRenderPipeline.GetType().Name,Is.EqualTo("UniversalRenderPipelineAsset"));
            for(int i=0;i<QualitySettings.names.Length;i++)
                Assert.That(QualitySettings.GetRenderPipelineAssetAt(i),Is.SameAs(GraphicsSettings.defaultRenderPipeline));
        }
    }
}
