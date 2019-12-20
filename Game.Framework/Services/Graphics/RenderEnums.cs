namespace Game.Framework.Services.Graphics
{

    //
    // Summary:
    //     Defines a blend mode.
    public enum Blend
    {
        //
        // Summary:
        //     Each component of the color is multiplied by {1, 1, 1, 1}.
        One = 0,
        //
        // Summary:
        //     Each component of the color is multiplied by {0, 0, 0, 0}.
        Zero = 1,
        //
        // Summary:
        //     Each component of the color is multiplied by the source color. {Rs, Gs, Bs, As},
        //     where Rs, Gs, Bs, As are color source values.
        SourceColor = 2,
        //
        // Summary:
        //     Each component of the color is multiplied by the inverse of the source color.
        //     {1 − Rs, 1 − Gs, 1 − Bs, 1 − As}, where Rs, Gs, Bs, As are color source values.
        InverseSourceColor = 3,
        //
        // Summary:
        //     Each component of the color is multiplied by the alpha value of the source. {As,
        //     As, As, As}, where As is the source alpha value.
        SourceAlpha = 4,
        //
        // Summary:
        //     Each component of the color is multiplied by the inverse of the alpha value of
        //     the source. {1 − As, 1 − As, 1 − As, 1 − As}, where As is the source alpha value.
        InverseSourceAlpha = 5,
        //
        // Summary:
        //     Each component color is multiplied by the destination color. {Rd, Gd, Bd, Ad},
        //     where Rd, Gd, Bd, Ad are color destination values.
        DestinationColor = 6,
        //
        // Summary:
        //     Each component of the color is multiplied by the inversed destination color.
        //     {1 − Rd, 1 − Gd, 1 − Bd, 1 − Ad}, where Rd, Gd, Bd, Ad are color destination
        //     values.
        InverseDestinationColor = 7,
        //
        // Summary:
        //     Each component of the color is multiplied by the alpha value of the destination.
        //     {Ad, Ad, Ad, Ad}, where Ad is the destination alpha value.
        DestinationAlpha = 8,
        //
        // Summary:
        //     Each component of the color is multiplied by the inversed alpha value of the
        //     destination. {1 − Ad, 1 − Ad, 1 − Ad, 1 − Ad}, where Ad is the destination alpha
        //     value.
        InverseDestinationAlpha = 9,
        //
        // Summary:
        //     Each component of the color is multiplied by a constant in the Microsoft.Xna.Framework.Graphics.GraphicsDevice.BlendFactor.
        BlendFactor = 10,
        //
        // Summary:
        //     Each component of the color is multiplied by a inversed constant in the Microsoft.Xna.Framework.Graphics.GraphicsDevice.BlendFactor.
        InverseBlendFactor = 11,
        //
        // Summary:
        //     Each component of the color is multiplied by either the alpha of the source color,
        //     or the inverse of the alpha of the source color, whichever is greater. {f, f,
        //     f, 1}, where f = min(As, 1 − As), where As is the source alpha value.
        SourceAlphaSaturation = 12
    }

    //
    // Summary:
    //     Defines a function for color blending.
    public enum BlendFunction
    {
        //
        // Summary:
        //     The function will adds destination to the source. (srcColor * srcBlend) + (destColor
        //     * destBlend)
        Add = 0,
        //
        // Summary:
        //     The function will subtracts destination from source. (srcColor * srcBlend) −
        //     (destColor * destBlend)
        Subtract = 1,
        //
        // Summary:
        //     The function will subtracts source from destination. (destColor * destBlend)
        //     - (srcColor * srcBlend)
        ReverseSubtract = 2,
        //
        // Summary:
        //     The function will extracts minimum of the source and destination. min((srcColor
        //     * srcBlend),(destColor * destBlend))
        Min = 3,
        //
        // Summary:
        //     The function will extracts maximum of the source and destination. max((srcColor
        //     * srcBlend),(destColor * destBlend))
        Max = 4
    }
    public enum RenderCameraType
    {
        World,
        Projection,
        View
    }
}