using System.Collections.Generic;
namespace NeonShooter.Core
{
    // Silhouettes and palettes matched to the browser renderer.
    public static class SpriteCatalog
    {
        public static readonly string[] Hero = new string[] { "....B...B....", "...BBBBBBB...", "...VBBGBBV...", "..CVBBGBBVC..", ".CVVBGGGBVVC.", "CVVPPGGGPPVVC", "CVVPBBBBBPVVC", "CVVP.BBB.PVVC", "CVV..PBP..VVC", "CV...P.P...VC", "C...........C" };
        public static readonly string[][] Enemies = new string[][] { new string[] { "....P....", "...PPP...", ".PPPPPPP.", "PPVPVPVPP", "PP.PPP.PP", "P...P...P" }, new string[] { "OO.....OO", "OOO...OOO", ".OOVVVOO.", "..OOGOO..", ".OOVVVOO.", "OO.....OO" }, new string[] { "..PPPPP..", ".PPVVVPP.", "PPVPGPVPP", "PPVVVVVPP", ".PP...PP.", "..P...P.." } };
        public static readonly string[] Star = new string[] { "....X....", "...XXX...", "...XXX...", "XXXXXXXXX", ".XXXXXXX.", "..XXXXX..", "..XX.XX..", ".XX...XX.", ".X.....X." };
        public static readonly string[] Heart = new string[] { ".XX.XX.", "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..." };
        public static readonly Dictionary<string,string[]> Icons = new Dictionary<string,string[]> {
            { "speed", new string[] { "....XX.", "...XX..", "..XX...", ".XXXXX.", "...XX..", "..XX...", ".XX...." } },
            { "assault", new string[] { ".X...X.", "XXX.XXX", ".X...X.", "XXX.XXX", ".X...X.", "XXX.XXX", ".X...X." } },
            { "fleet", new string[] { "...X...", "..XXX..", ".X.X.X.", "XXX.XXX", ".X...X.", "XXX.XXX", ".X...X." } },
            { "orbit", new string[] { "..XXX..", ".X...X.", "X..X..X", "X.XXX.X", "X..X..X", ".X...X.", "..XXX.." } },
            { "wingmen", new string[] { ".......", ".X...X.", "XXX.XXX", ".X.X.X.", "..XXX..", "...X...", "......." } },
            { "spread", new string[] { "X.X.X.X", ".XXX.X.", "XXX.XXX", ".X.X.X.", "..XXX..", "...X...", "......." } },
            { "scatter", new string[] { "X..X..X", ".X.X.X.", "..XXX..", "XXXXXXX", "..XXX..", ".X.X.X.", "X..X..X" } },
            { "mega", new string[] { "..XXX..", "..XXX..", "..XXX..", "..XXX..", "..XXX..", ".XXXXX.", "XXXXXXX" } },
            { "machine", new string[] { ".X...X.", "XXX.XXX", "X.X.X.X", "X.X.X.X", "XXX.XXX", "XXX.XXX", "XXX.XXX" } },
            { "laser", new string[] { ".X...X.", ".X...X.", "XXX..X.", ".X..XXX", ".X...X.", ".X...X.", ".X...X." } },
            { "missile", new string[] { "...X...", "..XXX..", "..XXX..", "..XXX..", ".XXXXX.", "XX.X.XX", "...X..." } },
            { "shield", new string[] { ".XXXXX.", "XX...XX", "X.XXX.X", "X..X..X", ".X...X.", "..X.X..", "...X..." } }
        };
        public static readonly Dictionary<int,string[]> Fleet = new Dictionary<int,string[]> {
            { 1, new string[] { "..C.....C..", "..VB...BV..", "..VBBGBBV..", ".CVBBGBBVC.", "CVVBBGBBVVC", "C..VBBBV..C", "...VB.BV...", "...C...C..." } },
            { 2, new string[] { "...C...C...", "...VBGBV...", "..VBBGBBV..", "CVVBBGBBVVC", "CVVBBBBBVVC", "CVV.BBB.VVC", "C...B.B...C" } },
            { -2, new string[] { ".....C.....", "....BGB....", "...BBGBB...", "C.BBBGBBB.C", "CVVBBGBBVVC", "CVVVBBBVVVC", ".CVVBBBVVC.", "..C.B.B.C.." } },
            { -1, new string[] { "....C....", "...BGB...", "...BGB...", "..VBGBV..", ".CVBGBVC.", "CVVBGBVVC", "CVVBBBVVC", "C..BBB..C", "...B.B..." } }
        };
        public static readonly Dictionary<int,Dictionary<char,string>> FleetPalettes = new Dictionary<int,Dictionary<char,string>> {
            { 1, new Dictionary<char,string> { { 'B', "#45338f" }, { 'V', "#9c78f5" }, { 'C', "#d9bcff" }, { 'G', "#f9edff" } } },
            { 2, new Dictionary<char,string> { { 'B', "#944334" }, { 'V', "#ed7548" }, { 'C', "#ffc18a" }, { 'G', "#fff3da" } } },
            { -2, new Dictionary<char,string> { { 'B', "#176b65" }, { 'V', "#36c99b" }, { 'C', "#9bffe1" }, { 'G', "#eafff5" } } },
            { -1, new Dictionary<char,string> { { 'B', "#926026" }, { 'V', "#efab38" }, { 'C', "#ffe78c" }, { 'G', "#fff8da" } } }
        };
    }
}
