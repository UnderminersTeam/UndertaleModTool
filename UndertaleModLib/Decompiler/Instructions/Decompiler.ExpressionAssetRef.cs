using System;
using System.Collections;
using UndertaleModLib.Models;
using static UndertaleModLib.Util.AssetReferenceTypes;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a reference to an asset in the resource tree, used in 2023.8+ only
    public class ExpressionAssetRef : Expression
    {
        public int AssetIndex;
        public RefType AssetRefType;

        public ExpressionAssetRef(UndertaleData data, int encodedResourceIndex)
        {
            Type = UndertaleInstruction.DataType.Variable;

            // Break down index - first 24 bits are the ID, the rest is the ref type
            AssetIndex = encodedResourceIndex & 0xffffff;
            AssetRefType = ConvertToRefType(data, encodedResourceIndex >> 24);
        }

        public ExpressionAssetRef(int resourceIndex, RefType resourceType)
        {
            Type = UndertaleInstruction.DataType.Variable;
            AssetIndex = resourceIndex;
            AssetRefType = resourceType;
        }

        internal override bool IsDuplicationSafe()
        {
            return true;
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            return this;
        }
        public override string ToString(DecompileContext context)
        {
            if (context.GlobalContext.Data != null)
            {
                IList assetList = null;
                switch (AssetRefType)
                {
                    case RefType.Sprite:
                        assetList = (IList)context.GlobalContext.Data.Sprites;
                        break;
                    case RefType.Background:
                        assetList = (IList)context.GlobalContext.Data.Backgrounds;
                        break;
                    case RefType.Sound:
                        assetList = (IList)context.GlobalContext.Data.Sounds;
                        break;
                    case RefType.Font:
                        assetList = (IList)context.GlobalContext.Data.Fonts;
                        break;
                    case RefType.Path:
                        assetList = (IList)context.GlobalContext.Data.Paths;
                        break;
                    case RefType.Timeline:
                        assetList = (IList)context.GlobalContext.Data.Timelines;
                        break;
                    case RefType.Room:
                        assetList = (IList)context.GlobalContext.Data.Rooms;
                        break;
                    case RefType.Object:
                        assetList = (IList)context.GlobalContext.Data.GameObjects;
                        break;
                    case RefType.Shader:
                        assetList = (IList)context.GlobalContext.Data.Shaders;
                        break;
                    case RefType.AnimCurve:
                        assetList = (IList)context.GlobalContext.Data.AnimationCurves;
                        break;
                    case RefType.Sequence:
                        assetList = (IList)context.GlobalContext.Data.Sequences;
                        break;
                    case RefType.ParticleSystem:
                        assetList = (IList)context.GlobalContext.Data.ParticleSystems;
                        break;
                }

                if (assetList != null && AssetIndex >= 0 && AssetIndex < assetList.Count)
                    return ((UndertaleNamedResource)assetList[AssetIndex]).Name.Content;
            }
            return $"/* ERROR: missing {AssetRefType} asset, using ID instead */ {AssetIndex}";
        }
        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            // Convert type to corresponding AssetIDType equivalent
            return AssetRefType switch
            {
                RefType.Object => AssetIDType.GameObject,
                RefType.Sprite => AssetIDType.Sprite,
                RefType.Sound => AssetIDType.Sound,
                RefType.Room => AssetIDType.Room,
                RefType.Background => AssetIDType.Background,
                RefType.Path => AssetIDType.Path,
                RefType.Font => AssetIDType.Font,
                RefType.Timeline => AssetIDType.Timeline,
                RefType.Shader => AssetIDType.Shader,
                RefType.Sequence => AssetIDType.Sequence,
                RefType.AnimCurve => AssetIDType.AnimCurve,
                RefType.ParticleSystem => AssetIDType.ParticleSystem,
                _ => throw new NotImplementedException($"Missing ref type {AssetRefType}")
            };
        }
    }
}