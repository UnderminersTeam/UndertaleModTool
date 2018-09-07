using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib
{
    public class UndertaleData
    {
        internal UndertaleChunkFORM FORM;

        public UndertaleGeneralInfo GeneralInfo => FORM.GEN8.Object;
        public UndertaleOptions Options => FORM.OPTN.Object;
        public UndertaleLanguage Language => FORM.LANG.Object;
        [Obsolete("Unused")]
        public IList<UndertaleExtension> Extensions => FORM.EXTN.List;
        public IList<UndertaleSound> Sounds => FORM.SOND.List;
        [Obsolete("Unused")]
        public IList<UndertaleAudioGroup> AudioGroups => FORM.AGRP.List;
        public IList<UndertaleSprite> Sprites => FORM.SPRT.List;
        public IList<UndertaleBackground> Backgrounds => FORM.BGND.List;
        public IList<UndertalePath> Paths => FORM.PATH.List;
        public IList<UndertaleScript> Scripts => FORM.SCPT.List;
        [Obsolete("Unused")]
        public IList<UndertaleGlobal> Globals => FORM.GLOB.List;
        [Obsolete("Unused")]
        public IList<UndertaleShader> Shaders => FORM.SHDR.List;
        public IList<UndertaleFont> Fonts => FORM.FONT.List;
        [Obsolete("Unused")]
        public IList<UndertaleTimeline> Timelines => FORM.TMLN.List;
        public IList<UndertaleGameObject> GameObjects => FORM.OBJT.List;
        public IList<UndertaleRoom> Rooms => FORM.ROOM.List;
        //[Obsolete("Unused")]
        // DataFile
        public IList<UndertaleTexturePage> TexturePages => FORM.TPAG.List;
        public IList<UndertaleCode> Code => FORM.CODE.List;
        public IList<UndertaleVariable> Variables => FORM.VARI.List;
        public uint Variables_Unknown1 => FORM.VARI.Unknown1;
        public uint Variables_Unknown1Again => FORM.VARI.Unknown1Again;
        public uint Variables_Unknown2 => FORM.VARI.Unknown2;
        public IList<UndertaleFunctionDeclaration> FunctionDeclarations => FORM.FUNC.Declarations;
        public IList<UndertaleFunctionDefinition> FunctionDefinitions => FORM.FUNC.Definitions;
        public IList<UndertaleString> Strings => FORM.STRG.List;
        public IList<UndertaleEmbeddedTexture> EmbeddedTextures => FORM.TXTR.List;
        public IList<UndertaleEmbeddedAudio> EmbeddedAudio => FORM.AUDO.List;
    }

    public static class UndertaleDataExtensionMethods
    {
        public static T ByName<T>(this IList<T> list, string name) where T : UndertaleNamedResource
        {
            foreach(var item in list)
            {
                if (item.Name.Content == name)
                    return item;
            }
            return default(T);
        }

        public static UndertaleString MakeString(this IList<UndertaleString> list, string content)
        {
            // TODO: without reference counting the strings, this may leave unused strings in the array
            foreach (UndertaleString str in list)
            {
                if (str.Content == content)
                    return str;
            }
            UndertaleString newString = new UndertaleString(content);
            list.Add(newString);
            return newString;
        }
    }
}
