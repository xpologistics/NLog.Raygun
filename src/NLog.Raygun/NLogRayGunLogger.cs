using NLog.Config;

namespace NLog.Raygun
{
    /// <summary>
    /// Utility class used to initialize the RayGun NLog Target
    /// </summary>
    public static class NLogRayGunLogger
    {
        private static readonly object Lock = new object();
        private static bool _initialized;

        public static void Initialize()
        {
            if (!_initialized)
            {
                lock (Lock)
                {
                    if (!_initialized)
                    {
                        ConfigurationItemFactory.Default.Targets.RegisterDefinition("RayGun", typeof (RayGunTarget));
                        LogManager.ReconfigExistingLoggers();
                        _initialized = true;
                    }
                }
            }
        }
    }
}
