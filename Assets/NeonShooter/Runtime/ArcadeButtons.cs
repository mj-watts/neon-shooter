using UnityEngine;
namespace NeonShooter.Unity
{
    public static class ArcadeButtons
    {
        public const float Height=44;
        public static GUIStyle CreateStyle(Font font)
        {
            var style=new GUIStyle
            {
                font=font,fontSize=15,fontStyle=FontStyle.Bold,
                alignment=TextAnchor.MiddleCenter,wordWrap=false,
                padding=new RectOffset(8,8,4,4),border=new RectOffset(0,0,0,0)
            };
            // Never inherit editor-skin colors, including hover, pressed and keyboard focus.
            foreach(var state in new[]{style.normal,style.hover,style.active,style.focused,style.onNormal,style.onHover,style.onActive,style.onFocused})
                state.textColor=new Color(202/255f,1,96/255f).linear;
            return style;
        }
    }
}
