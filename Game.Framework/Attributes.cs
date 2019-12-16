namespace Game.Framework
{
    using System;

    [AttributeUsage(AttributeTargets.Interface, Inherited = true)]
    public class GameServiceAttribute : Attribute
    {
        public GameServiceAttribute()
        {
        }
    }

    public enum VertexElementType
    {
        Float2,
        Float3,
        Color
    }

    public enum VertexSemantic
    {
        Position,
        Color,
        Texture
    }

    public struct VertexSchema
    {
        public readonly VertexElementType ElementType;
        public readonly VertexSemantic Semantic;
        public readonly int UsageIndex;

        public VertexSchema(VertexElementType elementType, VertexSemantic semantic, int usageIndex = 0)
        {
            ElementType = elementType;
            Semantic = semantic;
            UsageIndex = usageIndex;
        }
    }

    [AttributeUsage(AttributeTargets.Struct)]
    public class VertexGroupAttribute : Attribute
    {
        public readonly Type VertexType;
        public VertexGroupAttribute(Type vertexType)
        {
            VertexType = vertexType;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class VertexElementAttribute : Attribute
    {
        public readonly VertexSchema VertexSchema;
        public VertexElementAttribute(VertexElementType elementType, VertexSemantic semantic, int usageIndex = 0)
        {
            VertexSchema = new VertexSchema(elementType, semantic, usageIndex);
        }

    }
}