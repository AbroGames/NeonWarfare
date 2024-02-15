#if GODOT_REAL_T_IS_DOUBLE
global using real = double;
#else
global using real = float;
#endif

namespace KludgeBox;

public static class FloatHelper
{
#if GODOT_REAL_T_IS_DOUBLE
    public const bool RealIsDouble = true;
#else
    public const bool RealIsDouble = false;
#endif
}