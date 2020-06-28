namespace KubernetesTLSWatcher
{
    public static class Constants
    {
        public const string CHECKINTERVALSECS = "CHECK_INTERVAL_SECS";
        public const string PROMETHEUSPORT = "PROMETHEUS_PORT";
        public const string SECRETTYPE = "kubernetes.io/tls";
        public const string CRTKEY = ".crt";
    }
}