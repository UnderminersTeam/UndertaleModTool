namespace UndertaleModTool
{
    internal static class BuildInfo
    {
#if BUILD_SINGLEFILE_true
        public static bool IsSingleFile = true;
#else
        public static bool IsSingleFile = false;
#endif
    }
}
